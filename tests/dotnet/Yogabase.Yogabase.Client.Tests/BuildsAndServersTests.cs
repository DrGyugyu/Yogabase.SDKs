using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Yogabase.Yogabase;

namespace Yogabase.Tests;

/// <summary>
/// Wire-shape coverage for the two surfaces that replaced the withdrawn Data API: the player-facing
/// game-server allocation on the application gateway, and the developer-credentialled build upload
/// on the Control Plane. Both are asserted against a fake handler rather than a live installation,
/// so these tests pin the exact request the SDK emits and nothing about server behaviour.
/// </summary>
public sealed class BuildsAndServersTests
{
    private static readonly Guid ProjectId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid EnvironmentId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private const string BuildsRoute =
        "http://localhost:7100/api/v1/projects/11111111-1111-4111-8111-111111111111"
        + "/environments/22222222-2222-4222-8222-222222222222/builds";
    private const string BuildId = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";
    private const string DeveloperToken = "ybt_0123456789abcdefghijklmnopqrstuvwxyzABCDEF";
    private static readonly string[] ExpectedServerRoutes =
    [
        "GET http://localhost/v1/servers/srv-01",
        "POST http://localhost/v1/servers/srv-01/stop",
    ];
    private static readonly string[] ExpectedUploadFields =
        ["executable", "name", "version", "ports", "ports", "bundle"];
    private static readonly string[] BundleFieldOnly = ["bundle"];
    private static readonly string[] ExpectedPolledStates = ["checking", "checking", "refused"];
    private static readonly string[] ExpectedBundleEntries =
        ["Server.x86_64", "Server_Data/Managed/Assembly-CSharp.dll", "UnityPlayer.so"];
    private static readonly char[] HeaderParameterTerminators = [';', '\r', '\n'];

    // ---------------------------------------------------------------------------------------
    // Game servers
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task ServerStartUsesTheExactRouteBodyAndPlayerToken()
    {
        HttpRequestMessage? observed = null;
        string? body = null;
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            observed = Clone(request);
            body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return Json(HttpStatusCode.OK, Allocation());
        });
        await using var client = await ClientAsync(handler);

        var server = await client.Servers.StartAsync(
            new YogabaseServerStartRequest
            {
                Service = "battle",
                CustomData = new Dictionary<string, string>(StringComparer.Ordinal) { ["mode"] = "ranked" },
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, observed!.Method);
        Assert.Equal("http://localhost/v1/servers/start", observed.RequestUri!.AbsoluteUri);
        Assert.Equal(new AuthenticationHeaderValue("Bearer", "access-token"), observed.Headers.Authorization);
        Assert.Equal("{\"service\":\"battle\",\"customData\":{\"mode\":\"ranked\"}}", body);
        Assert.Equal("srv-01", server.ServerId);
        Assert.Equal("Allocated", server.State);
        Assert.Equal("10.0.0.7:31245", server.FindPort("game")!.Endpoint);
        Assert.Equal(7777, server.FindPort("game")!.InternalPort);
        Assert.Null(server.FindPort("absent"));
    }

    [Fact]
    public async Task ServerStartOmitsAbsentCustomData()
    {
        string? body = null;
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return Json(HttpStatusCode.OK, Allocation());
        });
        await using var client = await ClientAsync(handler);

        await client.Servers.StartAsync("battle", TestContext.Current.CancellationToken);

        Assert.Equal("{\"service\":\"battle\"}", body);
    }

    [Fact]
    // The contract pins a bare array, and an instance no member has observed an address for is
    // listed with empty networkPorts rather than omitted: a client must see it and not dial it.
    public async Task ServerListReadsTheBareArrayTheContractPins()
    {
        Uri? route = null;
        var handler = new ScriptedHandler((request, _) =>
        {
            route = request.RequestUri;
            return Task.FromResult(Json(
                HttpStatusCode.OK,
                "[{\"serverId\":\"srv-01\",\"state\":\"Ready\",\"networkPorts\":[]},"
                + Allocation().Replace("srv-01", "srv-02", StringComparison.Ordinal) + "]"));
        });
        await using var client = await ClientAsync(handler);

        var servers = await client.Servers.ListAsync(TestContext.Current.CancellationToken);

        Assert.Equal("http://localhost/v1/servers", route!.AbsoluteUri);
        Assert.Equal(2, servers.Count);
        Assert.Equal("srv-01", servers[0].ServerId);
        Assert.Empty(servers[0].NetworkPorts);
        Assert.Equal("10.0.0.7:31245", servers[1].FindPort("game")!.Endpoint);
    }

    [Theory]
    [InlineData("{\"servers\":[]}")]
    [InlineData("{\"unexpected\":[]}")]
    public async Task ServerListRefusesAnEnvelopeTheContractDoesNotDeclare(string payload)
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, payload)));
        await using var client = await ClientAsync(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(() =>
            client.Servers.ListAsync(TestContext.Current.CancellationToken));

        Assert.Equal("response_invalid", exception.Code);
    }

    [Fact]
    public async Task ServerGetAndStopUseTheExactRoutes()
    {
        var routes = new List<string>();
        var handler = new ScriptedHandler((request, _) =>
        {
            routes.Add(request.Method + " " + request.RequestUri!.AbsoluteUri);
            return Task.FromResult(request.Method == HttpMethod.Get
                ? Json(HttpStatusCode.OK, Allocation())
                : Json(HttpStatusCode.OK, Allocation().Replace("Allocated", "Ready", StringComparison.Ordinal)));
        });
        await using var client = await ClientAsync(handler);

        var read = await client.Servers.GetAsync("srv-01", TestContext.Current.CancellationToken);
        var stopped = await client.Servers.StopAsync("srv-01", TestContext.Current.CancellationToken);

        Assert.Equal("srv-01", read.ServerId);
        // A stop answers the instance's state after the release; it is never a deletion.
        Assert.Equal("Ready", stopped.State);
        Assert.Equal(ExpectedServerRoutes, routes);
    }

    [Fact]
    public async Task ServerStopWithoutABodyIsRefusedRatherThanReadAsNothing()
    {
        var handler = new ScriptedHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)));
        await using var client = await ClientAsync(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(() =>
            client.Servers.StopAsync("srv-01", TestContext.Current.CancellationToken));

        Assert.Equal("response_invalid", exception.Code);
    }

    [Fact]
    public async Task AServerRefusalCarriesTheContractCode()
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Json(
            HttpStatusCode.Conflict,
            "{\"type\":\"https://yogabase.io/problems/server-none-available\",\"title\":\"Game server request refused\","
            + "\"status\":409,\"detail\":\"No Ready instance.\",\"code\":\"server_none_available\",\"correlationId\":\"c-1\"}")));
        await using var client = await ClientAsync(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(() =>
            client.Servers.StartAsync("battle", TestContext.Current.CancellationToken));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal("server_none_available", exception.Code);
        Assert.Equal("No Ready instance.", exception.Message);
    }

    [Theory]
    // An unroutable external port, a missing node address, and an unsupported transport each make
    // the allocation useless. Returning it would send the caller to its own machine instead.
    [InlineData("{\"serverId\":\"srv-01\",\"state\":\"Allocated\",\"networkPorts\":"
        + "[{\"name\":\"game\",\"internalPort\":7777,\"externalPort\":0,\"protocol\":\"udp\",\"host\":\"10.0.0.7\"}]}")]
    [InlineData("{\"serverId\":\"srv-01\",\"state\":\"Allocated\",\"networkPorts\":"
        + "[{\"name\":\"game\",\"internalPort\":7777,\"externalPort\":31245,\"protocol\":\"udp\",\"host\":\"\"}]}")]
    [InlineData("{\"serverId\":\"srv-01\",\"state\":\"Allocated\",\"networkPorts\":"
        + "[{\"name\":\"game\",\"internalPort\":7777,\"externalPort\":31245,\"protocol\":\"sctp\",\"host\":\"10.0.0.7\"}]}")]
    [InlineData("{\"serverId\":\"\",\"state\":\"Allocated\",\"networkPorts\":[]}")]
    public async Task AnAllocationWithAnUnusableAddressIsRefused(string payload)
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, payload)));
        await using var client = await ClientAsync(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(() =>
            client.Servers.StartAsync("battle", TestContext.Current.CancellationToken));

        Assert.Equal("server_response_invalid", exception.Code);
    }

    // The Agones-shaped typed addresses the product will add once this client is the minimum: an
    // `address` and `addresses: [{type, address}]` on the instance and on each port. Today's
    // product sends neither, and a client that refuses unknown members must accept both first.
    private const string TypedAddressAllocation =
        "{\"serverId\":\"srv-01\",\"state\":\"Allocated\","
        + "\"address\":\"203.0.113.10\","
        + "\"addresses\":[{\"type\":\"Advertised\",\"address\":\"203.0.113.10\"},{\"type\":\"InternalIP\",\"address\":\"10.0.0.7\"},{\"type\":\"PodIP\",\"address\":\"10.42.0.9\"}],"
        + "\"networkPorts\":[{\"name\":\"game\",\"internalPort\":7777,\"externalPort\":31245,\"protocol\":\"udp\",\"host\":\"203.0.113.10\","
        + "\"address\":\"203.0.113.10\",\"addresses\":[{\"type\":\"Advertised\",\"address\":\"203.0.113.10\"},{\"type\":\"InternalIP\",\"address\":\"10.0.0.7\"}]}]}";

    [Fact]
    public async Task AnAllocationCarryingTypedAddressesReadsThemOnTheInstanceAndEachPort()
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, TypedAddressAllocation)));
        await using var client = await ClientAsync(handler);

        var server = await client.Servers.StartAsync("battle", TestContext.Current.CancellationToken);

        Assert.Equal("203.0.113.10", server.Address);
        Assert.Equal(
            [("Advertised", "203.0.113.10"), ("InternalIP", "10.0.0.7"), ("PodIP", "10.42.0.9")],
            server.Addresses!.Select(address => (address.Type, address.Address)));
        var game = server.FindPort("game")!;
        Assert.Equal("203.0.113.10", game.Address);
        Assert.Equal(2, game.Addresses!.Count);
        Assert.Equal("203.0.113.10:31245", game.Endpoint);
    }

    [Fact]
    public async Task AnAllocationWithoutTypedAddressesLeavesThemAbsent()
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, Allocation())));
        await using var client = await ClientAsync(handler);

        var server = await client.Servers.StartAsync("battle", TestContext.Current.CancellationToken);

        Assert.Null(server.Address);
        Assert.Null(server.Addresses);
        Assert.Null(server.FindPort("game")!.Address);
        Assert.Null(server.FindPort("game")!.Addresses);
    }

    [Theory]
    // An address outside the typed list the same answer names.
    [InlineData("{\"serverId\":\"srv-01\",\"state\":\"Allocated\",\"address\":\"198.51.100.1\","
        + "\"addresses\":[{\"type\":\"Advertised\",\"address\":\"203.0.113.10\"}],\"networkPorts\":[]}")]
    // A typed address with no type, or no address.
    [InlineData("{\"serverId\":\"srv-01\",\"state\":\"Allocated\",\"addresses\":[{\"type\":\"\",\"address\":\"203.0.113.10\"}],\"networkPorts\":[]}")]
    [InlineData("{\"serverId\":\"srv-01\",\"state\":\"Allocated\",\"addresses\":[{\"type\":\"InternalIP\",\"address\":\"\"}],\"networkPorts\":[]}")]
    // An empty address where one is stated.
    [InlineData("{\"serverId\":\"srv-01\",\"state\":\"Allocated\",\"address\":\"\",\"networkPorts\":[]}")]
    // The same fault on a port.
    [InlineData("{\"serverId\":\"srv-01\",\"state\":\"Allocated\",\"networkPorts\":[{\"name\":\"game\",\"internalPort\":7777,\"externalPort\":31245,"
        + "\"protocol\":\"udp\",\"host\":\"10.0.0.7\",\"address\":\"10.0.0.8\",\"addresses\":[{\"type\":\"InternalIP\",\"address\":\"10.0.0.7\"}]}]}")]
    public async Task ATypedAddressOutsideTheAgonesShapeIsRefused(string payload)
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, payload)));
        await using var client = await ClientAsync(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(() =>
            client.Servers.StartAsync("battle", TestContext.Current.CancellationToken));

        Assert.Equal("server_response_invalid", exception.Code);
    }

    [Fact]
    public async Task MoreThanSixteenTypedAddressesAreRefused()
    {
        var addresses = string.Join(",", Enumerable.Range(1, 17).Select(index => "{\"type\":\"InternalIP\",\"address\":\"10.0.0." + index + "\"}"));
        var payload = "{\"serverId\":\"srv-01\",\"state\":\"Allocated\",\"addresses\":[" + addresses + "],\"networkPorts\":[]}";
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, payload)));
        await using var client = await ClientAsync(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(() =>
            client.Servers.StartAsync("battle", TestContext.Current.CancellationToken));

        Assert.Equal("server_response_invalid", exception.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" battle")]
    // The canonical service id grammar the bundle contract pins: lower-case DNS-label shape, at
    // most 63 characters. Anything else is refused server-side as server_request_invalid.
    [InlineData("Battle")]
    [InlineData("battle_1")]
    [InlineData("-battle")]
    [InlineData("battle-")]
    [InlineData("a234567890123456789012345678901234567890123456789012345678901234")]
    public async Task ServerStartRejectsAMalformedServiceBeforeTransport(string service)
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(Json(HttpStatusCode.OK, Allocation()));
        });
        await using var client = await ClientAsync(handler);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Servers.StartAsync(service, TestContext.Current.CancellationToken));
        Assert.Equal(0, calls);
    }

    // ---------------------------------------------------------------------------------------
    // Builds
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task BuildUploadSendsMetadataBeforeTheBundleOnTheControlPlaneRoute()
    {
        HttpRequestMessage? observed = null;
        IReadOnlyList<MultipartField> parts = Array.Empty<MultipartField>();
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            observed = Clone(request);
            parts = await ReadMultipartAsync(request.Content!, cancellationToken);
            return Json(HttpStatusCode.Accepted, Checking());
        });
        using var builds = Builds(handler);
        using var bundle = new MemoryStream(Encoding.ASCII.GetBytes("PK-not-really-a-zip"));

        var build = await builds.UploadAsync(
            ProjectId,
            EnvironmentId,
            bundle,
            new YogabaseBuildUploadRequest
            {
                Executable = "Server.x86_64",
                Name = "battle",
                Version = "1.4.0",
                BundleFileName = "MyGame.zip",
                Ports =
                [
                    new YogabaseBuildPort { Name = "game", Port = 7777, Protocol = "udp" },
                    new YogabaseBuildPort { Name = "query", Port = 7778, Protocol = "tcp" },
                ],
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, observed!.Method);
        Assert.Equal(BuildsRoute, observed.RequestUri!.AbsoluteUri);
        Assert.Equal(new AuthenticationHeaderValue("Bearer", DeveloperToken), observed.Headers.Authorization);

        // The bundle is written last so a server reading the form as a stream can refuse a
        // malformed port or executable before it has buffered a few hundred megabytes of zip.
        Assert.Equal(ExpectedUploadFields, parts.Select(part => part.Name));
        Assert.Equal("Server.x86_64", parts[0].Body);
        Assert.Equal("battle", parts[1].Body);
        Assert.Equal("1.4.0", parts[2].Body);
        Assert.Equal("game=7777/udp", parts[3].Body);
        Assert.Equal("query=7778/tcp", parts[4].Body);
        Assert.Equal("MyGame.zip", parts[5].FileName);
        Assert.Equal("application/zip", parts[5].ContentType);
        Assert.Equal("PK-not-really-a-zip", parts[5].Body);

        Assert.True(bundle.CanRead, "The caller's bundle stream must survive the upload.");
        Assert.Equal(BuildId, build.BuildId);
        Assert.Equal("sha256:aa", build.Digest);
        Assert.Equal(YogabaseBuild.StateChecking, build.State);
        Assert.Equal("battle:1.4.0", build.Identity);
        Assert.Equal(1, build.Attempt);
        Assert.Equal("SelfContainedService", build.Kind);
        Assert.False(build.IsTerminal);
        // An accepted upload's check says nothing has been observed yet, and says so honestly.
        Assert.Equal(YogabaseBuildCheck.YogabaseDerived, build.Check.EvidenceOrigin);
        Assert.False(build.Check.Passed);
        Assert.Empty(build.Check.Findings);
        Assert.Empty(build.Check.Refusals);
    }

    [Fact]
    public async Task BuildUploadOmitsEveryFieldTheCallerDidNotSet()
    {
        IReadOnlyList<MultipartField> parts = Array.Empty<MultipartField>();
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            parts = await ReadMultipartAsync(request.Content!, cancellationToken);
            return Json(HttpStatusCode.Accepted, Checking());
        });
        using var builds = Builds(handler);
        using var bundle = new MemoryStream(Encoding.ASCII.GetBytes("bundle"));

        await builds.UploadAsync(ProjectId, EnvironmentId, bundle, null, TestContext.Current.CancellationToken);

        Assert.Equal(BundleFieldOnly, parts.Select(part => part.Name));
        Assert.Equal("build.zip", parts[0].FileName);
    }

    [Fact]
    public async Task BuildUploadRejectsDuplicatePortNamesBeforeTransport()
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(Json(HttpStatusCode.Accepted, "{}"));
        });
        using var builds = Builds(handler);
        using var bundle = new MemoryStream(Encoding.ASCII.GetBytes("bundle"));

        await Assert.ThrowsAsync<ArgumentException>(() => builds.UploadAsync(
            ProjectId,
            EnvironmentId,
            bundle,
            new YogabaseBuildUploadRequest
            {
                Ports =
                [
                    new YogabaseBuildPort { Name = "game", Port = 7777 },
                    new YogabaseBuildPort { Name = "game", Port = 7778 },
                ],
            },
            TestContext.Current.CancellationToken));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task ALambdaUploadSendsItsExecutionDeclarationBeforeTheBundle()
    {
        IReadOnlyList<MultipartField> parts = Array.Empty<MultipartField>();
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            parts = await ReadMultipartAsync(request.Content!, cancellationToken);
            return Json(HttpStatusCode.Accepted, Checking());
        });
        using var builds = Builds(handler);
        using var bundle = new MemoryStream(Encoding.ASCII.GetBytes("lambda-bundle"));

        await builds.UploadAsync(
            ProjectId,
            EnvironmentId,
            bundle,
            new YogabaseBuildUploadRequest
            {
                Name = "hello-world",
                Version = "6",
                Lambda = new YogabaseLambdaDeclaration
                {
                    ExecutionKind = YogabaseLambdaExecutionKind.Stateful,
                    RealtimeIngressBindings = ["match.input"],
                    RealtimeEgressBindings = ["match.events", "inventory.updated"],
                },
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(
            ["name", "version", "executionKind", "realtimeIngressBindings", "realtimeEgressBindings", "bundle"],
            parts.Select(part => part.Name));
        Assert.Equal("Stateful", parts[2].Body);
        Assert.Equal("match.input", parts[3].Body);
        Assert.Equal("match.events,inventory.updated", parts[4].Body);
    }

    [Fact]
    public async Task ALambdaWithNoBindingsSendsBothBindingFieldsEmpty()
    {
        IReadOnlyList<MultipartField> parts = Array.Empty<MultipartField>();
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            parts = await ReadMultipartAsync(request.Content!, cancellationToken);
            return Json(HttpStatusCode.Accepted, Checking());
        });
        using var builds = Builds(handler);
        using var bundle = new MemoryStream(Encoding.ASCII.GetBytes("lambda-bundle"));

        await builds.UploadAsync(
            ProjectId,
            EnvironmentId,
            bundle,
            new YogabaseBuildUploadRequest { Lambda = new YogabaseLambdaDeclaration() },
            TestContext.Current.CancellationToken);

        // An empty field declares none; an absent field is not a declaration, and the Control Plane
        // refuses a Lambda upload without it (BUILD_LAMBDA_DECLARATION_REQUIRED).
        Assert.Equal(
            ["executionKind", "realtimeIngressBindings", "realtimeEgressBindings", "bundle"],
            parts.Select(part => part.Name));
        Assert.Equal("Stateless", parts[0].Body);
        Assert.Equal(string.Empty, parts[1].Body);
        Assert.Equal(string.Empty, parts[2].Body);
    }

    [Fact]
    public async Task ARegisteringLambdaSendsItsRegisteredIdsAfterTheDeclaration()
    {
        IReadOnlyList<MultipartField> parts = Array.Empty<MultipartField>();
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            parts = await ReadMultipartAsync(request.Content!, cancellationToken);
            return Json(HttpStatusCode.Accepted, Checking());
        });
        using var builds = Builds(handler);
        using var bundle = new MemoryStream(Encoding.ASCII.GetBytes("lambda-bundle"));

        await builds.UploadAsync(
            ProjectId,
            EnvironmentId,
            bundle,
            new YogabaseBuildUploadRequest
            {
                Name = "adaptivesync.lambdas",
                Version = "3",
                Lambda = new YogabaseLambdaDeclaration
                {
                    RealtimeIngressBindings = ["claim_reward"],
                    RealtimeEgressBindings = ["demo.notify"],
                    RegisteredLambdaIds = ["claim_reward", "gacha_pull", "season.rollover"],
                },
            },
            TestContext.Current.CancellationToken);

        // The product reads registeredLambdaIds beside the execution declaration: a RegisterLambdas
        // bundle uploaded without a manifest is refused with BUILD_LAMBDA_REGISTRATION_REQUIRED.
        Assert.Equal(
            ["name", "version", "executionKind", "realtimeIngressBindings", "realtimeEgressBindings", "registeredLambdaIds", "bundle"],
            parts.Select(part => part.Name));
        Assert.Equal("claim_reward,gacha_pull,season.rollover", parts[5].Body);
    }

    [Fact]
    public async Task ASingleFunctionLambdaSendsNoRegisteredIdsField()
    {
        IReadOnlyList<MultipartField> parts = Array.Empty<MultipartField>();
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            parts = await ReadMultipartAsync(request.Content!, cancellationToken);
            return Json(HttpStatusCode.Accepted, Checking());
        });
        using var builds = Builds(handler);
        using var bundle = new MemoryStream(Encoding.ASCII.GetBytes("lambda-bundle"));

        await builds.UploadAsync(
            ProjectId,
            EnvironmentId,
            bundle,
            new YogabaseBuildUploadRequest { Lambda = new YogabaseLambdaDeclaration { RegisteredLambdaIds = [] } },
            TestContext.Current.CancellationToken);

        // A single-function (InvokeAsync) archive states no registration: the product refuses the
        // field beside one with BUILD_LAMBDA_REGISTRATION_UNEXPECTED, even empty.
        Assert.DoesNotContain(parts, part => part.Name == "registeredLambdaIds");
    }

    public static TheoryData<string?, string[], string[]> InvalidLambdaRegistrations() => new()
    {
        // Not a Lambda id: uppercase, a leading digit, a space, empty, 129 characters.
        { null, [], ["Claim"] },
        { null, [], ["1claim"] },
        { null, [], ["claim reward"] },
        { null, [], [""] },
        { null, [], [new string('a', 129)] },
        // Repeated, ordinally.
        { null, [], ["claim", "claim"] },
        // More than 64.
        { null, [], Enumerable.Range(0, 65).Select(index => "l" + index).ToArray() },
        // The build's own name stays reserved for the build itself.
        { "adaptivesync.lambdas", [], ["adaptivesync.lambdas"] },
        // A registering build receives realtime events only on a registered id.
        { null, ["other"], ["claim"] },
    };

    [Theory]
    [MemberData(nameof(InvalidLambdaRegistrations))]
    public async Task ARegistrationOutsideTheProductRulesIsRefusedBeforeTransport(string? name, string[] ingress, string[] ids)
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(Json(HttpStatusCode.Accepted, Checking()));
        });
        using var builds = Builds(handler);
        using var bundle = new MemoryStream(Encoding.ASCII.GetBytes("bundle"));

        var refusal = await Assert.ThrowsAsync<ArgumentException>(() => builds.UploadAsync(
            ProjectId,
            EnvironmentId,
            bundle,
            new YogabaseBuildUploadRequest
            {
                Name = name,
                Lambda = new YogabaseLambdaDeclaration { RealtimeIngressBindings = ingress, RegisteredLambdaIds = ids },
            },
            TestContext.Current.CancellationToken));
        Assert.Contains("registeredLambdaIds", refusal.Message + refusal.ParamName, StringComparison.Ordinal);
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData("claim_reward", true)]
    [InlineData("season.rollover-2", true)]
    [InlineData("a", true)]
    [InlineData("Claim", false)]
    [InlineData("_claim", false)]
    [InlineData("claim/reward", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ARegisteredLambdaIdIsTheProductGrammar(string? value, bool expected) =>
        Assert.Equal(expected, YogabaseLambdaDeclaration.IsRegisteredLambdaId(value));

    [Fact]
    public void ARegisteredLambdaIdIsAtMost128Characters()
    {
        Assert.True(YogabaseLambdaDeclaration.IsRegisteredLambdaId(new string('a', 128)));
        Assert.False(YogabaseLambdaDeclaration.IsRegisteredLambdaId(new string('a', 129)));
    }

    public static TheoryData<string[], string[]> InvalidLambdaBindings() => new()
    {
        { ["Match"], [] },
        { ["1match"], [] },
        { ["match events"], [] },
        { [""], [] },
        { [new string('a', 97)], [] },
        { [], ["dup", "dup"] },
        { [], Enumerable.Range(0, 17).Select(index => "b" + index).ToArray() },
    };

    [Theory]
    [MemberData(nameof(InvalidLambdaBindings))]
    public async Task ALambdaDeclarationOutsideTheAdmissionRulesIsRefusedBeforeTransport(string[] ingress, string[] egress)
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(Json(HttpStatusCode.Accepted, Checking()));
        });
        using var builds = Builds(handler);
        using var bundle = new MemoryStream(Encoding.ASCII.GetBytes("bundle"));

        await Assert.ThrowsAsync<ArgumentException>(() => builds.UploadAsync(
            ProjectId,
            EnvironmentId,
            bundle,
            new YogabaseBuildUploadRequest
            {
                Lambda = new YogabaseLambdaDeclaration { RealtimeIngressBindings = ingress, RealtimeEgressBindings = egress },
            },
            TestContext.Current.CancellationToken));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task ALambdaDeclarationRefusesAnUndefinedKindAndServerOnlyHints()
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(Json(HttpStatusCode.Accepted, Checking()));
        });
        using var builds = Builds(handler);
        using var bundle = new MemoryStream(Encoding.ASCII.GetBytes("bundle"));

        await Assert.ThrowsAsync<ArgumentException>(() => builds.UploadAsync(ProjectId, EnvironmentId, bundle,
            new YogabaseBuildUploadRequest { Lambda = new YogabaseLambdaDeclaration { ExecutionKind = (YogabaseLambdaExecutionKind)7 } },
            TestContext.Current.CancellationToken));
        // A Lambda has no executable or ports; stating them beside the declaration is the mistake of
        // uploading a server build as a Lambda, or the other way round.
        await Assert.ThrowsAsync<ArgumentException>(() => builds.UploadAsync(ProjectId, EnvironmentId, bundle,
            new YogabaseBuildUploadRequest { Executable = "Server.x86_64", Lambda = new YogabaseLambdaDeclaration() },
            TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => builds.UploadAsync(ProjectId, EnvironmentId, bundle,
            new YogabaseBuildUploadRequest
            {
                Ports = [new YogabaseBuildPort { Name = "game", Port = 7777 }],
                Lambda = new YogabaseLambdaDeclaration(),
            },
            TestContext.Current.CancellationToken));
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData("game", 0, "udp")]
    [InlineData("game", 70000, "udp")]
    [InlineData("game", 7777, "sctp")]
    [InlineData("ga=me", 7777, "udp")]
    [InlineData("ga/me", 7777, "udp")]
    [InlineData("", 7777, "udp")]
    public void APortFieldValueRefusesAnythingOutsideTheContract(string name, int port, string protocol)
    {
        var declared = new YogabaseBuildPort { Name = name, Port = port, Protocol = protocol };

        Assert.Throws<InvalidOperationException>(() => declared.ToFieldValue());
    }

    [Fact]
    public void APortFieldValueIsTheNameEqualsPortSlashProtocolForm() => Assert.Equal(
        "game=7777/udp",
        new YogabaseBuildPort { Name = "game", Port = 7777, Protocol = "udp" }.ToFieldValue());

    [Theory]
    // The Control Plane's route constrains the id to a UUID; every spelling of the same UUID is
    // sent in the canonical D form the route and the response both use.
    [InlineData("3f2504e0-4f89-11d3-9a0c-0305e82c3301")]
    [InlineData("3F2504E0-4F89-11D3-9A0C-0305E82C3301")]
    [InlineData("{3f2504e0-4f89-11d3-9a0c-0305e82c3301}")]
    [InlineData("3f2504e04f8911d39a0c0305e82c3301")]
    public async Task BuildGetUsesTheExactBuildRouteWithTheCanonicalIdentifier(string buildId)
    {
        Uri? route = null;
        var handler = new ScriptedHandler((request, _) =>
        {
            route = request.RequestUri;
            return Task.FromResult(Json(HttpStatusCode.OK, Terminal(YogabaseBuild.StateAdmitted)));
        });
        using var builds = Builds(handler);

        var build = await builds.GetAsync(ProjectId, EnvironmentId, buildId, TestContext.Current.CancellationToken);

        Assert.Equal(BuildsRoute + "/" + BuildId, route!.AbsoluteUri);
        Assert.True(build.IsAdmitted);
        Assert.True(build.Check.Passed);
        Assert.Equal(YogabaseBuildCheck.YogabaseDerived, build.Check.EvidenceOrigin);
        Assert.Equal("sha256:inv", build.Check.InventoryDigest);
        Assert.Equal("sha256:scan", build.Check.ScanDigest);
        Assert.Equal("sha256:db", build.Check.DatabaseDigest);
        Assert.Equal(
            DateTimeOffset.Parse("2026-09-09T00:00:00+00:00", System.Globalization.CultureInfo.InvariantCulture),
            build.Check.ObservedAt);
    }

    [Theory]
    [InlineData("bld-1")]
    [InlineData("")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task ABuildIdThatIsNotAUuidIsRefusedBeforeTransport(string buildId)
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(Json(HttpStatusCode.OK, Terminal(YogabaseBuild.StateAdmitted)));
        });
        using var builds = Builds(handler);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            builds.GetAsync(ProjectId, EnvironmentId, buildId, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            builds.RecheckAsync(ProjectId, EnvironmentId, buildId, TestContext.Current.CancellationToken));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task BuildRecheckPostsTheRecheckRouteAndReturnsTheBuild()
    {
        HttpRequestMessage? observed = null;
        var handler = new ScriptedHandler((request, _) =>
        {
            observed = Clone(request);
            return Task.FromResult(Json(HttpStatusCode.Accepted, Checking()));
        });
        using var builds = Builds(handler);

        var build = await builds.RecheckAsync(ProjectId, EnvironmentId, BuildId, TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, observed!.Method);
        Assert.Equal(BuildsRoute + "/" + BuildId + "/recheck", observed.RequestUri!.AbsoluteUri);
        Assert.Null(observed.Content);
        Assert.Equal(YogabaseBuild.StateChecking, build.State);
    }

    [Fact]
    public async Task ABuildRefusalCarriesTheControlPlaneCodeFieldAndCorrelation()
    {
        var handler = new ScriptedHandler((_, _) =>
        {
            var response = Json(
                HttpStatusCode.BadRequest,
                "{\"type\":\"https://yogabase.io/problems/build-bundle-unrecognized\",\"title\":\"The build request was refused\","
                + "\"status\":400,\"detail\":\"The archive holds neither a .NET Lambda bundle nor a Linux executable this product can launch.\","
                + "\"code\":\"BUILD_BUNDLE_UNRECOGNIZED\",\"correlationId\":\"corr-1\",\"fieldErrors\":{\"bundle\":[\"unrecognized\"]}}");
            response.Headers.TryAddWithoutValidation("X-Correlation-ID", "corr-1");
            return Task.FromResult(response);
        });
        using var builds = Builds(handler);
        using var bundle = new MemoryStream(Encoding.ASCII.GetBytes("bundle"));

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(() =>
            builds.UploadAsync(ProjectId, EnvironmentId, bundle, null, TestContext.Current.CancellationToken));

        Assert.Equal(400, exception.StatusCode);
        Assert.Equal("BUILD_BUNDLE_UNRECOGNIZED", exception.Code);
        Assert.Equal("corr-1", exception.TraceId);
    }

    [Fact]
    public async Task WaitForCheckPollsUntilTerminalAndReportsEveryPoll()
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(calls < 3
                ? Json(HttpStatusCode.OK, Checking())
                : Json(HttpStatusCode.OK, Refused()));
        });
        using var builds = Builds(handler);
        var observed = new RecordingProgress();

        var build = await builds.WaitForCheckAsync(
            ProjectId,
            EnvironmentId,
            BuildId,
            FastPolling(),
            observed,
            TestContext.Current.CancellationToken);

        Assert.Equal(3, calls);
        Assert.Equal(ExpectedPolledStates, observed.States);
        Assert.Equal(YogabaseBuild.StateRefused, build.State);
        Assert.True(build.IsTerminal);
        Assert.False(build.IsAdmitted);
        Assert.Equal(2, build.Attempt);
        var finding = Assert.Single(build.Check.Findings);
        Assert.Equal("CVE-2026-62901", finding.Id);
        Assert.Equal("System.Text.Json", finding.Package);
        Assert.Equal("8.0.0", finding.Version);
        Assert.Equal("8.0.5", finding.FixedIn);
        Assert.Equal("High", finding.Severity);
        Assert.Equal("Server_Data/Managed/System.Text.Json.dll", finding.Entry);
        var refusal = Assert.Single(build.Check.Refusals);
        Assert.Equal("bundle-executable-missing", refusal.Code);
        Assert.Equal("executable", refusal.Field);
        Assert.Equal("entry-absent", refusal.Cause);
    }

    [Fact]
    public async Task WaitForCheckRetriesATransientFailure()
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(calls == 1
                ? Json(HttpStatusCode.ServiceUnavailable, "{\"code\":\"unavailable\"}")
                : Json(HttpStatusCode.OK, Terminal(YogabaseBuild.StateAdmitted)));
        });
        using var builds = Builds(handler);

        var build = await builds.WaitForCheckAsync(
            ProjectId,
            EnvironmentId,
            BuildId,
            FastPolling(),
            progress: null,
            TestContext.Current.CancellationToken);

        Assert.Equal(2, calls);
        Assert.True(build.IsAdmitted);
    }

    [Fact]
    public async Task WaitForCheckSurfacesAPermanentFailure()
    {
        var handler = new ScriptedHandler((_, _) =>
            Task.FromResult(Json(HttpStatusCode.NotFound, "{\"code\":\"BUILD_NOT_FOUND\"}")));
        using var builds = Builds(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(() => builds.WaitForCheckAsync(
            ProjectId,
            EnvironmentId,
            BuildId,
            FastPolling(),
            progress: null,
            TestContext.Current.CancellationToken));

        Assert.Equal("BUILD_NOT_FOUND", exception.Code);
    }

    [Fact]
    public async Task WaitForCheckTimesOutWhileTheBuildStaysChecking()
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Json(
            HttpStatusCode.OK,
            Checking())));
        using var builds = Builds(handler);

        await Assert.ThrowsAsync<TimeoutException>(() => builds.WaitForCheckAsync(
            ProjectId,
            EnvironmentId,
            BuildId,
            new YogabaseBuildPollingOptions
            {
                Timeout = TimeSpan.FromMilliseconds(80),
                InitialDelay = TimeSpan.FromMilliseconds(1),
                MaximumDelay = TimeSpan.FromMilliseconds(5),
            },
            progress: null,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task WaitForCheckStopsWhenTheCallerCancels()
    {
        using var cancellation = new CancellationTokenSource();
        var handler = new ScriptedHandler((_, _) =>
        {
            cancellation.Cancel();
            return Task.FromResult(Json(
                HttpStatusCode.OK,
                Checking()));
        });
        using var builds = Builds(handler);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => builds.WaitForCheckAsync(
            ProjectId,
            EnvironmentId,
            BuildId,
            FastPolling(),
            progress: null,
            cancellation.Token));
    }

    [Fact]
    public async Task AnInvalidBuildResponseIsRefusedRatherThanReturnedEmpty()
    {
        var handler = new ScriptedHandler((_, _) =>
            Task.FromResult(Json(HttpStatusCode.Accepted, "{\"buildId\":\"bld-1\",\"digest\":\"\",\"state\":\"\"}")));
        using var builds = Builds(handler);
        using var bundle = new MemoryStream(Encoding.ASCII.GetBytes("bundle"));

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(() =>
            builds.UploadAsync(ProjectId, EnvironmentId, bundle, null, TestContext.Current.CancellationToken));

        Assert.Equal("build_response_invalid", exception.Code);
    }

    [Fact]
    public void TheBuildsClientRefusesAnInsecureNonLoopbackControlPlane() =>
        Assert.Throws<ArgumentException>(() => new YogabaseBuildsClient(
            new Uri("http://control-plane.example/"),
            DeveloperToken));

    [Theory]
    [InlineData("")]
    [InlineData("ybt_")]
    [InlineData("ybt_has whitespace in it 0123")]
    // A bearer without the ybt_ prefix is not a developer token to the Control Plane: its
    // authentication handler yields no result rather than a refusal, so the caller would see a
    // bare 401 with nothing to act on. Refuse it here, naming the prefix.
    [InlineData("dev-token-0123456789")]
    [InlineData("ybt_0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz")]
    public void TheBuildsClientRefusesAMalformedDeveloperToken(string token) =>
        Assert.Throws<ArgumentException>(() => new YogabaseBuildsClient(
            new Uri("https://control-plane.example/"),
            token));

    [Fact]
    public async Task TheBuildsClientIsUnusableAfterDisposal()
    {
        var builds = Builds(new ScriptedHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, "{}"))));
        builds.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            builds.GetAsync(ProjectId, EnvironmentId, BuildId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void ABundleZipUsesForwardSlashesAndKeepsThePlayerBinaryExecutable()
    {
        var root = Path.Combine(Path.GetTempPath(), "yogabase-bundle-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "Server_Data", "Managed"));
        File.WriteAllText(Path.Combine(root, "Server.x86_64"), "player");
        File.WriteAllText(Path.Combine(root, "UnityPlayer.so"), "native");
        File.WriteAllText(Path.Combine(root, "Server_Data", "Managed", "Assembly-CSharp.dll"), "managed");
        try
        {
            using var bundle = YogabaseBuildsClient.CreateBundleZip(root, "Server.x86_64");
            using var archive = new ZipArchive(bundle, ZipArchiveMode.Read);

            Assert.Equal(
                ExpectedBundleEntries,
                archive.Entries.Select(entry => entry.FullName).OrderBy(name => name, StringComparer.Ordinal));
            Assert.Equal(0x1ED, Mode(archive, "Server.x86_64"));
            Assert.Equal(0x1ED, Mode(archive, "UnityPlayer.so"));
            Assert.Equal(0x1A4, Mode(archive, "Server_Data/Managed/Assembly-CSharp.dll"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ABundleZipRefusesAMissingOrEmptyDirectory()
    {
        var missing = Path.Combine(Path.GetTempPath(), "yogabase-absent-" + Guid.NewGuid().ToString("N"));
        Assert.Throws<DirectoryNotFoundException>(() => YogabaseBuildsClient.CreateBundleZip(missing));

        Directory.CreateDirectory(missing);
        try
        {
            Assert.Throws<ArgumentException>(() => YogabaseBuildsClient.CreateBundleZip(missing));
        }
        finally
        {
            Directory.Delete(missing, recursive: true);
        }
    }

    // ---------------------------------------------------------------------------------------
    // The restored Data API's capability row
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void ABootstrapCarryingTheDataCapabilityIsAcceptedAndRead()
    {
        // The row an installation adds for a client that names data-rest, and the one an installation that
        // predates the 2026-09-08 removal published by default: the same id, read the same way.
        var withData = new List<YogabaseCapability>(YogabaseClientTests.Capabilities())
        {
            new() { Id = YogabaseCapability.DataRest, Available = true },
        };

        using var client = YogabaseClient.FromConfiguration(
            ConfigurationWith(withData),
            new YogabaseClientOptions
            {
                AllowInsecureLoopback = true,
                TransportHandler = new ScriptedHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, "{}"))),
            });

        Assert.True(client.Configuration.IsAvailable("auth"));
        Assert.True(client.Configuration.IsAvailable(YogabaseCapability.DataRest));
        Assert.Equal(YogabaseCapability.DataRest, YogabaseCapability.WithdrawnDataRest);
    }

    [Fact]
    public void ABootstrapCarryingBothRequestedRowsIsAcceptedAndASeventhRowIsRefused()
    {
        var six = new List<YogabaseCapability>(YogabaseClientTests.Capabilities())
        {
            new() { Id = YogabaseCapability.RealtimePresence, Available = false, UnavailableReason = "realtime-subscribe-unavailable" },
            new() { Id = YogabaseCapability.DataRest, Available = false, UnavailableReason = "data-api-disabled" },
        };
        using (var client = YogabaseClient.FromConfiguration(ConfigurationWith(six), new YogabaseClientOptions { AllowInsecureLoopback = true }))
        {
            Assert.Equal(6, client.Configuration.Capabilities.Count);
            Assert.False(client.Configuration.IsAvailable(YogabaseCapability.DataRest));
        }

        var duplicate = new List<YogabaseCapability>(six) { new() { Id = YogabaseCapability.DataRest, Available = true } };
        Assert.Throws<ArgumentException>(() => YogabaseClient.FromConfiguration(
            ConfigurationWith(duplicate),
            new YogabaseClientOptions { AllowInsecureLoopback = true }));
    }

    [Fact]
    public void ABootstrapCarryingAnUnknownCapabilityIsRefused()
    {
        var invalid = new List<YogabaseCapability>(YogabaseClientTests.Capabilities())
        {
            new() { Id = "data-graphql", Available = true },
        };

        Assert.Throws<ArgumentException>(() => YogabaseClient.FromConfiguration(
            ConfigurationWith(invalid),
            new YogabaseClientOptions { AllowInsecureLoopback = true }));
    }

    // ---------------------------------------------------------------------------------------

    private static int Mode(ZipArchive archive, string entryName) =>
        (archive.GetEntry(entryName)!.ExternalAttributes >> 16) & 0x1FF;

    private static YogabaseBuildPollingOptions FastPolling() => new()
    {
        Timeout = TimeSpan.FromSeconds(10),
        InitialDelay = TimeSpan.FromMilliseconds(1),
        MaximumDelay = TimeSpan.FromMilliseconds(2),
    };

    private static YogabaseBuildsClient Builds(HttpMessageHandler handler) => YogabaseClient.Builds(
        new Uri("http://localhost:7100/"),
        DeveloperToken,
        new YogabaseBuildsClientOptions
        {
            AllowInsecureLoopback = true,
            TransportHandler = handler,
        });

    private static async Task<YogabaseClient> ClientAsync(HttpMessageHandler handler)
    {
        var store = new InMemoryYogabaseSessionStore();
        await store.SaveAsync(
            new YogabaseTokenSet
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            },
            TestContext.Current.CancellationToken);
        return YogabaseClient.FromConfiguration(
            ConfigurationWith(YogabaseClientTests.Capabilities()),
            new YogabaseClientOptions
            {
                AllowInsecureLoopback = true,
                SessionStore = store,
                TransportHandler = handler,
            });
    }

    private static YogabaseClientConfiguration ConfigurationWith(IReadOnlyList<YogabaseCapability> capabilities) => new()
    {
        Schema = YogabaseClient.ClientConfigSchema,
        ProjectId = ProjectId,
        EnvironmentId = EnvironmentId,
        GatewayEndpoint = "http://localhost/",
        RealtimeEndpoint = "http://localhost:5001/",
        OidcAuthority = "http://localhost/p/11111111-1111-4111-8111-111111111111",
        OidcDiscoveryEndpoint = "http://localhost/.well-known/openid-configuration",
        OidcPublicClientId = "yogabase-native-preview",
        OidcScopes = ["openid", "offline_access"],
        OidcRedirectUris = ["io.yogabase.app:/oauth/callback"],
        RealtimePath = "/v1/realtime",
        Capabilities = capabilities,
        IssuedAt = DateTimeOffset.Parse("2026-09-08T00:00:00+00:00", System.Globalization.CultureInfo.InvariantCulture),
    };

    /// <summary>
    /// Minimal reader for the multipart body the build upload writes. Asserting on the parsed
    /// fields rather than on substrings keeps the test honest about part order, file name, and
    /// media type instead of accidentally matching text inside the bundle.
    /// </summary>
    private static async Task<IReadOnlyList<MultipartField>> ReadMultipartAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        var boundary = content.Headers.ContentType!.Parameters
            .Single(parameter => string.Equals(parameter.Name, "boundary", StringComparison.OrdinalIgnoreCase))
            .Value!
            .Trim('"');
        var body = await content.ReadAsStringAsync(cancellationToken);
        var fields = new List<MultipartField>();
        foreach (var section in body.Split("--" + boundary, StringSplitOptions.None))
        {
            var trimmed = section.Trim('\r', '\n');
            if (trimmed.Length == 0 || string.Equals(trimmed, "--", StringComparison.Ordinal))
            {
                continue;
            }

            // An empty field value leaves no blank line once the section is trimmed.
            var separator = trimmed.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            var headers = separator < 0 ? trimmed : trimmed[..separator];
            var value = separator < 0 ? string.Empty : trimmed[(separator + 4)..];
            fields.Add(new MultipartField(
                HeaderParameter(headers, "name="),
                HeaderParameter(headers, "filename="),
                HeaderValue(headers, "Content-Type:"),
                value));
        }

        return fields;
    }

    private static string? HeaderParameter(string headers, string prefix)
    {
        var start = headers.IndexOf(prefix, StringComparison.Ordinal);
        if (start < 0)
        {
            return null;
        }

        var value = headers[(start + prefix.Length)..];
        var end = value.IndexOfAny(HeaderParameterTerminators);
        return (end < 0 ? value : value[..end]).Trim().Trim('"');
    }

    private static string? HeaderValue(string headers, string prefix)
    {
        foreach (var line in headers.Split("\r\n", StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return line[prefix.Length..].Split(';')[0].Trim();
            }
        }

        return null;
    }

    private sealed record MultipartField(string? Name, string? FileName, string? ContentType, string Body);

    private sealed class RecordingProgress : IProgress<YogabaseBuild>
    {
        private readonly List<string> states = [];

        internal IReadOnlyList<string> States => states;

        public void Report(YogabaseBuild value) => states.Add(value.State);
    }

    private static string Allocation() => JsonSerializer.Serialize(new
    {
        serverId = "srv-01",
        state = "Allocated",
        networkPorts = new[]
        {
            new
            {
                name = "game",
                internalPort = 7777,
                externalPort = 31245,
                protocol = "udp",
                host = "10.0.0.7",
            },
        },
    });

    // Exactly the shape BuildEndpoints.Response writes: every field, every time.
    private static string Checking() => JsonSerializer.Serialize(new
    {
        buildId = BuildId,
        digest = "sha256:aa",
        state = "checking",
        identity = "battle:1.4.0",
        attempt = 1,
        kind = "SelfContainedService",
        check = new
        {
            evidenceOrigin = "yogabase-derived",
            inventoryDigest = "",
            scanDigest = "",
            databaseDigest = "",
            passed = false,
            observedAt = "2026-09-09T00:00:00+00:00",
            findings = Array.Empty<object>(),
            refusals = Array.Empty<object>(),
        },
    });

    private static string Terminal(string state) => JsonSerializer.Serialize(new
    {
        buildId = BuildId,
        digest = "sha256:aa",
        state,
        identity = "battle:1.4.0",
        attempt = 1,
        kind = "SelfContainedService",
        check = new
        {
            evidenceOrigin = "yogabase-derived",
            inventoryDigest = "sha256:inv",
            scanDigest = "sha256:scan",
            databaseDigest = "sha256:db",
            passed = true,
            observedAt = "2026-09-09T00:00:00+00:00",
            findings = Array.Empty<object>(),
            refusals = Array.Empty<object>(),
        },
    });

    private static string Refused() => JsonSerializer.Serialize(new
    {
        buildId = BuildId,
        digest = "sha256:aa",
        state = "refused",
        identity = "battle:1.4.0",
        attempt = 2,
        kind = "SelfContainedService",
        check = new
        {
            evidenceOrigin = "yogabase-derived",
            inventoryDigest = "sha256:inv",
            scanDigest = "sha256:scan",
            databaseDigest = "sha256:db",
            passed = false,
            observedAt = "2026-09-09T00:00:00+00:00",
            findings = new[]
            {
                new
                {
                    id = "CVE-2026-62901",
                    package = "System.Text.Json",
                    version = "8.0.0",
                    fixedIn = "8.0.5",
                    severity = "High",
                    entry = "Server_Data/Managed/System.Text.Json.dll",
                },
            },
            refusals = new[]
            {
                new
                {
                    code = "bundle-executable-missing",
                    field = "executable",
                    cause = "entry-absent",
                },
            },
        },
    });

    private static HttpResponseMessage Json(HttpStatusCode status, string json) => new(status)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static HttpRequestMessage Clone(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }

    private sealed class ScriptedHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
