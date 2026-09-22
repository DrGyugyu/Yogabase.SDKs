using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Yogabase.Lambda;

/// <summary>
/// One bounded stream read, spelled as the runtime API's <c>POST streams/{topic}/read</c> body:
/// exactly one of <see cref="FromOffset"/> and <see cref="FromTimestamp"/>, optionally one
/// <see cref="Partition"/> (without it every partition is read from the same position rule), and the
/// runtime's bounds on <see cref="Limit"/> (1 to 100 records), <see cref="MaxBytes"/> (1 byte to
/// 1 MiB) and <see cref="WaitSeconds"/> (0 to 5). A request outside them never leaves the Lambda.
/// </summary>
public sealed record LambdaStreamReadRequest
{
    public const int MaximumRecords = 100;
    public const int MaximumBytes = 1024 * 1024;
    public const int MaximumWaitSeconds = 5;

    /// <summary>The highest partition index a read may name.</summary>
    public const int MaximumPartitionIndex = 1023;

    public int? Partition { get; init; }
    public long? FromOffset { get; init; }
    public DateTimeOffset? FromTimestamp { get; init; }
    public int Limit { get; init; } = 16;
    public int MaxBytes { get; init; } = 256 * 1024;
    public int WaitSeconds { get; init; }

    /// <summary>A read starting at <paramref name="offset"/> on one partition, or on every partition.</summary>
    public static LambdaStreamReadRequest StartingAt(long offset, int? partition = null) =>
        new() { FromOffset = offset, Partition = partition };

    /// <summary>A read starting at the first record at or after <paramref name="timestamp"/>.</summary>
    public static LambdaStreamReadRequest StartingAt(DateTimeOffset timestamp, int? partition = null) =>
        new() { FromTimestamp = timestamp, Partition = partition };

    internal void Validate()
    {
        if ((FromOffset is null) == (FromTimestamp is null))
            throw new ArgumentException("A stream read starts at exactly one of fromOffset or fromTimestamp.");
        if (Partition is < 0 or > MaximumPartitionIndex)
            throw new ArgumentOutOfRangeException(nameof(Partition), "A partition index is 0 to 1023.");
        if (FromOffset is < 0)
            throw new ArgumentOutOfRangeException(nameof(FromOffset), "An offset is not negative.");
        if (FromTimestamp is { } timestamp && timestamp < DateTimeOffset.FromUnixTimeMilliseconds(0))
            throw new ArgumentOutOfRangeException(nameof(FromTimestamp), "A timestamp is not before the Unix epoch.");
        if (Limit is < 1 or > MaximumRecords)
            throw new ArgumentOutOfRangeException(nameof(Limit), "A read returns 1 to 100 records.");
        if (MaxBytes is < 1 or > MaximumBytes)
            throw new ArgumentOutOfRangeException(nameof(MaxBytes), "A read is bounded to 1 byte to 1 MiB.");
        if (WaitSeconds is < 0 or > MaximumWaitSeconds)
            throw new ArgumentOutOfRangeException(nameof(WaitSeconds), "A read waits 0 to 5 seconds.");
    }
}

/// <summary>
/// The records of one read in partition then offset order, the offset each partition read continues
/// from, and whether every partition read was at its end when the read finished.
/// </summary>
public sealed class LambdaStreamReadResult
{
    public IReadOnlyList<LambdaStreamRecord> Records { get; init; } = Array.Empty<LambdaStreamRecord>();
    public IReadOnlyList<LambdaStreamPosition> Next { get; init; } = Array.Empty<LambdaStreamPosition>();
    public bool EndOfTopic { get; init; }
}

/// <summary>One record exactly as the broker stores it. A record published without a key has no key.</summary>
public sealed class LambdaStreamRecord
{
    private byte[]? value;
    private byte[]? key;

    public int Partition { get; init; }
    public long Offset { get; init; }
    public long TimestampUnixMs { get; init; }
    public string? KeyBase64 { get; init; }
    public string ValueBase64 { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();

    /// <summary>The broker timestamp.</summary>
    [JsonIgnore]
    public DateTimeOffset Timestamp => DateTimeOffset.FromUnixTimeMilliseconds(TimestampUnixMs);

    /// <summary>The record key's bytes, or <see langword="null"/> for a keyless record.</summary>
    [JsonIgnore]
    public byte[]? Key => KeyBase64 is null ? null : key ??= Convert.FromBase64String(KeyBase64);

    /// <summary>The record value's bytes; a CDC event is <see cref="LambdaCdcEvent.Parse(ReadOnlySpan{byte})"/>-able.</summary>
    [JsonIgnore]
    public byte[] Value => value ??= Convert.FromBase64String(ValueBase64);

    internal bool IsWellFormed()
    {
        if (Partition is < 0 or > LambdaStreamReadRequest.MaximumPartitionIndex || Offset < 0 || ValueBase64 is null || Headers is null)
        {
            return false;
        }
        try
        {
            _ = Value;
            _ = Key;
        }
        catch (FormatException)
        {
            return false;
        }
        return true;
    }
}

/// <summary>The offset a reader continues from on one partition.</summary>
public sealed class LambdaStreamPosition
{
    public int Partition { get; init; }
    public long Offset { get; init; }
}

/// <summary>A project stream's logical topic and its partition count after a declaration.</summary>
public sealed class LambdaStreamDescription
{
    public const int MaximumDeclaredPartitions = 32;

    public string Topic { get; init; } = string.Empty;
    public int Partitions { get; init; }
}

/// <summary>
/// One message leased from a project durable queue. <see cref="Redelivered"/> is the broker's own
/// flag: the message was delivered before and not acknowledged (a negative acknowledgement with
/// requeue, an expired lease, or a lost broker channel).
/// </summary>
public sealed class LambdaQueueDelivery
{
    public const int MaximumMessagesPerReceive = 10;
    public const int MinimumLeaseSeconds = 5;
    public const int MaximumLeaseSeconds = 300;
    public const int MaximumReceiveWaitSeconds = 20;

    /// <summary>The runtime API's answer to settling a delivery that is not leased (404).</summary>
    public const string UnknownDelivery = "queue_delivery_unknown";

    private byte[]? body;

    public string DeliveryId { get; init; } = string.Empty;
    public string BodyBase64 { get; init; } = string.Empty;
    public bool Redelivered { get; init; }
    public DateTimeOffset LeaseExpiresAt { get; init; }

    /// <summary>The message body's bytes.</summary>
    [JsonIgnore]
    public byte[] Body => body ??= Convert.FromBase64String(BodyBase64);

    /// <summary>A delivery id is 32 lowercase hexadecimal characters.</summary>
    public static bool IsDeliveryId(string? value) =>
        value is { Length: 32 } && value.All(static character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    internal bool IsWellFormed()
    {
        if (!IsDeliveryId(DeliveryId) || BodyBase64 is null || LeaseExpiresAt == default)
        {
            return false;
        }
        try
        {
            _ = Body;
        }
        catch (FormatException)
        {
            return false;
        }
        return true;
    }
}

internal sealed class LambdaQueueReceiveAnswer
{
    public IReadOnlyList<LambdaQueueDelivery>? Deliveries { get; init; }
}
