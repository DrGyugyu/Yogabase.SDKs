using System;
using System.Collections.Generic;
using System.Globalization;

namespace Yogabase.Samples.ServerPathProbe;

/// <summary>
/// Everything this probe needs besides the sign-in context in <see cref="SignInContext"/>. Nothing
/// here is secret, so all of it is an ordinary command-line argument.
/// </summary>
internal sealed record CliOptions(
    Uri GatewayUrl,
    string PublishableKey,
    string Service,
    string FunctionId,
    string FunctionVersion,
    string PayloadJson,
    string PortName,
    string? AddressOverride,
    string UdpMessage,
    int UdpTimeoutMilliseconds,
    string? RealtimeTopic,
    int RealtimeSeconds,
    bool AllowInsecureLoopback,
    bool StopServerAfter,
    int TimeoutSeconds);

internal static class CliParser
{
    internal const string HelpText =
        """
        Yogabase.ServerPathProbe -- an Editor-free walk of the Unity demo's server-side path

        Exercises, from the owner's own terminal and with the owner's own player sign-in, the same
        five things the Unity demo client would exercise, without building or running Unity:
          1. sign-in context read from environment variables (never the command line);
          2. POST /v1/servers/start through Yogabase.Yogabase.Client.Servers, printing serverId and
             networkPorts;
          3. one raw UDP datagram sent to the allocated host:externalPort;
          4. one invocation of a named Lambda through Yogabase.Yogabase.Client.Functions;
          5. an optional realtime subscription that logs received event names for N seconds.

        This tool is a sample, not a published package: it is never packed, and it is not part of
        the product build graph. It never signs in, claims a seat, or creates an account -- it only
        consumes a player access token the owner already holds.

        USAGE
          dotnet run --project samples/dotnet/Yogabase.ServerPathProbe -- [options]

        REQUIRED OPTIONS
          --gateway-url <url>        Application gateway origin, e.g. https://api.example.com or
                                      http://127.0.0.1:8082 (with --allow-insecure-loopback).
          --publishable-key <key>    Non-secret sy_pub_... client-configuration key for the project.
          --service <id>             Canonical hosted-service id to allocate, e.g. adaptive-sync-demo.
          --function-id <id>         Lambda function id to invoke.

        OPTIONAL OPTIONS
          --function-version <v>     Function version to invoke. Default: 1
          --payload <json>           Inline UTF-8 payload sent to the Lambda. Default: {}
          --port-name <name>         Named port in the allocation to dial over UDP. Default: game
          --address-override <host>  Substitute this host for the allocation's own Host when sending
                                      the UDP datagram (e.g. 127.0.0.1 against local k3d, whose
                                      allocations carry a node-internal address a macOS client cannot
                                      route to). Printed plainly whenever it is applied. Default: none
          --udp-message <text>       Payload bytes of the one UDP datagram sent. Default:
                                      yogabase-server-path-probe
          --udp-timeout-ms <n>       Bounded wait (0-10000) for an unsolicited UDP reply after
                                      sending; most workloads never send one, so a timeout here is
                                      normal and is reported, not treated as a failure. Default: 500
          --realtime-topic <topic>   When given, connects, subscribes to this topic, and logs
                                      received event names for --realtime-seconds. Omit to skip step 5
                                      entirely. Default: none (skipped)
          --realtime-seconds <n>     Bounded 1-300. Default: 5
          --allow-insecure-loopback  Allow an http://127.0.0.1 or http://localhost gateway.
          --stop-server              Release the allocated instance (Servers.StopAsync) before exit.
          --timeout-seconds <n>      Bounded 5-600 overall wall-clock bound for the whole run.
                                      Default: 60
          -h, --help                 Print this text and exit 0.

        ENVIRONMENT VARIABLES (sign-in context; never accepted as a command-line argument)
          YOGABASE_PLAYER_ACCESS_TOKEN    Required. The signed-in player's OIDC access token. This
                                           tool does not sign in itself; the owner obtains the token
                                           out of band and exports it before running the probe.
          YOGABASE_PLAYER_REFRESH_TOKEN   Optional. Lets the client refresh the access token if the
                                           run outlives it.
          YOGABASE_DEVELOPER_TOKEN        Optional. Accepted and its presence is reported; reserved
                                           for a future Control-Plane step (e.g. a build upload). None
                                           of this probe's five steps is a Control-Plane call, so the
                                           value is read only far enough to say whether it was set and
                                           is then discarded.
        No token value is ever printed, logged, or written to disk by this tool.

        EXIT CODES
          0   every requested step completed
          2   bad arguments, or a required environment variable was missing or blank
          3   the client bootstrap (fetching the gateway's client-config document) failed
          4   step 2, Servers.StartAsync, failed
          5   step 3, sending the UDP datagram, failed
          6   step 4, the Lambda invocation, failed (a returned Failed/Rejected outcome is printed,
              not treated as this failure -- only a transport or protocol exception is)
          7   step 5, the realtime subscription, failed (skipping because the installation does not
              publish the realtime-subscribe capability is not this -- it is reported and exits 0)
          8   --timeout-seconds was reached, or the run was cancelled (Ctrl+C), before every
              requested step finished
          70  an unexpected, otherwise uncategorized error
        """;

    /// <summary>Parses <paramref name="args"/>. Returns <see langword="null"/> when help was requested.</summary>
    internal static CliOptions? Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        string? gatewayUrl = null;
        string? publishableKey = null;
        string? service = null;
        string? functionId = null;
        var functionVersion = "1";
        var payloadJson = "{}";
        var portName = "game";
        string? addressOverride = null;
        var udpMessage = "yogabase-server-path-probe";
        var udpTimeoutMilliseconds = 500;
        string? realtimeTopic = null;
        var realtimeSeconds = 5;
        var allowInsecureLoopback = false;
        var stopServerAfter = false;
        var timeoutSeconds = 60;

        var index = 0;
        while (index < args.Count)
        {
            var token = args[index];
            switch (token)
            {
                case "-h":
                case "--help":
                    return null;
                case "--gateway-url":
                    gatewayUrl = RequireValue(args, ref index, token);
                    break;
                case "--publishable-key":
                    publishableKey = RequireValue(args, ref index, token);
                    break;
                case "--service":
                    service = RequireValue(args, ref index, token);
                    break;
                case "--function-id":
                    functionId = RequireValue(args, ref index, token);
                    break;
                case "--function-version":
                    functionVersion = RequireValue(args, ref index, token);
                    break;
                case "--payload":
                    payloadJson = RequireValue(args, ref index, token);
                    break;
                case "--port-name":
                    portName = RequireValue(args, ref index, token);
                    break;
                case "--address-override":
                    addressOverride = RequireValue(args, ref index, token);
                    break;
                case "--udp-message":
                    udpMessage = RequireValue(args, ref index, token);
                    break;
                case "--udp-timeout-ms":
                    udpTimeoutMilliseconds = RequireIntValue(args, ref index, token, 0, 10_000);
                    break;
                case "--realtime-topic":
                    realtimeTopic = RequireValue(args, ref index, token);
                    break;
                case "--realtime-seconds":
                    realtimeSeconds = RequireIntValue(args, ref index, token, 1, 300);
                    break;
                case "--allow-insecure-loopback":
                    allowInsecureLoopback = true;
                    index += 1;
                    break;
                case "--stop-server":
                    stopServerAfter = true;
                    index += 1;
                    break;
                case "--timeout-seconds":
                    timeoutSeconds = RequireIntValue(args, ref index, token, 5, 600);
                    break;
                default:
                    throw new CliUsageException($"Unrecognized argument '{token}'. Run with --help for usage.");
            }
        }

        if (string.IsNullOrWhiteSpace(gatewayUrl))
        {
            throw new CliUsageException("--gateway-url is required.");
        }
        if (!Uri.TryCreate(gatewayUrl, UriKind.Absolute, out var gateway))
        {
            throw new CliUsageException("--gateway-url must be an absolute URL.");
        }
        if (string.IsNullOrWhiteSpace(publishableKey) || !publishableKey.StartsWith("sy_pub_", StringComparison.Ordinal))
        {
            throw new CliUsageException("--publishable-key is required and must start with 'sy_pub_'.");
        }
        if (string.IsNullOrWhiteSpace(service))
        {
            throw new CliUsageException("--service is required.");
        }
        if (string.IsNullOrWhiteSpace(functionId))
        {
            throw new CliUsageException("--function-id is required.");
        }
        if (string.IsNullOrWhiteSpace(portName))
        {
            throw new CliUsageException("--port-name must not be blank.");
        }
        if (string.IsNullOrEmpty(udpMessage))
        {
            throw new CliUsageException("--udp-message must not be empty.");
        }
        if (!string.IsNullOrWhiteSpace(realtimeTopic) && realtimeSeconds >= timeoutSeconds)
        {
            throw new CliUsageException(
                "--realtime-seconds must be less than --timeout-seconds, or the overall run is cut off "
                + "mid-subscription and reported as a timeout instead of a clean result. Raise --timeout-seconds.");
        }

        return new CliOptions(
            gateway,
            publishableKey,
            service,
            functionId,
            functionVersion,
            payloadJson,
            portName,
            string.IsNullOrWhiteSpace(addressOverride) ? null : addressOverride,
            udpMessage,
            udpTimeoutMilliseconds,
            string.IsNullOrWhiteSpace(realtimeTopic) ? null : realtimeTopic,
            realtimeSeconds,
            allowInsecureLoopback,
            stopServerAfter,
            timeoutSeconds);
    }

    private static string RequireValue(IReadOnlyList<string> args, ref int index, string flag)
    {
        if (index + 1 >= args.Count)
        {
            throw new CliUsageException($"{flag} requires a value.");
        }

        var value = args[index + 1];
        index += 2;
        return value;
    }

    private static int RequireIntValue(IReadOnlyList<string> args, ref int index, string flag, int minimum, int maximum)
    {
        var raw = RequireValue(args, ref index, flag);
        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            || value < minimum
            || value > maximum)
        {
            throw new CliUsageException(
                $"{flag} must be an integer between {minimum.ToString(CultureInfo.InvariantCulture)} and "
                + $"{maximum.ToString(CultureInfo.InvariantCulture)}.");
        }

        return value;
    }
}
