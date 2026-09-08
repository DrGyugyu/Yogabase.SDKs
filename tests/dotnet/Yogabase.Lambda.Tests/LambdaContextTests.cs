using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Yogabase.Lambda.Tests;

/// <summary>
/// Coverage for the Lambda runtime context against a fake service provider and a fake transport.
/// The wrapper's whole job is to read what the sandbox host registered and to speak the internal
/// runtime API exactly, so these tests pin both: which services are required, and which request
/// each call emits.
/// </summary>
public sealed class LambdaContextTests
{
    private static readonly Uri RuntimeRoot = new("http://gateway.internal/v1/lambda-runtime/");
    private const string PlayerSubject = "9f1d6b8a-3f2e-4c11-9c33-1f5a2b7c8d90";
    private static readonly string[] ExpectedClientNames =
        ["yogabase-storage", "yogabase-users", "yogabase-realtime"];
    private static readonly string[] ExpectedObjectKeys = ["saves/a", "saves/b"];

    // ---------------------------------------------------------------------------------------
    // Resolution
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void FromReadsTheLoggerCallerAndInvocationFacts()
    {
        var provider = new FakeProvider();

        var context = LambdaContext.From(provider);

        Assert.Same(provider.Logger, context.Logger);
        Assert.Equal("inv-1", context.Invocation.InvocationId);
        Assert.Equal("battle-pass", context.Invocation.FunctionId);
        Assert.Equal("3", context.Invocation.Version);
        Assert.Equal("22222222-2222-4222-8222-222222222222", context.Invocation.EnvironmentId);
        Assert.Equal(PlayerSubject, context.Caller.Subject);
    }

    [Fact]
    public void FromRefusesANullProvider() =>
        Assert.Throws<ArgumentNullException>(() => LambdaContext.From(null!));

    [Theory]
    [InlineData(typeof(ILogger))]
    [InlineData(typeof(ClaimsPrincipal))]
    [InlineData(typeof(IReadOnlyDictionary<string, string>))]
    public void FromRefusesAProviderThatIsNotTheRuntimes(Type missing)
    {
        var provider = new FakeProvider();
        provider.Remove(missing);

        var exception = Assert.Throws<InvalidOperationException>(() => LambdaContext.From(provider));

        Assert.Contains("InvokeAsync", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ALoggerFactoryStandsInForAnAbsentLogger()
    {
        var provider = new FakeProvider();
        provider.Remove(typeof(ILogger));
        provider.Register(typeof(ILoggerFactory), NullLoggerFactory.Instance);

        Assert.NotNull(LambdaContext.From(provider).Logger);
    }

    [Fact]
    public void AMissingInvocationFactIsRefusedNamingIt()
    {
        var provider = new FakeProvider();
        provider.Register(
            typeof(IReadOnlyDictionary<string, string>),
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [LambdaRoutes.InvocationIdKey] = "inv-1",
            });

        var exception = Assert.Throws<InvalidOperationException>(() => LambdaContext.From(provider));

        Assert.Contains(LambdaRoutes.FunctionIdKey, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheDatabaseIsTheDataSourceTheRuntimeRegistered()
    {
        var provider = new FakeProvider();

        Assert.Same(provider.DataSource, LambdaContext.From(provider).Database);
    }

    [Fact]
    public void AnAbsentDatabaseIsReportedOnlyWhenItIsUsed()
    {
        var provider = new FakeProvider();
        provider.Remove(typeof(DbDataSource));

        var context = LambdaContext.From(provider);

        Assert.Equal("battle-pass", context.Invocation.FunctionId);
        var exception = Assert.Throws<InvalidOperationException>(() => context.Database);
        Assert.Contains("DbDataSource", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EachNamedClientIsRequestedFromTheRuntimeFactoryByItsExactName()
    {
        var provider = new FakeProvider();
        var context = LambdaContext.From(provider);

        _ = context.Storage;
        _ = context.Users;
        _ = context.Realtime;

        Assert.Equal(ExpectedClientNames, provider.Factory.Requested);
    }

    [Fact]
    public void EachModuleIsResolvedOnce()
    {
        var provider = new FakeProvider();
        var context = LambdaContext.From(provider);

        Assert.Same(context.Storage, context.Storage);
        Assert.Single(provider.Factory.Requested);
    }

    // ---------------------------------------------------------------------------------------
    // Caller
    // ---------------------------------------------------------------------------------------

    [Theory]
    [InlineData("sub")]
    [InlineData(ClaimTypes.NameIdentifier)]
    public void TheCallerSubjectIsReadFromEitherClaimName(string claimType)
    {
        var provider = new FakeProvider();
        provider.Register(typeof(ClaimsPrincipal), Principal(claimType, PlayerSubject));

        var caller = LambdaContext.From(provider).Caller;

        Assert.Equal(PlayerSubject, caller.Subject);
        Assert.False(caller.IsSystem);
        Assert.True(caller.TryGetSubjectId(out var subjectId));
        Assert.Equal(Guid.Parse(PlayerSubject), subjectId);
        Assert.Equal(Guid.Parse(PlayerSubject), caller.RequireSubjectId());
    }

    [Fact]
    public void AScheduledInvocationIsNeverAttributedToAPlayer()
    {
        var provider = new FakeProvider();
        provider.Register(typeof(ClaimsPrincipal), Principal("sub", "lambda:battle-pass"));

        var caller = LambdaContext.From(provider).Caller;

        Assert.True(caller.IsSystem);
        Assert.Equal("battle-pass", caller.SystemFunctionId);
        Assert.False(caller.TryGetSubjectId(out var subjectId));
        Assert.Equal(Guid.Empty, subjectId);
        var exception = Assert.Throws<InvalidOperationException>(() => caller.RequireSubjectId());
        Assert.Contains("started by Yogabase", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ANonGuidSubjectIsRefusedRatherThanCoerced()
    {
        var provider = new FakeProvider();
        provider.Register(typeof(ClaimsPrincipal), Principal("sub", "not-a-guid"));

        var caller = LambdaContext.From(provider).Caller;

        Assert.False(caller.IsSystem);
        Assert.Null(caller.SystemFunctionId);
        Assert.False(caller.TryGetSubjectId(out _));
        Assert.Throws<InvalidOperationException>(() => caller.RequireSubjectId());
    }

    [Fact]
    public void AnAnonymousPrincipalHasNoSubject()
    {
        var provider = new FakeProvider();
        provider.Register(typeof(ClaimsPrincipal), new ClaimsPrincipal(new ClaimsIdentity()));

        var caller = LambdaContext.From(provider).Caller;

        Assert.Null(caller.Subject);
        Assert.False(caller.IsSystem);
        Assert.False(caller.TryGetSubjectId(out _));
    }

    // ---------------------------------------------------------------------------------------
    // Storage: writes
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task StoragePutUsesTheExactRouteBytesAndContentType()
    {
        var provider = new FakeProvider();
        var context = LambdaContext.From(provider);

        await context.Storage.PutAsync(
            "saves/player-1.json",
            Encoding.UTF8.GetBytes("{\"level\":7}"),
            "application/json",
            TestContext.Current.CancellationToken);

        var call = Assert.Single(provider.Factory.Handler.Calls);
        Assert.Equal(HttpMethod.Put, call.Method);
        // The gateway's route is objects/{**key}: Kestrel keeps an encoded separator (%2F) encoded
        // all the way into the route value and the gateway refuses any key containing '%', so the
        // separators must travel as separators.
        Assert.Equal(
            "http://gateway.internal/v1/lambda-runtime/objects/saves/player-1.json",
            call.Uri);
        Assert.Equal("application/json", call.ContentType);
        Assert.Equal("{\"level\":7}", call.Body);
    }

    [Theory]
    [InlineData("saves/player 1.json", "objects/saves/player%201.json")]
    [InlineData("sé/x", "objects/s%C3%A9/x")]
    [InlineData("q?x/y#z", "objects/q%3Fx/y%23z")]
    public async Task StorageEscapesEachKeySegmentOnItsOwn(string key, string route)
    {
        var provider = new FakeProvider();
        var context = LambdaContext.From(provider);

        await context.Storage.PutAsync(key, "x"u8.ToArray(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("http://gateway.internal/v1/lambda-runtime/" + route, Assert.Single(provider.Factory.Handler.Calls).Uri);
    }

    [Fact]
    public async Task StoragePutSurfacesTheStoresConflictAsATypedRefusal()
    {
        var provider = new FakeProvider();
        provider.Factory.Handler.Respond(_ => Json(
            HttpStatusCode.Conflict,
            "{\"title\":\"Lambda runtime API request refused.\",\"status\":409,"
            + "\"detail\":\"The logical object key already identifies different immutable content.\","
            + "\"code\":\"object_conflict\"}"));
        var context = LambdaContext.From(provider);

        var exception = await Assert.ThrowsAsync<LambdaApiException>(() =>
            context.Storage.PutAsync("saves/a", "y"u8.ToArray(), cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal(LambdaRefusalCodes.ObjectConflict, exception.Code);
        Assert.Equal("The logical object key already identifies different immutable content.", exception.Message);
    }

    [Fact]
    public async Task StoragePutRefusesAnEmptyObjectBeforeTransport()
    {
        var provider = new FakeProvider();
        var context = LambdaContext.From(provider);

        await Assert.ThrowsAsync<ArgumentException>(() => context.Storage.PutAsync(
            "saves/a",
            ReadOnlyMemory<byte>.Empty,
            cancellationToken: TestContext.Current.CancellationToken));
        Assert.Empty(provider.Factory.Handler.Calls);
    }

    [Theory]
    [InlineData("")]
    [InlineData("application/json; charset=utf-8")]
    [InlineData("text/\nplain")]
    public async Task StoragePutRefusesAMediaTypeTheGatewayWouldRefuseBeforeTransport(string contentType)
    {
        var provider = new FakeProvider();
        var context = LambdaContext.From(provider);

        await Assert.ThrowsAsync<ArgumentException>(() => context.Storage.PutAsync(
            "saves/a",
            "x"u8.ToArray(),
            contentType,
            TestContext.Current.CancellationToken));
        Assert.Empty(provider.Factory.Handler.Calls);
    }

    [Fact]
    public async Task StorageReplaceDeletesThenWritesTheSameKey()
    {
        var provider = new FakeProvider();
        var context = LambdaContext.From(provider);

        await context.Storage.ReplaceAsync(
            "saves/player-1.json",
            "{}"u8.ToArray(),
            "application/json",
            TestContext.Current.CancellationToken);

        Assert.Collection(
            provider.Factory.Handler.Calls,
            delete =>
            {
                Assert.Equal(HttpMethod.Delete, delete.Method);
                Assert.Equal("http://gateway.internal/v1/lambda-runtime/objects/saves/player-1.json", delete.Uri);
            },
            put =>
            {
                Assert.Equal(HttpMethod.Put, put.Method);
                Assert.Equal("http://gateway.internal/v1/lambda-runtime/objects/saves/player-1.json", put.Uri);
                Assert.Equal("{}", put.Body);
            });
    }

    [Fact]
    public async Task StorageReplaceValidatesTheWriteBeforeDeletingAnything()
    {
        var provider = new FakeProvider();
        var context = LambdaContext.From(provider);

        await Assert.ThrowsAsync<ArgumentException>(() => context.Storage.ReplaceAsync(
            "saves/a",
            ReadOnlyMemory<byte>.Empty,
            cancellationToken: TestContext.Current.CancellationToken));
        Assert.Empty(provider.Factory.Handler.Calls);
    }

    // ---------------------------------------------------------------------------------------
    // Storage: reads and deletes
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task StorageGetReturnsTheObjectBytes()
    {
        var provider = new FakeProvider();
        provider.Factory.Handler.Respond(_ => Bytes(HttpStatusCode.OK, "payload"));
        var context = LambdaContext.From(provider);

        var content = await context.Storage.GetAsync("saves/a", TestContext.Current.CancellationToken);

        Assert.Equal("payload", Encoding.UTF8.GetString(content));
        var call = Assert.Single(provider.Factory.Handler.Calls);
        Assert.Equal(HttpMethod.Get, call.Method);
        Assert.Equal("http://gateway.internal/v1/lambda-runtime/objects/saves/a", call.Uri);
    }

    [Fact]
    public async Task StorageTryGetAnswersNullForAnAbsentKey()
    {
        var provider = new FakeProvider();
        provider.Factory.Handler.Respond(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var context = LambdaContext.From(provider);

        Assert.Null(await context.Storage.TryGetAsync("saves/a", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task StorageGetSurfacesAnAbsentKeyAsARefusal()
    {
        var provider = new FakeProvider();
        provider.Factory.Handler.Respond(_ =>
            Json(HttpStatusCode.NotFound, "{\"code\":\"object_not_found\",\"detail\":\"No such object.\"}"));
        var context = LambdaContext.From(provider);

        var exception = await Assert.ThrowsAsync<LambdaApiException>(() =>
            context.Storage.GetAsync("saves/a", TestContext.Current.CancellationToken));

        Assert.Equal(404, exception.StatusCode);
        Assert.Equal(LambdaRefusalCodes.ObjectNotFound, exception.Code);
        Assert.Equal("No such object.", exception.Message);
    }

    [Fact]
    public async Task StorageDeleteUsesTheExactRouteAndTreatsAnAbsentKeyAsDeleted()
    {
        var provider = new FakeProvider();
        // The gateway answers 204 whether or not the key existed; the wrapper has no "not found"
        // path to take, so the same response covers both.
        provider.Factory.Handler.Respond(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var context = LambdaContext.From(provider);

        await context.Storage.DeleteAsync("saves/a", TestContext.Current.CancellationToken);
        await context.Storage.DeleteAsync("saves/a", TestContext.Current.CancellationToken);

        Assert.All(provider.Factory.Handler.Calls, call =>
        {
            Assert.Equal(HttpMethod.Delete, call.Method);
            Assert.Equal("http://gateway.internal/v1/lambda-runtime/objects/saves/a", call.Uri);
        });
        Assert.Equal(2, provider.Factory.Handler.Calls.Count);
    }

    // ---------------------------------------------------------------------------------------
    // Storage: key rules
    // ---------------------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("/saves/a")]
    [InlineData("saves//a")]
    [InlineData("saves/a/")]
    [InlineData("saves/./a")]
    [InlineData("saves/../a")]
    [InlineData("saves\\a")]
    [InlineData("saves/a%2Fb")]
    [InlineData("saves/a\0b")]
    [InlineData("projects/11111111-1111-4111-8111-111111111111/saves/a")]
    [InlineData("s3://bucket/a")]
    public async Task AKeyTheGatewayWouldRefuseIsRefusedBeforeTransport(string key)
    {
        var provider = new FakeProvider();
        var context = LambdaContext.From(provider);
        var token = TestContext.Current.CancellationToken;

        await Assert.ThrowsAsync<ArgumentException>(() => context.Storage.PutAsync(key, "x"u8.ToArray(), cancellationToken: token));
        await Assert.ThrowsAsync<ArgumentException>(() => context.Storage.ReplaceAsync(key, "x"u8.ToArray(), cancellationToken: token));
        await Assert.ThrowsAsync<ArgumentException>(() => context.Storage.GetAsync(key, token));
        await Assert.ThrowsAsync<ArgumentException>(() => context.Storage.TryGetAsync(key, token));
        await Assert.ThrowsAsync<ArgumentException>(() => context.Storage.DeleteAsync(key, token));
        Assert.Empty(provider.Factory.Handler.Calls);
    }

    [Fact]
    public async Task AKeyLongerThanTheGatewaysBoundIsRefusedBeforeTransport()
    {
        var provider = new FakeProvider();
        var context = LambdaContext.From(provider);
        var longest = new string('k', LambdaRoutes.MaximumStorageKeyLength);

        await context.Storage.DeleteAsync(longest, TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            context.Storage.DeleteAsync(longest + "k", TestContext.Current.CancellationToken));
        Assert.Single(provider.Factory.Handler.Calls);
    }

    [Theory]
    [InlineData("environments")]
    [InlineData("environments/22222222-2222-4222-8222-222222222222/artifacts/bundle.zip")]
    public async Task TheReservedArtifactNamespaceIsRefusedForEveryVerbBeforeTransport(string key)
    {
        var provider = new FakeProvider();
        var context = LambdaContext.From(provider);
        var token = TestContext.Current.CancellationToken;

        // The same exception, status and code the gateway answers, so one catch path covers a
        // refusal decided here and one decided there.
        foreach (var attempt in new Func<Task>[]
        {
            () => context.Storage.PutAsync(key, "x"u8.ToArray(), cancellationToken: token),
            () => context.Storage.ReplaceAsync(key, "x"u8.ToArray(), cancellationToken: token),
            () => context.Storage.GetAsync(key, token),
            () => context.Storage.TryGetAsync(key, token),
            () => context.Storage.DeleteAsync(key, token),
            () => context.Storage.ListAsync(key, cancellationToken: token),
            () => context.Storage.ListAllAsync(key, token),
        })
        {
            var exception = await Assert.ThrowsAsync<LambdaApiException>(attempt);
            Assert.Equal(403, exception.StatusCode);
            Assert.Equal(LambdaRefusalCodes.KeyReservedPrefix, exception.Code);
        }

        Assert.Empty(provider.Factory.Handler.Calls);
    }

    [Theory]
    [InlineData("environments/")]
    [InlineData("environment")]
    [InlineData("e")]
    public async Task AListingPrefixThatWouldEnumerateTheReservedNamespaceIsRefusedBeforeTransport(string prefix)
    {
        var provider = new FakeProvider();
        var context = LambdaContext.From(provider);

        // Without a separator, "environment" string-matches into "environments/…", which is exactly
        // the reserved layout; the gateway refuses it as a prefix and so does the wrapper.
        var exception = await Assert.ThrowsAsync<LambdaApiException>(() =>
            context.Storage.ListAsync(prefix, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(LambdaRefusalCodes.KeyReservedPrefix, exception.Code);
        Assert.Empty(provider.Factory.Handler.Calls);
    }

    [Fact]
    public async Task ADirectoryThatMerelyStartsLikeTheReservedSegmentIsAnOrdinaryKeyAndPrefix()
    {
        var provider = new FakeProvider();
        provider.Factory.Handler.Respond(request => request.Method == HttpMethod.Get
            ? Json(HttpStatusCode.OK, "{\"objects\":[{\"key\":\"environment/settings.json\"}],\"nextCursor\":null}")
            : new HttpResponseMessage(HttpStatusCode.Created));
        var context = LambdaContext.From(provider);

        await context.Storage.PutAsync(
            "environment/settings.json",
            "{}"u8.ToArray(),
            "application/json",
            TestContext.Current.CancellationToken);
        var page = await context.Storage.ListAsync("environment/", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(["environment/settings.json"], page.Keys);
        Assert.Equal(2, provider.Factory.Handler.Calls.Count);
    }

    [Fact]
    public void TheReservedRulesAreSpelledAsTheGatewaySpellsThem()
    {
        Assert.True(LambdaRoutes.IsReservedStorageKey("environments"));
        Assert.True(LambdaRoutes.IsReservedStorageKey("environments/x"));
        Assert.False(LambdaRoutes.IsReservedStorageKey("environment/x"));
        Assert.False(LambdaRoutes.IsReservedStorageKey("environmentss/x"));
        Assert.True(LambdaRoutes.IsReservedStoragePrefix("environments/"));
        Assert.True(LambdaRoutes.IsReservedStoragePrefix("env"));
        Assert.False(LambdaRoutes.IsReservedStoragePrefix("environment/"));
        Assert.False(LambdaRoutes.IsReservedStoragePrefix("saves/e"));
        Assert.False(LambdaRoutes.IsReservedStoragePrefix(""));
    }

    // ---------------------------------------------------------------------------------------
    // Storage: listing
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task StorageListReadsTheGatewaysPageShape()
    {
        var provider = new FakeProvider();
        provider.Factory.Handler.Respond(_ => Json(
            HttpStatusCode.OK,
            "{\"objects\":[{\"key\":\"saves/a\"},{\"key\":\"saves/b\"}],\"nextCursor\":\"saves/b\"}"));
        var context = LambdaContext.From(provider);

        var page = await context.Storage.ListAsync("saves/", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ExpectedObjectKeys, page.Keys);
        Assert.Equal("saves/b", page.NextCursor);
        Assert.Equal(
            "http://gateway.internal/v1/lambda-runtime/objects?prefix=saves%2F",
            Assert.Single(provider.Factory.Handler.Calls).Uri);
    }

    [Fact]
    public async Task StorageListSendsTheCursorAndLimitAsQueryParameters()
    {
        var provider = new FakeProvider();
        provider.Factory.Handler.Respond(_ => Json(
            HttpStatusCode.OK,
            "{\"objects\":[{\"key\":\"saves/c\"}],\"nextCursor\":null}"));
        var context = LambdaContext.From(provider);

        var page = await context.Storage.ListAsync("saves/", "saves/b", 2, TestContext.Current.CancellationToken);

        Assert.Equal(["saves/c"], page.Keys);
        Assert.Null(page.NextCursor);
        Assert.Equal(
            "http://gateway.internal/v1/lambda-runtime/objects?prefix=saves%2F&cursor=saves%2Fb&limit=2",
            Assert.Single(provider.Factory.Handler.Calls).Uri);
    }

    [Fact]
    public async Task StorageListWithoutAPrefixOmitsTheQuery()
    {
        var provider = new FakeProvider();
        provider.Factory.Handler.Respond(_ => Json(HttpStatusCode.OK, "{\"objects\":[]}"));
        var context = LambdaContext.From(provider);

        var page = await context.Storage.ListAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(page.Keys);
        Assert.Null(page.NextCursor);
        Assert.Equal(
            "http://gateway.internal/v1/lambda-runtime/objects",
            Assert.Single(provider.Factory.Handler.Calls).Uri);
    }

    [Theory]
    [InlineData("{\"unexpected\":true}")]
    [InlineData("[\"saves/a\",\"saves/b\"]")]
    [InlineData("{\"objects\":[\"saves/a\"]}")]
    [InlineData("{\"objects\":[{\"key\":1}]}")]
    [InlineData("{\"objects\":[{\"name\":\"saves/a\"}]}")]
    [InlineData("{\"objects\":[],\"nextCursor\":5}")]
    public async Task StorageListRefusesAShapeThatIsNotTheGateways(string payload)
    {
        var provider = new FakeProvider();
        provider.Factory.Handler.Respond(_ => Json(HttpStatusCode.OK, payload));
        var context = LambdaContext.From(provider);

        var exception = await Assert.ThrowsAsync<LambdaApiException>(() =>
            context.Storage.ListAsync(cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(502, exception.StatusCode);
        Assert.Equal(LambdaRefusalCodes.ResponseInvalid, exception.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(501)]
    public async Task StorageListRefusesAPageSizeOutsideTheGatewaysBoundBeforeTransport(int limit)
    {
        var provider = new FakeProvider();
        var context = LambdaContext.From(provider);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            context.Storage.ListAsync(limit: limit, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Empty(provider.Factory.Handler.Calls);
    }

    [Fact]
    public async Task StorageListRefusesACursorTheGatewayCouldNotHaveIssuedBeforeTransport()
    {
        var provider = new FakeProvider();
        var context = LambdaContext.From(provider);

        await Assert.ThrowsAsync<ArgumentException>(() => context.Storage.ListAsync(
            cursor: new string('c', LambdaRoutes.MaximumStorageCursorLength + 1),
            cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => context.Storage.ListAsync(
            cursor: "saves/\nb",
            cancellationToken: TestContext.Current.CancellationToken));
        Assert.Empty(provider.Factory.Handler.Calls);
    }

    [Fact]
    public async Task AListingRefusalCarriesTheGatewaysCode()
    {
        var provider = new FakeProvider();
        provider.Factory.Handler.Respond(_ => Json(
            HttpStatusCode.BadRequest,
            "{\"code\":\"cursor_invalid\",\"detail\":\"The listing cursor is not one this gateway issued.\"}"));
        var context = LambdaContext.From(provider);

        var exception = await Assert.ThrowsAsync<LambdaApiException>(() =>
            context.Storage.ListAsync(cursor: "stale", cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(400, exception.StatusCode);
        Assert.Equal(LambdaRefusalCodes.CursorInvalid, exception.Code);
    }

    [Fact]
    public async Task StorageListAllWalksEveryPageWithTheLargestPageSize()
    {
        var provider = new FakeProvider();
        provider.Factory.Handler.Respond(request => request.RequestUri!.Query switch
        {
            var query when query.Contains("cursor=saves%2Fb", StringComparison.Ordinal) =>
                Json(HttpStatusCode.OK, "{\"objects\":[{\"key\":\"saves/c\"}],\"nextCursor\":\"saves/c\"}"),
            var query when query.Contains("cursor=saves%2Fc", StringComparison.Ordinal) =>
                Json(HttpStatusCode.OK, "{\"objects\":[],\"nextCursor\":null}"),
            _ => Json(HttpStatusCode.OK, "{\"objects\":[{\"key\":\"saves/a\"},{\"key\":\"saves/b\"}],\"nextCursor\":\"saves/b\"}"),
        });
        var context = LambdaContext.From(provider);

        var keys = await context.Storage.ListAllAsync("saves/", TestContext.Current.CancellationToken);

        Assert.Equal(["saves/a", "saves/b", "saves/c"], keys);
        Assert.Equal(
            [
                "http://gateway.internal/v1/lambda-runtime/objects?prefix=saves%2F&limit=500",
                "http://gateway.internal/v1/lambda-runtime/objects?prefix=saves%2F&cursor=saves%2Fb&limit=500",
                "http://gateway.internal/v1/lambda-runtime/objects?prefix=saves%2F&cursor=saves%2Fc&limit=500",
            ],
            provider.Factory.Handler.Calls.Select(call => call.Uri));
    }

    [Fact]
    public async Task StorageListAllRefusesACursorThatDoesNotAdvance()
    {
        var provider = new FakeProvider();
        provider.Factory.Handler.Respond(_ => Json(
            HttpStatusCode.OK,
            "{\"objects\":[{\"key\":\"saves/a\"}],\"nextCursor\":\"same\"}"));
        var context = LambdaContext.From(provider);

        var exception = await Assert.ThrowsAsync<LambdaApiException>(() =>
            context.Storage.ListAllAsync(cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(LambdaRefusalCodes.ResponseInvalid, exception.Code);
        Assert.Equal(2, provider.Factory.Handler.Calls.Count);
    }

    // ---------------------------------------------------------------------------------------
    // Users
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task UserGetReadsTheRecordAndKeepsUnmodelledFields()
    {
        var provider = new FakeProvider();
        provider.Factory.Handler.Respond(_ => Json(
            HttpStatusCode.OK,
            "{\"subject\":\"" + PlayerSubject + "\",\"email\":\"a@example.com\",\"username\":\"a\","
                + "\"emailConfirmed\":true,\"banned\":false,\"lastSeenAt\":\"2026-09-08T00:00:00Z\"}"));
        var context = LambdaContext.From(provider);

        // Any spelling of the UUID is accepted; the gateway receives the canonical lower-case D form.
        var user = await context.Users.GetAsync(PlayerSubject.ToUpperInvariant(), TestContext.Current.CancellationToken);

        Assert.Equal(PlayerSubject, user!.Subject);
        Assert.Equal("a@example.com", user.Email);
        Assert.Equal("a", user.Username);
        Assert.True(user.EmailConfirmed);
        Assert.False(user.Banned);
        Assert.Null(user.Attributes);
        Assert.True(user.Additional!.ContainsKey("lastSeenAt"));
        Assert.Equal(
            "http://gateway.internal/v1/lambda-runtime/users/" + PlayerSubject,
            Assert.Single(provider.Factory.Handler.Calls).Uri);
    }

    [Theory]
    [InlineData("")]
    [InlineData("player-1")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task ASubjectThatIsNotAUuidIsRefusedBeforeTransport(string subject)
    {
        var provider = new FakeProvider();
        var context = LambdaContext.From(provider);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            context.Users.GetAsync(subject, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            context.Users.BanAsync(subject, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            context.Users.IssueTokenAsync(subject, ["play"], TestContext.Current.CancellationToken));
        Assert.Empty(provider.Factory.Handler.Calls);
    }

    [Fact]
    public async Task UserGetAnswersNullForAnUnknownSubject()
    {
        var provider = new FakeProvider();
        provider.Factory.Handler.Respond(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var context = LambdaContext.From(provider);

        Assert.Null(await context.Users.GetAsync(PlayerSubject, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UserCreateSendsTheExactBody()
    {
        var provider = new FakeProvider();
        provider.Factory.Handler.Respond(_ => Json(
            HttpStatusCode.Created,
            "{\"subject\":\"" + PlayerSubject + "\",\"banned\":false}"));
        var context = LambdaContext.From(provider);

        var user = await context.Users.CreateAsync(
            new LambdaUserCreate
            {
                Email = "a@example.com",
                Attributes = new Dictionary<string, string>(StringComparer.Ordinal) { ["tier"] = "gold" },
            },
            TestContext.Current.CancellationToken);

        var call = Assert.Single(provider.Factory.Handler.Calls);
        Assert.Equal(HttpMethod.Post, call.Method);
        Assert.Equal("http://gateway.internal/v1/lambda-runtime/users", call.Uri);
        Assert.Equal("{\"email\":\"a@example.com\",\"attributes\":{\"tier\":\"gold\"}}", call.Body);
        Assert.Equal(PlayerSubject, user.Subject);
    }

    [Fact]
    public async Task UserCreateRefusesARecordWithNoIdentity()
    {
        var provider = new FakeProvider();
        var context = LambdaContext.From(provider);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            context.Users.CreateAsync(new LambdaUserCreate(), TestContext.Current.CancellationToken));
        Assert.Empty(provider.Factory.Handler.Calls);
    }

    [Fact]
    public async Task UserBanPatchesTheBanStateOnly()
    {
        var provider = new FakeProvider();
        provider.Factory.Handler.Respond(_ => Json(
            HttpStatusCode.OK,
            "{\"subject\":\"" + PlayerSubject + "\",\"banned\":true}"));
        var context = LambdaContext.From(provider);

        var user = await context.Users.BanAsync(PlayerSubject, cancellationToken: TestContext.Current.CancellationToken);

        var call = Assert.Single(provider.Factory.Handler.Calls);
        Assert.Equal("PATCH", call.Method.Method);
        Assert.Equal("http://gateway.internal/v1/lambda-runtime/users/" + PlayerSubject, call.Uri);
        Assert.Equal("{\"banned\":true}", call.Body);
        Assert.True(user.Banned);
    }

    [Fact]
    public async Task UserUpdateRefusesAnEmptyChange()
    {
        var provider = new FakeProvider();
        var context = LambdaContext.From(provider);

        await Assert.ThrowsAsync<ArgumentException>(() => context.Users.UpdateAsync(
            PlayerSubject,
            new LambdaUserUpdate(),
            TestContext.Current.CancellationToken));
        Assert.Empty(provider.Factory.Handler.Calls);
    }

    // ---------------------------------------------------------------------------------------
    // Users: token issuance
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task IssuingATokenReturnsTheCredentialAndItsExpiry()
    {
        var provider = new FakeProvider();
        provider.Factory.Handler.Respond(_ => Json(
            HttpStatusCode.OK,
            "{\"accessToken\":\"opaque\",\"expiresAt\":\"2026-09-08T01:00:00+00:00\"}"));
        var context = LambdaContext.From(provider);

        var token = await context.Users.IssueTokenAsync(
            PlayerSubject,
            ["play"],
            TestContext.Current.CancellationToken);

        var call = Assert.Single(provider.Factory.Handler.Calls);
        Assert.Equal(HttpMethod.Post, call.Method);
        Assert.Equal(
            "http://gateway.internal/v1/lambda-runtime/users/" + PlayerSubject + "/tokens",
            call.Uri);
        Assert.Equal("{\"scopes\":[\"play\"]}", call.Body);
        Assert.Equal("opaque", token.AccessToken);
        Assert.Equal(
            DateTimeOffset.Parse("2026-09-08T01:00:00+00:00", System.Globalization.CultureInfo.InvariantCulture),
            token.ExpiresAt);
    }

    [Fact]
    public async Task AnUnusableTokenIsRefusedRatherThanHandedToTheCaller()
    {
        var provider = new FakeProvider();
        provider.Factory.Handler.Respond(_ => Json(HttpStatusCode.OK, "{\"accessToken\":\"\"}"));
        var context = LambdaContext.From(provider);

        var exception = await Assert.ThrowsAsync<LambdaApiException>(() =>
            context.Users.IssueTokenAsync(PlayerSubject, ["play"], TestContext.Current.CancellationToken));

        Assert.Equal(LambdaRefusalCodes.ResponseInvalid, exception.Code);
    }

    [Fact]
    public async Task IssuingATokenRequiresOneToSixteenDistinctScopes()
    {
        var provider = new FakeProvider();
        var context = LambdaContext.From(provider);
        var sixteen = Enumerable.Range(0, 16).Select(index => "scope-" + index).ToArray();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            context.Users.IssueTokenAsync(PlayerSubject, [], TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            context.Users.IssueTokenAsync(PlayerSubject, [.. sixteen, "scope-16"], TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            context.Users.IssueTokenAsync(PlayerSubject, [" "], TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            context.Users.IssueTokenAsync(PlayerSubject, ["play\n"], TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            context.Users.IssueTokenAsync(PlayerSubject, [new string('s', 129)], TestContext.Current.CancellationToken));
        Assert.Empty(provider.Factory.Handler.Calls);

        // The gateway counts distinct scopes, so seventeen with a duplicate is sixteen — and the
        // duplicate is collapsed on the wire exactly as the gateway would collapse it.
        provider.Factory.Handler.Respond(_ => Json(
            HttpStatusCode.OK,
            "{\"accessToken\":\"opaque\",\"expiresAt\":\"2026-09-08T01:00:00+00:00\"}"));
        await context.Users.IssueTokenAsync(PlayerSubject, [.. sixteen, "scope-0"], TestContext.Current.CancellationToken);
        Assert.Equal(
            "{\"scopes\":[" + string.Join(",", sixteen.Select(scope => "\"" + scope + "\"")) + "]}",
            Assert.Single(provider.Factory.Handler.Calls).Body);
    }

    [Theory]
    [InlineData("openid")]
    [InlineData("offline_access")]
    public async Task TheScopesTheGrantNeverIssuesAreRefusedBeforeTransportWithTheGatewaysCode(string scope)
    {
        var provider = new FakeProvider();
        var context = LambdaContext.From(provider);

        // openid would ask for an identity token and offline_access for a refresh token; the
        // realm's Lambda grant mints one access token for one user and nothing else.
        var exception = await Assert.ThrowsAsync<LambdaApiException>(() =>
            context.Users.IssueTokenAsync(PlayerSubject, ["play", scope], TestContext.Current.CancellationToken));

        Assert.Equal(400, exception.StatusCode);
        Assert.Equal(LambdaRefusalCodes.ScopeNotAllowed, exception.Code);
        Assert.Contains(scope, exception.Message, StringComparison.Ordinal);
        Assert.Empty(provider.Factory.Handler.Calls);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "scope_not_allowed")]
    [InlineData(HttpStatusCode.NotFound, "user_not_found")]
    [InlineData(HttpStatusCode.Forbidden, "user_banned")]
    [InlineData(HttpStatusCode.Forbidden, "user_email_unconfirmed")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "user_token_issuance_failed")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "user_token_issuance_unavailable")]
    public async Task ARefusedIssuanceCarriesTheRealmsOwnCodeAndStatus(HttpStatusCode status, string code)
    {
        var provider = new FakeProvider();
        provider.Factory.Handler.Respond(_ => Json(
            status,
            "{\"title\":\"Lambda runtime API request refused.\",\"status\":" + (int)status + ","
            + "\"detail\":\"Refused.\",\"code\":\"" + code + "\"}"));
        var context = LambdaContext.From(provider);

        var exception = await Assert.ThrowsAsync<LambdaApiException>(() =>
            context.Users.IssueTokenAsync(PlayerSubject, ["play"], TestContext.Current.CancellationToken));

        Assert.Equal((int)status, exception.StatusCode);
        Assert.Equal(code, exception.Code);
        Assert.Equal("Refused.", exception.Message);
    }

    // ---------------------------------------------------------------------------------------
    // Realtime
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task RealtimePublishSerializesToTheTopicRoute()
    {
        var provider = new FakeProvider();
        var context = LambdaContext.From(provider);

        await context.Realtime.PublishAsync(
            "battle.1",
            new { winner = "a", score = 3 },
            TestContext.Current.CancellationToken);

        var call = Assert.Single(provider.Factory.Handler.Calls);
        Assert.Equal(HttpMethod.Post, call.Method);
        Assert.Equal(
            "http://gateway.internal/v1/lambda-runtime/realtime/topics/battle.1/publish",
            call.Uri);
        Assert.Equal("application/json", call.ContentType);
        Assert.Equal("{\"winner\":\"a\",\"score\":3}", call.Body);
    }

    [Theory]
    [InlineData("  ")]
    // The gateway checks a topic as a Realtime binding id — [a-z][a-z0-9._-]{0,95} — and answers
    // topic_invalid for anything else. Escaping a '/' would only carry the refusal to the wire.
    [InlineData("battle/1")]
    [InlineData("Battle")]
    [InlineData("1battle")]
    [InlineData("battle 1")]
    [InlineData("a234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567")]
    public async Task RealtimePublishRefusesATopicTheGatewayWouldRefuseBeforeTransport(string topic)
    {
        var provider = new FakeProvider();
        var context = LambdaContext.From(provider);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            context.Realtime.PublishAsync(topic, new { a = 1 }, TestContext.Current.CancellationToken));
        Assert.Empty(provider.Factory.Handler.Calls);
    }

    [Fact]
    public async Task RealtimePublishRefusesAnOversizedPayloadBeforeTransport()
    {
        var provider = new FakeProvider();
        var context = LambdaContext.From(provider);
        var oversized = "{\"a\":\"" + new string('x', LambdaRealtime.MaximumPayloadBytes) + "\"}";

        await Assert.ThrowsAsync<ArgumentException>(() =>
            context.Realtime.PublishJsonAsync("battle", oversized, TestContext.Current.CancellationToken));
        Assert.Empty(provider.Factory.Handler.Calls);
    }

    [Fact]
    public async Task AFailedPublishCarriesTheRuntimeRefusalCode()
    {
        var provider = new FakeProvider();
        provider.Factory.Handler.Respond(_ =>
            Json(HttpStatusCode.Forbidden, "{\"code\":\"topic_forbidden\",\"title\":\"Not your topic.\"}"));
        var context = LambdaContext.From(provider);

        var exception = await Assert.ThrowsAsync<LambdaApiException>(() =>
            context.Realtime.PublishAsync("battle", new { a = 1 }, TestContext.Current.CancellationToken));

        Assert.Equal(403, exception.StatusCode);
        Assert.Equal("topic_forbidden", exception.Code);
        Assert.Equal("Not your topic.", exception.Message);
    }

    [Fact]
    public async Task AnOpaqueFailureFallsBackToAStatusDerivedCode()
    {
        var provider = new FakeProvider();
        provider.Factory.Handler.Respond(_ => Bytes(HttpStatusCode.BadGateway, "<html>gateway</html>"));
        var context = LambdaContext.From(provider);

        var exception = await Assert.ThrowsAsync<LambdaApiException>(() =>
            context.Realtime.PublishAsync("battle", new { a = 1 }, TestContext.Current.CancellationToken));

        Assert.Equal(502, exception.StatusCode);
        Assert.Equal(LambdaRefusalCodes.RequestFailed, exception.Code);
    }

    // ---------------------------------------------------------------------------------------

    private static ClaimsPrincipal Principal(string claimType, string value) =>
        new(new ClaimsIdentity([new Claim(claimType, value)], "yogabase"));

    private static HttpResponseMessage Json(HttpStatusCode status, string json) => new(status)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static HttpResponseMessage Bytes(HttpStatusCode status, string body) => new(status)
    {
        Content = new ByteArrayContent(Encoding.UTF8.GetBytes(body)),
    };

    private sealed record RecordedCall(HttpMethod Method, string Uri, string? ContentType, string Body);

    private sealed class FakeProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> services = [];

        internal FakeProvider()
        {
            Logger = NullLogger.Instance;
            DataSource = new FakeDataSource();
            Factory = new FakeHttpClientFactory();
            services[typeof(ILogger)] = Logger;
            services[typeof(DbDataSource)] = DataSource;
            services[typeof(ClaimsPrincipal)] = Principal("sub", PlayerSubject);
            services[typeof(IReadOnlyDictionary<string, string>)] =
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [LambdaRoutes.InvocationIdKey] = "inv-1",
                    [LambdaRoutes.FunctionIdKey] = "battle-pass",
                    [LambdaRoutes.VersionKey] = "3",
                    [LambdaRoutes.EnvironmentIdKey] = "22222222-2222-4222-8222-222222222222",
                };
            services[typeof(IHttpClientFactory)] = Factory;
        }

        internal ILogger Logger { get; }
        internal DbDataSource DataSource { get; }
        internal FakeHttpClientFactory Factory { get; }

        internal void Remove(Type serviceType) => services.Remove(serviceType);

        internal void Register(Type serviceType, object instance) => services[serviceType] = instance;

        public object? GetService(Type serviceType) =>
            services.TryGetValue(serviceType, out var instance) ? instance : null;
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly List<string> requested = [];

        internal RecordingHandler Handler { get; } = new();

        internal IReadOnlyList<string> Requested => requested;

        public HttpClient CreateClient(string name)
        {
            requested.Add(name);
            return new HttpClient(Handler, disposeHandler: false)
            {
                BaseAddress = RuntimeRoot,
                DefaultRequestHeaders = { Authorization = new("Bearer", "per-invocation-token") },
            };
        }
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly List<RecordedCall> calls = [];
        private Func<HttpRequestMessage, HttpResponseMessage> respond =
            _ => new HttpResponseMessage(HttpStatusCode.NoContent);

        internal List<RecordedCall> Calls => calls;

        internal void Respond(Func<HttpRequestMessage, HttpResponseMessage> handler) => respond = handler;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            calls.Add(new RecordedCall(
                request.Method,
                request.RequestUri!.AbsoluteUri,
                request.Content?.Headers.ContentType?.MediaType,
                body));
            return respond(request);
        }
    }

    private sealed class FakeDataSource : DbDataSource
    {
        public override string ConnectionString => "Host=fake;Database=project";

        protected override DbConnection CreateDbConnection() =>
            throw new NotSupportedException("The fake data source never opens a connection.");
    }
}
