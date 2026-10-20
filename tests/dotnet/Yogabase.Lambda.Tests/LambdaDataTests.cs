using System.Data.Common;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Yogabase.Lambda.Tests;

/// <summary>
/// Coverage for the typed data API wrapper (<see cref="LambdaData"/>) and the caller-scoped connection against a
/// fake runtime delegate: the exact request document each call hands the runtime, the identity it names, what it
/// refuses before anything is sent, and how it reads the runtime's answer and refusals. The document is the
/// runtime's: <c>{"identity","operation","table"|"function","request"}</c> in, and an answer that always says
/// <c>status</c> out. These tests say nothing about the runtime's own validation.
/// </summary>
public sealed class LambdaDataTests
{
    private const string Success = "{\"status\":200,\"rows\":[{\"id\":1,\"body\":\"hello\"}],\"affectedRows\":1,\"hasMore\":false}";
    private static readonly string[] EnvelopeMembers = ["function", "identity", "operation", "request", "table"];

    [Fact]
    public async Task ASelectHandsTheRuntimeOneDocumentAsTheLambdaIdentityAndReadsTheRows()
    {
        var runtime = new FakeDataApi(Success);
        var context = LambdaContext.From(new Provider(runtime));

        var result = await context.Data.From("notes").SelectAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("{\"identity\":\"lambda\",\"operation\":\"select\",\"table\":\"notes\",\"request\":{}}", Assert.Single(runtime.Documents));
        Assert.Equal(1, result.AffectedRows);
        Assert.False(result.HasMore);
        Assert.Equal("hello", Assert.Single(result.RowsAs<Note>()).Body);
        Assert.Equal(LambdaData.LambdaIdentity, context.Data.Identity);
        Assert.Same(context.Data, context.Data);
    }

    [Fact]
    public async Task AsCallerNamesTheCallerIdentityAndEachOperationItsOwnRequest()
    {
        var runtime = new FakeDataApi("{\"status\":200,\"rows\":[],\"affectedRows\":2,\"hasMore\":false}");
        var notes = LambdaContext.From(new Provider(runtime)).Data.AsCaller().From("notes");
        var row = JsonSerializer.SerializeToElement(new { id = 7, body = "hi" });
        var ct = TestContext.Current.CancellationToken;

        await notes.SelectAsync(
            new LambdaDataSelectRequest
            {
                Columns = ["id"],
                Filters = [LambdaDataFilter.Eq("owner", "me"), LambdaDataFilter.In("tag", Tags), LambdaDataFilter.NotNull("body")],
                Order = [LambdaDataOrder.DescendingBy("id")],
                Limit = 10,
                Offset = 20,
            },
            ct);
        await notes.InsertAsync(new LambdaDataInsertRequest { Rows = [row], Returning = ["*"] }, ct);
        await notes.UpsertAsync(new LambdaDataUpsertRequest { Rows = [row], OnConflict = ["id"] }, ct);
        await notes.UpdateAsync(new LambdaDataUpdateRequest { Values = JsonSerializer.SerializeToElement(new { body = "bye" }), Filters = [LambdaDataFilter.Eq("id", 7)] }, ct);
        await notes.DeleteAsync(new LambdaDataDeleteRequest { Filters = [LambdaDataFilter.Gte("id", 3)], Returning = ["id"] }, ct);

        Assert.Equal(
            [
                "{\"identity\":\"caller\",\"operation\":\"select\",\"table\":\"notes\",\"request\":{\"columns\":[\"id\"],\"filters\":["
                + "{\"column\":\"owner\",\"op\":\"eq\",\"value\":\"me\"},{\"column\":\"tag\",\"op\":\"in\",\"value\":[\"a\",\"b\"]},"
                + "{\"column\":\"body\",\"op\":\"notNull\"}],\"order\":[{\"column\":\"id\",\"descending\":true}],\"limit\":10,\"offset\":20}}",
                "{\"identity\":\"caller\",\"operation\":\"insert\",\"table\":\"notes\",\"request\":{\"rows\":[{\"id\":7,\"body\":\"hi\"}],\"returning\":[\"*\"]}}",
                "{\"identity\":\"caller\",\"operation\":\"upsert\",\"table\":\"notes\",\"request\":{\"rows\":[{\"id\":7,\"body\":\"hi\"}],\"onConflict\":[\"id\"],\"ignoreDuplicates\":false}}",
                "{\"identity\":\"caller\",\"operation\":\"update\",\"table\":\"notes\",\"request\":{\"values\":{\"body\":\"bye\"},\"filters\":[{\"column\":\"id\",\"op\":\"eq\",\"value\":7}]}}",
                "{\"identity\":\"caller\",\"operation\":\"delete\",\"table\":\"notes\",\"request\":{\"filters\":[{\"column\":\"id\",\"op\":\"gte\",\"value\":3}],\"returning\":[\"id\"]}}",
            ],
            runtime.Documents);
        Assert.All(runtime.Documents, static document =>
        {
            using var parsed = JsonDocument.Parse(document);
            // The runtime's envelope refuses any other member.
            Assert.True(parsed.RootElement.EnumerateObject().Select(static member => member.Name).ToHashSet().IsSubsetOf(EnvelopeMembers));
        });
    }

    [Fact]
    public async Task ADatabaseFunctionIsCalledByNameWithItsNamedArguments()
    {
        var runtime = new FakeDataApi(Success);
        var data = LambdaContext.From(new Provider(runtime)).Data;
        var ct = TestContext.Current.CancellationToken;

        await data.RpcAsync("leaderboard", JsonSerializer.SerializeToElement(new { season = 3 }), limit: 25, ct);
        await data.RpcAsync("refresh_scores", cancellationToken: ct);

        Assert.Equal(
            [
                "{\"identity\":\"lambda\",\"operation\":\"rpc\",\"function\":\"leaderboard\",\"request\":{\"arguments\":{\"season\":3},\"limit\":25}}",
                "{\"identity\":\"lambda\",\"operation\":\"rpc\",\"function\":\"refresh_scores\",\"request\":{}}",
            ],
            runtime.Documents);
    }

    [Fact]
    public async Task ARefusalIsThrownWithTheRuntimesStatusAndCode()
    {
        var runtime = new FakeDataApi(
            "{\"status\":403,\"code\":\"data_caller_identity_unavailable\",\"message\":\"This invocation has no signed-in caller, so there is no caller-scoped identity to run as.\"}");
        var notes = LambdaContext.From(new Provider(runtime)).Data.AsCaller().From("notes");

        var refused = await Assert.ThrowsAsync<LambdaApiException>(() => notes.SelectAsync(cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(403, refused.StatusCode);
        Assert.Equal(LambdaDataRefusalCodes.CallerIdentityUnavailable, refused.Code);
        Assert.StartsWith("This invocation has no signed-in caller", refused.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"rows\":[],\"affectedRows\":0,\"hasMore\":false}")]
    [InlineData("{\"status\":200,\"rows\":[],\"affectedRows\":0}")]
    [InlineData("{\"status\":200,\"affectedRows\":0,\"hasMore\":false}")]
    [InlineData("{\"status\":200,\"rows\":[],\"affectedRows\":-1,\"hasMore\":false}")]
    [InlineData("{\"status\":403,\"message\":\"no code\"}")]
    [InlineData("{\"status\":302,\"code\":\"data_request_invalid\"}")]
    public async Task AnAnswerThatIsNotTheDocumentedOneIsRefusedByName(string answer)
    {
        var notes = LambdaContext.From(new Provider(new FakeDataApi(answer))).Data.From("notes");

        var refused = await Assert.ThrowsAsync<LambdaApiException>(() => notes.SelectAsync(cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(502, refused.StatusCode);
        Assert.Equal(LambdaDataRefusalCodes.ResponseInvalid, refused.Code);
    }

    public static TheoryData<string, Func<LambdaData, Task>> Invalid()
    {
        var row = JsonSerializer.SerializeToElement(new { id = 1 });
        return new()
        {
            { "a qualified table", data => data.From("public.notes").SelectAsync() },
            { "a table ending in a newline", data => data.From("notes\n").SelectAsync() },
            { "a limit of zero", data => data.From("notes").SelectAsync(new LambdaDataSelectRequest { Limit = 0 }) },
            { "an offset over the bound", data => data.From("notes").SelectAsync(new LambdaDataSelectRequest { Offset = 100_001 }) },
            { "33 filters", data => data.From("notes").SelectAsync(new LambdaDataSelectRequest { Filters = [.. Enumerable.Repeat(LambdaDataFilter.Eq("id", 1), 33)] }) },
            { "an insert of no rows", data => data.From("notes").InsertAsync(new LambdaDataInsertRequest()) },
            { "an upsert without a target", data => data.From("notes").UpsertAsync(new LambdaDataUpsertRequest { Rows = [row] }) },
            { "an unfiltered update", data => data.From("notes").UpdateAsync(new LambdaDataUpdateRequest { Values = row }) },
            { "an unfiltered delete", data => data.From("notes").DeleteAsync(new LambdaDataDeleteRequest()) },
            { "a function that is not a name", data => data.RpcAsync("drop table x") },
            { "arguments that are not an object", data => data.RpcAsync("f", JsonSerializer.SerializeToElement(Tags)) },
            { "an argument that is not a name", data => data.RpcAsync("f", JsonSerializer.SerializeToElement(new Dictionary<string, int> { ["a-b"] = 1 })) },
            { "a function limit over the bound", data => data.RpcAsync("f", limit: 1001) },
            {
                "a document over the request limit",
                data => data.From("notes").InsertAsync(new LambdaDataInsertRequest { Rows = [JsonSerializer.SerializeToElement(new { body = new string('x', LambdaDataLimits.MaximumRequestBytes) })] })
            },
        };
    }

    [Theory]
    [MemberData(nameof(Invalid))]
    public async Task ARequestOutsideTheBoundsIsRefusedBeforeTheRuntimeSeesIt(string _, Func<LambdaData, Task> call)
    {
        var runtime = new FakeDataApi(Success);
        var data = LambdaContext.From(new Provider(runtime)).Data;

        await Assert.ThrowsAnyAsync<ArgumentException>(() => call(data));

        Assert.Empty(runtime.Documents);
    }

    [Fact]
    public void AMissingDelegateIsReportedByNameTheFirstTimeDataIsUsed()
    {
        var context = LambdaContext.From(new Provider(runtime: null));

        var missing = Assert.Throws<InvalidOperationException>(() => context.Data);

        Assert.Contains("typed data API", missing.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheCallerDatabaseIsTheRuntimesCallerEntryAndIsResolvedOnce()
    {
        var full = new NamedDataSource("lambda");
        var caller = new NamedDataSource("caller");
        var provider = new Provider(runtime: null);
        provider.Register(typeof(DbDataSource), full);
        provider.Register(
            typeof(IReadOnlyDictionary<string, DbDataSource>),
            new Dictionary<string, DbDataSource>(StringComparer.Ordinal) { ["lambda"] = full, ["caller"] = caller });
        var context = LambdaContext.From(provider);

        Assert.Same(caller, context.CallerDatabase);
        Assert.Same(caller, context.CallerDatabase);
        Assert.Same(full, context.Database);
    }

    [Fact]
    public void ARuntimeWithoutTheIdentityMapNamesWhatIsMissing()
    {
        var context = LambdaContext.From(new Provider(runtime: null));

        var missing = Assert.Throws<InvalidOperationException>(() => context.CallerDatabase);

        Assert.Contains("database identities", missing.Message, StringComparison.Ordinal);
    }

    private static readonly string[] Tags = ["a", "b"];

    private sealed record Note(int Id, string Body);

    private sealed class FakeDataApi(string answer)
    {
        internal List<string> Documents { get; } = [];

        internal ValueTask<ReadOnlyMemory<byte>> Send(ReadOnlyMemory<byte> document, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Documents.Add(Encoding.UTF8.GetString(document.Span));
            return ValueTask.FromResult<ReadOnlyMemory<byte>>(Encoding.UTF8.GetBytes(answer));
        }
    }

    private sealed class Provider : IServiceProvider
    {
        private readonly Dictionary<Type, object> services = [];

        internal Provider(FakeDataApi? runtime)
        {
            services[typeof(ILogger)] = NullLogger.Instance;
            services[typeof(ClaimsPrincipal)] = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "9f1d6b8a-3f2e-4c11-9c33-1f5a2b7c8d90")], "yogabase"));
            services[typeof(IReadOnlyDictionary<string, string>)] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [LambdaRoutes.InvocationIdKey] = "inv-1",
                [LambdaRoutes.FunctionIdKey] = "notes-api",
                [LambdaRoutes.VersionKey] = "1",
                [LambdaRoutes.EnvironmentIdKey] = "22222222-2222-4222-8222-222222222222",
            };
            if (runtime is not null)
            {
                services[typeof(Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask<ReadOnlyMemory<byte>>>)] =
                    (Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask<ReadOnlyMemory<byte>>>)runtime.Send;
            }
        }

        internal void Register(Type type, object instance) => services[type] = instance;

        public object? GetService(Type serviceType) => services.TryGetValue(serviceType, out var instance) ? instance : null;
    }

    private sealed class NamedDataSource(string name) : DbDataSource
    {
        public override string ConnectionString => "Host=" + name;

        protected override DbConnection CreateDbConnection() => throw new NotSupportedException();
    }
}
