using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Yogabase.Yogabase;

namespace Yogabase.Samples.ServerPathProbe;

internal static class Program
{
    private const int MaximumPreviewCharacters = 200;

    internal static async Task<int> Main(string[] args)
    {
        CliOptions? options;
        try
        {
            options = CliParser.Parse(args);
        }
        catch (CliUsageException exception)
        {
            Console.Error.WriteLine(exception.Message);
            Console.Error.WriteLine();
            Console.Error.WriteLine("Run with --help for usage.");
            return ExitCodes.UsageError;
        }

        if (options is null)
        {
            Console.WriteLine(CliParser.HelpText);
            return ExitCodes.Success;
        }

        SignInContext signIn;
        try
        {
            signIn = SignInContext.LoadFromEnvironment(Environment.GetEnvironmentVariable);
        }
        catch (CliUsageException exception)
        {
            Console.Error.WriteLine(exception.Message);
            Console.Error.WriteLine();
            Console.Error.WriteLine("Run with --help for usage.");
            return ExitCodes.UsageError;
        }

        Console.WriteLine("[0/5] Sign-in context loaded from the environment (values never printed).");
        Console.WriteLine("      player access token: present");
        Console.WriteLine($"      player refresh token: {(signIn.PlayerRefreshToken is null ? "absent" : "present")}");
        Console.WriteLine(
            $"      developer token: {(signIn.DeveloperTokenProvided ? "present (reserved, unused by this probe's five steps)" : "absent")}");

        using var overallCts = new CancellationTokenSource(TimeSpan.FromSeconds(options.TimeoutSeconds));
        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            overallCts.Cancel();
        };
        Console.CancelKeyPress += cancelHandler;
        try
        {
            return await RunAsync(options, signIn, overallCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (overallCts.IsCancellationRequested)
        {
            Console.Error.WriteLine(
                $"Timed out or cancelled before every requested step finished (--timeout-seconds {options.TimeoutSeconds.ToString(CultureInfo.InvariantCulture)}).");
            return ExitCodes.TimedOut;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Unexpected error: {exception}");
            return ExitCodes.UnexpectedError;
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }

    private static async Task<int> RunAsync(CliOptions options, SignInContext signIn, CancellationToken cancellationToken)
    {
        var sessionStore = new InMemoryYogabaseSessionStore();
        await sessionStore.SaveAsync(
            new YogabaseTokenSet
            {
                AccessToken = signIn.PlayerAccessToken,
                RefreshToken = signIn.PlayerRefreshToken,
                TokenType = "Bearer",
                // The real expiry belongs to whoever issued the token. This tool does not decode the
                // JWT to guess it, so it advertises a conservative lifetime to the client's own
                // expiry check; a token that is already expired still fails at the gateway with 401,
                // which step 1 below reports plainly.
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(2),
            },
            cancellationToken).ConfigureAwait(false);

        Console.WriteLine();
        Console.WriteLine($"[1/5] Bootstrapping the Yogabase client against {options.GatewayUrl}...");
        YogabaseClient client;
        try
        {
            client = await YogabaseClient.CreateAsync(
                options.GatewayUrl,
                options.PublishableKey,
                new YogabaseClientOptions
                {
                    SessionStore = sessionStore,
                    AllowInsecureLoopback = options.AllowInsecureLoopback,
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Console.Error.WriteLine($"      FAILED: {exception.Message}");
            return ExitCodes.ClientBootstrapFailed;
        }

        await using (client.ConfigureAwait(false))
        {
            Console.WriteLine("      client-config accepted. Capabilities:");
            foreach (var capabilityId in new[] { "auth", "storage", "functions-rest", "realtime-subscribe" })
            {
                Console.WriteLine(
                    $"        {capabilityId}: {(client.Configuration.IsAvailable(capabilityId) ? "available" : "unavailable")}");
            }

            Console.WriteLine();
            Console.WriteLine($"[2/5] Servers.StartAsync(\"{options.Service}\")...");
            YogabaseServer server;
            try
            {
                server = await client.Servers.StartAsync(options.Service, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Console.Error.WriteLine($"      FAILED: {exception.Message}");
                return ExitCodes.ServersStartFailed;
            }

            Console.WriteLine($"      serverId: {server.ServerId}");
            Console.WriteLine($"      state: {server.State}");
            Console.WriteLine($"      networkPorts ({server.NetworkPorts.Count.ToString(CultureInfo.InvariantCulture)}):");
            foreach (var port in server.NetworkPorts)
            {
                Console.WriteLine(
                    $"        {port.Name}: {port.Protocol}/{port.Endpoint} "
                    + $"(internal {port.InternalPort.ToString(CultureInfo.InvariantCulture)})");
            }

            Console.WriteLine();
            Console.WriteLine($"[3/5] Sending one UDP datagram to the \"{options.PortName}\" port...");
            var udpExitCode = await SendUdpProbeAsync(server, options, cancellationToken).ConfigureAwait(false);
            if (udpExitCode is int udpFailure)
            {
                return udpFailure;
            }

            Console.WriteLine();
            Console.WriteLine($"[4/5] Functions.InvokeAsync(\"{options.FunctionId}\", \"{options.FunctionVersion}\")...");
            var lambdaExitCode = await InvokeLambdaAsync(client, options, cancellationToken).ConfigureAwait(false);
            if (lambdaExitCode is int lambdaFailure)
            {
                return lambdaFailure;
            }

            Console.WriteLine();
            var realtimeExitCode = await SubscribeRealtimeAsync(client, options, cancellationToken).ConfigureAwait(false);
            if (realtimeExitCode is int realtimeFailure)
            {
                return realtimeFailure;
            }

            if (options.StopServerAfter)
            {
                Console.WriteLine();
                Console.WriteLine($"[cleanup] Servers.StopAsync(\"{server.ServerId}\")...");
                try
                {
                    var stopped = await client.Servers.StopAsync(server.ServerId, cancellationToken).ConfigureAwait(false);
                    Console.WriteLine($"      state after stop: {stopped.State}");
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    // Cleanup is best-effort: the walk itself already succeeded, so a failed release
                    // is reported but does not change the exit code.
                    Console.Error.WriteLine($"      stop failed (non-fatal): {exception.Message}");
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine("All requested steps completed.");
        return ExitCodes.Success;
    }

    private static async Task<int?> SendUdpProbeAsync(
        YogabaseServer server,
        CliOptions options,
        CancellationToken cancellationToken)
    {
        var port = server.FindPort(options.PortName);
        if (port is null)
        {
            Console.Error.WriteLine($"      FAILED: the allocation does not declare a \"{options.PortName}\" port.");
            return ExitCodes.UdpSendFailed;
        }

        var targetHost = options.AddressOverride ?? port.Host;
        if (options.AddressOverride is not null)
        {
            Console.WriteLine(
                $"      --address-override applied: dialing {targetHost} instead of the allocation's own host "
                + $"{port.Host} (this is expected on macOS against local k3d, whose node-observed addresses "
                + "are not routable from the host).");
        }

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(targetHost, cancellationToken).ConfigureAwait(false);
            if (addresses.Length == 0)
            {
                Console.Error.WriteLine($"      FAILED: {targetHost} did not resolve to any address.");
                return ExitCodes.UdpSendFailed;
            }

            var endpoint = new IPEndPoint(addresses[0], port.ExternalPort);
            using var udpClient = new UdpClient(addresses[0].AddressFamily);
            var payload = Encoding.UTF8.GetBytes(options.UdpMessage);
            var sent = await udpClient.SendAsync(payload, endpoint, cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"      sent {sent.ToString(CultureInfo.InvariantCulture)} byte(s) to {endpoint} ({port.Protocol}).");

            if (options.UdpTimeoutMilliseconds > 0)
            {
                using var receiveTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                receiveTimeout.CancelAfter(options.UdpTimeoutMilliseconds);
                try
                {
                    var reply = await udpClient.ReceiveAsync(receiveTimeout.Token).ConfigureAwait(false);
                    Console.WriteLine(
                        $"      received {reply.Buffer.Length.ToString(CultureInfo.InvariantCulture)} byte(s) back "
                        + $"from {reply.RemoteEndPoint}: {BoundedPreview.OfBytes(reply.Buffer, MaximumPreviewCharacters)}");
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine(
                        $"      no reply within {options.UdpTimeoutMilliseconds.ToString(CultureInfo.InvariantCulture)} ms "
                        + "(UDP is fire-and-forget; most workloads never echo, so this is expected, not a failure).");
                }
            }
            else
            {
                Console.WriteLine("      --udp-timeout-ms 0: not waiting for a reply.");
            }
        }
        catch (SocketException exception)
        {
            Console.Error.WriteLine($"      FAILED: {exception.Message}");
            return ExitCodes.UdpSendFailed;
        }

        return null;
    }

    private static async Task<int?> InvokeLambdaAsync(
        YogabaseClient client,
        CliOptions options,
        CancellationToken cancellationToken)
    {
        var request = new YogabaseInvocationRequest
        {
            IdempotencyKey = $"server-path-probe-{Guid.NewGuid():N}",
            CorrelationId = Guid.NewGuid().ToString("D"),
            Deadline = DateTimeOffset.UtcNow.AddSeconds(25),
            Mode = YogabaseInvocationMode.Synchronous,
            InlinePayload = Encoding.UTF8.GetBytes(options.PayloadJson),
        };

        YogabaseInvocationResult result;
        try
        {
            result = await client.Functions
                .InvokeAsync(options.FunctionId, options.FunctionVersion, request, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Console.Error.WriteLine($"      FAILED: {exception.Message}");
            return ExitCodes.LambdaInvokeFailed;
        }

        Console.WriteLine($"      outcome: {result.Outcome}");
        Console.WriteLine($"      invocationId: {result.InvocationId}");
        Console.WriteLine($"      correlationId: {result.CorrelationId}");
        if (result.FailureCode is not null)
        {
            Console.WriteLine($"      failureCode: {result.FailureCode}");
        }
        if (result.ResultReference is not null)
        {
            Console.WriteLine($"      resultReference: {result.ResultReference}");
        }
        Console.WriteLine($"      result preview: {BoundedPreview.OfBytes(result.InlineResult, MaximumPreviewCharacters)}");
        return null;
    }

    private static async Task<int?> SubscribeRealtimeAsync(
        YogabaseClient client,
        CliOptions options,
        CancellationToken cancellationToken)
    {
        if (options.RealtimeTopic is null)
        {
            Console.WriteLine("[5/5] Realtime: no --realtime-topic given, skipping.");
            return null;
        }

        Console.WriteLine(
            $"[5/5] Realtime: subscribing to \"{options.RealtimeTopic}\" for "
            + $"{options.RealtimeSeconds.ToString(CultureInfo.InvariantCulture)}s...");

        if (!client.Configuration.IsAvailable("realtime-subscribe"))
        {
            Console.WriteLine(
                "      this installation's client-config does not report realtime-subscribe as available; "
                + "skipping (this is not a failure of this probe).");
            return null;
        }

        var receivedCount = 0;

        Task OnMessage(YogabaseRealtimeEvent realtimeEvent)
        {
            receivedCount++;
            Console.WriteLine(
                $"      event: topic={realtimeEvent.Topic ?? "(none)"} eventId={realtimeEvent.EventId} "
                + $"correlationId={realtimeEvent.CorrelationId} "
                + $"payload={BoundedPreview.OfJson(realtimeEvent.Payload, MaximumPreviewCharacters)}");
            return Task.CompletedTask;
        }

        client.Realtime.MessageReceived += OnMessage;
        try
        {
            await client.Realtime.ConnectAsync(cancellationToken).ConfigureAwait(false);
            await using (await client.Realtime.SubscribeAsync(options.RealtimeTopic, cancellationToken).ConfigureAwait(false))
            {
                Console.WriteLine(
                    $"      subscribed. Listening for {options.RealtimeSeconds.ToString(CultureInfo.InvariantCulture)}s...");
                await Task.Delay(TimeSpan.FromSeconds(options.RealtimeSeconds), cancellationToken).ConfigureAwait(false);
            }

            await client.Realtime.DisconnectAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (NotSupportedException exception)
        {
            Console.WriteLine($"      the realtime surface refused this capability ({exception.Message}); skipping.");
            return null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Console.Error.WriteLine($"      FAILED: {exception.Message}");
            return ExitCodes.RealtimeFailed;
        }
        finally
        {
            client.Realtime.MessageReceived -= OnMessage;
        }

        Console.WriteLine($"      received {receivedCount.ToString(CultureInfo.InvariantCulture)} event(s) in the window.");
        return null;
    }
}
