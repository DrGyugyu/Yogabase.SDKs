using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Yogabase.Yogabase;

namespace Yogabase.Tests;

/// <summary>
/// The raw-JSON payload accessor and the realtime presence API (owner decision, 2026-09-17), driven
/// against a real SignalR hub on loopback that speaks the product hub's presence surface:
/// <c>TrackPresence</c>, <c>UntrackPresence</c>, <c>GetPresence</c> and the <c>presence</c> client
/// method, with the product's bounded error codes as <see cref="HubException"/> messages.
/// </summary>
public sealed class RealtimePresenceTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    [Fact]
    public void PayloadJsonIsTheRawPayloadTextAndIsNeverSerialized()
    {
        using var document = JsonDocument.Parse("{\"score\":12,\"tags\":[\"a\",true],\"nested\":{\"x\":null}}");
        var message = new YogabaseRealtimeEvent { Payload = document.RootElement.Clone() };

        Assert.Equal("{\"score\":12,\"tags\":[\"a\",true],\"nested\":{\"x\":null}}", message.PayloadJson);
        Assert.Null(new YogabaseRealtimeEvent().PayloadJson);
        Assert.DoesNotContain("payloadJson", JsonSerializer.Serialize(message, WebJson), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BootstrapRequestsThePresenceRowAndAcceptsIt()
    {
        string? requested = null;
        var handler = new ScriptedHandler(request =>
        {
            requested = request.Headers.TryGetValues("X-Yogabase-Accept-Capabilities", out var values)
                ? string.Join(",", values)
                : null;
            return Json(HttpStatusCode.OK, JsonSerializer.Serialize(Configuration("http://localhost:5001/", presence: true), WebJson));
        });

        await using var client = await YogabaseClient.CreateAsync(
            new Uri("http://localhost/"),
            "sy_pub_0123456789abcdef",
            new YogabaseClientOptions { AllowInsecureLoopback = true, TransportHandler = handler },
            TestContext.Current.CancellationToken);

        Assert.Equal("realtime-presence", requested);
        Assert.True(client.Configuration.IsAvailable("realtime-presence"));
        Assert.Equal(5, client.Configuration.Capabilities.Count);
    }

    [Fact]
    public async Task AGatewayThatIgnoresTheHeaderLeavesPresenceUnavailableAndNothingConnects()
    {
        var handler = new ScriptedHandler(_ =>
            Json(HttpStatusCode.OK, JsonSerializer.Serialize(Configuration("http://127.0.0.1:1/", presence: null), WebJson)));
        await using var client = await YogabaseClient.CreateAsync(
            new Uri("http://localhost/"),
            "sy_pub_0123456789abcdef",
            new YogabaseClientOptions { AllowInsecureLoopback = true, TransportHandler = handler },
            TestContext.Current.CancellationToken);

        Assert.False(client.Configuration.IsAvailable("realtime-presence"));
        // Port 1 accepts nothing: reaching the network would fail differently and more slowly.
        await Assert.ThrowsAsync<NotSupportedException>(() => client.Realtime.TrackPresenceAsync("lobby", TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<NotSupportedException>(() => client.Realtime.GetPresenceAsync("lobby", TestContext.Current.CancellationToken));
        Assert.False(client.Realtime.IsConnected);
    }

    [Fact]
    public async Task APresenceRowReportedUnavailableRefusesTheCallsLocally()
    {
        await using var client = YogabaseClient.FromConfiguration(
            Configuration("http://127.0.0.1:1/", presence: false),
            new YogabaseClientOptions { AllowInsecureLoopback = true });

        await Assert.ThrowsAsync<NotSupportedException>(() => client.Realtime.TrackPresenceAsync("lobby", TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<NotSupportedException>(() => client.Realtime.UntrackPresenceAsync("lobby", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TrackGetUntrackAndJoinEventsReachTheHub()
    {
        await using var hub = await PresenceHubHost.StartAsync();
        await using var client = hub.Client();
        var joined = new TaskCompletionSource<YogabaseRealtimePresenceEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Realtime.PresenceChanged += presence =>
        {
            joined.TrySetResult(presence);
            return Task.CompletedTask;
        };

        await client.Realtime.TrackPresenceAsync("lobby", TestContext.Current.CancellationToken);
        var presence = await joined.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.Equal(("lobby", "user-1", YogabaseRealtimePresenceEvent.Join), (presence.Topic, presence.Subject, presence.Kind));

        var snapshot = await client.Realtime.GetPresenceAsync("lobby", TestContext.Current.CancellationToken);
        Assert.Equal("lobby", snapshot.Topic);
        Assert.False(snapshot.Truncated);
        var member = Assert.Single(snapshot.Members);
        Assert.Equal(("user-1", 1), (member.Subject, member.Connections));

        await client.Realtime.UntrackPresenceAsync("lobby", TestContext.Current.CancellationToken);
        Assert.Equal(["TrackPresence:lobby", "GetPresence:lobby", "UntrackPresence:lobby"], hub.Calls.ToArray());
    }

    [Theory]
    [InlineData("presence_unavailable", 503)]
    [InlineData("presence_topic_invalid", 400)]
    [InlineData("presence_topic_limit", 409)]
    [InlineData("presence_store_unavailable", 503)]
    public async Task TheHubsBoundedErrorsBecomeTypedApiErrors(string code, int status)
    {
        await using var hub = await PresenceHubHost.StartAsync(failWith: code);
        await using var client = hub.Client();

        var track = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Realtime.TrackPresenceAsync("lobby", TestContext.Current.CancellationToken));
        var get = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Realtime.GetPresenceAsync("lobby", TestContext.Current.CancellationToken));

        Assert.Equal((code, status), (track.Code, track.StatusCode));
        Assert.Equal((code, status), (get.Code, get.StatusCode));
    }

    [Fact]
    public async Task TheSeventeenthTrackedTopicIsRefusedBeforeItReachesTheHub()
    {
        await using var hub = await PresenceHubHost.StartAsync();
        await using var client = hub.Client();
        for (var index = 0; index < YogabaseRealtimePresenceLimits.MaximumTrackedTopicsPerConnection; index++)
        {
            await client.Realtime.TrackPresenceAsync($"room-{index}", TestContext.Current.CancellationToken);
        }
        // Tracking a topic already tracked is a no-op on the hub, so it does not count twice.
        await client.Realtime.TrackPresenceAsync("room-0", TestContext.Current.CancellationToken);

        var refused = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Realtime.TrackPresenceAsync("room-16", TestContext.Current.CancellationToken));

        Assert.Equal("presence_topic_limit", refused.Code);
        Assert.DoesNotContain("TrackPresence:room-16", hub.Calls);
    }

    [Theory]
    [InlineData("other-topic", 1, 1)]
    [InlineData("lobby", 1001, 1)]
    [InlineData("lobby", 1, 0)]
    public async Task ASnapshotOutsideTheContractIsRefused(string answeredTopic, int members, int connections)
    {
        await using var hub = await PresenceHubHost.StartAsync(snapshot: new PresenceSnapshot(
            answeredTopic,
            Enumerable.Range(0, members).Select(index => new PresenceMember($"user-{index:D4}", connections)).ToArray(),
            false));
        await using var client = hub.Client();

        var refused = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Realtime.GetPresenceAsync("lobby", TestContext.Current.CancellationToken));

        Assert.Equal("realtime_presence_invalid", refused.Code);
    }

    [Theory]
    [InlineData("Lobby")]
    [InlineData("")]
    [InlineData("a b")]
    public async Task AnInvalidTopicIsRefusedBeforeConnecting(string topic)
    {
        await using var client = YogabaseClient.FromConfiguration(
            Configuration("http://127.0.0.1:1/", presence: true),
            new YogabaseClientOptions { AllowInsecureLoopback = true });

        await Assert.ThrowsAsync<ArgumentException>(() => client.Realtime.TrackPresenceAsync(topic, TestContext.Current.CancellationToken));
        Assert.False(client.Realtime.IsConnected);
    }

    [Fact]
    public async Task TrackedTopicsAreTrackedAgainWhenTheConnectionIsReestablished()
    {
        await using var hub = await PresenceHubHost.StartAsync();
        await using var client = hub.Client();
        await client.Realtime.TrackPresenceAsync("lobby", TestContext.Current.CancellationToken);
        await client.Realtime.UntrackPresenceAsync("lobby", TestContext.Current.CancellationToken);
        await client.Realtime.TrackPresenceAsync("arena", TestContext.Current.CancellationToken);

        // Presence belongs to a connection: a new connection is absent everywhere until it tracks
        // again. A server-side abort closes this one for good, so connect again explicitly.
        hub.AbortConnections();
        await WaitUntilAsync(() => !client.Realtime.IsConnected, TimeSpan.FromSeconds(10));
        await client.Realtime.ConnectAsync(TestContext.Current.CancellationToken);
        await hub.WaitForCallAsync("TrackPresence:arena", 2, TimeSpan.FromSeconds(10));

        Assert.Equal(1, hub.Calls.Count(call => call == "TrackPresence:lobby"));
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (!condition())
        {
            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new TimeoutException("The condition did not hold in time.");
            }
            await Task.Delay(TimeSpan.FromMilliseconds(25), TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task AMessageCarriesItsPayloadAsRawJsonToo()
    {
        await using var hub = await PresenceHubHost.StartAsync();
        await using var client = hub.Client();
        var received = new TaskCompletionSource<YogabaseRealtimeEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Realtime.MessageReceived += message =>
        {
            received.TrySetResult(message);
            return Task.CompletedTask;
        };

        // TrackPresence subscribes the connection, and this hub answers it with one topic message.
        await client.Realtime.TrackPresenceAsync("scores", TestContext.Current.CancellationToken);
        var message = await received.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        Assert.Equal("scores", message.Topic);
        using var payload = JsonDocument.Parse(message.PayloadJson!);
        Assert.Equal(42, payload.RootElement.GetProperty("points").GetInt32());
        Assert.Equal(message.Payload.GetRawText(), message.PayloadJson);
    }

    internal static YogabaseClientConfiguration Configuration(string realtimeEndpoint, bool? presence) => new()
    {
        Schema = YogabaseClient.ClientConfigSchema,
        ProjectId = Guid.Parse("11111111-1111-4111-8111-111111111111"),
        EnvironmentId = Guid.Parse("22222222-2222-4222-8222-222222222222"),
        GatewayEndpoint = "http://localhost/",
        RealtimeEndpoint = realtimeEndpoint,
        OidcAuthority = "http://localhost/p/11111111-1111-4111-8111-111111111111",
        OidcDiscoveryEndpoint = "http://localhost/.well-known/openid-configuration",
        OidcPublicClientId = "stackyoga-native-preview",
        OidcScopes = ["openid", "offline_access"],
        OidcRedirectUris = ["com.stackyoga.app:/oauth/callback"],
        RealtimePath = "/v1/realtime",
        Capabilities = presence is null
            ? CoreCapabilities()
            : [.. CoreCapabilities(), new YogabaseCapability
            {
                Id = "realtime-presence",
                Available = presence.Value,
                UnavailableReason = presence.Value ? null : "presence-disabled",
            }],
        IssuedAt = DateTimeOffset.Parse("2026-09-22T00:00:00+00:00", System.Globalization.CultureInfo.InvariantCulture),
    };

    private static YogabaseCapability[] CoreCapabilities() =>
    [
        new() { Id = "auth", Available = true },
        new() { Id = "storage", Available = true },
        new() { Id = "functions-rest", Available = true },
        new() { Id = "realtime-subscribe", Available = true },
    ];

    private static HttpResponseMessage Json(HttpStatusCode status, string json) => new(status)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private sealed class ScriptedHandler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(send(request));
    }

    internal sealed record PresenceMember(string Subject, int Connections);

    internal sealed record PresenceSnapshot(string Topic, IReadOnlyList<PresenceMember> Members, bool Truncated);

    internal sealed record PresenceEvent(string Topic, string Subject, string Kind);

    internal sealed record TopicMessage(string Schema, string EventId, string InvocationId, string CorrelationId, JsonElement Payload, string Topic);

    /// <summary>Records every call and answers the way the product hub does, with one subject: user-1.</summary>
    internal sealed class PresenceHubState
    {
        public ConcurrentQueue<string> Calls { get; } = new();
        public ConcurrentDictionary<string, HubCallerContext> Connections { get; } = new();
        public string? FailWith { get; init; }
        public PresenceSnapshot? Snapshot { get; init; }
    }

    internal sealed class PresenceHub(PresenceHubState state) : Hub
    {
        public override Task OnConnectedAsync()
        {
            state.Connections[Context.ConnectionId] = Context;
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            state.Connections.TryRemove(Context.ConnectionId, out _);
            return base.OnDisconnectedAsync(exception);
        }

        public Task Subscribe(string topic)
        {
            state.Calls.Enqueue($"Subscribe:{topic}");
            return Groups.AddToGroupAsync(Context.ConnectionId, topic);
        }

        public Task Unsubscribe(string topic)
        {
            state.Calls.Enqueue($"Unsubscribe:{topic}");
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, topic);
        }

        public async Task TrackPresence(string topic)
        {
            state.Calls.Enqueue($"TrackPresence:{topic}");
            Fail();
            await Groups.AddToGroupAsync(Context.ConnectionId, topic);
            await Clients.Group(topic).SendAsync("presence", new PresenceEvent(topic, "user-1", "join"));
            if (topic == "scores")
            {
                using var payload = JsonDocument.Parse("{\"points\":42}");
                await Clients.Group(topic).SendAsync("message", new TopicMessage(
                    "yogabase.lambda-realtime-event.v1",
                    "33333333-3333-4333-8333-333333333333",
                    "44444444-4444-4444-8444-444444444444",
                    "correlation-1",
                    payload.RootElement.Clone(),
                    topic));
            }
        }

        public Task UntrackPresence(string topic)
        {
            state.Calls.Enqueue($"UntrackPresence:{topic}");
            Fail();
            return Task.CompletedTask;
        }

        public Task<PresenceSnapshot> GetPresence(string topic)
        {
            state.Calls.Enqueue($"GetPresence:{topic}");
            Fail();
            return Task.FromResult(state.Snapshot ?? new PresenceSnapshot(topic, [new PresenceMember("user-1", 1)], false));
        }

        private void Fail()
        {
            if (state.FailWith is { } code)
            {
                throw new HubException(code);
            }
        }
    }

    internal sealed class PresenceHubHost : IAsyncDisposable
    {
        private readonly WebApplication application;
        private readonly PresenceHubState state;

        private PresenceHubHost(WebApplication application, PresenceHubState state, Uri address)
        {
            this.application = application;
            this.state = state;
            Address = address;
        }

        public Uri Address { get; }

        public IReadOnlyCollection<string> Calls => state.Calls;

        public static async Task<PresenceHubHost> StartAsync(string? failWith = null, PresenceSnapshot? snapshot = null)
        {
            var state = new PresenceHubState { FailWith = failWith, Snapshot = snapshot };
            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Logging.ClearProviders();
            builder.Services.AddSingleton(state);
            builder.Services.AddSignalR();
            var application = builder.Build();
            application.MapHub<PresenceHub>("/v1/realtime");
            await application.StartAsync(TestContext.Current.CancellationToken);
            var address = application.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            return new PresenceHubHost(application, state, new Uri(address + "/"));
        }

        public YogabaseClient Client() => YogabaseClient.FromConfiguration(
            Configuration(Address.AbsoluteUri, presence: true),
            new YogabaseClientOptions { AllowInsecureLoopback = true });

        public void AbortConnections()
        {
            foreach (var connection in state.Connections.Values)
            {
                connection.Abort();
            }
        }

        public async Task WaitForCallAsync(string call, int count, TimeSpan timeout)
        {
            var deadline = DateTimeOffset.UtcNow + timeout;
            while (state.Calls.Count(recorded => recorded == call) < count)
            {
                if (DateTimeOffset.UtcNow > deadline)
                {
                    throw new TimeoutException($"'{call}' was not seen {count} times; calls: {string.Join(", ", state.Calls)}");
                }
                await Task.Delay(TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await application.StopAsync(CancellationToken.None);
            await application.DisposeAsync();
        }
    }
}
