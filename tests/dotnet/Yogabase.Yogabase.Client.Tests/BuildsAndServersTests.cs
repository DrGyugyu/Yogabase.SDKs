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

    [Theory]
    [InlineData("[{\"serverId\":\"srv-01\",\"state\":\"Ready\",\"networkPorts\":[]}]")]
    [InlineData("{\"servers\":[{\"serverId\":\"srv-01\",\"state\":\"Ready\",\"networkPorts\":[]}]}")]
    [InlineData("{\"items\":[{\"serverId\":\"srv-01\",\"state\":\"Ready\",\"networkPorts\":[]}]}")]
    public async Task ServerListReadsBothTheBareArrayAndTheEnvelope(string payload)
    {
        Uri? route = null;
        var handler = new ScriptedHandler((request, _) =>
        {
            route = request.RequestUri;
            return Task.FromResult(Json(HttpStatusCode.OK, payload));
        });
        await using var client = await ClientAsync(handler);

        var servers = await client.Servers.ListAsync(TestContext.Current.CancellationToken);

        Assert.Equal("http://localhost/v1/servers", route!.AbsoluteUri);
        Assert.Equal("srv-01", Assert.Single(servers).ServerId);
    }

    [Fact]
    public async Task ServerListRefusesAnUnrecognizedEnvelope()
    {
        var handler = new ScriptedHandler((_, _) =>
            Task.FromResult(Json(HttpStatusCode.OK, "{\"unexpected\":[]}")));
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
                : new HttpResponseMessage(HttpStatusCode.NoContent));
        });
        await using var client = await ClientAsync(handler);

        var read = await client.Servers.GetAsync("srv-01", TestContext.Current.CancellationToken);
        var stopped = await client.Servers.StopAsync("srv-01", TestContext.Current.CancellationToken);

        Assert.Equal("srv-01", read.ServerId);
        Assert.Null(stopped);
        Assert.Equal(ExpectedServerRoutes, routes);
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

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" battle")]
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
            return Json(
                HttpStatusCode.Accepted,
                "{\"buildId\":\"bld-1\",\"digest\":\"sha256:aa\",\"state\":\"checking\"}");
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
        Assert.Equal(new AuthenticationHeaderValue("Bearer", "dev-token-0123456789"), observed.Headers.Authorization);

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
        Assert.Equal("bld-1", build.BuildId);
        Assert.Equal("sha256:aa", build.Digest);
        Assert.Equal(YogabaseBuild.StateChecking, build.State);
        Assert.False(build.IsTerminal);
    }

    [Fact]
    public async Task BuildUploadOmitsEveryFieldTheCallerDidNotSet()
    {
        IReadOnlyList<MultipartField> parts = Array.Empty<MultipartField>();
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            parts = await ReadMultipartAsync(request.Content!, cancellationToken);
            return Json(
                HttpStatusCode.Accepted,
                "{\"buildId\":\"bld-1\",\"digest\":\"sha256:aa\",\"state\":\"checking\"}");
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

    [Fact]
    public async Task BuildGetUsesTheExactBuildRouteAndEscapesTheIdentifier()
    {
        Uri? route = null;
        var handler = new ScriptedHandler((request, _) =>
        {
            route = request.RequestUri;
            return Task.FromResult(Json(HttpStatusCode.OK, Terminal(YogabaseBuild.StateAdmitted)));
        });
        using var builds = Builds(handler);

        var build = await builds.GetAsync(ProjectId, EnvironmentId, "bld 1", TestContext.Current.CancellationToken);

        Assert.Equal(BuildsRoute + "/bld%201", route!.AbsoluteUri);
        Assert.True(build.IsAdmitted);
        Assert.True(build.Check!.Passed);
    }

    [Fact]
    public async Task WaitForCheckPollsUntilTerminalAndReportsEveryPoll()
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(calls < 3
                ? Json(HttpStatusCode.OK, "{\"buildId\":\"bld-1\",\"digest\":\"sha256:aa\",\"state\":\"checking\"}")
                : Json(HttpStatusCode.OK, Refused()));
        });
        using var builds = Builds(handler);
        var observed = new RecordingProgress();

        var build = await builds.WaitForCheckAsync(
            ProjectId,
            EnvironmentId,
            "bld-1",
            FastPolling(),
            observed,
            TestContext.Current.CancellationToken);

        Assert.Equal(3, calls);
        Assert.Equal(ExpectedPolledStates, observed.States);
        Assert.Equal(YogabaseBuild.StateRefused, build.State);
        Assert.True(build.IsTerminal);
        Assert.False(build.IsAdmitted);
        var finding = Assert.Single(build.Check!.Findings);
        Assert.Equal("CVE-2026-62901", finding.Id);
        Assert.Equal("System.Text.Json", finding.Package);
        Assert.Equal("8.0.0", finding.Version);
        Assert.Equal("8.0.5", finding.FixedIn);
        Assert.Equal("High", finding.Severity);
        Assert.Equal("Server_Data/Managed/System.Text.Json.dll", finding.Entry);
        var refusal = Assert.Single(build.Check.Refusals);
        Assert.Equal("bundle_executable_missing", refusal.Code);
        Assert.Equal("executable", refusal.Path);
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
            "bld-1",
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
            Task.FromResult(Json(HttpStatusCode.NotFound, "{\"code\":\"build_not_found\"}")));
        using var builds = Builds(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(() => builds.WaitForCheckAsync(
            ProjectId,
            EnvironmentId,
            "bld-1",
            FastPolling(),
            progress: null,
            TestContext.Current.CancellationToken));

        Assert.Equal("build_not_found", exception.Code);
    }

    [Fact]
    public async Task WaitForCheckTimesOutWhileTheBuildStaysChecking()
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Json(
            HttpStatusCode.OK,
            "{\"buildId\":\"bld-1\",\"digest\":\"sha256:aa\",\"state\":\"checking\"}")));
        using var builds = Builds(handler);

        await Assert.ThrowsAsync<TimeoutException>(() => builds.WaitForCheckAsync(
            ProjectId,
            EnvironmentId,
            "bld-1",
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
                "{\"buildId\":\"bld-1\",\"digest\":\"sha256:aa\",\"state\":\"checking\"}"));
        });
        using var builds = Builds(handler);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => builds.WaitForCheckAsync(
            ProjectId,
            EnvironmentId,
            "bld-1",
            FastPolling(),
            progress: null,
            cancellation.Token));
    }

    [Fact]
    public async Task AnInvalidBuildResponseIsRefusedRatherThanReturnedEmpty()
    {
        var handler = new ScriptedHandler((_, _) =>
            Task.FromResult(Json(HttpStatusCode.Accepted, "{\"buildId\":\"\",\"digest\":\"\",\"state\":\"\"}")));
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
            "dev-token-0123456789"));

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("has whitespace in it 0123")]
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
            builds.GetAsync(ProjectId, EnvironmentId, "bld-1", TestContext.Current.CancellationToken));
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
    // Withdrawn Data API
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void ABootstrapStillCarryingTheWithdrawnDataCapabilityIsAcceptedAndIgnored()
    {
        var legacy = new List<YogabaseCapability>(YogabaseClientTests.Capabilities())
        {
            new() { Id = YogabaseCapability.WithdrawnDataRest, Available = true },
        };

        using var client = YogabaseClient.FromConfiguration(
            ConfigurationWith(legacy),
            new YogabaseClientOptions
            {
                AllowInsecureLoopback = true,
                TransportHandler = new ScriptedHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, "{}"))),
            });

        Assert.True(client.Configuration.IsAvailable("auth"));
        Assert.False(client.Configuration.IsAvailable(YogabaseCapability.WithdrawnDataRest));
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
        "dev-token-0123456789",
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
        OidcPublicClientId = "stackyoga-native-preview",
        OidcScopes = ["openid", "offline_access"],
        OidcRedirectUris = ["com.stackyoga.app:/oauth/callback"],
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

            var separator = trimmed.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            var headers = trimmed[..separator];
            var value = trimmed[(separator + 4)..];
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

    private static string Terminal(string state) => JsonSerializer.Serialize(new
    {
        buildId = "bld-1",
        digest = "sha256:aa",
        state,
        check = new { passed = true, findings = Array.Empty<object>(), refusals = Array.Empty<object>() },
    });

    private static string Refused() => JsonSerializer.Serialize(new
    {
        buildId = "bld-1",
        digest = "sha256:aa",
        state = "refused",
        check = new
        {
            passed = false,
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
                    code = "bundle_executable_missing",
                    path = "executable",
                    message = "The declared executable is not present in the bundle.",
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
