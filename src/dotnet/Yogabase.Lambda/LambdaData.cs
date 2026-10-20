using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Yogabase.Lambda;

/// <summary>
/// The refusal codes of the Lambda's typed data API, as <see cref="LambdaApiException.Code"/> carries them: the
/// client Data API's codes plus the ones only a Lambda's call can meet (a database function, the caller identity,
/// the runtime's database). Every code is the runtime's own spelling.
/// </summary>
public static class LambdaDataRefusalCodes
{
    public const string RequestInvalid = "data_request_invalid";
    public const string RequestTooLarge = "data_request_too_large";
    public const string OperationUnknown = "data_operation_unknown";
    public const string TableNotExposed = "data_table_not_exposed";
    public const string ColumnNotExposed = "data_column_not_exposed";
    public const string OperationNotPermitted = "data_operation_not_permitted";
    public const string FilterInvalid = "data_filter_invalid";
    public const string FilterRequired = "data_filter_required";
    public const string ValueInvalid = "data_value_invalid";
    public const string LimitExceeded = "data_limit_exceeded";
    public const string RowNotPermitted = "data_row_not_permitted";
    public const string ConstraintViolated = "data_constraint_violated";
    public const string UniqueViolation = "data_unique_violation";
    public const string ForeignKeyViolation = "data_foreign_key_violation";
    public const string NotNullViolation = "data_not_null_violation";
    public const string CheckViolation = "data_check_violation";
    public const string UpsertTargetInvalid = "data_upsert_target_invalid";

    /// <summary>No callable database function of that name; a Lambda's <see cref="LambdaData.RpcAsync"/> only.</summary>
    public const string FunctionNotFound = "data_function_not_found";

    /// <summary>The function has more than one overload, so it cannot be called by named arguments.</summary>
    public const string FunctionAmbiguous = "data_function_ambiguous";

    /// <summary>The arguments do not name the function's input arguments.</summary>
    public const string FunctionArgumentsInvalid = "data_function_arguments_invalid";

    public const string ResultTooLarge = "data_result_too_large";
    public const string TransactionConflict = "data_transaction_conflict";
    public const string StatementTimeout = "data_statement_timeout";
    public const string IdentityUnavailable = "data_api_identity_unavailable";

    /// <summary>
    /// 403: <see cref="LambdaData.AsCaller"/> on an invocation with no signed-in caller (a schedule, an actor tick,
    /// a workflow step or a broker delivery). Never answered by running as the full-access identity instead.
    /// </summary>
    public const string CallerIdentityUnavailable = "data_caller_identity_unavailable";

    public const string Unavailable = "data_api_unavailable";

    /// <summary>503: this runtime's database is not available to the data API.</summary>
    public const string DatabaseUnavailable = "data_database_unavailable";

    /// <summary>The answer was not the documented document; the wrapper's own code.</summary>
    public const string ResponseInvalid = "data_response_invalid";
}

/// <summary>The bounds the typed data API holds a request to; the same as the client Data API's.</summary>
public static class LambdaDataLimits
{
    public const int DefaultLimit = 100;
    public const int MaximumLimit = 1000;
    public const int MaximumOffset = 100_000;
    public const int MaximumColumns = 128;
    public const int MaximumFilters = 32;
    public const int MaximumInValues = 100;
    public const int MaximumOrderTerms = 8;
    public const int MaximumWriteRows = 500;
    public const int MaximumConflictColumns = 16;
    public const int MaximumRpcArguments = 32;
    public const int MaximumReturnedRows = 1000;
    public const int MaximumResultBytes = 4 * 1024 * 1024;
    public const int MaximumValueBytes = 1024 * 1024;

    /// <summary>One request document as sent: a value at its limit and 64 KiB of structure around it.</summary>
    public const int MaximumRequestBytes = MaximumValueBytes + (64 * 1024);
}

/// <summary>One predicate of a select, update or delete; build it with the factory for its operator.</summary>
public sealed class LambdaDataFilter
{
    private static readonly string[] Operators = ["eq", "neq", "gt", "gte", "lt", "lte", "in", "like", "ilike", "isNull", "notNull"];

    private LambdaDataFilter(string column, string op, JsonElement? value)
    {
        Column = column;
        Operator = op;
        Value = value;
    }

    public string Column { get; }

    /// <summary><c>eq neq gt gte lt lte in like ilike isNull notNull</c>.</summary>
    public string Operator { get; }

    public JsonElement? Value { get; }

    public static LambdaDataFilter Eq<T>(string column, T value) => Compare(column, "eq", value);

    public static LambdaDataFilter Neq<T>(string column, T value) => Compare(column, "neq", value);

    public static LambdaDataFilter Gt<T>(string column, T value) => Compare(column, "gt", value);

    public static LambdaDataFilter Gte<T>(string column, T value) => Compare(column, "gte", value);

    public static LambdaDataFilter Lt<T>(string column, T value) => Compare(column, "lt", value);

    public static LambdaDataFilter Lte<T>(string column, T value) => Compare(column, "lte", value);

    public static LambdaDataFilter In<T>(string column, IEnumerable<T> values)
    {
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        return Of(column, "in", JsonSerializer.SerializeToElement(values.ToArray(), LambdaDataJson.Values));
    }

    public static LambdaDataFilter Like(string column, string pattern) => Compare(column, "like", pattern);

    public static LambdaDataFilter ILike(string column, string pattern) => Compare(column, "ilike", pattern);

    public static LambdaDataFilter IsNull(string column) => Of(column, "isNull", null);

    public static LambdaDataFilter NotNull(string column) => Of(column, "notNull", null);

    /// <summary>A filter from its parts, checked as the runtime would check them.</summary>
    public static LambdaDataFilter Of(string column, string op, JsonElement? value)
    {
        LambdaDataValidation.Name(column, nameof(column));
        if (op is null || !Operators.Contains(op, StringComparer.Ordinal))
        {
            throw new ArgumentException("The filter operator is not one the data API accepts.", nameof(op));
        }

        var present = value is { ValueKind: not JsonValueKind.Undefined };
        switch (op)
        {
            case "isNull" or "notNull":
                if (present)
                {
                    throw new ArgumentException($"A {op} filter carries no value.", nameof(value));
                }

                return new LambdaDataFilter(column, op, null);
            case "in":
                if (!present
                    || value!.Value.ValueKind != JsonValueKind.Array
                    || value.Value.GetArrayLength() is < 1 or > LambdaDataLimits.MaximumInValues
                    || value.Value.EnumerateArray().Any(static item => item.ValueKind == JsonValueKind.Null))
                {
                    throw new ArgumentException("An in filter carries an array of 1 to 100 non-null values.", nameof(value));
                }

                break;
            case "like" or "ilike":
                if (!present || value!.Value.ValueKind != JsonValueKind.String)
                {
                    throw new ArgumentException($"A {op} filter carries a string pattern.", nameof(value));
                }

                break;
            default:
                if (!present || value!.Value.ValueKind == JsonValueKind.Null)
                {
                    throw new ArgumentException($"An {op} filter carries one non-null value; use IsNull or NotNull for null.", nameof(value));
                }

                break;
        }

        return new LambdaDataFilter(column, op, value!.Value.Clone());
    }

    private static LambdaDataFilter Compare<T>(string column, string op, T value) =>
        Of(column, op, JsonSerializer.SerializeToElement(value, LambdaDataJson.Values));
}

/// <summary>One ordering term of a select.</summary>
public sealed class LambdaDataOrder
{
    public string Column { get; set; } = string.Empty;

    public bool Descending { get; set; }

    /// <summary>Null keeps the database's default: nulls last ascending, first descending.</summary>
    public bool? NullsFirst { get; set; }

    public static LambdaDataOrder Ascending(string column) => new() { Column = column };

    public static LambdaDataOrder DescendingBy(string column) => new() { Column = column, Descending = true };
}

/// <summary>A select: no column list (or <c>["*"]</c>) reads every column the identity may read.</summary>
public sealed class LambdaDataSelectRequest
{
    public IReadOnlyList<string>? Columns { get; set; }

    public IReadOnlyList<LambdaDataFilter>? Filters { get; set; }

    public IReadOnlyList<LambdaDataOrder>? Order { get; set; }

    public int? Limit { get; set; }

    public int? Offset { get; set; }
}

/// <summary>An insert of 1 to 500 rows, each a JSON object of column to value.</summary>
public sealed class LambdaDataInsertRequest
{
    public IReadOnlyList<JsonElement> Rows { get; set; } = Array.Empty<JsonElement>();

    public IReadOnlyList<string>? Returning { get; set; }
}

/// <summary>An insert whose conflict on <see cref="OnConflict"/> updates the other supplied columns, or does nothing.</summary>
public sealed class LambdaDataUpsertRequest
{
    public IReadOnlyList<JsonElement> Rows { get; set; } = Array.Empty<JsonElement>();

    public IReadOnlyList<string> OnConflict { get; set; } = Array.Empty<string>();

    public bool IgnoreDuplicates { get; set; }

    public IReadOnlyList<string>? Returning { get; set; }
}

/// <summary>An update; at least one filter is required.</summary>
public sealed class LambdaDataUpdateRequest
{
    public JsonElement Values { get; set; }

    public IReadOnlyList<LambdaDataFilter> Filters { get; set; } = Array.Empty<LambdaDataFilter>();

    public IReadOnlyList<string>? Returning { get; set; }
}

/// <summary>A delete; at least one filter is required.</summary>
public sealed class LambdaDataDeleteRequest
{
    public IReadOnlyList<LambdaDataFilter> Filters { get; set; } = Array.Empty<LambdaDataFilter>();

    public IReadOnlyList<string>? Returning { get; set; }
}

/// <summary>One answer: the rows as the database rendered them, the rows touched, and whether a select has another page.</summary>
public sealed class LambdaDataResult
{
    internal LambdaDataResult(IReadOnlyList<JsonElement> rows, int affectedRows, bool hasMore)
    {
        Rows = rows;
        AffectedRows = affectedRows;
        HasMore = hasMore;
    }

    public IReadOnlyList<JsonElement> Rows { get; }

    public int AffectedRows { get; }

    public bool HasMore { get; }

    /// <summary>Each row read as <typeparamref name="T"/>, with web defaults unless options are given.</summary>
    public IReadOnlyList<T> RowsAs<T>(JsonSerializerOptions? options = null) =>
        Array.AsReadOnly(Rows.Select(row => row.Deserialize<T>(options ?? LambdaDataJson.Values)!).ToArray());
}

/// <summary>
/// The typed data API the runtime registers for a Lambda (owner decision, 2026-09-17): one request document in and
/// one answer document out through the runtime's delegate, with the client Data API's validation, bounds and
/// refusal codes, plus <see cref="RpcAsync"/> for a database function. A thin serializer over that delegate, not a
/// wider privilege: no SQL text, no role name and no connection string travels through it.
/// </summary>
/// <remarks>
/// <para>
/// By default a request runs as the Lambda's full-access <c>yogabase_lambda</c> identity, which bypasses
/// row-level security and never changes the schema. <see cref="AsCaller"/> runs it as the signed-in caller under
/// row-level security, which is the answer the caller would get; an invocation with no caller refuses it with
/// <see cref="LambdaDataRefusalCodes.CallerIdentityUnavailable"/>. The caller scope is correctness, not a
/// boundary against the Lambda's own author.
/// </para>
/// </remarks>
public sealed class LambdaData
{
    /// <summary>The identity a request runs as by default: the full-access Lambda identity.</summary>
    public const string LambdaIdentity = "lambda";

    /// <summary>The identity of <see cref="AsCaller"/>: the signed-in caller, under row-level security.</summary>
    public const string CallerIdentity = "caller";

    private readonly Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask<ReadOnlyMemory<byte>>> send;

    internal LambdaData(Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask<ReadOnlyMemory<byte>>> send, string identity)
    {
        this.send = send;
        Identity = identity;
    }

    /// <summary><see cref="LambdaIdentity"/> or <see cref="CallerIdentity"/>.</summary>
    public string Identity { get; }

    /// <summary>The same data API, run as the signed-in caller under row-level security.</summary>
    public LambdaData AsCaller() => Identity == CallerIdentity ? this : new LambdaData(send, CallerIdentity);

    /// <summary>The operations on one table, as this view's identity.</summary>
    public LambdaDataTable From(string table)
    {
        LambdaDataValidation.Name(table, nameof(table));
        return new LambdaDataTable(this, table);
    }

    /// <summary>
    /// Calls one database function by its named arguments (a function with an unnamed input argument, or with
    /// more than one overload, cannot be called this way) and reads what it returns as rows.
    /// </summary>
    /// <param name="function">The function's name in the project's schema.</param>
    /// <param name="arguments">A JSON object of argument name to value, or <see langword="null"/> for none.</param>
    /// <param name="limit">The most rows to read back, 1 to 1000, or <see langword="null"/> for the default.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    public Task<LambdaDataResult> RpcAsync(
        string function,
        JsonElement? arguments = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        LambdaDataValidation.Name(function, nameof(function));
        if (arguments is { ValueKind: not JsonValueKind.Undefined } given)
        {
            if (given.ValueKind != JsonValueKind.Object || given.EnumerateObject().Count() > LambdaDataLimits.MaximumRpcArguments)
            {
                throw new ArgumentException("A function's arguments are one JSON object of at most 32 named arguments.", nameof(arguments));
            }

            foreach (var argument in given.EnumerateObject())
            {
                LambdaDataValidation.Name(argument.Name, nameof(arguments));
            }
        }

        if (limit is < 1 or > LambdaDataLimits.MaximumLimit)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "A function call reads 1 to 1000 rows.");
        }

        var request = new LambdaDataWire.Rpc { Arguments = arguments is { ValueKind: not JsonValueKind.Undefined } ? arguments : null, Limit = limit };
        return SendAsync("rpc", table: null, function, request, cancellationToken);
    }

    internal async Task<LambdaDataResult> SendAsync<TRequest>(
        string operation,
        string? table,
        string? function,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var document = JsonSerializer.SerializeToUtf8Bytes(
            new LambdaDataWire.Envelope<TRequest>
            {
                Identity = Identity,
                Operation = operation,
                Table = table,
                Function = function,
                Request = request,
            },
            LambdaDataJson.Wire);
        if (document.Length > LambdaDataLimits.MaximumRequestBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                $"A data API request is one JSON document of at most {LambdaDataLimits.MaximumRequestBytes} bytes as sent.");
        }

        var answer = await send(document, cancellationToken).ConfigureAwait(false);
        return LambdaDataWire.Read(answer);
    }
}

/// <summary>The five operations on one table, as the identity of the <see cref="LambdaData"/> it came from.</summary>
public sealed class LambdaDataTable
{
    private readonly LambdaData data;

    internal LambdaDataTable(LambdaData data, string table)
    {
        this.data = data;
        Table = table;
    }

    public string Table { get; }

    public Task<LambdaDataResult> SelectAsync(LambdaDataSelectRequest? request = null, CancellationToken cancellationToken = default) =>
        data.SendAsync("select", Table, function: null, LambdaDataWire.ForSelect(request), cancellationToken);

    public Task<LambdaDataResult> InsertAsync(LambdaDataInsertRequest request, CancellationToken cancellationToken = default) =>
        data.SendAsync("insert", Table, function: null, LambdaDataWire.ForInsert(request), cancellationToken);

    public Task<LambdaDataResult> UpsertAsync(LambdaDataUpsertRequest request, CancellationToken cancellationToken = default) =>
        data.SendAsync("upsert", Table, function: null, LambdaDataWire.ForUpsert(request), cancellationToken);

    public Task<LambdaDataResult> UpdateAsync(LambdaDataUpdateRequest request, CancellationToken cancellationToken = default) =>
        data.SendAsync("update", Table, function: null, LambdaDataWire.ForUpdate(request), cancellationToken);

    public Task<LambdaDataResult> DeleteAsync(LambdaDataDeleteRequest request, CancellationToken cancellationToken = default) =>
        data.SendAsync("delete", Table, function: null, LambdaDataWire.ForDelete(request), cancellationToken);
}

internal static class LambdaDataJson
{
    internal static readonly JsonSerializerOptions Values = new(JsonSerializerDefaults.Web);

    internal static readonly JsonSerializerOptions Wire = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}

internal static class LambdaDataValidation
{
    // The client contract's name rule, with .NET's \z for its end ("$" in .NET also matches before a final newline).
    private static readonly Regex NamePattern = new("^[A-Za-z_][A-Za-z0-9_$]{0,62}\\z", RegexOptions.CultureInvariant);

    internal static void Name(string? value, string parameterName)
    {
        if (value is null || !NamePattern.IsMatch(value))
        {
            throw new ArgumentException(
                "A table, column or function name is 1 to 63 ASCII letters, digits, '_' or '$', not starting with a digit or '$'.",
                parameterName);
        }
    }

    internal static void Columns(IReadOnlyList<string>? columns, string parameterName)
    {
        if (columns is null || columns.Count == 1 && string.Equals(columns[0], "*", StringComparison.Ordinal))
        {
            return;
        }

        if (columns.Count is < 1 or > LambdaDataLimits.MaximumColumns
            || columns.Distinct(StringComparer.Ordinal).Count() != columns.Count)
        {
            throw new ArgumentException("A column list names 1 to 128 distinct columns, or is [\"*\"].", parameterName);
        }

        foreach (var column in columns)
        {
            Name(column, parameterName);
        }
    }

    internal static void Filters(IReadOnlyList<LambdaDataFilter>? filters, bool required, string parameterName)
    {
        if (filters is null || filters.Count == 0)
        {
            if (required)
            {
                throw new ArgumentException("An update or delete needs at least one filter: there is no unfiltered write.", parameterName);
            }

            return;
        }

        if (filters.Count > LambdaDataLimits.MaximumFilters || filters.Any(static filter => filter is null))
        {
            throw new ArgumentException("A request carries at most 32 filters.", parameterName);
        }
    }

    internal static void Rows(IReadOnlyList<JsonElement>? rows, string parameterName)
    {
        if (rows is null || rows.Count is < 1 or > LambdaDataLimits.MaximumWriteRows)
        {
            throw new ArgumentException("A write carries 1 to 500 rows.", parameterName);
        }

        foreach (var row in rows)
        {
            Row(row, parameterName);
        }
    }

    internal static void Row(JsonElement row, string parameterName)
    {
        if (row.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("A row is one JSON object of column to value.", parameterName);
        }

        var count = 0;
        foreach (var property in row.EnumerateObject())
        {
            Name(property.Name, parameterName);
            if (++count > LambdaDataLimits.MaximumColumns)
            {
                throw new ArgumentException("A row names at most 128 columns.", parameterName);
            }
        }
    }
}

/// <summary>The documents exactly as the runtime reads them (it refuses an unknown member).</summary>
internal static class LambdaDataWire
{
    internal sealed class Envelope<TRequest>
    {
        [JsonPropertyName("identity")]
        public string Identity { get; set; } = LambdaData.LambdaIdentity;

        [JsonPropertyName("operation")]
        public string Operation { get; set; } = string.Empty;

        [JsonPropertyName("table")]
        public string? Table { get; set; }

        [JsonPropertyName("function")]
        public string? Function { get; set; }

        [JsonPropertyName("request")]
        public TRequest? Request { get; set; }
    }

    internal sealed class Filter
    {
        [JsonPropertyName("column")]
        public string Column { get; set; } = string.Empty;

        [JsonPropertyName("op")]
        public string Op { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public JsonElement? Value { get; set; }
    }

    internal sealed class Order
    {
        [JsonPropertyName("column")]
        public string Column { get; set; } = string.Empty;

        [JsonPropertyName("descending")]
        public bool Descending { get; set; }

        [JsonPropertyName("nullsFirst")]
        public bool? NullsFirst { get; set; }
    }

    internal sealed class Select
    {
        [JsonPropertyName("columns")]
        public IReadOnlyList<string>? Columns { get; set; }

        [JsonPropertyName("filters")]
        public IReadOnlyList<Filter>? Filters { get; set; }

        [JsonPropertyName("order")]
        public IReadOnlyList<Order>? Order { get; set; }

        [JsonPropertyName("limit")]
        public int? Limit { get; set; }

        [JsonPropertyName("offset")]
        public int? Offset { get; set; }
    }

    internal sealed class Insert
    {
        [JsonPropertyName("rows")]
        public IReadOnlyList<JsonElement> Rows { get; set; } = Array.Empty<JsonElement>();

        [JsonPropertyName("returning")]
        public IReadOnlyList<string>? Returning { get; set; }
    }

    internal sealed class Upsert
    {
        [JsonPropertyName("rows")]
        public IReadOnlyList<JsonElement> Rows { get; set; } = Array.Empty<JsonElement>();

        [JsonPropertyName("onConflict")]
        public IReadOnlyList<string> OnConflict { get; set; } = Array.Empty<string>();

        [JsonPropertyName("ignoreDuplicates")]
        public bool IgnoreDuplicates { get; set; }

        [JsonPropertyName("returning")]
        public IReadOnlyList<string>? Returning { get; set; }
    }

    internal sealed class Update
    {
        [JsonPropertyName("values")]
        public JsonElement Values { get; set; }

        [JsonPropertyName("filters")]
        public IReadOnlyList<Filter> Filters { get; set; } = Array.Empty<Filter>();

        [JsonPropertyName("returning")]
        public IReadOnlyList<string>? Returning { get; set; }
    }

    internal sealed class Delete
    {
        [JsonPropertyName("filters")]
        public IReadOnlyList<Filter> Filters { get; set; } = Array.Empty<Filter>();

        [JsonPropertyName("returning")]
        public IReadOnlyList<string>? Returning { get; set; }
    }

    internal sealed class Rpc
    {
        [JsonPropertyName("arguments")]
        public JsonElement? Arguments { get; set; }

        [JsonPropertyName("limit")]
        public int? Limit { get; set; }
    }

    private static Filter[]? Filters(IReadOnlyList<LambdaDataFilter>? filters) =>
        filters is null || filters.Count == 0
            ? null
            : filters.Select(static filter => new Filter { Column = filter.Column, Op = filter.Operator, Value = filter.Value }).ToArray();

    internal static Select ForSelect(LambdaDataSelectRequest? request)
    {
        request ??= new LambdaDataSelectRequest();
        LambdaDataValidation.Columns(request.Columns, nameof(request));
        LambdaDataValidation.Filters(request.Filters, required: false, nameof(request));
        if (request.Order is { Count: > LambdaDataLimits.MaximumOrderTerms } || request.Order?.Any(static term => term is null) == true)
        {
            throw new ArgumentException("A select orders by at most 8 terms.", nameof(request));
        }

        foreach (var term in request.Order ?? Array.Empty<LambdaDataOrder>())
        {
            LambdaDataValidation.Name(term.Column, nameof(request));
        }

        if (request.Limit is < 1 or > LambdaDataLimits.MaximumLimit)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "A select reads 1 to 1000 rows per page.");
        }

        if (request.Offset is < 0 or > LambdaDataLimits.MaximumOffset)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "A select's offset is 0 to 100000.");
        }

        return new Select
        {
            Columns = request.Columns,
            Filters = Filters(request.Filters),
            Order = request.Order is null || request.Order.Count == 0
                ? null
                : request.Order.Select(static term => new Order { Column = term.Column, Descending = term.Descending, NullsFirst = term.NullsFirst }).ToArray(),
            Limit = request.Limit,
            Offset = request.Offset,
        };
    }

    internal static Insert ForInsert(LambdaDataInsertRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        LambdaDataValidation.Rows(request.Rows, nameof(request));
        LambdaDataValidation.Columns(request.Returning, nameof(request));
        return new Insert { Rows = request.Rows, Returning = request.Returning };
    }

    internal static Upsert ForUpsert(LambdaDataUpsertRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        LambdaDataValidation.Rows(request.Rows, nameof(request));
        if (request.OnConflict is null
            || request.OnConflict.Count is < 1 or > LambdaDataLimits.MaximumConflictColumns
            || request.OnConflict.Distinct(StringComparer.Ordinal).Count() != request.OnConflict.Count)
        {
            throw new ArgumentException("An upsert names 1 to 16 distinct conflict columns.", nameof(request));
        }

        foreach (var column in request.OnConflict)
        {
            LambdaDataValidation.Name(column, nameof(request));
        }

        LambdaDataValidation.Columns(request.Returning, nameof(request));
        return new Upsert { Rows = request.Rows, OnConflict = request.OnConflict, IgnoreDuplicates = request.IgnoreDuplicates, Returning = request.Returning };
    }

    internal static Update ForUpdate(LambdaDataUpdateRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        LambdaDataValidation.Row(request.Values, nameof(request));
        LambdaDataValidation.Filters(request.Filters, required: true, nameof(request));
        LambdaDataValidation.Columns(request.Returning, nameof(request));
        return new Update { Values = request.Values, Filters = Filters(request.Filters)!, Returning = request.Returning };
    }

    internal static Delete ForDelete(LambdaDataDeleteRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        LambdaDataValidation.Filters(request.Filters, required: true, nameof(request));
        LambdaDataValidation.Columns(request.Returning, nameof(request));
        return new Delete { Filters = Filters(request.Filters)!, Returning = request.Returning };
    }

    /// <summary>
    /// The answer document. Every answer says <c>status</c>; a success also carries <c>rows</c>, <c>affectedRows</c>
    /// and <c>hasMore</c>, and a refusal <c>code</c> and <c>message</c>, which is thrown as a
    /// <see cref="LambdaApiException"/> with that status and code.
    /// </summary>
    internal static LambdaDataResult Read(ReadOnlyMemory<byte> answer)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(answer, new JsonDocumentOptions { MaxDepth = 64 });
        }
        catch (JsonException)
        {
            throw Invalid();
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("status", out var statusElement)
                || statusElement.ValueKind != JsonValueKind.Number
                || !statusElement.TryGetInt32(out var status))
            {
                throw Invalid();
            }

            if (status != 200)
            {
                var code = root.TryGetProperty("code", out var codeElement) && codeElement.ValueKind == JsonValueKind.String
                    ? codeElement.GetString()
                    : null;
                var message = root.TryGetProperty("message", out var messageElement) && messageElement.ValueKind == JsonValueKind.String
                    ? messageElement.GetString()
                    : null;
                if (string.IsNullOrEmpty(code) || status is < 400 or > 599)
                {
                    throw Invalid();
                }

                throw new LambdaApiException(status, code!, message ?? "The data API refused the request.");
            }

            if (!root.TryGetProperty("rows", out var rows)
                || rows.ValueKind != JsonValueKind.Array
                || rows.GetArrayLength() > LambdaDataLimits.MaximumReturnedRows
                || !root.TryGetProperty("affectedRows", out var affected)
                || affected.ValueKind != JsonValueKind.Number
                || !affected.TryGetInt32(out var affectedRows)
                || affectedRows < 0
                || !root.TryGetProperty("hasMore", out var hasMore)
                || hasMore.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                throw Invalid();
            }

            return new LambdaDataResult(
                Array.AsReadOnly(rows.EnumerateArray().Select(static row => row.Clone()).ToArray()),
                affectedRows,
                hasMore.GetBoolean());
        }
    }

    private static LambdaApiException Invalid() =>
        new(502, LambdaDataRefusalCodes.ResponseInvalid, "The data API answered with a document that is not the documented shape.");
}
