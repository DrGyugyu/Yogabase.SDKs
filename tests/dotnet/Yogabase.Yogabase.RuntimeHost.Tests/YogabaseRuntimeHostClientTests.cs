using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Yogabase.Yogabase.RuntimeHost;

namespace Yogabase.RuntimeHost.Tests;

public sealed class YogabaseRuntimeHostClientTests
{
    private const string LeaseBody =
        "{\"state\":\"Healthy\",\"heartbeatSequence\":1,\"leaseExpiresAt\":\"2026-09-04T00:00:00+00:00\",\"eventSequence\":7}";

    private const string AssignmentsBody =
        "{\"events\":[{\"sequence\":1,\"kind\":\"allocated\",\"state\":\"Allocated\",\"allocationId\":\"alloc-1\",\"occurredAt\":\"2026-09-04T00:00:00+00:00\"}],\"resumeToken\":\"1\"}";

    private const string ErrorBody =
        "{\"code\":\"RUNTIME_HOST_RELAY_ALLOCATION_BACKEND_ONLY\",\"detail\":\"Allocation state is backend-only.\"}";

    [Fact]
    public async Task EachVerbHitsItsExactRouteAndHttpMethod()
    {
        var requests = new List<CapturedRequest>();
        var handler = new ScriptedHandler((request, _) =>
        {
            requests.Add(new CapturedRequest(request.Method, request.RequestUri!));
            return Task.FromResult(request.RequestUri!.AbsolutePath.EndsWith("assignments", StringComparison.Ordinal)
                ? Json(HttpStatusCode.OK, AssignmentsBody)
                : Json(HttpStatusCode.OK, LeaseBody));
        });
        await using var client = Client(handler);

        var heartbeat = await client.HeartbeatOnceAsync(TestContext.Current.CancellationToken);
        await client.ReadyAsync(TestContext.Current.CancellationToken);
        await client.ReserveAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
        await client.ShutdownAsync(TestContext.Current.CancellationToken);
        await client.WatchAssignmentsOnceAsync(null, TestContext.Current.CancellationToken);

        Assert.Equal(5, requests.Count);
        Assert.Equal(HttpMethod.Post, requests[0].Method);
        Assert.Equal("http://127.0.0.1:8090/runtime-host/v1/self/heartbeat", requests[0].Uri.AbsoluteUri);
        Assert.Equal(HttpMethod.Post, requests[1].Method);
        Assert.Equal("http://127.0.0.1:8090/runtime-host/v1/self/ready", requests[1].Uri.AbsoluteUri);
        Assert.Equal(HttpMethod.Post, requests[2].Method);
        Assert.Equal("http://127.0.0.1:8090/runtime-host/v1/self/reserve", requests[2].Uri.AbsoluteUri);
        Assert.Equal(HttpMethod.Post, requests[3].Method);
        Assert.Equal("http://127.0.0.1:8090/runtime-host/v1/self/shutdown", requests[3].Uri.AbsoluteUri);
        Assert.Equal(HttpMethod.Get, requests[4].Method);
        Assert.Equal("http://127.0.0.1:8090/runtime-host/v1/self/assignments", requests[4].Uri.AbsoluteUri);

        Assert.Equal("Healthy", heartbeat.State);
        Assert.Equal(1, heartbeat.HeartbeatSequence);
        Assert.Equal(
            DateTimeOffset.Parse("2026-09-04T00:00:00+00:00", CultureInfo.InvariantCulture),
            heartbeat.LeaseExpiresAt);
        Assert.Equal(7, heartbeat.EventSequence);
    }

    [Fact]
    public async Task HeartbeatSequenceIncrementsMonotonically()
    {
        var bodies = new List<string>();
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            bodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return Json(HttpStatusCode.OK, LeaseBody);
        });
        await using var client = Client(handler);

        await client.HeartbeatOnceAsync(TestContext.Current.CancellationToken);
        await client.HeartbeatOnceAsync(TestContext.Current.CancellationToken);
        await client.HeartbeatOnceAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, bodies.Count);
        Assert.Equal(1L, ReadLong(bodies[0], "sequence"));
        Assert.Equal(2L, ReadLong(bodies[1], "sequence"));
        Assert.Equal(3L, ReadLong(bodies[2], "sequence"));
    }

    [Fact]
    public async Task HeartbeatLoopSurvivesATransientFailureAndKeepsGoing()
    {
        var callCount = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            var count = Interlocked.Increment(ref callCount);
            return Task.FromResult(count == 1
                ? Json(HttpStatusCode.InternalServerError, "{\"code\":\"INTERNAL\",\"detail\":\"boom\"}")
                : Json(HttpStatusCode.OK, LeaseBody));
        });
        await using var client = Client(handler, TimeSpan.FromMilliseconds(20));

        client.Connect();
        await WaitUntilAsync(
            () => Volatile.Read(ref callCount) >= 3,
            TestContext.Current.CancellationToken);

        Assert.True(Volatile.Read(ref callCount) >= 3);
    }

    [Fact]
    public async Task DisposeAsyncCancelsTheHeartbeatLoopAndIsIdempotent()
    {
        var callCount = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            Interlocked.Increment(ref callCount);
            return Task.FromResult(Json(HttpStatusCode.OK, LeaseBody));
        });
        var client = Client(handler, TimeSpan.FromMilliseconds(20));

        client.Connect();
        await WaitUntilAsync(
            () => Volatile.Read(ref callCount) >= 2,
            TestContext.Current.CancellationToken);

        var firstDispose = await Record.ExceptionAsync(() => client.DisposeAsync().AsTask());
        Assert.Null(firstDispose);

        var countAfterDispose = Volatile.Read(ref callCount);
        await Task.Delay(TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken);
        Assert.Equal(countAfterDispose, Volatile.Read(ref callCount));

        var secondDispose = await Record.ExceptionAsync(() => client.DisposeAsync().AsTask());
        Assert.Null(secondDispose);
    }

    [Theory]
    [InlineData(1d, 1)]
    [InlineData(7200000d, 600)]
    [InlineData(30000d, 30)]
    public async Task ReserveClampsTheRequestedWindowInSeconds(double windowMilliseconds, int expectedSeconds)
    {
        string? body = null;
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return Json(HttpStatusCode.OK, LeaseBody);
        });
        await using var client = Client(handler);

        await client.ReserveAsync(TimeSpan.FromMilliseconds(windowMilliseconds), TestContext.Current.CancellationToken);

        Assert.Equal(expectedSeconds, ReadInt(body!, "seconds"));
    }

    [Fact]
    public async Task WatchAssignmentsOnceWithNoResumeTokenSendsNoQueryString()
    {
        Uri? route = null;
        var handler = new ScriptedHandler((request, _) =>
        {
            route = request.RequestUri;
            return Task.FromResult(Json(HttpStatusCode.OK, AssignmentsBody));
        });
        await using var client = Client(handler);

        await client.WatchAssignmentsOnceAsync(null, TestContext.Current.CancellationToken);

        Assert.Equal(string.Empty, route!.Query);
        Assert.Equal("http://127.0.0.1:8090/runtime-host/v1/self/assignments", route.AbsoluteUri);
    }

    [Fact]
    public async Task WatchAssignmentsOnceEscapesTheResumeTokenInTheQuery()
    {
        Uri? route = null;
        var handler = new ScriptedHandler((request, _) =>
        {
            route = request.RequestUri;
            return Task.FromResult(Json(HttpStatusCode.OK, AssignmentsBody));
        });
        await using var client = Client(handler);

        await client.WatchAssignmentsOnceAsync("a b&c", TestContext.Current.CancellationToken);

        Assert.Equal("?resumeToken=a%20b%26c", route!.Query);
    }

    [Fact]
    public async Task WatchAssignmentsAsyncKeepsThePreviousResumeTokenWhenAPageReturnsNoNewToken()
    {
        var queries = new List<string>();
        using var stopAfterThreeRequests = new CancellationTokenSource();
        var handler = new ScriptedHandler((request, _) =>
        {
            queries.Add(request.RequestUri!.Query);
            var resumeToken = queries.Count == 1 ? "5" : string.Empty;
            if (queries.Count >= 3)
            {
                stopAfterThreeRequests.Cancel();
            }
            return Task.FromResult(Json(HttpStatusCode.OK, AssignmentsBodyWithToken(resumeToken)));
        });
        await using var client = Client(handler);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            stopAfterThreeRequests.Token,
            TestContext.Current.CancellationToken);

        try
        {
            await foreach (var _ in client.WatchAssignmentsAsync(TimeSpan.FromMilliseconds(5), linked.Token))
            {
            }
        }
        catch (OperationCanceledException)
        {
        }

        Assert.True(queries.Count >= 3);
        Assert.Equal(string.Empty, queries[0]);
        Assert.Equal("?resumeToken=5", queries[1]);
        Assert.Equal("?resumeToken=5", queries[2]);
    }

    [Fact]
    public async Task APageCarriesTheHeadStateAndSequenceWhenTheControlPlaneStatesThem()
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Json(
            HttpStatusCode.OK,
            "{\"events\":[],\"resumeToken\":\"4\",\"headState\":\"Allocated\",\"headSequence\":9}")));
        await using var client = Client(handler);

        var page = await client.WatchAssignmentsOnceAsync("4", TestContext.Current.CancellationToken);

        Assert.Equal("Allocated", page.HeadState);
        Assert.Equal(9, page.HeadSequence);
        Assert.True(page.HasHead);
        Assert.Empty(page.Events);
        Assert.Equal("4", page.ResumeToken);
    }

    [Fact]
    public async Task APageFromAControlPlaneThatPredatesTheHeadCarriesNone()
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, AssignmentsBody)));
        await using var client = Client(handler);

        var page = await client.WatchAssignmentsOnceAsync(null, TestContext.Current.CancellationToken);

        Assert.Null(page.HeadState);
        Assert.Null(page.HeadSequence);
        Assert.False(page.HasHead);
        Assert.Single(page.Events);
    }

    [Theory]
    [InlineData("{\"events\":[],\"resumeToken\":\"\",\"headState\":7,\"headSequence\":\"x\"}")]
    [InlineData("{\"events\":[],\"resumeToken\":\"\",\"headState\":null,\"headSequence\":null}")]
    [InlineData("{\"events\":[],\"resumeToken\":\"\",\"headState\":\"\",\"headSequence\":-1}")]
    [InlineData("{\"events\":[],\"resumeToken\":\"\",\"headState\":\"\",\"headSequence\":0}")]
    public async Task AMalformedHeadIsNoHeadRatherThanAFailure(string body)
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, body)));
        await using var client = Client(handler);

        var page = await client.WatchAssignmentsOnceAsync(null, TestContext.Current.CancellationToken);

        Assert.False(page.HasHead);
        Assert.Null(page.HeadState);
        Assert.Null(page.HeadSequence);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(long.MinValue)]
    public async Task AHeadSequenceBelowOneIsReadAsNoSequence(long headSequence)
    {
        // The contract makes headSequence at least 1; 0 and below are outside it, so they read as
        // an absent sequence does: the state the page states is kept, the sequence is not.
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Json(
            HttpStatusCode.OK,
            string.Create(
                CultureInfo.InvariantCulture,
                $"{{\"events\":[],\"resumeToken\":\"3\",\"headState\":\"Ready\",\"headSequence\":{headSequence}}}"))));
        await using var client = Client(handler);

        var page = await client.WatchAssignmentsOnceAsync("3", TestContext.Current.CancellationToken);
        var current = await client.ReadCurrentStateAsync(TestContext.Current.CancellationToken);

        Assert.Null(page.HeadSequence);
        Assert.Equal("Ready", page.HeadState);
        Assert.True(page.HasHead);
        Assert.Null(current.Sequence);
        Assert.Equal("Ready", current.State);
        Assert.True(current.ReadFromHead);
    }

    [Fact]
    public async Task AHeadSequenceOfOneIsTheSmallestTheContractAllows()
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Json(
            HttpStatusCode.OK,
            "{\"events\":[],\"resumeToken\":\"1\",\"headState\":\"Healthy\",\"headSequence\":1}")));
        await using var client = Client(handler);

        var page = await client.WatchAssignmentsOnceAsync("1", TestContext.Current.CancellationToken);

        Assert.Equal(1L, page.HeadSequence);
        Assert.Equal("Healthy", page.HeadState);
    }

    [Fact]
    public async Task CurrentStateIsOneReadWhenThePageStatesTheHead()
    {
        var reads = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            reads++;
            // The head can be newer than the newest event: a lapsed lease reads Unknown although no
            // event records the lapse.
            return Task.FromResult(Json(
                HttpStatusCode.OK,
                "{\"events\":[{\"sequence\":1,\"kind\":\"registered\",\"state\":\"Ready\",\"allocationId\":null,\"occurredAt\":\"2026-09-04T00:00:00+00:00\"}],"
                + "\"resumeToken\":\"1\",\"headState\":\"Unknown\",\"headSequence\":1}"));
        });
        await using var client = Client(handler);

        var current = await client.ReadCurrentStateAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, reads);
        Assert.Equal("Unknown", current.State);
        Assert.Equal(1, current.Sequence);
        Assert.True(current.ReadFromHead);
        Assert.False(current.IsReady);
    }

    [Fact]
    public async Task CurrentStateFallsBackToPagingToTheHeadWhenThePageStatesNone()
    {
        var queries = new List<string>();
        var handler = new ScriptedHandler((request, _) =>
        {
            queries.Add(request.RequestUri!.Query);
            // A full page (100 events) is not the head; the short page after it is.
            var body = queries.Count == 1
                ? PageOf(Enumerable.Range(1, 100).Select(sequence => (sequence, "Healthy")), "100")
                : PageOf([(101, "Ready")], "101");
            return Task.FromResult(Json(HttpStatusCode.OK, body));
        });
        await using var client = Client(handler);

        var current = await client.ReadCurrentStateAsync(TestContext.Current.CancellationToken);

        Assert.Equal([string.Empty, "?resumeToken=100"], queries);
        Assert.Equal("Ready", current.State);
        Assert.Equal(101, current.Sequence);
        Assert.False(current.ReadFromHead);
        Assert.True(current.IsReady);
    }

    [Fact]
    public async Task CurrentStateOfAStreamWithNoEventsAndNoHeadIsUnknownToTheCaller()
    {
        var handler = new ScriptedHandler((_, _) =>
            Task.FromResult(Json(HttpStatusCode.OK, "{\"events\":[],\"resumeToken\":\"\"}")));
        await using var client = Client(handler);

        var current = await client.ReadCurrentStateAsync(TestContext.Current.CancellationToken);

        Assert.Null(current.State);
        Assert.Null(current.Sequence);
        Assert.False(current.IsReady);
    }

    [Theory]
    [InlineData("Ready", true)]
    [InlineData("Reserved", true)]
    [InlineData("Allocated", true)]
    [InlineData("Healthy", false)]
    [InlineData("Starting", false)]
    [InlineData("Draining", false)]
    [InlineData("Unknown", false)]
    [InlineData("SomethingNew", false)]
    public void AStateNameTheClientDoesNotKnowIsNotReady(string state, bool ready) =>
        Assert.Equal(ready, new RuntimeHostCurrentState(state, 1, ReadFromHead: true).IsReady);

    [Fact]
    public async Task ErrorResponseSurfacesAsYogabaseRuntimeHostException()
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Json(HttpStatusCode.Forbidden, ErrorBody)));
        await using var client = Client(handler);

        var exception = await Assert.ThrowsAsync<YogabaseRuntimeHostException>(
            () => client.ReadyAsync(TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
        Assert.Equal("RUNTIME_HOST_RELAY_ALLOCATION_BACKEND_ONLY", exception.Code);
        Assert.Contains(
            "RUNTIME_HOST_RELAY_ALLOCATION_BACKEND_ONLY",
            exception.BodyJson,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RefusalWithoutRetryAfterLeavesItNull()
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Json(HttpStatusCode.Forbidden, ErrorBody)));
        await using var client = Client(handler);

        var exception = await Assert.ThrowsAsync<YogabaseRuntimeHostException>(
            () => client.ReadyAsync(TestContext.Current.CancellationToken));

        Assert.Null(exception.RetryAfterSeconds);
    }

    [Fact]
    public async Task PendingRefusalCarriesRetryAfterSeconds()
    {
        var handler = new ScriptedHandler((_, _) =>
            Task.FromResult(Json(HttpStatusCode.ServiceUnavailable, PendingBody(retryAfterSeconds: 7))));
        // A zero budget throws on the very first RUNTIME_HOST_INSTANCE_PENDING refusal, so the
        // hint can be asserted without waiting anything out.
        await using var client = PendingClient(handler, startupPendingBudget: TimeSpan.Zero);

        var exception = await Assert.ThrowsAsync<YogabaseRuntimeHostException>(
            () => client.ReadyAsync(TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
        Assert.Equal("RUNTIME_HOST_INSTANCE_PENDING", exception.Code);
        Assert.Equal(7, exception.RetryAfterSeconds);
    }

    [Fact]
    public async Task HeartbeatOnceAsyncWaitsOutInstancePendingThenSucceeds()
    {
        var callCount = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            var count = Interlocked.Increment(ref callCount);
            return Task.FromResult(count <= 2
                ? Json(HttpStatusCode.ServiceUnavailable, PendingBody(retryAfterSeconds: 5))
                : Json(HttpStatusCode.OK, LeaseBody));
        });
        var recordedDelays = new List<TimeSpan>();
        await using var client = PendingClient(handler, startupPendingBudget: TimeSpan.FromSeconds(60), recordedDelays);

        var lease = await client.HeartbeatOnceAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Healthy", lease.State);
        Assert.Equal(3, Volatile.Read(ref callCount));
        Assert.Equal([TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5)], recordedDelays);
    }

    [Fact]
    public async Task HeartbeatOnceAsyncGivesUpWhenPendingOutlastsTheBudget()
    {
        var callCount = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            Interlocked.Increment(ref callCount);
            // No retryAfterSeconds hint: the client falls back to its 2-second default wait.
            return Task.FromResult(Json(HttpStatusCode.ServiceUnavailable, PendingBody(retryAfterSeconds: null)));
        });
        var recordedDelays = new List<TimeSpan>();
        await using var client = PendingClient(handler, startupPendingBudget: TimeSpan.FromSeconds(5), recordedDelays);

        var exception = await Assert.ThrowsAsync<YogabaseRuntimeHostException>(
            () => client.HeartbeatOnceAsync(TestContext.Current.CancellationToken));

        Assert.Equal("RUNTIME_HOST_INSTANCE_PENDING", exception.Code);
        // Two 2-second waits (elapsed 4s) are within the 5-second budget; a third would reach 6s
        // and is refused before it is taken, so exactly 3 attempts and 2 waits are made.
        Assert.Equal(3, Volatile.Read(ref callCount));
        Assert.Equal([TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2)], recordedDelays);
    }

    [Theory]
    [InlineData("RUNTIME_HOST_STALE_EPOCH", 403)]
    [InlineData("RUNTIME_HOST_SHUTDOWN_IN_PROGRESS", 409)]
    [InlineData("RUNTIME_HOST_TERMINATED", 409)]
    [InlineData("RUNTIME_HOST_WRONG_GENERATION", 403)]
    [InlineData("RUNTIME_HOST_INVALID_TRANSITION", 409)]
    [InlineData("RUNTIME_HOST_READINESS_EVIDENCE_REQUIRED", 409)]
    // Same status as a pending refusal, and a retry hint in the body: only the code decides.
    [InlineData("RUNTIME_HOST_UNAVAILABLE", 503)]
    public async Task ReadyAsyncDoesNotRetryStaleEpochOrShutdownInProgress(string code, int statusCode)
    {
        var callCount = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            Interlocked.Increment(ref callCount);
            return Task.FromResult(Json((HttpStatusCode)statusCode, TerminalBody(code)));
        });
        var recordedDelays = new List<TimeSpan>();
        await using var client = PendingClient(handler, startupPendingBudget: TimeSpan.FromSeconds(60), recordedDelays);

        var exception = await Assert.ThrowsAsync<YogabaseRuntimeHostException>(
            () => client.ReadyAsync(TestContext.Current.CancellationToken));

        Assert.Equal(code, exception.Code);
        Assert.Equal(1, Volatile.Read(ref callCount));
        Assert.Empty(recordedDelays);
    }

    [Fact]
    public async Task ReadyAsyncWaitsOutInstancePendingThenSucceeds()
    {
        var paths = new List<string>();
        var handler = new ScriptedHandler((request, _) =>
        {
            lock (paths)
            {
                paths.Add(request.RequestUri!.AbsolutePath);
                return Task.FromResult(paths.Count == 1
                    ? Json(HttpStatusCode.ServiceUnavailable, PendingBody(retryAfterSeconds: null))
                    : Json(HttpStatusCode.OK, LeaseBody));
            }
        });
        var recordedDelays = new List<TimeSpan>();
        await using var client = PendingClient(handler, startupPendingBudget: TimeSpan.FromSeconds(60), recordedDelays);

        var lease = await client.ReadyAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Healthy", lease.State);
        Assert.Equal(["/runtime-host/v1/self/ready", "/runtime-host/v1/self/ready"], paths);
        Assert.Equal([TimeSpan.FromSeconds(2)], recordedDelays);
    }

    [Fact]
    public async Task CancellingWhileWaitingOutInstancePendingStopsWithoutAnotherAttempt()
    {
        var callCount = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            Interlocked.Increment(ref callCount);
            return Task.FromResult(Json(HttpStatusCode.ServiceUnavailable, PendingBody(retryAfterSeconds: 2)));
        });
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await using var client = new YogabaseRuntimeHostClient(
            new YogabaseRuntimeHostOptions
            {
                Endpoint = new Uri("http://127.0.0.1:8090"),
                StartupPendingBudget = TimeSpan.FromSeconds(60),
                // The caller cancels while the client is waiting; the delay observes the token the
                // client passed it, exactly as Task.Delay would.
                StartupPendingDelay = (_, token) =>
                {
                    cancellation.Cancel();
                    return Task.FromCanceled(token);
                },
            },
            handler);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.HeartbeatOnceAsync(cancellation.Token));

        Assert.Equal(1, Volatile.Read(ref callCount));
    }

    [Theory]
    [InlineData("{\"code\":\"RUNTIME_HOST_INSTANCE_PENDING\",\"retryAfterSeconds\":\"2\"}")]
    [InlineData("{\"code\":\"RUNTIME_HOST_INSTANCE_PENDING\",\"retryAfterSeconds\":2.5}")]
    [InlineData("{\"code\":\"RUNTIME_HOST_INSTANCE_PENDING\",\"retryAfterSeconds\":99999999999}")]
    [InlineData("{\"code\":\"RUNTIME_HOST_INSTANCE_PENDING\",\"retryAfterSeconds\":null}")]
    [InlineData("{\"retryAfterSeconds\":1,\"retryAfterSeconds\":2}")]
    [InlineData("[2]")]
    [InlineData("not json")]
    public void AMalformedRetryHintIsIgnoredRatherThanThrown(string bodyJson)
    {
        var exception = new YogabaseRuntimeHostException(HttpStatusCode.ServiceUnavailable, "RUNTIME_HOST_INSTANCE_PENDING", bodyJson);

        Assert.Null(exception.RetryAfterSeconds);
        Assert.Equal(bodyJson, exception.BodyJson);
    }

    [Fact]
    public async Task HeartbeatLoopReportsASwallowedFailureThroughTheCallback()
    {
        var handler = new ScriptedHandler((_, _) =>
            Task.FromResult(Json(HttpStatusCode.Forbidden, ErrorBody)));
        await using var client = Client(handler, TimeSpan.FromMilliseconds(20));
        var observed = new List<Exception>();

        // The callback itself throws; that must not stop it from being called again, nor break
        // the loop's own resilience contract.
        client.Connect(exception =>
        {
            lock (observed)
            {
                observed.Add(exception);
            }
            throw new InvalidOperationException("callback boom");
        });

        await WaitUntilAsync(
            () =>
            {
                lock (observed)
                {
                    return observed.Count >= 2;
                }
            },
            TestContext.Current.CancellationToken);

        List<Exception> snapshot;
        lock (observed)
        {
            snapshot = [.. observed];
        }
        Assert.True(snapshot.Count >= 2);
        Assert.All(snapshot, exception =>
        {
            var runtimeHostException = Assert.IsType<YogabaseRuntimeHostException>(exception);
            Assert.Equal("RUNTIME_HOST_RELAY_ALLOCATION_BACKEND_ONLY", runtimeHostException.Code);
        });
    }

    public static TheoryData<string, int, bool> BodylessRelayAnswers()
    {
        // The relay answers 413, 429 and 504 with no body at all. Only 429 (retry once a pending
        // call is answered) and 504 (the call may or may not have taken effect) are worth repeating.
        var data = new TheoryData<string, int, bool>();
        foreach (var verb in new[] { "heartbeat", "ready", "reserve", "shutdown", "assignments" })
        {
            data.Add(verb, 413, false);
            data.Add(verb, 429, true);
            data.Add(verb, 504, true);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(BodylessRelayAnswers))]
    public async Task ABodylessRelayAnswerSurfacesAsARuntimeHostExceptionCarryingItsStatus(
        string verb,
        int statusCode,
        bool retryable)
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)statusCode)));
        await using var client = Client(handler);

        // The exact type: a bodyless answer used to surface as a JSON parse error.
        var exception = await Assert.ThrowsAsync<YogabaseRuntimeHostException>(
            () => CallAsync(client, verb, TestContext.Current.CancellationToken));

        Assert.Equal((HttpStatusCode)statusCode, exception.StatusCode);
        Assert.Equal("UNKNOWN", exception.Code);
        Assert.Equal(string.Empty, exception.BodyJson);
        Assert.Null(exception.RetryAfterSeconds);
        Assert.Equal(retryable, exception.IsRetryable);
        Assert.Contains($"{statusCode} UNKNOWN", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(429)]
    [InlineData(504)]
    public async Task ABodylessRetryableAnswerIsNotWaitedOutLikeAnInstancePendingRefusal(int statusCode)
    {
        var callCount = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            Interlocked.Increment(ref callCount);
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)statusCode));
        });
        var recordedDelays = new List<TimeSpan>();
        await using var client = PendingClient(handler, startupPendingBudget: TimeSpan.FromSeconds(60), recordedDelays);

        var heartbeat = await Assert.ThrowsAsync<YogabaseRuntimeHostException>(
            () => client.HeartbeatOnceAsync(TestContext.Current.CancellationToken));
        var ready = await Assert.ThrowsAsync<YogabaseRuntimeHostException>(
            () => client.ReadyAsync(TestContext.Current.CancellationToken));

        Assert.True(heartbeat.IsRetryable);
        Assert.True(ready.IsRetryable);
        Assert.Equal(2, Volatile.Read(ref callCount));
        Assert.Empty(recordedDelays);
    }

    [Fact]
    public async Task AHeartbeatThatTimedOutIsRepeatedWithTheNextSequence()
    {
        // 504: no answer within the relay's timeout, so the heartbeat may have been accepted; a
        // heartbeat is repeated with the next sequence rather than the one that was sent.
        var bodies = new List<string>();
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            bodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return bodies.Count == 1
                ? new HttpResponseMessage(HttpStatusCode.GatewayTimeout)
                : Json(HttpStatusCode.OK, LeaseBody);
        });
        await using var client = Client(handler);

        var timedOut = await Assert.ThrowsAsync<YogabaseRuntimeHostException>(
            () => client.HeartbeatOnceAsync(TestContext.Current.CancellationToken));
        await client.HeartbeatOnceAsync(TestContext.Current.CancellationToken);

        Assert.True(timedOut.IsRetryable);
        Assert.Equal(1L, ReadLong(bodies[0], "sequence"));
        Assert.Equal(2L, ReadLong(bodies[1], "sequence"));
    }

    [Fact]
    public async Task TheHeartbeatLoopReportsABodylessAnswerAndKeepsGoing()
    {
        var callCount = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            var count = Interlocked.Increment(ref callCount);
            return Task.FromResult(count == 1
                ? new HttpResponseMessage((HttpStatusCode)429)
                : Json(HttpStatusCode.OK, LeaseBody));
        });
        await using var client = Client(handler, TimeSpan.FromMilliseconds(20));
        var observed = new List<Exception>();

        client.Connect(exception =>
        {
            lock (observed)
            {
                observed.Add(exception);
            }
        });
        await WaitUntilAsync(() => Volatile.Read(ref callCount) >= 3, TestContext.Current.CancellationToken);

        lock (observed)
        {
            var reported = Assert.IsType<YogabaseRuntimeHostException>(Assert.Single(observed));
            Assert.Equal((HttpStatusCode)429, reported.StatusCode);
            Assert.True(reported.IsRetryable);
        }
    }

    [Theory]
    [InlineData(HttpStatusCode.BadGateway, "<html>bad gateway</html>")]
    [InlineData(HttpStatusCode.InternalServerError, "[1,2]")]
    [InlineData(HttpStatusCode.InternalServerError, "{\"code\":5}")]
    [InlineData(HttpStatusCode.InternalServerError, "{\"code\":\"\"}")]
    [InlineData(HttpStatusCode.MethodNotAllowed, "   ")]
    public async Task ARefusalWhoseBodyNamesNoCodeStillSurfacesAsARuntimeHostException(HttpStatusCode status, string body)
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "text/plain"),
        }));
        await using var client = Client(handler);

        var exception = await Assert.ThrowsAsync<YogabaseRuntimeHostException>(
            () => client.ShutdownAsync(TestContext.Current.CancellationToken));

        Assert.Equal(status, exception.StatusCode);
        Assert.Equal("UNKNOWN", exception.Code);
        Assert.Equal(body, exception.BodyJson);
        Assert.False(exception.IsRetryable);
    }

    [Fact]
    public async Task OnlyTheInstancePendingCodeIsRetryableAmongTheCodedRefusals()
    {
        var pending = await Assert.ThrowsAsync<YogabaseRuntimeHostException>(async () =>
        {
            var handler = new ScriptedHandler((_, _) =>
                Task.FromResult(Json(HttpStatusCode.ServiceUnavailable, PendingBody(retryAfterSeconds: 2))));
            await using var client = PendingClient(handler, startupPendingBudget: TimeSpan.Zero);
            await client.ReadyAsync(TestContext.Current.CancellationToken);
        });
        var terminal = await Assert.ThrowsAsync<YogabaseRuntimeHostException>(async () =>
        {
            var handler = new ScriptedHandler((_, _) => Task.FromResult(Json(HttpStatusCode.Forbidden, ErrorBody)));
            await using var client = Client(handler);
            await client.ReadyAsync(TestContext.Current.CancellationToken);
        });

        Assert.True(pending.IsRetryable);
        Assert.False(terminal.IsRetryable);
        Assert.False(new YogabaseRuntimeHostException("no status").IsRetryable);
    }

    [Fact]
    public async Task AnAssignmentEventKeepsTheLowerCaseKindTheRelaySent()
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Json(
            HttpStatusCode.OK,
            "{\"events\":[{\"sequence\":3,\"kind\":\"allocation-released\",\"state\":\"Ready\",\"allocationId\":null,\"occurredAt\":\"2026-09-04T00:00:00+00:00\"}],\"resumeToken\":\"3\"}")));
        await using var client = Client(handler);

        var page = await client.WatchAssignmentsOnceAsync(null, TestContext.Current.CancellationToken);

        Assert.Equal("allocation-released", Assert.Single(page.Events).Kind);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, "{\"title\":\"Forbidden\",\"status\":403}")]
    [InlineData(HttpStatusCode.BadRequest, "{\"type\":\"about:blank\",\"title\":\"Bad Request\",\"detail\":\"x\"}")]
    [InlineData(HttpStatusCode.Conflict, "{\"title\":\"Conflict\",\"code\":null}")]
    [InlineData(HttpStatusCode.Conflict, "{\"title\":\"Conflict\",\"code\":\"\"}")]
    [InlineData(HttpStatusCode.Conflict, "{\"title\":\"Conflict\",\"code\":7}")]
    public async Task ARefusalThatOnlyCarriesATitleReadsAsUnknown(HttpStatusCode status, string body)
    {
        // The relay's refusals are {code, detail}, never an RFC 9457 problem document, so a title is
        // not a code and is not read as one.
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Json(status, body)));
        await using var client = Client(handler);

        var exception = await Assert.ThrowsAsync<YogabaseRuntimeHostException>(
            () => client.ShutdownAsync(TestContext.Current.CancellationToken));

        Assert.Equal("UNKNOWN", exception.Code);
        Assert.Equal(status, exception.StatusCode);
        Assert.Equal(body, exception.BodyJson);
        Assert.False(exception.IsRetryable);
    }

    [Fact]
    public async Task ARefusalNamesItselfByItsCodeEvenWhenItAlsoCarriesATitle()
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Json(
            HttpStatusCode.Conflict,
            "{\"title\":\"Conflict\",\"code\":\"RUNTIME_HOST_TERMINATED\",\"detail\":\"x\"}")));
        await using var client = Client(handler);

        var exception = await Assert.ThrowsAsync<YogabaseRuntimeHostException>(
            () => client.ShutdownAsync(TestContext.Current.CancellationToken));

        Assert.Equal("RUNTIME_HOST_TERMINATED", exception.Code);
    }

    // ----------------------------------------------------------------------------------------
    // The request timeout
    // ----------------------------------------------------------------------------------------

    [Theory]
    [InlineData("heartbeat")]
    [InlineData("ready")]
    [InlineData("reserve")]
    [InlineData("shutdown")]
    [InlineData("assignments")]
    public async Task ACallThatGetsNoAnswerWithinTheRequestTimeoutThrowsARetryableRuntimeHostException(string verb)
    {
        var handler = new ScriptedHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return Json(HttpStatusCode.OK, LeaseBody);
        });
        await using var client = Client(handler, requestTimeout: TimeSpan.FromMilliseconds(100));

        // The exact type: HttpClient's own timeout used to reach the caller as a bare
        // TaskCanceledException, indistinguishable from the caller cancelling.
        var exception = await Assert.ThrowsAsync<YogabaseRuntimeHostException>(
            () => CallAsync(client, verb, TestContext.Current.CancellationToken));

        Assert.Equal(YogabaseRuntimeHostException.RequestTimeoutCode, exception.Code);
        Assert.Equal((HttpStatusCode)0, exception.StatusCode);
        Assert.True(exception.IsRetryable);
        Assert.Equal(string.Empty, exception.BodyJson);
        Assert.Null(exception.RetryAfterSeconds);
        Assert.Contains("timed out", exception.Message, StringComparison.Ordinal);
        Assert.Contains("request timeout of 0.1 seconds", exception.Message, StringComparison.Ordinal);
        Assert.IsAssignableFrom<OperationCanceledException>(exception.InnerException);
    }

    [Theory]
    [InlineData("heartbeat")]
    [InlineData("ready")]
    [InlineData("reserve")]
    [InlineData("shutdown")]
    [InlineData("assignments")]
    public async Task TheCallersOwnCancellationStillCancelsAsAnOperationCanceledException(string verb)
    {
        var handler = new ScriptedHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return Json(HttpStatusCode.OK, LeaseBody);
        });
        await using var client = Client(handler);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));

        // ThrowsAnyAsync<OperationCanceledException> does not accept a YogabaseRuntimeHostException.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CallAsync(client, verb, cancellation.Token));
    }

    [Fact]
    public async Task DisposingTheClientUnderACallIsACancellationNotARequestTimeout()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new ScriptedHandler(async (_, cancellationToken) =>
        {
            started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return Json(HttpStatusCode.OK, LeaseBody);
        });
        var client = Client(handler);
        var call = client.ShutdownAsync(TestContext.Current.CancellationToken);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        await client.DisposeAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => call.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TheHeartbeatLoopReportsALocalTimeoutAndKeepsGoing()
    {
        var callCount = 0;
        var handler = new ScriptedHandler(async (_, cancellationToken) =>
        {
            if (Interlocked.Increment(ref callCount) == 1)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            return Json(HttpStatusCode.OK, LeaseBody);
        });
        await using var client = Client(handler, TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(100));
        var observed = new List<Exception>();

        client.Connect(exception =>
        {
            lock (observed)
            {
                observed.Add(exception);
            }
        });
        await WaitUntilAsync(() => Volatile.Read(ref callCount) >= 3, TestContext.Current.CancellationToken);

        lock (observed)
        {
            var reported = Assert.IsType<YogabaseRuntimeHostException>(Assert.Single(observed));
            Assert.Equal(YogabaseRuntimeHostException.RequestTimeoutCode, reported.Code);
            Assert.True(reported.IsRetryable);
        }
    }

    [Fact]
    public async Task ALocalTimeoutIsNotWaitedOutLikeAnInstancePendingRefusal()
    {
        var callCount = 0;
        var handler = new ScriptedHandler(async (_, cancellationToken) =>
        {
            Interlocked.Increment(ref callCount);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return Json(HttpStatusCode.OK, LeaseBody);
        });
        var recordedDelays = new List<TimeSpan>();
        await using var client = new YogabaseRuntimeHostClient(
            new YogabaseRuntimeHostOptions
            {
                Endpoint = new Uri("http://127.0.0.1:8090"),
                RequestTimeout = TimeSpan.FromMilliseconds(100),
                StartupPendingBudget = TimeSpan.FromSeconds(60),
                StartupPendingDelay = (delay, _) =>
                {
                    recordedDelays.Add(delay);
                    return Task.CompletedTask;
                },
            },
            handler);

        var exception = await Assert.ThrowsAsync<YogabaseRuntimeHostException>(
            () => client.ReadyAsync(TestContext.Current.CancellationToken));

        Assert.True(exception.IsRetryable);
        Assert.Equal(1, Volatile.Read(ref callCount));
        Assert.Empty(recordedDelays);
    }

    // ----------------------------------------------------------------------------------------
    // The assignment watch
    // ----------------------------------------------------------------------------------------

    [Fact]
    public async Task AWatchEndsByThrowingOnATerminalRefusalAfterDeliveringTheEventsBeforeIt()
    {
        var queries = new List<string>();
        var handler = new ScriptedHandler((request, _) =>
        {
            queries.Add(request.RequestUri!.Query);
            return Task.FromResult(queries.Count == 1
                ? Json(HttpStatusCode.OK, AssignmentsBodyWithToken("1"))
                : Json(HttpStatusCode.BadRequest, TerminalBody("RUNTIME_HOST_RESUME_TOKEN_INVALID")));
        });
        await using var client = Client(handler);
        var reported = new List<Exception>();
        var events = new List<RuntimeHostAssignmentEvent>();
        using var guard = WatchGuard();

        var exception = await Assert.ThrowsAsync<YogabaseRuntimeHostException>(async () =>
        {
            await foreach (var assignmentEvent in client.WatchAssignmentsAsync(
                TimeSpan.FromMilliseconds(5),
                reported.Add,
                guard.Token))
            {
                events.Add(assignmentEvent);
            }
        });

        Assert.Equal("RUNTIME_HOST_RESUME_TOKEN_INVALID", exception.Code);
        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.False(exception.IsRetryable);
        Assert.Equal(1L, Assert.Single(events).Sequence);

        // The watch is over: nothing polls again, and nothing was reported through the callback.
        await Task.Delay(TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken);
        Assert.Equal(["", "?resumeToken=1"], queries);
        Assert.Empty(reported);
    }

    [Fact]
    public async Task AWatchWithNoCallbackEndsOnATerminalRefusalToo()
    {
        var callCount = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            Interlocked.Increment(ref callCount);
            return Task.FromResult(Json(HttpStatusCode.Forbidden, TerminalBody("RUNTIME_HOST_STALE_EPOCH")));
        });
        await using var client = Client(handler);
        using var guard = WatchGuard();

        var exception = await Assert.ThrowsAsync<YogabaseRuntimeHostException>(async () =>
        {
            await foreach (var _ in client.WatchAssignmentsAsync(TimeSpan.FromMilliseconds(5), guard.Token))
            {
            }
        });

        Assert.Equal("RUNTIME_HOST_STALE_EPOCH", exception.Code);
        Assert.Equal(1, Volatile.Read(ref callCount));
    }

    [Fact]
    public async Task AWatchKeepsPollingThroughRetryableAnswersAndTransportFaultsWithTheSameToken()
    {
        var queries = new List<string>();
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            queries.Add(request.RequestUri!.Query);
            switch (queries.Count)
            {
                case 1:
                    return Json(HttpStatusCode.OK, AssignmentsBodyWithToken("5"));
                case 2:
                    return Json(HttpStatusCode.ServiceUnavailable, PendingBody(retryAfterSeconds: 2));
                case 3:
                    return new HttpResponseMessage((HttpStatusCode)429);
                case 4:
                    return new HttpResponseMessage(HttpStatusCode.GatewayTimeout);
                case 5:
                    throw new HttpRequestException("connection refused");
                case 6:
                    // No answer within the request timeout: the client's own timeout, retryable.
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                    return Json(HttpStatusCode.OK, PageOf([], "5"));
                default:
                    return Json(HttpStatusCode.OK, PageOf([(6, "Ready")], "6"));
            }
        });
        await using var client = Client(handler, requestTimeout: TimeSpan.FromMilliseconds(100));
        var reported = new List<Exception>();

        var events = await CollectUntilAsync(client, lastSequence: 6, reported.Add);

        Assert.Equal([1L, 6L], events.Select(item => item.Sequence));
        Assert.Equal(
            ["", "?resumeToken=5", "?resumeToken=5", "?resumeToken=5", "?resumeToken=5", "?resumeToken=5", "?resumeToken=5"],
            queries);
        Assert.Empty(reported);
    }

    [Fact]
    public async Task AWatchReportsAFailureItDoesNotRetrySilentlyOnceAndKeepsPolling()
    {
        const string BadGateway = "<html>bad gateway</html>";
        var callCount = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            var response = Interlocked.Increment(ref callCount) switch
            {
                // The same unexplained failure three polls running ...
                1 or 2 or 3 => new HttpResponseMessage(HttpStatusCode.BadGateway)
                {
                    Content = new StringContent(BadGateway, Encoding.UTF8, "text/html"),
                },
                // ... then a different one twice: a page that is not JSON.
                4 or 5 => Json(HttpStatusCode.OK, "not json"),
                6 => Json(HttpStatusCode.OK, PageOf([(7, "Ready")], "7")),
                // The first failure again after a good page is a new report.
                7 => new HttpResponseMessage(HttpStatusCode.BadGateway)
                {
                    Content = new StringContent(BadGateway, Encoding.UTF8, "text/html"),
                },
                _ => Json(HttpStatusCode.OK, PageOf([(8, "Ready")], "8")),
            };
            return Task.FromResult(response);
        });
        await using var client = Client(handler);
        var reported = new List<Exception>();

        // The callback throws; that must not end the watch.
        var events = await CollectUntilAsync(
            client,
            lastSequence: 8,
            exception =>
            {
                reported.Add(exception);
                throw new InvalidOperationException("callback boom");
            });

        Assert.Equal([7L, 8L], events.Select(item => item.Sequence));
        Assert.Equal(8, Volatile.Read(ref callCount));
        Assert.Equal(3, reported.Count);
        var first = Assert.IsType<YogabaseRuntimeHostException>(reported[0]);
        Assert.Equal(HttpStatusCode.BadGateway, first.StatusCode);
        Assert.Equal("UNKNOWN", first.Code);
        Assert.IsAssignableFrom<JsonException>(reported[1]);
        var again = Assert.IsType<YogabaseRuntimeHostException>(reported[2]);
        Assert.Equal(HttpStatusCode.BadGateway, again.StatusCode);
    }

    [Fact]
    public async Task AWatchReportsARefusalTheDocumentDoesNotNameForTheRouteAndKeepsPolling()
    {
        var callCount = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            var count = Interlocked.Increment(ref callCount);
            return Task.FromResult(count == 1
                ? Json(HttpStatusCode.Conflict, TerminalBody("RUNTIME_HOST_TERMINATED"))
                : Json(HttpStatusCode.OK, PageOf([(2, "Terminated")], "2")));
        });
        await using var client = Client(handler);
        var reported = new List<Exception>();

        var events = await CollectUntilAsync(client, lastSequence: 2, reported.Add);

        Assert.Equal(2L, Assert.Single(events).Sequence);
        var refusal = Assert.IsType<YogabaseRuntimeHostException>(Assert.Single(reported));
        Assert.Equal("RUNTIME_HOST_TERMINATED", refusal.Code);
    }

    [Fact]
    public async Task DisposingTheClientEndsARunningWatchInsteadOfPollingADisposedClient()
    {
        var firstPoll = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new ScriptedHandler((_, _) =>
        {
            firstPoll.TrySetResult();
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)429));
        });
        var client = Client(handler);
        var reported = new List<Exception>();
        using var guard = WatchGuard();
        var watch = Record.ExceptionAsync(async () =>
        {
            await foreach (var _ in client.WatchAssignmentsAsync(TimeSpan.FromMilliseconds(5), reported.Add, guard.Token))
            {
            }
        });
        await firstPoll.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        await client.DisposeAsync();
        var exception = await watch.AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.True(exception is ObjectDisposedException or OperationCanceledException, exception?.ToString());
        Assert.Empty(reported);
    }

    [Fact]
    public async Task CancellingAWatchStillEndsItEvenWhileItIsRetrying()
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)429)));
        await using var client = Client(handler);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        stop.CancelAfter(TimeSpan.FromMilliseconds(100));

        // The 429s are retried silently until the caller's token ends the watch; the end is a
        // cancellation (or, when it lands in the delay, a clean end), never a runtime-host exception.
        var exception = await Record.ExceptionAsync(async () =>
        {
            await foreach (var _ in client.WatchAssignmentsAsync(TimeSpan.FromMilliseconds(5), stop.Token))
            {
            }
        });

        Assert.True(exception is null or OperationCanceledException);
    }

    private static Task CallAsync(YogabaseRuntimeHostClient client, string verb, CancellationToken cancellationToken) =>
        verb switch
        {
            "heartbeat" => client.HeartbeatOnceAsync(cancellationToken),
            "ready" => client.ReadyAsync(cancellationToken),
            "reserve" => client.ReserveAsync(TimeSpan.FromSeconds(30), cancellationToken),
            "shutdown" => client.ShutdownAsync(cancellationToken),
            "assignments" => client.WatchAssignmentsOnceAsync(null, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(verb), verb, "Not a relay verb."),
        };

    [Fact]
    public void PublicApiExposesNoAllocationShapedMembers()
    {
        var assembly = typeof(YogabaseRuntimeHostClient).Assembly;
        var publicTypes = assembly.GetTypes().Where(type => type.IsPublic || type.IsNestedPublic).ToArray();

        // Guard against a vacuous pass: if reflection stopped finding the public surface, the
        // emptiness assertions below would succeed while proving nothing.
        Assert.Contains(typeof(YogabaseRuntimeHostClient), publicTypes);
        Assert.Contains(typeof(RuntimeHostAssignmentEvent), publicTypes);

        var allocationMethods = publicTypes
            .SelectMany(type => type.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(method => !method.IsSpecialName && method.Name.Contains("Alloc", StringComparison.OrdinalIgnoreCase))
            .Select(method => $"{method.DeclaringType!.FullName}.{method.Name}")
            .ToArray();
        Assert.Empty(allocationMethods);

        var allocationRouteConstants = publicTypes
            .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => field.GetRawConstantValue() as string)
            .Where(value => value is not null && value.Contains("/allocations", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.Empty(allocationRouteConstants);
    }

    [Fact]
    public void PublicApiExposesNoInstanceIdentifierMember()
    {
        var assembly = typeof(YogabaseRuntimeHostClient).Assembly;
        var publicTypes = assembly.GetTypes().Where(type => type.IsPublic || type.IsNestedPublic).ToArray();
        var offenders = new List<string>();

        // Guard against a vacuous pass, and prove the scan reaches the members it claims to check:
        // RuntimeHostAssignmentEvent carries AllocationId, which is the nearest legitimate
        // neighbour of the name this test forbids.
        Assert.Contains(typeof(YogabaseRuntimeHostClient), publicTypes);
        Assert.NotNull(typeof(RuntimeHostAssignmentEvent).GetProperty(
            nameof(RuntimeHostAssignmentEvent.AllocationId),
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));

        foreach (var type in publicTypes)
        {
            const BindingFlags memberFlags =
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

            offenders.AddRange(type.GetProperties(memberFlags)
                .Where(member => LooksLikeInstanceId(member.Name))
                .Select(member => $"{type.FullName}.{member.Name} (property)"));

            offenders.AddRange(type.GetFields(memberFlags)
                .Where(member => LooksLikeInstanceId(member.Name))
                .Select(member => $"{type.FullName}.{member.Name} (field)"));

            offenders.AddRange(type.GetMethods(memberFlags)
                .Where(method => !method.IsSpecialName && LooksLikeInstanceId(method.Name))
                .Select(method => $"{type.FullName}.{method.Name} (method)"));

            offenders.AddRange(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .SelectMany(constructor => constructor.GetParameters())
                .Where(parameter => LooksLikeInstanceId(parameter.Name ?? string.Empty))
                .Select(parameter => $"{type.FullName}..ctor({parameter.Name}) (constructor parameter)"));
        }

        Assert.Empty(offenders);
    }

    [Fact]
    public async Task NoRequestEverCarriesACredential()
    {
        var requests = new List<HttpRequestMessage>();
        var handler = new ScriptedHandler((request, _) =>
        {
            requests.Add(CloneRequest(request));
            return Task.FromResult(request.RequestUri!.AbsolutePath.EndsWith("assignments", StringComparison.Ordinal)
                ? Json(HttpStatusCode.OK, AssignmentsBody)
                : Json(HttpStatusCode.OK, LeaseBody));
        });
        await using var client = Client(handler);

        await client.HeartbeatOnceAsync(TestContext.Current.CancellationToken);
        await client.ReadyAsync(TestContext.Current.CancellationToken);
        await client.ReserveAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
        await client.ShutdownAsync(TestContext.Current.CancellationToken);
        await client.WatchAssignmentsOnceAsync(null, TestContext.Current.CancellationToken);

        Assert.Equal(5, requests.Count);
        foreach (var request in requests)
        {
            Assert.Null(request.Headers.Authorization);
            Assert.All(request.Headers, header => Assert.False(IsCredentialHeaderName(header.Key)));
        }
    }

    private static YogabaseRuntimeHostClient Client(
        HttpMessageHandler handler,
        TimeSpan? heartbeatInterval = null,
        TimeSpan? requestTimeout = null) =>
        new(
            new YogabaseRuntimeHostOptions
            {
                Endpoint = new Uri("http://127.0.0.1:8090"),
                HeartbeatInterval = heartbeatInterval ?? TimeSpan.FromSeconds(2),
                RequestTimeout = requestTimeout ?? TimeSpan.FromSeconds(10),
            },
            handler);

    // A watch that should have ended on its own and does not fails after ten seconds as a
    // cancellation, rather than hanging the run.
    private static CancellationTokenSource WatchGuard()
    {
        var guard = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        guard.CancelAfter(TimeSpan.FromSeconds(10));
        return guard;
    }

    // Watches until the event with `lastSequence` (or a later one) arrives and then ends the watch
    // from the consumer's side, so the watch's own end is a clean one. A watch that never gets there
    // fails after ten seconds as a cancellation instead of hanging the run.
    private static async Task<List<RuntimeHostAssignmentEvent>> CollectUntilAsync(
        YogabaseRuntimeHostClient client,
        long lastSequence,
        Action<Exception>? onFailure = null)
    {
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        stop.CancelAfter(TimeSpan.FromSeconds(10));
        var events = new List<RuntimeHostAssignmentEvent>();
        await foreach (var assignmentEvent in client.WatchAssignmentsAsync(
            TimeSpan.FromMilliseconds(5),
            onFailure,
            stop.Token))
        {
            events.Add(assignmentEvent);
            if (assignmentEvent.Sequence >= lastSequence)
            {
                stop.Cancel();
            }
        }

        return events;
    }

    // A client whose RUNTIME_HOST_INSTANCE_PENDING wait never actually sleeps: `recordedDelays`
    // (when supplied) captures each wait the client would have taken, so
    // StartupPendingBudget/retryAfterSeconds behaviour is assertable without a real-time test.
    private static YogabaseRuntimeHostClient PendingClient(
        HttpMessageHandler handler,
        TimeSpan startupPendingBudget,
        List<TimeSpan>? recordedDelays = null) =>
        new(
            new YogabaseRuntimeHostOptions
            {
                Endpoint = new Uri("http://127.0.0.1:8090"),
                HeartbeatInterval = TimeSpan.FromSeconds(2),
                RequestTimeout = TimeSpan.FromSeconds(10),
                StartupPendingBudget = startupPendingBudget,
                StartupPendingDelay = (delay, _) =>
                {
                    recordedDelays?.Add(delay);
                    return Task.CompletedTask;
                },
            },
            handler);

    private static string PendingBody(int? retryAfterSeconds) =>
        retryAfterSeconds is { } seconds
            ? $"{{\"code\":\"RUNTIME_HOST_INSTANCE_PENDING\",\"detail\":\"pending\",\"retryAfterSeconds\":{seconds}}}"
            : "{\"code\":\"RUNTIME_HOST_INSTANCE_PENDING\",\"detail\":\"pending\"}";

    // Carries a retry hint on purpose: whether a refusal is waited out is decided by its code alone.
    private static string TerminalBody(string code) =>
        $"{{\"code\":\"{code}\",\"detail\":\"terminal\",\"retryAfterSeconds\":1}}";

    private static bool LooksLikeInstanceId(string name) =>
        name.Contains("instanceid", StringComparison.OrdinalIgnoreCase);

    private static bool IsCredentialHeaderName(string name) =>
        string.Equals(name, "Authorization", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, "X-Api-Key", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, "Api-Key", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, "Cookie", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, "Proxy-Authorization", StringComparison.OrdinalIgnoreCase)
        || name.StartsWith("X-Yogabase-", StringComparison.OrdinalIgnoreCase);

    private static string PageOf(IEnumerable<(int Sequence, string State)> events, string resumeToken) =>
        "{\"events\":["
        + string.Join(",", events.Select(item => string.Create(
            CultureInfo.InvariantCulture,
            $"{{\"sequence\":{item.Sequence},\"kind\":\"heartbeat\",\"state\":\"{item.State}\",\"allocationId\":null,\"occurredAt\":\"2026-09-04T00:00:00+00:00\"}}")))
        + "],\"resumeToken\":\"" + resumeToken + "\"}";

    private static string AssignmentsBodyWithToken(string resumeToken) =>
        "{\"events\":[{\"sequence\":1,\"kind\":\"allocated\",\"state\":\"Allocated\",\"allocationId\":\"alloc-1\",\"occurredAt\":\"2026-09-04T00:00:00+00:00\"}],\"resumeToken\":\""
        + resumeToken
        + "\"}";

    private static long ReadLong(string body, string property)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty(property).GetInt64();
    }

    private static int ReadInt(string body, string property)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty(property).GetInt32();
    }

    private static async Task WaitUntilAsync(
        Func<bool> condition,
        CancellationToken cancellationToken,
        TimeSpan? timeout = null)
    {
        var deadline = DateTimeOffset.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
        while (!condition())
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TimeoutException("The expected condition was not met before the timeout elapsed.");
            }
            await Task.Delay(TimeSpan.FromMilliseconds(10), cancellationToken);
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string json) => new(status)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static HttpRequestMessage CloneRequest(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        return clone;
    }

    private sealed record CapturedRequest(HttpMethod Method, Uri Uri);

    private sealed class ScriptedHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
