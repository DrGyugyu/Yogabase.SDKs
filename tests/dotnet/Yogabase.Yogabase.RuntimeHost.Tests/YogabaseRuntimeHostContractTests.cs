using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Yogabase.Yogabase.RuntimeHost;

namespace Yogabase.RuntimeHost.Tests;

/// <summary>
/// Holds the authored runtime-host client to <c>contracts/yogabase-runtime-host-self-v1.openapi.json</c>,
/// the mirrored document that describes the loopback relay routes the client speaks. The SDK does not
/// generate from that document, so nothing else would notice a re-mirror that adds, moves or renumbers
/// a route, a limit or a refusal code the client relies on. <c>ContractDigestTests</c> in the client
/// test project pins the file's bytes; these tests read it structurally and compare the client with it.
/// Where the two disagree, the document is the contract.
/// </summary>
public sealed class YogabaseRuntimeHostContractTests
{
    private const string SolutionFileName = "Yogabase.SDKs.slnx";
    private const string ContractFileName = "yogabase-runtime-host-self-v1.openapi.json";
    private const string PendingCode = "RUNTIME_HOST_INSTANCE_PENDING";

    private static readonly string[] RelayVerbs = ["heartbeat", "ready", "reserve", "shutdown", "assignments"];

    private static readonly HashSet<string> HttpMethods =
        new(["get", "put", "post", "delete", "options", "head", "patch", "trace"], StringComparer.Ordinal);

    private static readonly Lazy<string> RepoRoot = new(FindRepoRoot);

    private static readonly Lazy<JsonDocument> Contract = new(() =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoRoot.Value, "contracts", ContractFileName))));

    private static JsonElement Root => Contract.Value.RootElement;

    private static JsonElement Relay => Root.GetProperty("x-yogabase-runtime-host-relay");

    private static JsonElement Schemas => Root.GetProperty("components").GetProperty("schemas");

    // ----------------------------------------------------------------------------------------
    // Routes
    // ----------------------------------------------------------------------------------------

    [Fact]
    public void TheDocumentDescribesTheLoopbackRelayTheClientDefaultsTo()
    {
        var server = Root.GetProperty("servers")[0].GetProperty("url").GetString();

        Assert.Equal(YogabaseRuntimeHostOptions.DefaultEndpoint, server);
        Assert.Equal(0, Root.GetProperty("security").GetArrayLength());
        Assert.Equal(5, Root.GetProperty("paths").EnumerateObject().Count());
    }

    [Fact]
    public async Task TheClientsRouteSetEqualsTheDocumentsOperations()
    {
        var requests = new List<string>();
        var handler = new ScriptedHandler((request, _) =>
        {
            requests.Add($"{request.Method.Method} {request.RequestUri!.AbsolutePath}");
            return Task.FromResult(request.RequestUri.AbsolutePath.EndsWith("assignments", StringComparison.Ordinal)
                ? Ok("{\"events\":[],\"resumeToken\":\"\"}")
                : Ok(LeaseBody));
        });
        await using var client = Client(handler);

        foreach (var verb in RelayVerbs)
        {
            await CallAsync(client, verb);
        }

        // The other two read paths issue the same assignments request.
        await client.WatchAssignmentsOnceAsync("7", TestContext.Current.CancellationToken);
        await client.ReadCurrentStateAsync(TestContext.Current.CancellationToken);

        var documented = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var path in Root.GetProperty("paths").EnumerateObject())
        {
            foreach (var operation in path.Value.EnumerateObject().Where(member => HttpMethods.Contains(member.Name)))
            {
                documented.Add($"{operation.Name.ToUpperInvariant()} {path.Name}");
            }
        }

        Assert.Equal(5, documented.Count);
        Assert.Equal(documented, new SortedSet<string>(requests, StringComparer.Ordinal));
    }

    [Fact]
    public void TheClientHasNoPublicCallThisRouteTestDoesNotExercise()
    {
        // A new public call on the client may add a route. This list names every public instance
        // method; a new one fails here until it is added to the route test above (or recorded as
        // issuing no request of its own).
        var methods = typeof(YogabaseRuntimeHostClient)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal)
            {
                "Connect",
                "HeartbeatOnceAsync",
                "ReadyAsync",
                "ReserveAsync",
                "ShutdownAsync",
                "WatchAssignmentsOnceAsync",
                "ReadCurrentStateAsync",
                "WatchAssignmentsAsync",
                "DisposeAsync",
            },
            methods);
    }

    [Fact]
    public async Task TheResumeTokenTravelsAsTheDocumentedQueryParameter()
    {
        var parameter = Root.GetProperty("paths")
            .GetProperty("/runtime-host/v1/self/assignments")
            .GetProperty("get")
            .GetProperty("parameters")
            .EnumerateArray()
            .Single();
        Assert.Equal("query", parameter.GetProperty("in").GetString());
        Assert.False(parameter.GetProperty("required").GetBoolean());

        Uri? route = null;
        var handler = new ScriptedHandler((request, _) =>
        {
            route = request.RequestUri;
            return Task.FromResult(Ok("{\"events\":[],\"resumeToken\":\"12\"}"));
        });
        await using var client = Client(handler);

        await client.WatchAssignmentsOnceAsync("12", TestContext.Current.CancellationToken);

        Assert.Equal($"?{parameter.GetProperty("name").GetString()}=12", route!.Query);
        Assert.Matches(Schemas.GetProperty("ResumeToken").GetProperty("pattern").GetString()!, "12");
    }

    [Fact]
    public async Task AnAllocationRouteIsNeverRequested()
    {
        // The document keeps allocation off this surface: /runtime-host/v1/self/allocate is refused by
        // the relay itself and /allocations is not mapped at all.
        var requests = new List<string>();
        var handler = new ScriptedHandler((request, _) =>
        {
            requests.Add(request.RequestUri!.AbsolutePath);
            return Task.FromResult(request.RequestUri.AbsolutePath.EndsWith("assignments", StringComparison.Ordinal)
                ? Ok("{\"events\":[],\"resumeToken\":\"\"}")
                : Ok(LeaseBody));
        });
        await using var client = Client(handler);

        foreach (var verb in RelayVerbs)
        {
            await CallAsync(client, verb);
        }

        Assert.Equal(5, requests.Count);
        Assert.DoesNotContain(requests, path => path.Contains("alloc", StringComparison.OrdinalIgnoreCase));
        Assert.False(Root.GetProperty("paths").TryGetProperty("/runtime-host/v1/self/allocate", out _));
    }

    [Fact]
    public async Task TheRelaysAllocateRefusalSurfacesAsATerminalRuntimeHostException()
    {
        var allocate = Relay.GetProperty("outsideTheAllowlist").GetProperty("allocate");
        var status = allocate.GetProperty("status").GetInt32();
        var code = allocate.GetProperty("code").GetString()!;
        var callCount = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            Interlocked.Increment(ref callCount);
            return Task.FromResult(Json(
                (HttpStatusCode)status,
                $"{{\"code\":\"{code}\",\"detail\":\"Allocation is backend-only.\"}}"));
        });
        var delays = new List<TimeSpan>();
        await using var client = PendingClient(handler, TimeSpan.FromSeconds(60), delays);

        var exception = await Assert.ThrowsAsync<YogabaseRuntimeHostException>(
            () => client.ReadyAsync(TestContext.Current.CancellationToken));

        Assert.Equal(403, status);
        Assert.Equal("RUNTIME_HOST_RELAY_ALLOCATION_BACKEND_ONLY", code);
        Assert.Equal((HttpStatusCode)status, exception.StatusCode);
        Assert.Equal(code, exception.Code);
        Assert.False(exception.IsRetryable);
        Assert.Null(exception.RetryAfterSeconds);
        Assert.Equal(1, Volatile.Read(ref callCount));
        Assert.Empty(delays);
    }

    // ----------------------------------------------------------------------------------------
    // Refusal codes
    // ----------------------------------------------------------------------------------------

    [Fact]
    public void TheDocumentsRefusalCodeEnumIsEveryCodeItNames()
    {
        var union = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var section in Relay.GetProperty("refusalCodes").EnumerateObject())
        {
            foreach (var verb in section.Value.EnumerateObject())
            {
                foreach (var code in verb.Value.EnumerateObject())
                {
                    union.Add(code.Name);
                }
            }
        }

        var outside = Relay.GetProperty("outsideTheAllowlist");
        union.Add(outside.GetProperty("allocate").GetProperty("code").GetString()!);
        union.Add(outside.GetProperty("anyOtherVerb").GetProperty("code").GetString()!);
        union.Add(Relay.GetProperty("gate").GetProperty("code").GetString()!);

        Assert.Equal(union, DocumentRefusalCodes());
    }

    [Fact]
    public void TheClientsRefusalCodesAreASubsetOfTheDocuments()
    {
        var documented = DocumentRefusalCodes();
        var named = ClientRefusalCodes();

        // Guard against a vacuous pass: the scan must reach the code the client acts on.
        Assert.Contains(PendingCode, named);
        Assert.Equal(PendingCode, PendingCodeConstant());

        Assert.Empty(named.Except(documented));

        // The three codes the instance-addressed contract owns can never reach a relay call.
        foreach (var neverHere in new[]
        {
            "RUNTIME_HOST_WRONG_POD",
            "RUNTIME_HOST_INSTANCE_NOT_FOUND",
            "RUNTIME_HOST_BINDING_REJECTED",
        })
        {
            Assert.DoesNotContain(neverHere, documented);
            Assert.DoesNotContain(neverHere, named);
        }
    }

    [Fact]
    public void OnlyTheInstancePendingCodeIsRetryableAmongTheDocumentsCodedRefusals()
    {
        var statuses = DocumentRefusalStatuses();
        Assert.Equal(DocumentRefusalCodes(), statuses.Keys.ToHashSet(StringComparer.Ordinal));
        Assert.Contains(PendingCode, Relay.GetProperty("refusals").GetString(), StringComparison.Ordinal);

        foreach (var (code, status) in statuses)
        {
            var exception = new YogabaseRuntimeHostException(
                (HttpStatusCode)status,
                code,
                $"{{\"code\":\"{code}\",\"detail\":\"x\"}}");

            Assert.Equal(code == PendingCode, exception.IsRetryable);
        }
    }

    [Fact]
    public void TheBodylessStatusesAreExactlyThe413And429And504TheClientKnows()
    {
        var bodyless = Relay.GetProperty("bodylessStatuses").EnumerateObject().Select(item => item.Name).ToArray();
        Assert.Equal(["413", "429", "504"], bodyless.OrderBy(name => name, StringComparer.Ordinal));

        Assert.False(Bodyless(413).IsRetryable);
        Assert.True(Bodyless(429).IsRetryable);
        Assert.True(Bodyless(504).IsRetryable);

        static YogabaseRuntimeHostException Bodyless(int status) =>
            new((HttpStatusCode)status, "UNKNOWN", string.Empty);
    }

    public static TheoryData<string, string, int> TerminalRefusalsOfTheWaitedOutCalls()
    {
        // The two calls that wait out RUNTIME_HOST_INSTANCE_PENDING. Every other code the document
        // lists for them, and the relay's own, is terminal for the call that produced it.
        var data = new TheoryData<string, string, int>();
        foreach (var verb in new[] { "heartbeat", "ready" })
        {
            foreach (var section in new[] { "relay", "controlPlane" })
            {
                foreach (var code in Relay.GetProperty("refusalCodes").GetProperty(section).GetProperty(verb).EnumerateObject())
                {
                    if (code.Name != PendingCode)
                    {
                        data.Add(verb, code.Name, code.Value.GetInt32());
                    }
                }
            }
        }

        var gate = Relay.GetProperty("gate");
        data.Add("ready", gate.GetProperty("code").GetString()!, gate.GetProperty("status").GetInt32());
        return data;
    }

    [Theory]
    [MemberData(nameof(TerminalRefusalsOfTheWaitedOutCalls))]
    public async Task EveryTerminalRefusalTheDocumentListsIsThrownAtOnce(string verb, string code, int status)
    {
        var callCount = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            Interlocked.Increment(ref callCount);
            // A retry hint in the body on purpose: only the code decides whether a refusal is waited out.
            return Task.FromResult(Json(
                (HttpStatusCode)status,
                $"{{\"code\":\"{code}\",\"detail\":\"terminal\",\"retryAfterSeconds\":1}}"));
        });
        var delays = new List<TimeSpan>();
        await using var client = PendingClient(handler, TimeSpan.FromSeconds(60), delays);

        var exception = await Assert.ThrowsAsync<YogabaseRuntimeHostException>(
            () => CallAsync(client, verb));

        Assert.Equal(code, exception.Code);
        Assert.Equal((HttpStatusCode)status, exception.StatusCode);
        Assert.False(exception.IsRetryable);
        Assert.Equal(1, Volatile.Read(ref callCount));
        Assert.Empty(delays);
    }

    public static TheoryData<string, int> TerminalRefusalsOfTheAssignmentsRoute()
    {
        // Every code the document lists for the assignments route, from the relay and from the
        // Control Plane, except RUNTIME_HOST_INSTANCE_PENDING, the one retryable code.
        var data = new TheoryData<string, int>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var section in new[] { "relay", "controlPlane" })
        {
            foreach (var code in Relay.GetProperty("refusalCodes").GetProperty(section).GetProperty("assignments").EnumerateObject())
            {
                if (code.Name != PendingCode && seen.Add(code.Name))
                {
                    data.Add(code.Name, code.Value.GetInt32());
                }
            }
        }

        return data;
    }

    [Fact]
    public void TheAssignmentsRouteListsThePendingRefusalAndTheTerminalOnesAreTheRest()
    {
        var codes = Relay.GetProperty("refusalCodes").GetProperty("controlPlane").GetProperty("assignments");

        // Guard against a vacuous theory: the pending refusal is on this route, and so is the
        // resume-token refusal and the stale-epoch one the watch must end on.
        Assert.True(codes.TryGetProperty(PendingCode, out _));
        Assert.True(codes.TryGetProperty("RUNTIME_HOST_STALE_EPOCH", out _));
        Assert.Contains(
            "RUNTIME_HOST_RESUME_TOKEN_INVALID",
            Relay.GetProperty("refusalCodes").GetProperty("relay").GetProperty("assignments").EnumerateObject().Select(code => code.Name));
        Assert.NotEmpty(TerminalRefusalsOfTheAssignmentsRoute());
    }

    [Theory]
    [MemberData(nameof(TerminalRefusalsOfTheAssignmentsRoute))]
    public async Task EveryTerminalRefusalTheDocumentListsForAssignmentsEndsTheWatch(string code, int status)
    {
        var callCount = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            Interlocked.Increment(ref callCount);
            // A retry hint in the body on purpose: only the code decides whether a refusal is terminal.
            return Task.FromResult(Json(
                (HttpStatusCode)status,
                $"{{\"code\":\"{code}\",\"detail\":\"terminal\",\"retryAfterSeconds\":1}}"));
        });
        await using var client = Client(handler);
        var reported = new List<Exception>();
        // A watch that should have ended on its own and does not fails after ten seconds as a
        // cancellation, rather than hanging the run.
        using var guard = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        guard.CancelAfter(TimeSpan.FromSeconds(10));

        var exception = await Assert.ThrowsAsync<YogabaseRuntimeHostException>(async () =>
        {
            await foreach (var _ in client.WatchAssignmentsAsync(
                TimeSpan.FromMilliseconds(5),
                reported.Add,
                guard.Token))
            {
            }
        });

        Assert.Equal(code, exception.Code);
        Assert.Equal((HttpStatusCode)status, exception.StatusCode);
        Assert.False(exception.IsRetryable);
        Assert.Equal(1, Volatile.Read(ref callCount));
        Assert.Empty(reported);
    }

    [Fact]
    public async Task TheInstancePendingRefusalDoesNotEndTheWatchAndItIsNotReported()
    {
        var status = Relay.GetProperty("refusalCodes").GetProperty("controlPlane").GetProperty("assignments")
            .GetProperty(PendingCode).GetInt32();
        var callCount = 0;
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        stop.CancelAfter(TimeSpan.FromSeconds(10));
        var handler = new ScriptedHandler((_, _) =>
        {
            var count = Interlocked.Increment(ref callCount);
            return Task.FromResult(count < 3
                ? Json((HttpStatusCode)status, $"{{\"code\":\"{PendingCode}\",\"detail\":\"pending\",\"retryAfterSeconds\":2}}")
                : Ok("{\"events\":[{\"sequence\":1,\"kind\":\"registered\",\"state\":\"Starting\",\"allocationId\":null,\"occurredAt\":\"2026-10-20T00:00:00+00:00\"}],\"resumeToken\":\"1\"}"));
        });
        await using var client = Client(handler);
        var reported = new List<Exception>();
        var kinds = new List<string>();

        await foreach (var assignmentEvent in client.WatchAssignmentsAsync(
            TimeSpan.FromMilliseconds(5),
            reported.Add,
            stop.Token))
        {
            kinds.Add(assignmentEvent.Kind);
            stop.Cancel();
        }

        Assert.Equal(["registered"], kinds);
        Assert.Equal(3, Volatile.Read(ref callCount));
        Assert.Empty(reported);
    }

    [Fact]
    public async Task TheClosedGateAnswerIsReportedByTheWatchRatherThanEndingIt()
    {
        // RUNTIME_HOST_UNAVAILABLE is not listed for the assignments route (this build's gate is
        // open and the document says it never answers), so the watch does not end on it: it reports
        // it once and keeps polling.
        var gate = Relay.GetProperty("gate");
        var code = gate.GetProperty("code").GetString()!;
        var status = gate.GetProperty("status").GetInt32();
        Assert.DoesNotContain(
            code,
            Relay.GetProperty("refusalCodes").GetProperty("controlPlane").GetProperty("assignments").EnumerateObject().Select(item => item.Name));
        var callCount = 0;
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        stop.CancelAfter(TimeSpan.FromSeconds(10));
        var handler = new ScriptedHandler((_, _) =>
        {
            var count = Interlocked.Increment(ref callCount);
            return Task.FromResult(count < 3
                ? Json((HttpStatusCode)status, $"{{\"code\":\"{code}\",\"detail\":\"closed\"}}")
                : Ok("{\"events\":[{\"sequence\":1,\"kind\":\"registered\",\"state\":\"Starting\",\"allocationId\":null,\"occurredAt\":\"2026-10-20T00:00:00+00:00\"}],\"resumeToken\":\"1\"}"));
        });
        await using var client = Client(handler);
        var reported = new List<Exception>();
        var events = 0;

        await foreach (var _ in client.WatchAssignmentsAsync(TimeSpan.FromMilliseconds(5), reported.Add, stop.Token))
        {
            events++;
            stop.Cancel();
        }

        Assert.Equal(1, events);
        Assert.Equal(3, Volatile.Read(ref callCount));
        var refusal = Assert.IsType<YogabaseRuntimeHostException>(Assert.Single(reported));
        Assert.Equal(code, refusal.Code);
        Assert.Equal((HttpStatusCode)status, refusal.StatusCode);
    }

    // ----------------------------------------------------------------------------------------
    // Limits and request shapes
    // ----------------------------------------------------------------------------------------

    [Fact]
    public async Task TheClientsLimitsAreTheRelaysLimits()
    {
        var limits = Relay.GetProperty("limits");
        var minimum = limits.GetProperty("minimumReservationSeconds").GetInt32();
        var maximum = limits.GetProperty("maximumReservationSeconds").GetInt32();
        var reserve = Schemas.GetProperty("ReserveRequest").GetProperty("properties").GetProperty("seconds");
        Assert.Equal(minimum, reserve.GetProperty("minimum").GetInt32());
        Assert.Equal(maximum, reserve.GetProperty("maximum").GetInt32());

        // The page size, and the page's own maxItems.
        var pageSize = limits.GetProperty("maximumEventsPerPage").GetInt32();
        Assert.Equal(RuntimeHostAssignmentPage.MaximumEventsPerPage, pageSize);
        Assert.Equal(
            pageSize,
            Schemas.GetProperty("AssignmentPage").GetProperty("properties").GetProperty("events").GetProperty("maxItems").GetInt32());

        // The reservation window is clamped to the document's bounds, never refused.
        var bodies = new List<string>();
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            bodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return Ok(LeaseBody);
        });
        await using var client = Client(handler);
        await client.ReserveAsync(TimeSpan.Zero, TestContext.Current.CancellationToken);
        await client.ReserveAsync(TimeSpan.FromDays(1), TestContext.Current.CancellationToken);
        await client.ReserveAsync(TimeSpan.FromSeconds(minimum + 1), TestContext.Current.CancellationToken);

        Assert.Equal(minimum, ReadInt(bodies[0], "seconds"));
        Assert.Equal(maximum, ReadInt(bodies[1], "seconds"));
        Assert.Equal(minimum + 1, ReadInt(bodies[2], "seconds"));

        // Nothing the client sends comes near the body limit.
        var bodyLimit = limits.GetProperty("maximumRequestBodyBytes").GetInt32();
        Assert.All(bodies, body => Assert.True(Encoding.UTF8.GetByteCount(body) < bodyLimit));
    }

    [Fact]
    public void TheDefaultRequestTimeoutOutlastsTheRelaysAnswerTimeout()
    {
        var answerTimeout = Relay.GetProperty("limits").GetProperty("answerTimeoutSeconds").GetInt32();
        var defaultTimeout = new YogabaseRuntimeHostOptions().RequestTimeout;

        // The figure the client carries is the document's, so a re-mirror that moves it fails here.
        Assert.Equal(YogabaseRuntimeHostOptions.RelayAnswerTimeoutSeconds, answerTimeout);
        Assert.True(YogabaseRuntimeHostOptions.RequestTimeoutMarginSeconds > 0);

        // A client that waits for the whole answer timeout still has time to receive the relay's own
        // bodyless 504 instead of cancelling locally just before it.
        Assert.Equal(
            TimeSpan.FromSeconds(answerTimeout + YogabaseRuntimeHostOptions.RequestTimeoutMarginSeconds),
            defaultTimeout);
        Assert.True(defaultTimeout > TimeSpan.FromSeconds(answerTimeout));
        Assert.Contains("answerTimeoutSeconds", Relay.GetProperty("bodylessStatuses").GetProperty("504").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ThePendingWaitDefaultsToTheRelaysRetryHint()
    {
        var hint = Relay.GetProperty("limits").GetProperty("instancePendingRetryAfterSeconds").GetInt32();
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Json(
            HttpStatusCode.ServiceUnavailable,
            $"{{\"code\":\"{PendingCode}\",\"detail\":\"pending\"}}")));
        var delays = new List<TimeSpan>();
        await using var client = PendingClient(handler, TimeSpan.FromSeconds(hint), delays);

        // One wait of the default fits the budget; the second would not.
        var exception = await Assert.ThrowsAsync<YogabaseRuntimeHostException>(
            () => client.ReadyAsync(TestContext.Current.CancellationToken));

        Assert.Equal(PendingCode, exception.Code);
        Assert.Equal([TimeSpan.FromSeconds(hint)], delays);

        // The refusal's own hint is a positive integer, which is what the client accepts as a hint.
        Assert.Equal(
            1,
            Schemas.GetProperty("RelayRefusal").GetProperty("properties").GetProperty("retryAfterSeconds").GetProperty("minimum").GetInt32());
    }

    [Fact]
    public void TheDefaultHeartbeatIntervalRenewsTheLeaseWellInsideIt()
    {
        var lease = TimeSpan.FromSeconds(Relay.GetProperty("limits").GetProperty("leaseSeconds").GetInt32());

        Assert.True(new YogabaseRuntimeHostOptions().HeartbeatInterval * 2 <= lease);
    }

    [Fact]
    public async Task EveryRequestBodyFitsTheDocumentsRequestSchema()
    {
        var bodies = new Dictionary<string, string>(StringComparer.Ordinal);
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            bodies[request.RequestUri!.AbsolutePath.Split('/')[^1]] = await request.Content!.ReadAsStringAsync(cancellationToken);
            return Ok(LeaseBody);
        });
        await using var client = Client(handler);

        await client.HeartbeatOnceAsync(TestContext.Current.CancellationToken);
        await client.ReserveAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
        await client.ReadyAsync(TestContext.Current.CancellationToken);
        await client.ShutdownAsync(TestContext.Current.CancellationToken);

        AssertFitsSchema(bodies["heartbeat"], Schemas.GetProperty("HeartbeatRequest"));
        AssertFitsSchema(bodies["reserve"], Schemas.GetProperty("ReserveRequest"));

        // ready and shutdown read no body at all, and the document declares none.
        foreach (var verb in new[] { "ready", "shutdown" })
        {
            var operation = Root.GetProperty("paths").GetProperty($"/runtime-host/v1/self/{verb}").GetProperty("post");
            Assert.False(operation.TryGetProperty("requestBody", out _));
            Assert.Equal("{}", bodies[verb]);
        }
    }

    private static void AssertFitsSchema(string body, JsonElement schema)
    {
        using var document = JsonDocument.Parse(body);
        var properties = schema.GetProperty("properties");
        var members = document.RootElement.EnumerateObject().Select(member => member.Name).ToHashSet(StringComparer.Ordinal);

        Assert.All(members, member => Assert.True(properties.TryGetProperty(member, out _), $"{member} is not declared."));
        foreach (var required in schema.GetProperty("required").EnumerateArray())
        {
            Assert.Contains(required.GetString()!, members);
        }

        foreach (var member in document.RootElement.EnumerateObject())
        {
            var declared = properties.GetProperty(member.Name);
            Assert.Equal(JsonValueKind.Number, member.Value.ValueKind);
            Assert.True(member.Value.GetInt64() >= declared.GetProperty("minimum").GetInt64());
            if (declared.TryGetProperty("maximum", out var maximum))
            {
                Assert.True(member.Value.GetInt64() <= maximum.GetInt64());
            }
        }
    }

    // ----------------------------------------------------------------------------------------
    // Answers
    // ----------------------------------------------------------------------------------------

    [Fact]
    public async Task TheClientReadsALeaseBuiltFromEveryMemberTheDocumentDeclares()
    {
        var lease = Sample(Schemas.GetProperty("LeaseResponse"), includeOptional: true);
        var requiredOnly = Sample(Schemas.GetProperty("LeaseResponse"), includeOptional: false);
        Assert.Equal(
            Schemas.GetProperty("LeaseResponse").GetProperty("required").GetArrayLength(),
            requiredOnly.AsObject().Count);
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Ok(lease.ToJsonString())));
        await using var client = Client(handler);

        var read = await client.ReadyAsync(TestContext.Current.CancellationToken);

        Assert.Equal(lease["state"]!.GetValue<string>(), read.State);
        Assert.Equal(lease["heartbeatSequence"]!.GetValue<long>(), read.HeartbeatSequence);
        Assert.Equal(DateTimeOffset.Parse(lease["leaseExpiresAt"]!.GetValue<string>(), CultureInfo.InvariantCulture), read.LeaseExpiresAt);
        Assert.Equal(lease["eventSequence"]!.GetValue<long>(), read.EventSequence);
        Assert.Contains(read.State, DocumentStates());
    }

    [Fact]
    public async Task TheClientReadsAnAssignmentPageBuiltFromEveryMemberTheDocumentDeclares()
    {
        var page = Sample(Schemas.GetProperty("AssignmentPage"), includeOptional: true);
        Assert.Single(page["events"]!.AsArray());
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Ok(page.ToJsonString())));
        await using var client = Client(handler);

        var read = await client.WatchAssignmentsOnceAsync(null, TestContext.Current.CancellationToken);

        var sample = page["events"]![0]!;
        var assignmentEvent = Assert.Single(read.Events);
        Assert.Equal(sample["sequence"]!.GetValue<long>(), assignmentEvent.Sequence);
        Assert.Equal(sample["kind"]!.GetValue<string>(), assignmentEvent.Kind);
        Assert.Equal(sample["state"]!.GetValue<string>(), assignmentEvent.State);
        Assert.Equal(sample["allocationId"]!.GetValue<string>(), assignmentEvent.AllocationId);
        Assert.Equal(page["resumeToken"]!.GetValue<string>(), read.ResumeToken);
        Assert.Equal(page["headState"]!.GetValue<string>(), read.HeadState);
        Assert.Equal(page["headSequence"]!.GetValue<long>(), read.HeadSequence);
        Assert.True(read.HasHead);
    }

    [Fact]
    public async Task APageWithOnlyTheRequiredMembersHasNoHead()
    {
        var page = Sample(Schemas.GetProperty("AssignmentPage"), includeOptional: false);
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Ok(page.ToJsonString())));
        await using var client = Client(handler);

        var read = await client.WatchAssignmentsOnceAsync(null, TestContext.Current.CancellationToken);

        Assert.False(read.HasHead);
        Assert.Null(read.HeadState);
        Assert.Null(read.HeadSequence);
    }

    [Fact]
    public void TheStatesTheClientTreatsAsReadyAreStatesTheDocumentDeclares()
    {
        var states = DocumentStates();
        Assert.Equal(9, states.Count);

        foreach (var state in states)
        {
            Assert.Equal(
                state is "Ready" or "Reserved" or "Allocated",
                new RuntimeHostCurrentState(state, 1, ReadFromHead: true).IsReady);
        }
    }

    // ----------------------------------------------------------------------------------------
    // The Unity copy
    // ----------------------------------------------------------------------------------------

    [Theory]
    [InlineData("Compatibility.cs")]
    [InlineData("RuntimeHostModels.cs")]
    [InlineData("YogabaseRuntimeHostClient.cs")]
    [InlineData("YogabaseRuntimeHostOptions.cs")]
    public void TheUnityRuntimeFolderIsTheDotNetSourceWithABlockScopedNamespace(string fileName)
    {
        var dotnet = File.ReadAllLines(Path.Combine(RepoRoot.Value, "src", "dotnet", "Yogabase.Yogabase.RuntimeHost", fileName));
        var unity = File.ReadAllLines(Path.Combine(RepoRoot.Value, "languages", "unity", "io.yogabase.runtime-host", "Runtime", fileName)).ToList();

        // Unity compiles at C# 9, which has no file-scoped namespace: the copy opens a block after
        // the namespace line and closes it after the last type. Undo exactly that.
        var namespaceLine = unity.FindIndex(line => line.StartsWith("namespace ", StringComparison.Ordinal));
        Assert.True(namespaceLine >= 0 && unity[namespaceLine + 1] == "{", "The Unity copy must open a block namespace.");
        unity[namespaceLine] += ";";
        unity.RemoveAt(namespaceLine + 1);
        var closing = -1;
        for (var index = unity.Count - 1; index > 0 && closing < 0; index--)
        {
            if (unity[index] == "}" && unity[index - 1] == string.Empty)
            {
                closing = index;
            }
        }

        Assert.True(closing > 0, "The Unity copy must close its namespace block.");
        unity.RemoveRange(closing - 1, 2);

        Assert.Equal(dotnet.Select(line => line.TrimEnd()), unity.Select(line => line.TrimEnd()));
    }

    // ----------------------------------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------------------------------

    private const string LeaseBody =
        "{\"state\":\"Healthy\",\"heartbeatSequence\":1,\"leaseExpiresAt\":\"2026-09-04T00:00:00+00:00\",\"eventSequence\":7}";

    private static HashSet<string> DocumentRefusalCodes() =>
        Schemas.GetProperty("RelayRefusalCode").GetProperty("enum").EnumerateArray()
            .Select(code => code.GetString()!)
            .ToHashSet(StringComparer.Ordinal);

    private static HashSet<string> DocumentStates() =>
        Schemas.GetProperty("RuntimeHostState").GetProperty("enum").EnumerateArray()
            .Select(state => state.GetString()!)
            .ToHashSet(StringComparer.Ordinal);

    // Each code's status as the document states it (every code is answered under one status).
    private static Dictionary<string, int> DocumentRefusalStatuses()
    {
        var statuses = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var section in Relay.GetProperty("refusalCodes").EnumerateObject())
        {
            foreach (var verb in section.Value.EnumerateObject())
            {
                foreach (var code in verb.Value.EnumerateObject())
                {
                    if (statuses.TryGetValue(code.Name, out var existing))
                    {
                        Assert.Equal(existing, code.Value.GetInt32());
                    }

                    statuses[code.Name] = code.Value.GetInt32();
                }
            }
        }

        var outside = Relay.GetProperty("outsideTheAllowlist");
        foreach (var name in new[] { "allocate", "anyOtherVerb" })
        {
            var entry = outside.GetProperty(name);
            statuses[entry.GetProperty("code").GetString()!] = entry.GetProperty("status").GetInt32();
        }

        var gate = Relay.GetProperty("gate");
        statuses[gate.GetProperty("code").GetString()!] = gate.GetProperty("status").GetInt32();
        return statuses;
    }

    // Every RUNTIME_HOST_* name that appears in the runtime-host sources of both copies, comments
    // included: a code the client documents is a code it claims the relay answers with.
    private static HashSet<string> ClientRefusalCodes()
    {
        var directories = new[]
        {
            Path.Combine(RepoRoot.Value, "src", "dotnet", "Yogabase.Yogabase.RuntimeHost"),
            Path.Combine(RepoRoot.Value, "languages", "unity", "io.yogabase.runtime-host", "Runtime"),
        };
        var codes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var directory in directories)
        {
            var files = Directory.GetFiles(directory, "*.cs", SearchOption.TopDirectoryOnly);
            Assert.NotEmpty(files);
            foreach (var file in files)
            {
                // The lookbehind keeps YOGABASE_RUNTIME_HOST_ENDPOINT, an environment variable, out.
                foreach (Match match in Regex.Matches(File.ReadAllText(file), "(?<![A-Z_])RUNTIME_HOST_[A-Z_]+"))
                {
                    codes.Add(match.Value);
                }
            }
        }

        return codes;
    }

    private static string PendingCodeConstant()
    {
        var field = typeof(YogabaseRuntimeHostException).GetField(
            "InstancePendingCode",
            BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
        return (string)field!.GetRawConstantValue()!;
    }

    // A value of the shape a schema describes, with the members it requires (and, when asked, all
    // the ones it declares): enough to read an answer the document says the relay can send.
    private static JsonNode Sample(JsonElement schema, bool includeOptional)
    {
        if (schema.TryGetProperty("$ref", out var reference))
        {
            return Sample(Schemas.GetProperty(reference.GetString()!.Split('/')[^1]), includeOptional);
        }

        if (schema.TryGetProperty("enum", out var values))
        {
            return JsonValue.Create(values[0].GetString())!;
        }

        var type = schema.GetProperty("type");
        var kind = type.ValueKind == JsonValueKind.Array
            ? type.EnumerateArray().Select(item => item.GetString()!).First(name => name != "null")
            : type.GetString()!;
        switch (kind)
        {
            case "integer":
                return JsonValue.Create(schema.TryGetProperty("minimum", out var minimum) ? minimum.GetInt64() : 1L)!;
            case "string":
                var format = schema.TryGetProperty("format", out var formatValue) ? formatValue.GetString() : null;
                return format switch
                {
                    "date-time" => JsonValue.Create("2026-10-20T00:00:00+00:00")!,
                    "uuid" => JsonValue.Create("3f2504e0-4f89-41d3-9a0c-0305e82c3301")!,
                    _ => JsonValue.Create(schema.TryGetProperty("pattern", out _) ? "1" : "sample")!,
                };
            case "array":
                return new JsonArray(Sample(schema.GetProperty("items"), includeOptional));
            case "object":
                var required = schema.TryGetProperty("required", out var requiredNames)
                    ? requiredNames.EnumerateArray().Select(item => item.GetString()!).ToHashSet(StringComparer.Ordinal)
                    : [];
                var result = new JsonObject();
                foreach (var property in schema.GetProperty("properties").EnumerateObject())
                {
                    if (includeOptional || required.Contains(property.Name))
                    {
                        result[property.Name] = Sample(property.Value, includeOptional);
                    }
                }

                return result;
            default:
                throw new InvalidOperationException($"No sample for schema type {kind}.");
        }
    }

    private static async Task CallAsync(YogabaseRuntimeHostClient client, string verb)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        switch (verb)
        {
            case "heartbeat":
                await client.HeartbeatOnceAsync(cancellationToken);
                break;
            case "ready":
                await client.ReadyAsync(cancellationToken);
                break;
            case "reserve":
                await client.ReserveAsync(TimeSpan.FromSeconds(30), cancellationToken);
                break;
            case "shutdown":
                await client.ShutdownAsync(cancellationToken);
                break;
            case "assignments":
                await client.WatchAssignmentsOnceAsync(null, cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(verb), verb, "Not a relay verb.");
        }
    }

    private static YogabaseRuntimeHostClient Client(HttpMessageHandler handler) =>
        new(
            new YogabaseRuntimeHostOptions
            {
                Endpoint = new Uri("http://127.0.0.1:8090"),
                HeartbeatInterval = TimeSpan.FromSeconds(2),
                RequestTimeout = TimeSpan.FromSeconds(10),
            },
            handler);

    private static YogabaseRuntimeHostClient PendingClient(
        HttpMessageHandler handler,
        TimeSpan startupPendingBudget,
        List<TimeSpan> recordedDelays) =>
        new(
            new YogabaseRuntimeHostOptions
            {
                Endpoint = new Uri("http://127.0.0.1:8090"),
                StartupPendingBudget = startupPendingBudget,
                StartupPendingDelay = (delay, _) =>
                {
                    recordedDelays.Add(delay);
                    return Task.CompletedTask;
                },
            },
            handler);

    private static int ReadInt(string body, string property)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty(property).GetInt32();
    }

    private static HttpResponseMessage Ok(string json) => Json(HttpStatusCode.OK, json);

    private static HttpResponseMessage Json(HttpStatusCode status, string json) => new(status)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate {SolutionFileName} by walking up from {AppContext.BaseDirectory}.");
    }

    private sealed class ScriptedHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
