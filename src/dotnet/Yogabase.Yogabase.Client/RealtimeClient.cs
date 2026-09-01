using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    private readonly object eventGate = new();
    private readonly HashSet<string> deliveredEventIds = new(StringComparer.Ordinal);
    private readonly Queue<string> deliveredEventOrder = new();
    private HubConnection? connection;
    private IDisposable? messageRegistration;

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

    public async ValueTask DisposeAsync()
    {
        if (connection is not null)
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            connection = null;
        }
        messageRegistration?.Dispose();
        messageRegistration = null;
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

    private static void ValidateTopic(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic)
            || topic.Length > 96
            || !string.Equals(topic, topic.Trim(), StringComparison.Ordinal)
            || topic.Any(character => !(character is >= 'a' and <= 'z'
                or >= '0' and <= '9'
                or '.' or '-' or '_')))
        {
            throw new ArgumentException("Realtime topics use 1..96 lowercase [a-z0-9._-] characters.", nameof(topic));
        }
    }

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
