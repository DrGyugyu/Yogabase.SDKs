namespace Yogabase.Samples.ServerPathProbe;

/// <summary>
/// One exit code per failure point, so an owner or a script can tell which step of the walk did
/// not complete without parsing console text. Documented verbatim in <c>--help</c> and the sample
/// README; keep both in sync with this file.
/// </summary>
internal static class ExitCodes
{
    /// <summary>Every requested step completed without an unhandled transport or protocol error.</summary>
    internal const int Success = 0;

    /// <summary>Bad or missing command-line arguments, or a required environment variable was absent or blank.</summary>
    internal const int UsageError = 2;

    /// <summary>The client bootstrap (<c>YogabaseClient.CreateAsync</c>, the gateway's client-config document) failed.</summary>
    internal const int ClientBootstrapFailed = 3;

    /// <summary>Step 2, <c>Servers.StartAsync</c>, threw (no address delivered, refusal, or transport failure).</summary>
    internal const int ServersStartFailed = 4;

    /// <summary>Step 3, sending the one UDP datagram, threw a socket exception.</summary>
    internal const int UdpSendFailed = 5;

    /// <summary>Step 4, <c>Functions.InvokeAsync</c>, threw (a returned Failed/Rejected outcome is not this — it is printed, not thrown).</summary>
    internal const int LambdaInvokeFailed = 6;

    /// <summary>Step 5, the realtime subscription, threw something other than the capability-absent case, which is a skip, not a failure.</summary>
    internal const int RealtimeFailed = 7;

    /// <summary>The overall <c>--timeout-seconds</c> bound was reached before every requested step finished.</summary>
    internal const int TimedOut = 8;

    /// <summary>Anything else unhandled.</summary>
    internal const int UnexpectedError = 70;
}
