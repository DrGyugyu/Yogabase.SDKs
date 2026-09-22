using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Yogabase.Lambda;

/// <summary>What a change data capture event records.</summary>
public enum LambdaCdcOperation
{
    Insert,
    Update,
    Delete,
    Truncate,
}

/// <summary>
/// One <c>yogabase.cdc.v1</c> change event, as the installation's CDC publisher writes it to a project
/// stream: the record value a consumer trigger hands a Lambda, or a <see cref="LambdaStreamRecord.Value"/>.
/// </summary>
/// <remarks>
/// <para>
/// Values are the database's text output, and SQL NULL is <see langword="null"/>. An update carries
/// <see cref="OldKey"/> only when it changed the key, and lists in <see cref="UnchangedColumns"/> the
/// columns the engine omitted from <see cref="NewValues"/>. When the event did not fit the broker's
/// payload limit, <see cref="Oversized"/> is set and <see cref="NewValues"/> is absent (re-read the row
/// by <see cref="Key"/>); when even the key did not fit, <see cref="KeyOmitted"/> is set as well and
/// <see cref="Key"/> is absent. A truncate names its table only.
/// </para>
/// <para>
/// The parser checks the whole v1 shape, per operation, and throws <see cref="FormatException"/>
/// naming the first thing wrong; a property it does not know is ignored, so an additive field in a
/// later publisher does not break it. Dispatch on <see cref="SchemaVersion"/>: a v2 event is refused.
/// </para>
/// </remarks>
public sealed class LambdaCdcEvent
{
    /// <summary>The event schema this parser reads.</summary>
    public const string SchemaVersion = "yogabase.cdc.v1";

    private static readonly IReadOnlyList<string> NoColumns = Array.AsReadOnly(Array.Empty<string>());

    private LambdaCdcEvent(
        string streamId, string table, LambdaCdcOperation operation, string lsn, string commitLsn,
        DateTimeOffset commitTimestamp, long transactionId, int sequence,
        IReadOnlyDictionary<string, string?>? key, IReadOnlyDictionary<string, string?>? oldKey,
        IReadOnlyDictionary<string, string?>? newValues, IReadOnlyList<string> unchangedColumns,
        bool oversized, bool keyOmitted)
    {
        StreamId = streamId;
        Table = table;
        var separator = table.IndexOf('.');
        TableSchema = table.Substring(0, separator);
        TableName = table.Substring(separator + 1);
        Operation = operation;
        Lsn = lsn;
        CommitLsn = commitLsn;
        CommitTimestamp = commitTimestamp;
        TransactionId = transactionId;
        Sequence = sequence;
        Key = key;
        OldKey = oldKey;
        NewValues = newValues;
        UnchangedColumns = unchangedColumns;
        Oversized = oversized;
        KeyOmitted = keyOmitted;
    }

    public string StreamId { get; }

    /// <summary><c>schema.table</c>.</summary>
    public string Table { get; }

    public string TableSchema { get; }
    public string TableName { get; }
    public LambdaCdcOperation Operation { get; }

    /// <summary>The change's position, <c>X/X</c>.</summary>
    public string Lsn { get; }

    /// <summary>The transaction's final position, <c>X/X</c>.</summary>
    public string CommitLsn { get; }

    public DateTimeOffset CommitTimestamp { get; }
    public long TransactionId { get; }

    /// <summary>The change's 0-based position within its transaction.</summary>
    public int Sequence { get; }

    /// <summary>Primary key column to value; absent for a truncate and when <see cref="KeyOmitted"/>.</summary>
    public IReadOnlyDictionary<string, string?>? Key { get; }

    /// <summary>The key before an update that changed it; otherwise absent.</summary>
    public IReadOnlyDictionary<string, string?>? OldKey { get; }

    /// <summary>Every column the engine sent for an insert or update; absent when <see cref="Oversized"/>.</summary>
    public IReadOnlyDictionary<string, string?>? NewValues { get; }

    /// <summary>Update columns the engine omitted as unchanged; empty otherwise.</summary>
    public IReadOnlyList<string> UnchangedColumns { get; }

    public bool Oversized { get; }
    public bool KeyOmitted { get; }

    /// <summary>Parses one UTF-8 event.</summary>
    /// <exception cref="FormatException">The bytes are not a well-formed <c>yogabase.cdc.v1</c> event.</exception>
    public static LambdaCdcEvent Parse(ReadOnlySpan<byte> utf8Json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(utf8Json.ToArray(), new JsonDocumentOptions { MaxDepth = 16 });
        }
        catch (JsonException exception)
        {
            throw new FormatException("A yogabase.cdc.v1 event is one JSON object.", exception);
        }
        using (document)
        {
            return Read(document.RootElement);
        }
    }

    /// <inheritdoc cref="Parse(ReadOnlySpan{byte})"/>
    public static LambdaCdcEvent Parse(string json)
    {
        if (json is null) throw new ArgumentNullException(nameof(json));
        return Parse(Encoding.UTF8.GetBytes(json));
    }

    /// <summary>Parses one UTF-8 event, or answers <see langword="false"/> without throwing.</summary>
    public static bool TryParse(ReadOnlySpan<byte> utf8Json, out LambdaCdcEvent? change)
    {
        try
        {
            change = Parse(utf8Json);
            return true;
        }
        catch (FormatException)
        {
            change = null;
            return false;
        }
    }

    private static LambdaCdcEvent Read(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object) throw Invalid("the event is not a JSON object");
        if (RequiredString(root, "schema") != SchemaVersion) throw Invalid("schema is not " + SchemaVersion);
        var streamId = RequiredString(root, "streamId");
        if (streamId.Length is 0 or > 128) throw Invalid("streamId is empty or too long");
        var table = RequiredString(root, "table");
        if (!LambdaCdcRecordKey.IsTableName(table)) throw Invalid("table is not a lowercase schema.table");
        var operation = RequiredString(root, "operation") switch
        {
            "insert" => LambdaCdcOperation.Insert,
            "update" => LambdaCdcOperation.Update,
            "delete" => LambdaCdcOperation.Delete,
            "truncate" => LambdaCdcOperation.Truncate,
            _ => throw Invalid("operation is not insert, update, delete or truncate"),
        };
        var lsn = RequiredString(root, "lsn");
        var commitLsn = RequiredString(root, "commitLsn");
        if (!IsLsn(lsn) || !IsLsn(commitLsn)) throw Invalid("lsn and commitLsn are X/X hexadecimal positions");
        var commitTimestamp = root.TryGetProperty("commitTimestamp", out var timestamp)
            && timestamp.ValueKind == JsonValueKind.String
            && DateTimeOffset.TryParse(timestamp.GetString(), CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsedTimestamp)
            ? parsedTimestamp
            : throw Invalid("commitTimestamp is not an ISO 8601 timestamp");
        var transactionId = root.TryGetProperty("transactionId", out var transaction)
            && transaction.ValueKind == JsonValueKind.Number && transaction.TryGetInt64(out var parsedTransaction)
            && parsedTransaction is >= 0 and <= uint.MaxValue
            ? parsedTransaction
            : throw Invalid("transactionId is not a 32-bit transaction id");
        var sequence = root.TryGetProperty("sequence", out var position)
            && position.ValueKind == JsonValueKind.Number && position.TryGetInt32(out var parsedSequence)
            && parsedSequence >= 0
            ? parsedSequence
            : throw Invalid("sequence is not a non-negative position");

        var key = OptionalValues(root, "key");
        var oldKey = OptionalValues(root, "oldKey");
        var newValues = OptionalValues(root, "newValues");
        var oversized = OptionalFlag(root, "oversized");
        var keyOmitted = OptionalFlag(root, "keyOmitted");
        IReadOnlyList<string> unchanged = NoColumns;
        if (root.TryGetProperty("unchangedColumns", out var columns))
        {
            if (columns.ValueKind != JsonValueKind.Array
                || columns.EnumerateArray().Any(static column => column.ValueKind != JsonValueKind.String || column.GetString()!.Length == 0))
                throw Invalid("unchangedColumns is not a list of column names");
            unchanged = Array.AsReadOnly(columns.EnumerateArray().Select(static column => column.GetString()!).ToArray());
        }

        switch (operation)
        {
            case LambdaCdcOperation.Truncate:
                if (key is not null || oldKey is not null || newValues is not null || oversized || keyOmitted || unchanged.Count > 0)
                    throw Invalid("a truncate carries no key, values or flags");
                break;
            case LambdaCdcOperation.Delete:
                if (newValues is not null || oversized || oldKey is not null || unchanged.Count > 0)
                    throw Invalid("a delete carries only its key");
                RequireKey(key, keyOmitted);
                break;
            default:
                RequireKey(key, keyOmitted);
                if (keyOmitted && !oversized) throw Invalid("keyOmitted is set without oversized");
                if (oversized ? newValues is not null : newValues is null)
                    throw Invalid("newValues must be present exactly when the event is not oversized");
                if (operation == LambdaCdcOperation.Insert && (oldKey is not null || unchanged.Count > 0))
                    throw Invalid("an insert carries no oldKey or unchangedColumns");
                if (keyOmitted && oldKey is not null) throw Invalid("oldKey is present without its key");
                break;
        }

        return new LambdaCdcEvent(streamId, table, operation, lsn, commitLsn, commitTimestamp, transactionId,
            sequence, key, oldKey, newValues, unchanged, oversized, keyOmitted);
    }

    private static void RequireKey(IReadOnlyDictionary<string, string?>? key, bool keyOmitted)
    {
        if (keyOmitted ? key is not null : key is null)
            throw Invalid("key must be present exactly when keyOmitted is not set");
    }

    private static string RequiredString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()!
            : throw Invalid(name + " is missing or not a string");

    private static bool OptionalFlag(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value)) return false;
        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw Invalid(name + " is not a boolean"),
        };
    }

    private static ReadOnlyDictionary<string, string?>? OptionalValues(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value)) return null;
        if (value.ValueKind != JsonValueKind.Object) throw Invalid(name + " is not an object of column values");
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var column in value.EnumerateObject())
        {
            if (column.Name.Length == 0 || values.ContainsKey(column.Name))
                throw Invalid(name + " names a column twice or not at all");
            values[column.Name] = column.Value.ValueKind switch
            {
                JsonValueKind.String => column.Value.GetString(),
                JsonValueKind.Null => null,
                _ => throw Invalid(name + " values are text or null"),
            };
        }
        return new ReadOnlyDictionary<string, string?>(values);
    }

    // PostgreSQL's %X/%X: two unpadded hexadecimal halves.
    private static bool IsLsn(string value)
    {
        var separator = value.IndexOf('/');
        return separator is > 0 and <= 8
            && value.Length - separator - 1 is > 0 and <= 8
            && value.Where((_, index) => index != separator).All(static character =>
                character is >= '0' and <= '9' or >= 'A' and <= 'F' or >= 'a' and <= 'f');
    }

    private static FormatException Invalid(string reason) => new("Not a yogabase.cdc.v1 event: " + reason + ".");
}

/// <summary>
/// A CDC record's Kafka key: <c>schema.table:</c> followed by 32 lowercase hexadecimal characters of a
/// SHA-256 over the row's canonical key. It is stable per row — every change to one row lands on one
/// partition in commit order — and carries no row value. A truncate's key hashes the literal text
/// <c>truncate</c>.
/// </summary>
public sealed class LambdaCdcRecordKey
{
    private const int HashLength = 32;

    private LambdaCdcRecordKey(string table, string rowHash)
    {
        Table = table;
        RowHash = rowHash;
    }

    /// <summary><c>schema.table</c>.</summary>
    public string Table { get; }

    /// <summary>The 32 lowercase hexadecimal characters after the table.</summary>
    public string RowHash { get; }

    /// <summary>Parses the key bytes of one CDC record.</summary>
    /// <exception cref="FormatException">The bytes are not a CDC record key.</exception>
    public static LambdaCdcRecordKey Parse(ReadOnlySpan<byte> utf8) =>
        TryParse(utf8, out var key)
            ? key!
            : throw new FormatException("A CDC record key is schema.table: followed by 32 lowercase hexadecimal characters.");

    /// <inheritdoc cref="Parse(ReadOnlySpan{byte})"/>
    public static LambdaCdcRecordKey Parse(string value)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        return Parse(Encoding.UTF8.GetBytes(value));
    }

    /// <summary>Parses the key bytes of one CDC record, or answers <see langword="false"/>.</summary>
    public static bool TryParse(ReadOnlySpan<byte> utf8, out LambdaCdcRecordKey? key)
    {
        key = null;
        if (utf8.Length is < 3 + 1 + HashLength or > 127 + 1 + HashLength)
        {
            return false;
        }
        var text = Encoding.UTF8.GetString(utf8.ToArray());
        var separator = text.LastIndexOf(':');
        if (separator < 0 || text.Length - separator - 1 != HashLength)
        {
            return false;
        }
        var table = text.Substring(0, separator);
        var hash = text.Substring(separator + 1);
        if (!IsTableName(table) || !hash.All(static character => character is >= '0' and <= '9' or >= 'a' and <= 'f'))
        {
            return false;
        }
        key = new LambdaCdcRecordKey(table, hash);
        return true;
    }

    public override string ToString() => Table + ":" + RowHash;

    /// <summary>
    /// Exactly <c>schema.table</c>, each part an unquoted lowercase identifier: a letter or underscore,
    /// then up to 62 letters, digits or underscores.
    /// </summary>
    internal static bool IsTableName(string? value)
    {
        if (value is null) return false;
        var separator = value.IndexOf('.');
        return separator > 0
            && IsIdentifier(value.Substring(0, separator))
            && IsIdentifier(value.Substring(separator + 1));
    }

    private static bool IsIdentifier(string part) =>
        part.Length is >= 1 and <= 63
        && part[0] is >= 'a' and <= 'z' or '_'
        && part.All(static character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_');
}
