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

    private static YogabaseRuntimeHostClient Client(HttpMessageHandler handler, TimeSpan? heartbeatInterval = null) =>
        new(
            new YogabaseRuntimeHostOptions
            {
                Endpoint = new Uri("http://127.0.0.1:8090"),
                HeartbeatInterval = heartbeatInterval ?? TimeSpan.FromSeconds(2),
                RequestTimeout = TimeSpan.FromSeconds(10),
            },
            handler);

    private static bool LooksLikeInstanceId(string name) =>
        name.Contains("instanceid", StringComparison.OrdinalIgnoreCase);

    private static bool IsCredentialHeaderName(string name) =>
        string.Equals(name, "Authorization", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, "X-Api-Key", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, "Api-Key", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, "Cookie", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, "Proxy-Authorization", StringComparison.OrdinalIgnoreCase)
        || name.StartsWith("X-Yogabase-", StringComparison.OrdinalIgnoreCase);

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
