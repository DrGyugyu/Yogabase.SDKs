using System;

namespace Yogabase.Samples.ServerPathProbe;

/// <summary>
/// A bad argument, a missing required value, or a missing/blank required environment variable.
/// The message is always safe to print: nothing that constructs this exception ever puts a token
/// value into its message.
/// </summary>
internal sealed class CliUsageException : Exception
{
    internal CliUsageException(string message) : base(message)
    {
    }
}
