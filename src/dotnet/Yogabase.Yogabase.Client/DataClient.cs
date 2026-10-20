using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Yogabase.Yogabase;

/// <summary>
/// The refusal codes of the client Data API, as the mirrored contract lists them under
/// <c>x-yogabase-data-api</c>. A refusal is a problem document whose <c>code</c> is one of these, read into
/// <see cref="YogabaseApiException.Code"/>; none quotes a row.
/// </summary>
public static class YogabaseDataErrors
{
    /// <summary>401: the token has no single canonical subject.</summary>
    public const string CanonicalSubjectRequired = "canonical_subject_required";

    /// <summary>401: the token names no project.</summary>
    public const string ProjectBindingRequired = "project_binding_required";

    /// <summary>401: the token was issued for another project.</summary>
    public const string ProjectBindingMismatch = "project_binding_mismatch";

    /// <summary>400: the request is not the documented shape.</summary>
    public const string RequestInvalid = "data_request_invalid";

    /// <summary>
    /// 413: the request document is over <see cref="YogabaseDataLimits.MaximumRequestBytes"/> as sent, or one
    /// value in it over <see cref="YogabaseDataLimits.MaximumValueBytes"/>;
    /// <see cref="YogabaseApiException.LimitBytes"/> and <see cref="YogabaseApiException.MaxBodyBytes"/> name both.
    /// </summary>
    public const string RequestTooLarge = "data_request_too_large";

    /// <summary>400: the operation is not one the route serves.</summary>
    public const string OperationUnknown = "data_operation_unknown";

    /// <summary>
    /// 403: no table of that name is exposed to the signed-in user. A table that does not exist, one the
    /// developer did not grant and one without row-level security are the same answer.
    /// </summary>
    public const string TableNotExposed = "data_table_not_exposed";

    /// <summary>403: a column the request names is not exposed to the signed-in user.</summary>
    public const string ColumnNotExposed = "data_column_not_exposed";

    /// <summary>403: the signed-in user may not run this operation on the table.</summary>
    public const string OperationNotPermitted = "data_operation_not_permitted";

    /// <summary>400: a filter is not one the API accepts.</summary>
    public const string FilterInvalid = "data_filter_invalid";

    /// <summary>400: an update or delete without a filter.</summary>
    public const string FilterRequired = "data_filter_required";

    /// <summary>400: a value the column's type does not take.</summary>
    public const string ValueInvalid = "data_value_invalid";

    /// <summary>400: a bound of <see cref="YogabaseDataLimits"/> exceeded.</summary>
    public const string LimitExceeded = "data_limit_exceeded";

    /// <summary>403: a policy's <c>WITH CHECK</c> refused the row.</summary>
    public const string RowNotPermitted = "data_row_not_permitted";

    /// <summary>409: a constraint refused the statement.</summary>
    public const string ConstraintViolated = "data_constraint_violated";

    /// <summary>409: a unique constraint refused the statement.</summary>
    public const string UniqueViolation = "data_unique_violation";

    /// <summary>409: a foreign key refused the statement.</summary>
    public const string ForeignKeyViolation = "data_foreign_key_violation";

    /// <summary>400: a required column was null.</summary>
    public const string NotNullViolation = "data_not_null_violation";

    /// <summary>400: a check constraint refused the row.</summary>
    public const string CheckViolation = "data_check_violation";

    /// <summary>400: an upsert's <c>onConflict</c> columns are not a primary key or unique index.</summary>
    public const string UpsertTargetInvalid = "data_upsert_target_invalid";

    /// <summary>413: the rows the statement returned are over <see cref="YogabaseDataLimits.MaximumResultBytes"/>.</summary>
    public const string ResultTooLarge = "data_result_too_large";

    /// <summary>409: a transaction conflict; the same request may be sent again.</summary>
    public const string TransactionConflict = "data_transaction_conflict";

    /// <summary>The statement ran past <see cref="YogabaseDataLimits.StatementTimeoutSeconds"/>.</summary>
    public const string StatementTimeout = "data_statement_timeout";

    /// <summary>503: the API has no identity to run as.</summary>
    public const string IdentityUnavailable = "data_api_identity_unavailable";

    /// <summary>503: the API could not reach the project database or read its schema.</summary>
    public const string Unavailable = "data_api_unavailable";

    /// <summary>Every code above, in the contract's order.</summary>
    public static IReadOnlyList<string> All { get; } = Array.AsReadOnly(new[]
    {
        CanonicalSubjectRequired, ProjectBindingRequired, ProjectBindingMismatch, RequestInvalid, RequestTooLarge,
        OperationUnknown, TableNotExposed, ColumnNotExposed, OperationNotPermitted, FilterInvalid, FilterRequired,
        ValueInvalid, LimitExceeded, RowNotPermitted, ConstraintViolated, UniqueViolation, ForeignKeyViolation,
        NotNullViolation, CheckViolation, UpsertTargetInvalid, ResultTooLarge, TransactionConflict, StatementTimeout,
        IdentityUnavailable, Unavailable,
    });
}

/// <summary>The bounds the client Data API states under <c>x-yogabase-data-api</c>, checked here before a request is sent.</summary>
public static class YogabaseDataLimits
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
    public const int MaximumReturnedRows = 1000;
    public const int MaximumResultBytes = 4 * 1024 * 1024;
    public const int MaximumValueBytes = 1024 * 1024;

    /// <summary>A whole request document as it is sent, escapes included: a value at its limit and 64 KiB of structure.</summary>
    public const int MaximumRequestBytes = MaximumValueBytes + (64 * 1024);

    public const int StatementTimeoutSeconds = 5;

    /// <summary>The longest table or column name the database keeps.</summary>
    public const int MaximumNameLength = 63;
}

/// <summary>The filter operators the client Data API accepts; anything else is refused before it is sent.</summary>
public static class YogabaseDataFilterOperators
{
    public const string Equal = "eq";
    public const string NotEqual = "neq";
    public const string GreaterThan = "gt";
    public const string GreaterThanOrEqual = "gte";
    public const string LessThan = "lt";
    public const string LessThanOrEqual = "lte";
    public const string In = "in";
    public const string Like = "like";
    public const string ILike = "ilike";
    public const string IsNull = "isNull";
    public const string NotNull = "notNull";

    /// <summary>Every operator, in the contract's order.</summary>
    public static IReadOnlyList<string> All { get; } = Array.AsReadOnly(new[]
    {
        Equal, NotEqual, GreaterThan, GreaterThanOrEqual, LessThan, LessThanOrEqual, In, Like, ILike, IsNull, NotNull,
    });
}

/// <summary>
/// One predicate of a select, update or delete. Build one with the factory for its operator; the value is
/// plain JSON, which the database parses with the column's own input function.
/// </summary>
public sealed class YogabaseDataFilter
{
    private YogabaseDataFilter(string column, string op, JsonElement? value)
    {
        Column = column;
        Operator = op;
        Value = value;
    }

    /// <summary>The column, exactly as the database names it.</summary>
    public string Column { get; }

    /// <summary>One of <see cref="YogabaseDataFilterOperators"/>.</summary>
    public string Operator { get; }

    /// <summary>
    /// The value: absent for <c>isNull</c> and <c>notNull</c>, an array of 1 to 100 non-null values for
    /// <c>in</c>, a string pattern for <c>like</c> and <c>ilike</c>, otherwise one non-null JSON value.
    /// </summary>
    public JsonElement? Value { get; }

    public static YogabaseDataFilter Eq<T>(string column, T value) => Compare(column, YogabaseDataFilterOperators.Equal, value);

    public static YogabaseDataFilter Neq<T>(string column, T value) => Compare(column, YogabaseDataFilterOperators.NotEqual, value);

    public static YogabaseDataFilter Gt<T>(string column, T value) => Compare(column, YogabaseDataFilterOperators.GreaterThan, value);

    public static YogabaseDataFilter Gte<T>(string column, T value) => Compare(column, YogabaseDataFilterOperators.GreaterThanOrEqual, value);

    public static YogabaseDataFilter Lt<T>(string column, T value) => Compare(column, YogabaseDataFilterOperators.LessThan, value);

    public static YogabaseDataFilter Lte<T>(string column, T value) => Compare(column, YogabaseDataFilterOperators.LessThanOrEqual, value);

    public static YogabaseDataFilter In<T>(string column, IEnumerable<T> values)
    {
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        var element = JsonSerializer.SerializeToElement(values.ToArray(), YogabaseDataJson.Values);
        return Of(column, YogabaseDataFilterOperators.In, element);
    }

    public static YogabaseDataFilter Like(string column, string pattern) => Compare(column, YogabaseDataFilterOperators.Like, pattern);

    public static YogabaseDataFilter ILike(string column, string pattern) => Compare(column, YogabaseDataFilterOperators.ILike, pattern);

    public static YogabaseDataFilter IsNull(string column) => Of(column, YogabaseDataFilterOperators.IsNull, null);

    public static YogabaseDataFilter NotNull(string column) => Of(column, YogabaseDataFilterOperators.NotNull, null);

    /// <summary>A filter from its parts, checked as the gateway would check them.</summary>
    public static YogabaseDataFilter Of(string column, string op, JsonElement? value)
    {
        YogabaseDataValidation.Name(column, nameof(column));
        if (op is null || !YogabaseDataFilterOperators.All.Contains(op, StringComparer.Ordinal))
        {
            throw new ArgumentException("The filter operator is not one the Data API accepts.", nameof(op));
        }

        var present = value is { ValueKind: not JsonValueKind.Undefined };
        switch (op)
        {
            case YogabaseDataFilterOperators.IsNull or YogabaseDataFilterOperators.NotNull:
                if (present)
                {
                    throw new ArgumentException($"A {op} filter carries no value.", nameof(value));
                }

                return new YogabaseDataFilter(column, op, null);
            case YogabaseDataFilterOperators.In:
                if (!present
                    || value!.Value.ValueKind != JsonValueKind.Array
                    || value.Value.GetArrayLength() is < 1 or > YogabaseDataLimits.MaximumInValues
                    || value.Value.EnumerateArray().Any(static item => item.ValueKind == JsonValueKind.Null))
                {
                    throw new ArgumentException("An in filter carries an array of 1 to 100 non-null values.", nameof(value));
                }

                break;
            case YogabaseDataFilterOperators.Like or YogabaseDataFilterOperators.ILike:
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

        return new YogabaseDataFilter(column, op, value!.Value.Clone());
    }

    private static YogabaseDataFilter Compare<T>(string column, string op, T value) =>
        Of(column, op, JsonSerializer.SerializeToElement(value, YogabaseDataJson.Values));
}

/// <summary>One ordering term of a select.</summary>
public sealed class YogabaseDataOrder
{
    /// <summary>The column, exactly as the database names it.</summary>
    public string Column { get; set; } = string.Empty;

    /// <summary>Descending rather than ascending.</summary>
    public bool Descending { get; set; }

    /// <summary>Nulls first or last; <see langword="null"/> keeps the database's default (last ascending, first descending).</summary>
    public bool? NullsFirst { get; set; }

    public static YogabaseDataOrder Ascending(string column) => new() { Column = column };

    public static YogabaseDataOrder DescendingBy(string column) => new() { Column = column, Descending = true };
}

/// <summary>
/// A select. No column list (or <c>["*"]</c>) reads every column the signed-in user may read; no limit reads
/// <see cref="YogabaseDataLimits.DefaultLimit"/> rows.
/// </summary>
public sealed class YogabaseDataSelectRequest
{
    public IReadOnlyList<string>? Columns { get; set; }

    public IReadOnlyList<YogabaseDataFilter>? Filters { get; set; }

    public IReadOnlyList<YogabaseDataOrder>? Order { get; set; }

    /// <summary>1 to <see cref="YogabaseDataLimits.MaximumLimit"/>, or <see langword="null"/> for the default.</summary>
    public int? Limit { get; set; }

    /// <summary>0 to <see cref="YogabaseDataLimits.MaximumOffset"/>, or <see langword="null"/> for none.</summary>
    public int? Offset { get; set; }
}

/// <summary>An insert of 1 to <see cref="YogabaseDataLimits.MaximumWriteRows"/> rows, each a JSON object of column to value.</summary>
public sealed class YogabaseDataInsertRequest
{
    public IReadOnlyList<JsonElement> Rows { get; set; } = Array.Empty<JsonElement>();

    /// <summary>The columns to read back from the written rows, <c>["*"]</c> for every readable one, or none.</summary>
    public IReadOnlyList<string>? Returning { get; set; }
}

/// <summary>
/// An insert whose conflict on <see cref="OnConflict"/> (the columns of a primary key or unique index) updates
/// every other supplied column, or does nothing when <see cref="IgnoreDuplicates"/> is set.
/// </summary>
public sealed class YogabaseDataUpsertRequest
{
    public IReadOnlyList<JsonElement> Rows { get; set; } = Array.Empty<JsonElement>();

    public IReadOnlyList<string> OnConflict { get; set; } = Array.Empty<string>();

    public bool IgnoreDuplicates { get; set; }

    public IReadOnlyList<string>? Returning { get; set; }
}

/// <summary>An update of the rows the filters and the signed-in user's policies select; at least one filter is required.</summary>
public sealed class YogabaseDataUpdateRequest
{
    /// <summary>A JSON object of column to new value.</summary>
    public JsonElement Values { get; set; }

    public IReadOnlyList<YogabaseDataFilter> Filters { get; set; } = Array.Empty<YogabaseDataFilter>();

    public IReadOnlyList<string>? Returning { get; set; }
}

/// <summary>A delete of the rows the filters and the signed-in user's policies select; at least one filter is required.</summary>
public sealed class YogabaseDataDeleteRequest
{
    public IReadOnlyList<YogabaseDataFilter> Filters { get; set; } = Array.Empty<YogabaseDataFilter>();

    public IReadOnlyList<string>? Returning { get; set; }
}

/// <summary>
/// One answer: the rows, each as the database rendered it with <c>to_json</c> (timestamps in UTC), how many
/// rows the statement touched, and, for a select, whether another page exists past this one.
/// </summary>
public sealed class YogabaseDataResult
{
    internal YogabaseDataResult(IReadOnlyList<JsonElement> rows, int affectedRows, bool hasMore)
    {
        Rows = rows;
        AffectedRows = affectedRows;
        HasMore = hasMore;
    }

    public IReadOnlyList<JsonElement> Rows { get; }

    public int AffectedRows { get; }

    public bool HasMore { get; }

    /// <summary>Each row read as <typeparamref name="T"/>, with web defaults (camelCase, case-insensitive) unless options are given.</summary>
    public IReadOnlyList<T> RowsAs<T>(JsonSerializerOptions? options = null) =>
        Array.AsReadOnly(Rows.Select(row => row.Deserialize<T>(options ?? YogabaseDataJson.Values)!).ToArray());
}

/// <summary>
/// The client Data API: a signed-in user's reads and writes of the project's exposed tables, under row-level
/// security as that user. It is available where the bootstrap's requested <c>data-rest</c> row says so (the
/// client names the row in <c>X-Yogabase-Accept-Capabilities</c>); a call on an installation that does not
/// serve it is refused with <see cref="NotSupportedException"/> before anything is sent. There is no raw SQL,
/// no function call and no way to choose a role here: the table is the only thing a request names.
/// </summary>
public interface IYogabaseData
{
    Task<YogabaseDataResult> SelectAsync(string table, YogabaseDataSelectRequest? request = null, CancellationToken cancellationToken = default);

    Task<YogabaseDataResult> InsertAsync(string table, YogabaseDataInsertRequest request, CancellationToken cancellationToken = default);

    Task<YogabaseDataResult> UpsertAsync(string table, YogabaseDataUpsertRequest request, CancellationToken cancellationToken = default);

    Task<YogabaseDataResult> UpdateAsync(string table, YogabaseDataUpdateRequest request, CancellationToken cancellationToken = default);

    Task<YogabaseDataResult> DeleteAsync(string table, YogabaseDataDeleteRequest request, CancellationToken cancellationToken = default);
}

internal static class YogabaseDataJson
{
    /// <summary>How a caller's values become JSON, and how rows are read back by <see cref="YogabaseDataResult.RowsAs{T}"/>.</summary>
    internal static readonly JsonSerializerOptions Values = new(JsonSerializerDefaults.Web);

    /// <summary>The wire document: exact member names, nothing null written.</summary>
    internal static readonly JsonSerializerOptions Wire = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}

internal static class YogabaseDataValidation
{
    // The contract's pattern, with .NET's \z for its end: a JSON Schema "$" matches only at the end, where
    // .NET's also matches before a final newline.
    private static readonly Regex NamePattern = new("^[A-Za-z_][A-Za-z0-9_$]{0,62}\\z", RegexOptions.CultureInvariant);

    internal static void Name(string? value, string parameterName)
    {
        if (value is null || !NamePattern.IsMatch(value))
        {
            throw new ArgumentException(
                "A table or column name is 1 to 63 ASCII letters, digits, '_' or '$', not starting with a digit or '$'.",
                parameterName);
        }
    }

    internal static void Columns(IReadOnlyList<string>? columns, string parameterName)
    {
        if (columns is null)
        {
            return;
        }

        if (columns.Count == 1 && string.Equals(columns[0], "*", StringComparison.Ordinal))
        {
            return;
        }

        if (columns.Count is < 1 or > YogabaseDataLimits.MaximumColumns
            || columns.Distinct(StringComparer.Ordinal).Count() != columns.Count)
        {
            throw new ArgumentException("A column list names 1 to 128 distinct columns, or is [\"*\"].", parameterName);
        }

        foreach (var column in columns)
        {
            Name(column, parameterName);
        }
    }

    internal static void Filters(IReadOnlyList<YogabaseDataFilter>? filters, bool required, string parameterName)
    {
        if (filters is null || filters.Count == 0)
        {
            if (required)
            {
                throw new ArgumentException("An update or delete needs at least one filter: there is no unfiltered write.", parameterName);
            }

            return;
        }

        if (filters.Count > YogabaseDataLimits.MaximumFilters || filters.Any(static filter => filter is null))
        {
            throw new ArgumentException("A request carries at most 32 filters.", parameterName);
        }
    }

    internal static void Rows(IReadOnlyList<JsonElement>? rows, string parameterName)
    {
        if (rows is null || rows.Count is < 1 or > YogabaseDataLimits.MaximumWriteRows)
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
            if (++count > YogabaseDataLimits.MaximumColumns)
            {
                throw new ArgumentException("A row names at most 128 columns.", parameterName);
            }
        }
    }
}

/// <summary>The request documents exactly as the gateway reads them (it refuses an unknown member).</summary>
internal static class YogabaseDataWire
{
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

    internal sealed class Result
    {
        [JsonPropertyName("rows")]
        public List<JsonElement>? Rows { get; set; }

        [JsonPropertyName("affectedRows")]
        public int? AffectedRows { get; set; }

        [JsonPropertyName("hasMore")]
        public bool? HasMore { get; set; }
    }

    internal static IReadOnlyList<Filter>? Filters(IReadOnlyList<YogabaseDataFilter>? filters) =>
        filters is null || filters.Count == 0
            ? null
            : filters.Select(static filter => new Filter { Column = filter.Column, Op = filter.Operator, Value = filter.Value }).ToArray();

    internal static IReadOnlyList<Order>? Orders(IReadOnlyList<YogabaseDataOrder>? order) =>
        order is null || order.Count == 0
            ? null
            : order.Select(static term => new Order { Column = term.Column, Descending = term.Descending, NullsFirst = term.NullsFirst }).ToArray();

    /// <summary>The select document, the shape a Lambda's typed call carries too.</summary>
    internal static Select ForSelect(YogabaseDataSelectRequest? request)
    {
        request ??= new YogabaseDataSelectRequest();
        YogabaseDataValidation.Columns(request.Columns, nameof(request));
        YogabaseDataValidation.Filters(request.Filters, required: false, nameof(request));
        if (request.Order is { Count: > YogabaseDataLimits.MaximumOrderTerms }
            || request.Order?.Any(static term => term is null) == true)
        {
            throw new ArgumentException("A select orders by at most 8 terms.", nameof(request));
        }

        foreach (var term in request.Order ?? Array.Empty<YogabaseDataOrder>())
        {
            YogabaseDataValidation.Name(term.Column, nameof(request));
        }

        if (request.Limit is < 1 or > YogabaseDataLimits.MaximumLimit)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "A select reads 1 to 1000 rows per page.");
        }

        if (request.Offset is < 0 or > YogabaseDataLimits.MaximumOffset)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "A select's offset is 0 to 100000.");
        }

        return new Select
        {
            Columns = request.Columns,
            Filters = Filters(request.Filters),
            Order = Orders(request.Order),
            Limit = request.Limit,
            Offset = request.Offset,
        };
    }

    internal static Insert ForInsert(YogabaseDataInsertRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        YogabaseDataValidation.Rows(request.Rows, nameof(request));
        YogabaseDataValidation.Columns(request.Returning, nameof(request));
        return new Insert { Rows = request.Rows, Returning = request.Returning };
    }

    internal static Upsert ForUpsert(YogabaseDataUpsertRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        YogabaseDataValidation.Rows(request.Rows, nameof(request));
        if (request.OnConflict is null
            || request.OnConflict.Count is < 1 or > YogabaseDataLimits.MaximumConflictColumns
            || request.OnConflict.Distinct(StringComparer.Ordinal).Count() != request.OnConflict.Count)
        {
            throw new ArgumentException("An upsert names 1 to 16 distinct conflict columns.", nameof(request));
        }

        foreach (var column in request.OnConflict)
        {
            YogabaseDataValidation.Name(column, nameof(request));
        }

        YogabaseDataValidation.Columns(request.Returning, nameof(request));
        return new Upsert
        {
            Rows = request.Rows,
            OnConflict = request.OnConflict,
            IgnoreDuplicates = request.IgnoreDuplicates,
            Returning = request.Returning,
        };
    }

    internal static Update ForUpdate(YogabaseDataUpdateRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        YogabaseDataValidation.Row(request.Values, nameof(request));
        YogabaseDataValidation.Filters(request.Filters, required: true, nameof(request));
        YogabaseDataValidation.Columns(request.Returning, nameof(request));
        return new Update { Values = request.Values, Filters = Filters(request.Filters)!, Returning = request.Returning };
    }

    internal static Delete ForDelete(YogabaseDataDeleteRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        YogabaseDataValidation.Filters(request.Filters, required: true, nameof(request));
        YogabaseDataValidation.Columns(request.Returning, nameof(request));
        return new Delete { Filters = Filters(request.Filters)!, Returning = request.Returning };
    }

    /// <summary>
    /// The request bytes, held to <see cref="YogabaseDataLimits.MaximumRequestBytes"/> as sent, which is how the
    /// gateway counts them: a request over it is refused here rather than read by the gateway and refused there.
    /// </summary>
    internal static byte[] Serialize<T>(T document)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(document, YogabaseDataJson.Wire);
        if (bytes.Length > YogabaseDataLimits.MaximumRequestBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(document),
                $"A Data API request is one JSON document of at most {YogabaseDataLimits.MaximumRequestBytes} bytes as sent.");
        }

        return bytes;
    }

    /// <summary>A success document, read strictly: the three members, rows no more than the result bound allows.</summary>
    internal static YogabaseDataResult Read(Result? wire, int status)
    {
        if (wire?.Rows is null
            || wire.AffectedRows is null or < 0
            || wire.HasMore is null
            || wire.Rows.Count > YogabaseDataLimits.MaximumReturnedRows)
        {
            throw new YogabaseApiException(status is 0 ? 502 : status, "response_invalid", "Yogabase returned an invalid Data API result.");
        }

        return new YogabaseDataResult(
            Array.AsReadOnly(wire.Rows.Select(static row => row.Clone()).ToArray()),
            wire.AffectedRows.Value,
            wire.HasMore.Value);
    }
}

internal sealed class YogabaseDataClient : IYogabaseData
{
    private readonly YogabaseClientConfiguration configuration;
    private readonly YogabaseHttpTransport transport;

    internal YogabaseDataClient(YogabaseClientConfiguration configuration, YogabaseHttpTransport transport)
    {
        this.configuration = configuration;
        this.transport = transport;
    }

    public Task<YogabaseDataResult> SelectAsync(string table, YogabaseDataSelectRequest? request = null, CancellationToken cancellationToken = default) =>
        SendAsync(table, YogabaseRoutes.DataSelect, () => YogabaseDataWire.ForSelect(request), cancellationToken);

    public Task<YogabaseDataResult> InsertAsync(string table, YogabaseDataInsertRequest request, CancellationToken cancellationToken = default) =>
        SendAsync(table, YogabaseRoutes.DataInsert, () => YogabaseDataWire.ForInsert(request), cancellationToken);

    public Task<YogabaseDataResult> UpsertAsync(string table, YogabaseDataUpsertRequest request, CancellationToken cancellationToken = default) =>
        SendAsync(table, YogabaseRoutes.DataUpsert, () => YogabaseDataWire.ForUpsert(request), cancellationToken);

    public Task<YogabaseDataResult> UpdateAsync(string table, YogabaseDataUpdateRequest request, CancellationToken cancellationToken = default) =>
        SendAsync(table, YogabaseRoutes.DataUpdate, () => YogabaseDataWire.ForUpdate(request), cancellationToken);

    public Task<YogabaseDataResult> DeleteAsync(string table, YogabaseDataDeleteRequest request, CancellationToken cancellationToken = default) =>
        SendAsync(table, YogabaseRoutes.DataDelete, () => YogabaseDataWire.ForDelete(request), cancellationToken);

    private async Task<YogabaseDataResult> SendAsync<TDocument>(
        string table,
        Func<string, string> route,
        Func<TDocument> build,
        CancellationToken cancellationToken)
    {
        YogabaseClient.EnsureCapability(configuration, YogabaseCapability.DataRest);
        YogabaseDataValidation.Name(table, nameof(table));
        var bytes = YogabaseDataWire.Serialize(build());
        var response = await transport.SendAsync(
            HttpMethod.Post,
            route(table),
            body: null,
            authenticated: true,
            cancellationToken,
            configure: request =>
            {
                var content = new ByteArrayContent(bytes);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
                request.Content = content;
            }).ConfigureAwait(false);
        var status = (int)response.StatusCode;
        if (status != 200)
        {
            response.Dispose();
            throw new YogabaseApiException(502, "response_invalid", $"Yogabase answered a Data API request with HTTP {status}.");
        }

        var wire = await YogabaseHttpTransport.ReadJsonAsync<YogabaseDataWire.Result>(response, cancellationToken).ConfigureAwait(false);
        return YogabaseDataWire.Read(wire, status);
    }
}
