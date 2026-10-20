using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Yogabase.Yogabase;

namespace Yogabase.Tests;

/// <summary>
/// Wire-shape coverage for the client Data API (<see cref="IYogabaseData"/>) against a fake handler: the exact
/// request each operation sends, what it refuses before anything is sent, and how it reads the gateway's answer
/// and refusals. The shapes are the mirrored client contract's (<c>/v1/data/{table}/…</c>, the Data request
/// schemas and <c>x-yogabase-data-api</c>), which <see cref="DataContractTests"/> holds the client's constants to.
/// These tests say nothing about the gateway's own behaviour.
/// </summary>
public sealed class DataClientTests
{
    private static readonly Guid ProjectId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid EnvironmentId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private const string Route = "http://localhost/v1/data/notes/";
    private static readonly string[] Tags = ["x", "y"];
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);
    private const string Result = "{\"rows\":[{\"id\":1,\"body\":\"hello\",\"created_at\":\"2026-10-20T01:02:03Z\"}],\"affectedRows\":1,\"hasMore\":true}";

    [Fact]
    public async Task ASelectWithNoRequestSendsAnEmptyDocumentWithTheUserTokenAndReadsTheRows()
    {
        var (handler, calls) = Recording(HttpStatusCode.OK, Result);
        await using var client = await ClientAsync(handler);

        var result = await client.Data.SelectAsync("notes", cancellationToken: TestContext.Current.CancellationToken);

        var call = Assert.Single(calls);
        Assert.Equal(HttpMethod.Post, call.Method);
        Assert.Equal(Route + "select", call.Uri);
        Assert.Equal(new AuthenticationHeaderValue("Bearer", "access-token"), call.Authorization);
        Assert.Equal("application/json", call.ContentType);
        Assert.Equal("{}", call.Body);
        Assert.Equal(1, result.AffectedRows);
        Assert.True(result.HasMore);
        var row = Assert.Single(result.Rows);
        Assert.Equal("hello", row.GetProperty("body").GetString());
        var typed = Assert.Single(result.RowsAs<Note>());
        Assert.Equal(1, typed.Id);
        Assert.Equal("hello", typed.Body);
    }

    [Fact]
    public async Task ASelectSendsItsColumnsFiltersOrderAndPageInTheContractsMemberNames()
    {
        var (handler, calls) = Recording(HttpStatusCode.OK, "{\"rows\":[],\"affectedRows\":0,\"hasMore\":false}");
        await using var client = await ClientAsync(handler);

        await client.Data.SelectAsync(
            "notes",
            new YogabaseDataSelectRequest
            {
                Columns = ["id", "body"],
                Filters =
                [
                    YogabaseDataFilter.Eq("owner", "a@example.test"),
                    YogabaseDataFilter.Gte("score", 10),
                    YogabaseDataFilter.In("tag", Tags),
                    YogabaseDataFilter.ILike("body", "%hi%"),
                    YogabaseDataFilter.IsNull("deleted_at"),
                ],
                Order = [YogabaseDataOrder.DescendingBy("created_at"), new YogabaseDataOrder { Column = "id", NullsFirst = true }],
                Limit = 50,
                Offset = 100,
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(
            "{\"columns\":[\"id\",\"body\"],\"filters\":["
            + "{\"column\":\"owner\",\"op\":\"eq\",\"value\":\"a@example.test\"},"
            + "{\"column\":\"score\",\"op\":\"gte\",\"value\":10},"
            + "{\"column\":\"tag\",\"op\":\"in\",\"value\":[\"x\",\"y\"]},"
            + "{\"column\":\"body\",\"op\":\"ilike\",\"value\":\"%hi%\"},"
            + "{\"column\":\"deleted_at\",\"op\":\"isNull\"}],"
            + "\"order\":[{\"column\":\"created_at\",\"descending\":true},{\"column\":\"id\",\"descending\":false,\"nullsFirst\":true}],"
            + "\"limit\":50,\"offset\":100}",
            Assert.Single(calls).Body);
    }

    [Fact]
    public async Task EachWriteSendsItsOwnDocumentToItsOwnRoute()
    {
        var (handler, calls) = Recording(HttpStatusCode.OK, "{\"rows\":[],\"affectedRows\":1,\"hasMore\":false}");
        await using var client = await ClientAsync(handler);
        var row = JsonSerializer.SerializeToElement(new { id = 7, body = "hi" });
        var ct = TestContext.Current.CancellationToken;

        await client.Data.InsertAsync("notes", new YogabaseDataInsertRequest { Rows = [row], Returning = ["*"] }, ct);
        await client.Data.UpsertAsync("notes", new YogabaseDataUpsertRequest { Rows = [row], OnConflict = ["id"], IgnoreDuplicates = true }, ct);
        await client.Data.UpdateAsync(
            "notes",
            new YogabaseDataUpdateRequest { Values = JsonSerializer.SerializeToElement(new { body = "bye" }), Filters = [YogabaseDataFilter.Eq("id", 7)], Returning = ["id"] },
            ct);
        await client.Data.DeleteAsync("notes", new YogabaseDataDeleteRequest { Filters = [YogabaseDataFilter.Lt("id", 3)] }, ct);

        Assert.Equal(
            [
                (Route + "insert", "{\"rows\":[{\"id\":7,\"body\":\"hi\"}],\"returning\":[\"*\"]}"),
                (Route + "upsert", "{\"rows\":[{\"id\":7,\"body\":\"hi\"}],\"onConflict\":[\"id\"],\"ignoreDuplicates\":true}"),
                (Route + "update", "{\"values\":{\"body\":\"bye\"},\"filters\":[{\"column\":\"id\",\"op\":\"eq\",\"value\":7}],\"returning\":[\"id\"]}"),
                (Route + "delete", "{\"filters\":[{\"column\":\"id\",\"op\":\"lt\",\"value\":3}]}"),
            ],
            calls.Select(static call => (call.Uri, call.Body!)).ToArray());
        Assert.All(calls, static call => Assert.Equal(HttpMethod.Post, call.Method));
    }

    [Fact]
    public async Task ARefusalIsReadByItsCodeAndATooLargeRequestNamesBothLimits()
    {
        var answers = new Queue<HttpResponseMessage>(
        [
            Problem(HttpStatusCode.Forbidden, "{\"title\":\"Data API request refused\",\"status\":403,\"detail\":\"No table of that name is exposed to this identity.\",\"code\":\"data_table_not_exposed\"}"),
            Problem(HttpStatusCode.RequestEntityTooLarge, "{\"title\":\"Data API request refused\",\"status\":413,\"code\":\"data_request_too_large\",\"limitBytes\":1048576,\"maxBodyBytes\":1114112}"),
            Problem(HttpStatusCode.Conflict, "{\"title\":\"Data API request refused\",\"status\":409,\"code\":\"data_unique_violation\"}"),
        ]);
        var handler = new ScriptedHandler((_, _) => Task.FromResult(answers.Dequeue()));
        await using var client = await ClientAsync(handler);
        var ct = TestContext.Current.CancellationToken;

        var hidden = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Data.SelectAsync("secrets", cancellationToken: ct));
        Assert.Equal(403, hidden.StatusCode);
        Assert.Equal(YogabaseDataErrors.TableNotExposed, hidden.Code);

        var large = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Data.SelectAsync("notes", cancellationToken: ct));
        Assert.Equal(413, large.StatusCode);
        Assert.Equal(YogabaseDataErrors.RequestTooLarge, large.Code);
        Assert.Equal(YogabaseDataLimits.MaximumValueBytes, large.LimitBytes);
        Assert.Equal(YogabaseDataLimits.MaximumRequestBytes, large.MaxBodyBytes);

        var unique = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Data.InsertAsync(
            "notes",
            new YogabaseDataInsertRequest { Rows = [JsonSerializer.SerializeToElement(new { id = 1 })] },
            ct));
        Assert.Equal(YogabaseDataErrors.UniqueViolation, unique.Code);
    }

    [Theory]
    [InlineData("{\"rows\":[],\"affectedRows\":0}")]
    [InlineData("{\"rows\":[],\"hasMore\":false}")]
    [InlineData("{\"affectedRows\":0,\"hasMore\":false}")]
    [InlineData("{\"rows\":[],\"affectedRows\":-1,\"hasMore\":false}")]
    [InlineData("{\"rows\":[],\"affectedRows\":0,\"hasMore\":false,\"status\":200}")]
    [InlineData("[]")]
    public async Task AnAnswerThatIsNotTheResultDocumentIsRefused(string body)
    {
        var (handler, _) = Recording(HttpStatusCode.OK, body);
        await using var client = await ClientAsync(handler);

        var refused = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Data.SelectAsync("notes", cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal("response_invalid", refused.Code);
    }

    [Fact]
    public async Task MoreRowsThanTheResultBoundAreRefused()
    {
        var rows = string.Join(",", Enumerable.Repeat("{}", YogabaseDataLimits.MaximumReturnedRows + 1));
        var (handler, _) = Recording(HttpStatusCode.OK, "{\"rows\":[" + rows + "],\"affectedRows\":0,\"hasMore\":false}");
        await using var client = await ClientAsync(handler);

        var refused = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Data.SelectAsync("notes", cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal("response_invalid", refused.Code);
    }

    [Fact]
    public async Task AnUnavailableDataRowRefusesEveryCallBeforeAnythingIsSent()
    {
        var (handler, calls) = Recording(HttpStatusCode.OK, Result);
        await using var client = await ClientAsync(handler, new YogabaseCapability { Id = YogabaseCapability.DataRest, Available = false, UnavailableReason = "data-api-disabled" });
        await using var withoutRow = await ClientAsync(handler, row: null);
        var ct = TestContext.Current.CancellationToken;

        await Assert.ThrowsAsync<NotSupportedException>(() => client.Data.SelectAsync("notes", cancellationToken: ct));
        await Assert.ThrowsAsync<NotSupportedException>(() => withoutRow.Data.DeleteAsync("notes", new YogabaseDataDeleteRequest { Filters = [YogabaseDataFilter.Eq("id", 1)] }, ct));

        Assert.Empty(calls);
    }

    public static TheoryData<string, Func<IYogabaseData, Task>> Invalid()
    {
        var row = JsonSerializer.SerializeToElement(new { id = 1 });
        return new()
        {
            { "a table name with a dot", data => data.SelectAsync("public.notes") },
            { "a table name with a slash", data => data.SelectAsync("notes/x") },
            { "a table name of 64 characters", data => data.SelectAsync(new string('t', 64)) },
            { "a table name starting with a digit", data => data.SelectAsync("1notes") },
            { "a table name ending in a newline", data => data.SelectAsync("notes\n") },
            { "a limit of zero", data => data.SelectAsync("notes", new YogabaseDataSelectRequest { Limit = 0 }) },
            { "a limit over 1000", data => data.SelectAsync("notes", new YogabaseDataSelectRequest { Limit = 1001 }) },
            { "an offset over 100000", data => data.SelectAsync("notes", new YogabaseDataSelectRequest { Offset = 100_001 }) },
            { "a negative offset", data => data.SelectAsync("notes", new YogabaseDataSelectRequest { Offset = -1 }) },
            { "a repeated column", data => data.SelectAsync("notes", new YogabaseDataSelectRequest { Columns = ["id", "id"] }) },
            { "an empty column list", data => data.SelectAsync("notes", new YogabaseDataSelectRequest { Columns = [] }) },
            { "a column that is not a name", data => data.SelectAsync("notes", new YogabaseDataSelectRequest { Columns = ["id; drop"] }) },
            { "33 filters", data => data.SelectAsync("notes", new YogabaseDataSelectRequest { Filters = [.. Enumerable.Repeat(YogabaseDataFilter.Eq("id", 1), 33)] }) },
            { "nine order terms", data => data.SelectAsync("notes", new YogabaseDataSelectRequest { Order = [.. Enumerable.Repeat(YogabaseDataOrder.Ascending("id"), 9)] }) },
            { "an insert of no rows", data => data.InsertAsync("notes", new YogabaseDataInsertRequest()) },
            { "an insert of 501 rows", data => data.InsertAsync("notes", new YogabaseDataInsertRequest { Rows = [.. Enumerable.Repeat(row, 501)] }) },
            { "a row that is not an object", data => data.InsertAsync("notes", new YogabaseDataInsertRequest { Rows = [JsonSerializer.SerializeToElement(1)] }) },
            { "an upsert with no conflict target", data => data.UpsertAsync("notes", new YogabaseDataUpsertRequest { Rows = [row] }) },
            { "an upsert with 17 conflict columns", data => data.UpsertAsync("notes", new YogabaseDataUpsertRequest { Rows = [row], OnConflict = [.. Enumerable.Range(0, 17).Select(static i => "c" + i)] }) },
            { "an update with no filter", data => data.UpdateAsync("notes", new YogabaseDataUpdateRequest { Values = row }) },
            { "an update whose values are not an object", data => data.UpdateAsync("notes", new YogabaseDataUpdateRequest { Values = JsonSerializer.SerializeToElement("x"), Filters = [YogabaseDataFilter.Eq("id", 1)] }) },
            { "a delete with no filter", data => data.DeleteAsync("notes", new YogabaseDataDeleteRequest()) },
            {
                "a request over the document limit",
                data => data.InsertAsync("notes", new YogabaseDataInsertRequest { Rows = [JsonSerializer.SerializeToElement(new { body = new string('x', YogabaseDataLimits.MaximumRequestBytes) })] })
            },
        };
    }

    [Theory]
    [MemberData(nameof(Invalid))]
    public async Task ARequestOutsideTheContractsBoundsIsRefusedBeforeItIsSent(string _, Func<IYogabaseData, Task> call)
    {
        var (handler, calls) = Recording(HttpStatusCode.OK, Result);
        await using var client = await ClientAsync(handler);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => call(client.Data));

        Assert.Empty(calls);
    }

    [Fact]
    public async Task ARequestAtTheDocumentLimitIsSent()
    {
        var (handler, calls) = Recording(HttpStatusCode.OK, "{\"rows\":[],\"affectedRows\":1,\"hasMore\":false}");
        await using var client = await ClientAsync(handler);
        var overhead = Encoding.UTF8.GetByteCount("{\"rows\":[{\"body\":\"\"}]}");

        await client.Data.InsertAsync(
            "notes",
            new YogabaseDataInsertRequest { Rows = [JsonSerializer.SerializeToElement(new { body = new string('x', YogabaseDataLimits.MaximumRequestBytes - overhead) })] },
            TestContext.Current.CancellationToken);

        Assert.Equal(YogabaseDataLimits.MaximumRequestBytes, Encoding.UTF8.GetByteCount(Assert.Single(calls).Body!));
    }

    [Fact]
    public void FiltersAreHeldToTheirOperatorsShape()
    {
        Assert.Throws<ArgumentException>(() => YogabaseDataFilter.Eq<string?>("id", null));
        Assert.Throws<ArgumentException>(() => YogabaseDataFilter.In("id", Array.Empty<int>()));
        Assert.Throws<ArgumentException>(() => YogabaseDataFilter.In("id", Enumerable.Range(0, 101)));
        Assert.Throws<ArgumentException>(() => YogabaseDataFilter.In("id", new int?[] { 1, null }));
        Assert.Throws<ArgumentException>(() => YogabaseDataFilter.Like("body", null!));
        Assert.Throws<ArgumentException>(() => YogabaseDataFilter.Of("id", "between", JsonSerializer.SerializeToElement(1)));
        Assert.Throws<ArgumentException>(() => YogabaseDataFilter.Of("id", "isNull", JsonSerializer.SerializeToElement(1)));
        Assert.Throws<ArgumentException>(() => YogabaseDataFilter.Eq("a b", 1));
        Assert.Equal(100, YogabaseDataFilter.In("id", Enumerable.Range(0, 100)).Value!.Value.GetArrayLength());
        Assert.Null(YogabaseDataFilter.NotNull("id").Value);
    }

    [Fact]
    public async Task TheBootstrapRequestsTheDataRowAndAnInstallationThatServesItIsRead()
    {
        string? requested = null;
        var handler = new ScriptedHandler((request, _) =>
        {
            requested = request.Headers.TryGetValues(YogabaseClient.AcceptCapabilitiesHeader, out var values) ? string.Join(",", values) : null;
            var configuration = Configuration(
            [
                .. YogabaseClientTests.Capabilities(),
                new YogabaseCapability { Id = "realtime-presence", Available = false, UnavailableReason = "realtime-subscribe-unavailable" },
                new YogabaseCapability { Id = "data-rest", Available = true },
            ]);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(configuration, WebJson), Encoding.UTF8, "application/json"),
            });
        });

        await using var client = await YogabaseClient.CreateAsync(
            new Uri("http://localhost/"),
            "sy_pub_0123456789abcdef",
            new YogabaseClientOptions { AllowInsecureLoopback = true, TransportHandler = handler },
            TestContext.Current.CancellationToken);

        Assert.Equal("realtime-presence, data-rest", requested);
        Assert.Equal(6, client.Configuration.Capabilities.Count);
        Assert.True(client.Configuration.IsAvailable(YogabaseCapability.DataRest));
        Assert.False(client.Configuration.IsAvailable(YogabaseCapability.RealtimePresence));
    }

    private sealed record Note(int Id, string Body);

    private sealed record Call(HttpMethod Method, string Uri, AuthenticationHeaderValue? Authorization, string? ContentType, string? Body);

    private static (HttpMessageHandler Handler, List<Call> Calls) Recording(HttpStatusCode status, string body)
    {
        var calls = new List<Call>();
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            calls.Add(new Call(
                request.Method,
                request.RequestUri!.AbsoluteUri,
                request.Headers.Authorization,
                request.Content?.Headers.ContentType?.MediaType,
                request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken)));
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        });
        return (handler, calls);
    }

    private static HttpResponseMessage Problem(HttpStatusCode status, string json) => new(status)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/problem+json"),
    };

    private static Task<YogabaseClient> ClientAsync(HttpMessageHandler handler) =>
        ClientAsync(handler, new YogabaseCapability { Id = YogabaseCapability.DataRest, Available = true });

    private static async Task<YogabaseClient> ClientAsync(HttpMessageHandler handler, YogabaseCapability? row)
    {
        var store = new InMemoryYogabaseSessionStore();
        await store.SaveAsync(
            new YogabaseTokenSet { AccessToken = "access-token", RefreshToken = "refresh-token", ExpiresAt = DateTimeOffset.UtcNow.AddHours(1) },
            TestContext.Current.CancellationToken);
        IReadOnlyList<YogabaseCapability> capabilities = row is null
            ? YogabaseClientTests.Capabilities()
            : [.. YogabaseClientTests.Capabilities(), row];
        return YogabaseClient.FromConfiguration(
            Configuration(capabilities),
            new YogabaseClientOptions { AllowInsecureLoopback = true, SessionStore = store, TransportHandler = handler });
    }

    private static YogabaseClientConfiguration Configuration(IReadOnlyList<YogabaseCapability> capabilities) => new()
    {
        Schema = YogabaseClient.ClientConfigSchema,
        ProjectId = ProjectId,
        EnvironmentId = EnvironmentId,
        GatewayEndpoint = "http://localhost/",
        RealtimeEndpoint = "http://localhost:5001/",
        OidcAuthority = "http://localhost/p/11111111-1111-4111-8111-111111111111",
        OidcDiscoveryEndpoint = "http://localhost/.well-known/openid-configuration",
        OidcPublicClientId = "yogabase-native-preview",
        OidcScopes = ["openid", "offline_access"],
        OidcRedirectUris = ["io.yogabase.app:/oauth/callback"],
        RealtimePath = "/v1/realtime",
        Capabilities = capabilities,
        IssuedAt = DateTimeOffset.Parse("2026-10-20T00:00:00+00:00", System.Globalization.CultureInfo.InvariantCulture),
    };

    private sealed class ScriptedHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            send(request, cancellationToken);
    }
}

/// <summary>
/// The Data API's figures and names as the mirrored contract states them under <c>x-yogabase-data-api</c> and in
/// its Data schemas, held to the constants the .NET client checks before sending and names refusals by.
/// </summary>
public sealed class DataContractTests
{
    [Fact]
    public async Task TheLimitsOperatorsCodesAndNameRuleAreTheContracts()
    {
        using var document = await ReadContractAsync();
        var root = document.RootElement;
        var data = root.GetProperty("x-yogabase-data-api");
        var limits = data.GetProperty("limits");

        Assert.Equal("data-rest", data.GetProperty("capability").GetString());
        Assert.Equal(YogabaseCapability.DataRest, data.GetProperty("capability").GetString());
        Assert.Equal(YogabaseDataLimits.DefaultLimit, limits.GetProperty("defaultLimit").GetInt32());
        Assert.Equal(YogabaseDataLimits.MaximumLimit, limits.GetProperty("maximumLimit").GetInt32());
        Assert.Equal(YogabaseDataLimits.MaximumOffset, limits.GetProperty("maximumOffset").GetInt32());
        Assert.Equal(YogabaseDataLimits.MaximumColumns, limits.GetProperty("maximumColumns").GetInt32());
        Assert.Equal(YogabaseDataLimits.MaximumFilters, limits.GetProperty("maximumFilters").GetInt32());
        Assert.Equal(YogabaseDataLimits.MaximumInValues, limits.GetProperty("maximumInValues").GetInt32());
        Assert.Equal(YogabaseDataLimits.MaximumOrderTerms, limits.GetProperty("maximumOrderTerms").GetInt32());
        Assert.Equal(YogabaseDataLimits.MaximumWriteRows, limits.GetProperty("maximumWriteRows").GetInt32());
        Assert.Equal(YogabaseDataLimits.MaximumConflictColumns, limits.GetProperty("maximumConflictColumns").GetInt32());
        Assert.Equal(YogabaseDataLimits.MaximumReturnedRows, limits.GetProperty("maximumReturnedRows").GetInt32());
        Assert.Equal(YogabaseDataLimits.MaximumResultBytes, limits.GetProperty("maximumResultBytes").GetInt32());
        Assert.Equal(YogabaseDataLimits.MaximumValueBytes, limits.GetProperty("maximumValueBytes").GetInt32());
        Assert.Equal(YogabaseDataLimits.MaximumRequestBytes, limits.GetProperty("maximumRequestBytes").GetInt32());
        Assert.Equal(YogabaseDataLimits.StatementTimeoutSeconds, limits.GetProperty("statementTimeoutSeconds").GetInt32());

        Assert.Equal(
            data.GetProperty("filterOperators").EnumerateArray().Select(static value => value.GetString()!),
            YogabaseDataFilterOperators.All);
        Assert.Equal(
            data.GetProperty("refusalCodes").EnumerateArray().Select(static value => value.GetString()!),
            YogabaseDataErrors.All);
        var constants = typeof(YogabaseDataErrors).GetFields()
            .Where(static field => field.IsLiteral)
            .Select(static field => (string)field.GetRawConstantValue()!)
            .Order(StringComparer.Ordinal);
        Assert.Equal(YogabaseDataErrors.All.Order(StringComparer.Ordinal), constants);

        var components = root.GetProperty("components");
        var tablePattern = components.GetProperty("parameters").GetProperty("Table").GetProperty("schema").GetProperty("pattern").GetString();
        var columnPattern = components.GetProperty("schemas").GetProperty("DataColumnName").GetProperty("pattern").GetString();
        Assert.Equal(tablePattern, columnPattern);
        // A JSON Schema pattern's "$" is the end of the text; .NET's "$" also matches before a final newline.
        Assert.EndsWith("$", tablePattern, StringComparison.Ordinal);
        var pattern = new System.Text.RegularExpressions.Regex(tablePattern![..^1] + "\\z");
        // Every Latin-1 character as a one- and a two-character name, held to the client's own rule.
        for (var c = 0; c < 256; c++)
        {
            foreach (var name in new[] { ((char)c).ToString(), "a" + (char)c })
            {
                var accepted = Accepts(name);
                Assert.True(pattern.IsMatch(name) == accepted, $"The client and the contract disagree on {name}.");
            }
        }

        Assert.True(Accepts(new string('n', 63)));
        Assert.False(Accepts(new string('n', 64)));
    }

    private static bool Accepts(string name)
    {
        try
        {
            _ = YogabaseDataFilter.IsNull(name);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static async Task<JsonDocument> ReadContractAsync()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Yogabase.SDKs.slnx")))
            {
                return JsonDocument.Parse(await File.ReadAllTextAsync(
                    Path.Combine(directory.FullName, "contracts", "yogabase-client-v1.openapi.json"),
                    TestContext.Current.CancellationToken));
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate Yogabase.SDKs.slnx.");
    }
}
