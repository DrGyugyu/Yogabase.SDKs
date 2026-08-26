using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace StackYoga.YogaBase;

internal sealed class YogaBaseRealtimeClient : IYogaBaseRealtime, IAsyncDisposable
{
    private readonly YogaBaseClientConfiguration configuration;
    private readonly Func<CancellationToken, Task<string?>> accessToken;
    private readonly SemaphoreSlim connectionGate = new(1, 1);
    private readonly object subscriptionGate = new();
    private readonly HashSet<string> subscriptions = new(StringComparer.Ordinal);
    private HubConnection? connection;

    internal YogaBaseRealtimeClient(
        YogaBaseClientConfiguration configuration,
        Func<CancellationToken, Task<string?>> accessToken)
    {
        this.configuration = configuration;
        this.accessToken = accessToken;
    }

    public bool IsConnected => connection?.State == HubConnectionState.Connected;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        YogaBaseClient.EnsureCapability(configuration, "realtime-subscribe");
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
                    new Uri(configuration.GatewayEndpoint, UriKind.Absolute),
                    configuration.RealtimePath.TrimStart('/'));
                connection = new HubConnectionBuilder()
                    .WithUrl(endpoint, httpOptions =>
                    {
                        httpOptions.AccessTokenProvider = () => accessToken(CancellationToken.None);
                    })
                    .Build();
                connection.Closed += _ =>
                {
                    lock (subscriptionGate)
                    {
                        subscriptions.Clear();
                    }
                    return Task.CompletedTask;
                };
            }

            await connection.StartAsync(cancellationToken).ConfigureAwait(false);
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
            }
            await connection.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IAsyncDisposable> SubscribeAsync(
        string topic,
        CancellationToken cancellationToken = default)
    {
        YogaBaseClient.EnsureCapability(configuration, "realtime-subscribe");
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
            return new Subscription(this, topic);
        }
        catch
        {
            lock (subscriptionGate)
            {
                subscriptions.Remove(topic);
            }
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (connection is not null)
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            connection = null;
        }
        connectionGate.Dispose();
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

    private async ValueTask UnsubscribeAsync(string topic)
    {
        lock (subscriptionGate)
        {
            if (!subscriptions.Remove(topic))
            {
                return;
            }
        }
        if (connection?.State == HubConnectionState.Connected)
        {
            await connection.SendAsync("Unsubscribe", topic).ConfigureAwait(false);
        }
    }

    private sealed class Subscription : IAsyncDisposable
    {
        private readonly YogaBaseRealtimeClient owner;
        private readonly string topic;
        private bool disposed;

        internal Subscription(YogaBaseRealtimeClient owner, string topic)
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
