using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace Yogabase.Yogabase;

internal sealed class YogabaseRealtimeClient : IYogabaseRealtime, IAsyncDisposable
{
    private readonly YogabaseClientConfiguration configuration;
    private readonly Func<CancellationToken, Task<string?>> accessToken;
    private readonly SemaphoreSlim connectionGate = new(1, 1);
    private readonly object subscriptionGate = new();
    // A subscription remains desired until its owner is disposed. This lets transient SignalR
    // reconnects restore the server-side subscription without requiring application code to
    // recreate every listener.
    private readonly HashSet<string> subscriptions = new(StringComparer.Ordinal);
    private readonly HashSet<string> activeSubscriptions = new(StringComparer.Ordinal);
    // Presence belongs to a connection, so a tracked topic stays desired until it is untracked and
    // is tracked again whenever the connection is re-established. Guarded by subscriptionGate.
    private readonly HashSet<string> trackedTopics = new(StringComparer.Ordinal);
    private readonly object eventGate = new();
    private readonly HashSet<string> deliveredEventIds = new(StringComparer.Ordinal);
    private readonly Queue<string> deliveredEventOrder = new();
    private HubConnection? connection;
    private IDisposable? messageRegistration;
    private IDisposable? presenceRegistration;

    private const int MaximumRememberedEventIds = 4096;

    internal YogabaseRealtimeClient(
        YogabaseClientConfiguration configuration,
        Func<CancellationToken, Task<string?>> accessToken)
    {
        this.configuration = configuration;
        this.accessToken = accessToken;
    }

    public bool IsConnected => connection?.State == HubConnectionState.Connected;

    public event Func<YogabaseRealtimeEvent, Task>? MessageReceived;

    public event Func<YogabaseRealtimePresenceEvent, Task>? PresenceChanged;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        YogabaseClient.EnsureCapability(configuration, "realtime-subscribe");
        await connectionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (connection?.State == HubConnectionState.Connected)
            {
                return;
            }
            if (connection is null)
            {
                var endpoint = new Uri(
                    new Uri(configuration.RealtimeEndpoint, UriKind.Absolute),
                    configuration.RealtimePath.TrimStart('/'));
                connection = new HubConnectionBuilder()
                    .WithUrl(endpoint, httpOptions =>
                    {
                        httpOptions.AccessTokenProvider = () => accessToken(CancellationToken.None);
                    })
                    .WithAutomaticReconnect(new[]
                    {
                        TimeSpan.Zero,
                        TimeSpan.FromSeconds(2),
                        TimeSpan.FromSeconds(10),
                        TimeSpan.FromSeconds(30),
                    })
                    .Build();
                messageRegistration = connection.On<YogabaseRealtimeEvent>(
                    "message",
                    HandleMessageAsync);
                presenceRegistration = connection.On<YogabaseRealtimePresenceEvent>(
                    "presence",
                    HandlePresenceAsync);
                connection.Closed += _ =>
                {
                    lock (subscriptionGate)
                    {
                        activeSubscriptions.Clear();
                    }
                    return Task.CompletedTask;
                };
                connection.Reconnecting += _ =>
                {
                    lock (subscriptionGate)
                    {
                        activeSubscriptions.Clear();
                    }
                    return Task.CompletedTask;
                };
                connection.Reconnected += _ => RestoreSubscriptionsAsync();
            }

            await connection.StartAsync(cancellationToken).ConfigureAwait(false);
            await RestoreSubscriptionsAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            connectionGate.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (connection is not null)
        {
            lock (subscriptionGate)
            {
                subscriptions.Clear();
                activeSubscriptions.Clear();
                trackedTopics.Clear();
            }
            await connection.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IAsyncDisposable> SubscribeAsync(
        string topic,
        CancellationToken cancellationToken = default)
    {
        YogabaseClient.EnsureCapability(configuration, "realtime-subscribe");
        ValidateTopic(topic);
        await ConnectAsync(cancellationToken).ConfigureAwait(false);
        var activeConnection = connection
            ?? throw new InvalidOperationException("The realtime connection was not created.");
        lock (subscriptionGate)
        {
            if (!subscriptions.Add(topic))
            {
                throw new InvalidOperationException("This realtime topic already has an active subscription.");
            }
        }
        try
        {
            await activeConnection.SendAsync("Subscribe", topic, cancellationToken).ConfigureAwait(false);
            lock (subscriptionGate)
            {
                if (subscriptions.Contains(topic))
                {
                    activeSubscriptions.Add(topic);
                }
            }
            return new Subscription(this, topic);
        }
        catch
        {
            lock (subscriptionGate)
            {
                subscriptions.Remove(topic);
                activeSubscriptions.Remove(topic);
            }
            throw;
        }
    }

    public async Task<YogabaseRealtimeLambdaInvocationReceipt> InvokeLambdaAsync(
        YogabaseRealtimeLambdaInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        YogabaseClient.EnsureCapability(configuration, "realtime-subscribe");
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        ValidateTopic(request.BindingId);
        ValidateBoundedValue(request.IdempotencyKey, 256, nameof(request.IdempotencyKey));
        ValidateBoundedValue(request.CorrelationId, 128, nameof(request.CorrelationId));
        if (request.Payload is null || request.Payload.Length > 1_048_576)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Realtime Lambda payloads are limited to one MiB.");
        }
        var now = DateTimeOffset.UtcNow;
        if (request.Deadline <= now || request.Deadline - now > TimeSpan.FromMinutes(60))
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Realtime Lambda deadlines must be in the future and within 60 minutes.");
        }
        if (request.InstanceKey is not null)
        {
            ExtendedLambdaValidation.ValidateInstanceKey(request.InstanceKey, nameof(request.InstanceKey));
        }

        await ConnectAsync(cancellationToken).ConfigureAwait(false);
        var activeConnection = connection
            ?? throw new InvalidOperationException("The realtime connection was not created.");
        var receipt = await activeConnection.InvokeAsync<YogabaseRealtimeLambdaInvocationReceipt>(
            "InvokeLambda",
            request,
            cancellationToken).ConfigureAwait(false);
        ValidateReceipt(receipt, request.CorrelationId);
        return receipt;
    }

    public async Task TrackPresenceAsync(string topic, CancellationToken cancellationToken = default)
    {
        EnsurePresence();
        ValidateTopic(topic);
        lock (subscriptionGate)
        {
            if (!trackedTopics.Contains(topic)
                && trackedTopics.Count >= YogabaseRealtimePresenceLimits.MaximumTrackedTopicsPerConnection)
            {
                throw PresenceError(YogabaseRealtimePresenceLimits.TopicLimit);
            }
        }

        await ConnectAsync(cancellationToken).ConfigureAwait(false);
        var activeConnection = connection
            ?? throw new InvalidOperationException("The realtime connection was not created.");
        try
        {
            await activeConnection.InvokeAsync("TrackPresence", topic, cancellationToken).ConfigureAwait(false);
        }
        catch (HubException exception)
        {
            throw MapPresenceError(exception);
        }
        lock (subscriptionGate)
        {
            trackedTopics.Add(topic);
        }
    }

    public async Task UntrackPresenceAsync(string topic, CancellationToken cancellationToken = default)
    {
        EnsurePresence();
        ValidateTopic(topic);
        bool wasTracked;
        lock (subscriptionGate)
        {
            wasTracked = trackedTopics.Remove(topic);
        }

        // Presence ends with its connection, so a disconnected client has nothing left to withdraw.
        var activeConnection = connection;
        if (activeConnection?.State != HubConnectionState.Connected)
        {
            return;
        }
        try
        {
            await activeConnection.InvokeAsync("UntrackPresence", topic, cancellationToken).ConfigureAwait(false);
        }
        catch (HubException exception)
        {
            if (wasTracked)
            {
                lock (subscriptionGate)
                {
                    trackedTopics.Add(topic);
                }
            }
            throw MapPresenceError(exception);
        }
    }

    public async Task<YogabaseRealtimePresenceSnapshot> GetPresenceAsync(
        string topic,
        CancellationToken cancellationToken = default)
    {
        EnsurePresence();
        ValidateTopic(topic);
        await ConnectAsync(cancellationToken).ConfigureAwait(false);
        var activeConnection = connection
            ?? throw new InvalidOperationException("The realtime connection was not created.");
        YogabaseRealtimePresenceSnapshot? snapshot;
        try
        {
            snapshot = await activeConnection.InvokeAsync<YogabaseRealtimePresenceSnapshot>(
                "GetPresence",
                topic,
                cancellationToken).ConfigureAwait(false);
        }
        catch (HubException exception)
        {
            throw MapPresenceError(exception);
        }

        if (snapshot is null
            || !string.Equals(snapshot.Topic, topic, StringComparison.Ordinal)
            || snapshot.Members is null
            || snapshot.Members.Count > YogabaseRealtimePresenceLimits.MaximumSnapshotMembers
            || snapshot.Members.Any(static member => member is null || !IsSubject(member.Subject) || member.Connections < 1)
            || snapshot.Members.Select(static member => member.Subject).Distinct(StringComparer.Ordinal).Count()
                != snapshot.Members.Count)
        {
            throw new YogabaseApiException(502, "realtime_presence_invalid", "Yogabase returned an invalid realtime presence snapshot.");
        }

        return new YogabaseRealtimePresenceSnapshot
        {
            Topic = snapshot.Topic,
            Members = Array.AsReadOnly(snapshot.Members
                .Select(static member => new YogabaseRealtimePresenceMember
                {
                    Subject = member.Subject,
                    Connections = member.Connections,
                })
                .ToArray()),
            Truncated = snapshot.Truncated,
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (connection is not null)
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            connection = null;
        }
        messageRegistration?.Dispose();
        messageRegistration = null;
        presenceRegistration?.Dispose();
        presenceRegistration = null;
        connectionGate.Dispose();
    }

    private async Task HandleMessageAsync(YogabaseRealtimeEvent message)
    {
        if (message is null
            || !string.Equals(message.Schema, YogabaseRealtimeEvent.CurrentSchema, StringComparison.Ordinal)
            || !IsCanonicalGuid(message.EventId)
            || !IsCanonicalGuid(message.InvocationId)
            || string.IsNullOrWhiteSpace(message.CorrelationId)
            || message.CorrelationId.Length > 128
            // Absent is allowed -- an older gateway does not send a topic -- but a present one must
            // be a real topic, because handlers dispatch on it.
            || (message.Topic is not null && !IsTopic(message.Topic))
            || message.Payload.ValueKind == System.Text.Json.JsonValueKind.Undefined)
        {
            throw new YogabaseApiException(502, "realtime_event_invalid", "Yogabase returned an invalid realtime event.");
        }

        lock (eventGate)
        {
            if (!deliveredEventIds.Add(message.EventId))
            {
                return;
            }
            deliveredEventOrder.Enqueue(message.EventId);
            while (deliveredEventOrder.Count > MaximumRememberedEventIds)
            {
                deliveredEventIds.Remove(deliveredEventOrder.Dequeue());
            }
        }

        var handlers = MessageReceived;
        if (handlers is null)
        {
            return;
        }
        foreach (Func<YogabaseRealtimeEvent, Task> handler in handlers.GetInvocationList())
        {
            await handler(message).ConfigureAwait(false);
        }
    }

    private async Task HandlePresenceAsync(YogabaseRealtimePresenceEvent presence)
    {
        if (presence is null
            || !IsTopic(presence.Topic)
            || !IsSubject(presence.Subject)
            || presence.Kind is not (YogabaseRealtimePresenceEvent.Join or YogabaseRealtimePresenceEvent.Leave))
        {
            throw new YogabaseApiException(502, "realtime_presence_invalid", "Yogabase returned an invalid realtime presence event.");
        }

        var handlers = PresenceChanged;
        if (handlers is null)
        {
            return;
        }
        foreach (Func<YogabaseRealtimePresenceEvent, Task> handler in handlers.GetInvocationList())
        {
            await handler(presence).ConfigureAwait(false);
        }
    }

    private void EnsurePresence()
    {
        YogabaseClient.EnsureCapability(configuration, "realtime-subscribe");
        YogabaseClient.EnsureCapability(configuration, YogabaseCapability.RealtimePresence);
    }

    // A presence subject is the gateway's canonical application subject: opaque here, but always a
    // bounded, non-blank value with no surrounding whitespace.
    private static bool IsSubject(string? subject) =>
        !string.IsNullOrWhiteSpace(subject)
        && subject!.Length <= 256
        && string.Equals(subject, subject.Trim(), StringComparison.Ordinal);

    private static readonly (string Code, int Status)[] PresenceErrorStatuses =
    [
        (YogabaseRealtimePresenceLimits.Unavailable, 503),
        (YogabaseRealtimePresenceLimits.TopicInvalid, 400),
        (YogabaseRealtimePresenceLimits.TopicLimit, 409),
        (YogabaseRealtimePresenceLimits.StoreUnavailable, 503),
    ];

    /// <summary>
    /// The hub answers presence refusals with a <see cref="HubException"/> whose message is the bounded
    /// code; the SignalR client prefixes it ("... HubException: presence_topic_limit"). Anything
    /// else is not a presence refusal and is rethrown unchanged.
    /// </summary>
    private static Exception MapPresenceError(HubException exception)
    {
        var message = exception.Message ?? string.Empty;
        foreach (var (code, _) in PresenceErrorStatuses)
        {
            if (string.Equals(message, code, StringComparison.Ordinal)
                || message.EndsWith(": " + code, StringComparison.Ordinal))
            {
                return PresenceError(code);
            }
        }
        return exception;
    }

    private static YogabaseApiException PresenceError(string code)
    {
        foreach (var (known, status) in PresenceErrorStatuses)
        {
            if (string.Equals(known, code, StringComparison.Ordinal))
            {
                return new YogabaseApiException(status, code, code switch
                {
                    YogabaseRealtimePresenceLimits.Unavailable => "Realtime presence is not enabled for this runtime.",
                    YogabaseRealtimePresenceLimits.TopicInvalid => "The presence topic is outside the realtime topic grammar.",
                    YogabaseRealtimePresenceLimits.TopicLimit => "This connection already tracks the maximum number of presence topics.",
                    _ => "The realtime presence store is unavailable.",
                });
            }
        }
        throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown presence code.");
    }

    private static void ValidateTopic(string topic)
    {
        if (!IsTopic(topic))
        {
            throw new ArgumentException("Realtime topics use 1..96 lowercase [a-z0-9._-] characters.", nameof(topic));
        }
    }

    /// <summary>The server's own topic grammar, used both to refuse a bad subscription and to check an inbound event's topic.</summary>
    private static bool IsTopic(string? topic) =>
        !string.IsNullOrWhiteSpace(topic)
        && topic!.Length <= 96
        && string.Equals(topic, topic.Trim(), StringComparison.Ordinal)
        && topic.All(character => character is >= 'a' and <= 'z'
            or >= '0' and <= '9'
            or '.' or '-' or '_');

    private static void ValidateBoundedValue(string value, int maximumLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length > maximumLength
            || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException("A bounded non-empty value is required.", parameterName);
        }
    }

    private static void ValidateReceipt(
        YogabaseRealtimeLambdaInvocationReceipt? receipt,
        string correlationId)
    {
        if (receipt is null
#if NET10_0_OR_GREATER
            || !Enum.IsDefined(receipt.Outcome)
#else
            || !Enum.IsDefined(typeof(YogabaseRealtimeLambdaInvocationOutcome), receipt.Outcome)
#endif
            || !string.Equals(receipt.CorrelationId, correlationId, StringComparison.Ordinal)
            || receipt.FailureCode is not null && !ExtendedLambdaValidation.IsFailureCode(receipt.FailureCode)
            || (receipt.Outcome is YogabaseRealtimeLambdaInvocationOutcome.Accepted
                    or YogabaseRealtimeLambdaInvocationOutcome.Replayed
                    or YogabaseRealtimeLambdaInvocationOutcome.Completed)
                && !IsCanonicalGuid(receipt.InvocationId)
            || receipt.InvocationId is null
            || receipt.InvocationId.Length > 128)
        {
            throw new YogabaseApiException(502, "realtime_receipt_invalid", "Yogabase returned an invalid realtime Lambda receipt.");
        }
    }

    private static bool IsCanonicalGuid(string? value) =>
        Guid.TryParseExact(value, "D", out var parsed)
        && string.Equals(parsed.ToString("D"), value, StringComparison.Ordinal);

    private async ValueTask UnsubscribeAsync(string topic)
    {
        var shouldSend = false;
        lock (subscriptionGate)
        {
            if (!subscriptions.Remove(topic))
            {
                return;
            }
            shouldSend = activeSubscriptions.Remove(topic);
        }
        if (shouldSend && connection?.State == HubConnectionState.Connected)
        {
            await connection.SendAsync("Unsubscribe", topic).ConfigureAwait(false);
        }
    }

    private async Task RestoreSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        var activeConnection = connection;
        if (activeConnection?.State != HubConnectionState.Connected)
        {
            return;
        }

        string[] topics;
        lock (subscriptionGate)
        {
            activeSubscriptions.Clear();
            topics = subscriptions.ToArray();
        }

        foreach (var topic in topics)
        {
            await activeConnection.SendAsync("Subscribe", topic, cancellationToken).ConfigureAwait(false);
            var stillDesired = false;
            lock (subscriptionGate)
            {
                if (subscriptions.Contains(topic))
                {
                    activeSubscriptions.Add(topic);
                    stillDesired = true;
                }
            }
            if (!stillDesired && activeConnection.State == HubConnectionState.Connected)
            {
                // An owner can dispose while a reconnect subscription is in flight. Undo the
                // server-side subscription rather than leaving a topic active with no local owner.
                await activeConnection.SendAsync("Unsubscribe", topic, cancellationToken).ConfigureAwait(false);
            }
        }

        // A new connection is absent everywhere until it tracks again. Tracking also subscribes it.
        string[] tracked;
        lock (subscriptionGate)
        {
            tracked = trackedTopics.ToArray();
        }
        foreach (var topic in tracked)
        {
            try
            {
                await activeConnection.InvokeAsync("TrackPresence", topic, cancellationToken).ConfigureAwait(false);
            }
            catch (HubException)
            {
                // The hub refused it on this connection (presence switched off, store unavailable):
                // it is no longer tracked, and the next TrackPresenceAsync reports why.
                lock (subscriptionGate)
                {
                    trackedTopics.Remove(topic);
                }
            }
        }
    }

    private sealed class Subscription : IAsyncDisposable
    {
        private readonly YogabaseRealtimeClient owner;
        private readonly string topic;
        private bool disposed;

        internal Subscription(YogabaseRealtimeClient owner, string topic)
        {
            this.owner = owner;
            this.topic = topic;
        }

        public async ValueTask DisposeAsync()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            await owner.UnsubscribeAsync(topic).ConfigureAwait(false);
        }
    }
}
