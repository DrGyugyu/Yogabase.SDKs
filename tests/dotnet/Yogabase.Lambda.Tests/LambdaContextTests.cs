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
    // Storage
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
        Assert.Equal(
            "http://gateway.internal/v1/lambda-runtime/objects/saves%2Fplayer-1.json",
            call.Uri);
        Assert.Equal("application/json", call.ContentType);
        Assert.Equal("{\"level\":7}", call.Body);
    }

    [Fact]
    public async Task StorageGetReturnsTheObjectBytes()
    {
        var provider = new FakeProvider();
        provider.Factory.Handler.Respond(_ => Bytes(HttpStatusCode.OK, "payload"));
        var context = LambdaContext.From(provider);

        var content = await context.Storage.GetAsync("saves/a", TestContext.Current.CancellationToken);

        Assert.Equal("payload", Encoding.UTF8.GetString(content));
        Assert.Equal(HttpMethod.Get, Assert.Single(provider.Factory.Handler.Calls).Method);
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
        Assert.Equal("object_not_found", exception.Code);
        Assert.Equal("No such object.", exception.Message);
    }

    [Fact]
    public async Task StorageDeleteUsesTheExactRoute()
    {
        var provider = new FakeProvider();
        var context = LambdaContext.From(provider);

        await context.Storage.DeleteAsync("saves/a", TestContext.Current.CancellationToken);

        var call = Assert.Single(provider.Factory.Handler.Calls);
        Assert.Equal(HttpMethod.Delete, call.Method);
        Assert.Equal("http://gateway.internal/v1/lambda-runtime/objects/saves%2Fa", call.Uri);
    }

    [Theory]
    [InlineData("[\"saves/a\",\"saves/b\"]")]
    [InlineData("[{\"key\":\"saves/a\"},{\"key\":\"saves/b\"}]")]
    [InlineData("{\"objects\":[\"saves/a\",\"saves/b\"]}")]
    [InlineData("{\"items\":[{\"key\":\"saves/a\"},{\"key\":\"saves/b\"}]}")]
    public async Task StorageListReadsEveryShapeTheRuntimeMightReturn(string payload)
    {
        var provider = new FakeProvider();
        provider.Factory.Handler.Respond(_ => Json(HttpStatusCode.OK, payload));
        var context = LambdaContext.From(provider);

        var keys = await context.Storage.ListAsync("saves/", TestContext.Current.CancellationToken);

        Assert.Equal(ExpectedObjectKeys, keys);
        Assert.Equal(
            "http://gateway.internal/v1/lambda-runtime/objects?prefix=saves%2F",
            Assert.Single(provider.Factory.Handler.Calls).Uri);
    }

    [Fact]
    public async Task StorageListRefusesAnUnrecognizedShape()
    {
        var provider = new FakeProvider();
        provider.Factory.Handler.Respond(_ => Json(HttpStatusCode.OK, "{\"unexpected\":true}"));
        var context = LambdaContext.From(provider);

        var exception = await Assert.ThrowsAsync<LambdaApiException>(() =>
            context.Storage.ListAsync(cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal("response_invalid", exception.Code);
    }

    [Fact]
    public async Task StorageListWithoutAPrefixOmitsTheQuery()
    {
        var provider = new FakeProvider();
        provider.Factory.Handler.Respond(_ => Json(HttpStatusCode.OK, "[]"));
        var context = LambdaContext.From(provider);

        await context.Storage.ListAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(
            "http://gateway.internal/v1/lambda-runtime/objects",
            Assert.Single(provider.Factory.Handler.Calls).Uri);
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
            "{\"subject\":\"" + PlayerSubject + "\",\"email\":\"a@example.com\",\"banned\":false,"
                + "\"attributes\":{\"tier\":\"gold\"},\"lastSeenAt\":\"2026-09-08T00:00:00Z\"}"));
        var context = LambdaContext.From(provider);

        var user = await context.Users.GetAsync(PlayerSubject, TestContext.Current.CancellationToken);

        Assert.Equal(PlayerSubject, user!.Subject);
        Assert.Equal("a@example.com", user.Email);
        Assert.False(user.Banned);
        Assert.Equal("gold", user.Attributes!["tier"]);
        Assert.True(user.Additional!.ContainsKey("lastSeenAt"));
        Assert.Equal(
            "http://gateway.internal/v1/lambda-runtime/users/" + PlayerSubject,
            Assert.Single(provider.Factory.Handler.Calls).Uri);
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
            context.Users.IssueTokenAsync(PlayerSubject, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal("response_invalid", exception.Code);
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
            "battle/1",
            new { winner = "a", score = 3 },
            TestContext.Current.CancellationToken);

        var call = Assert.Single(provider.Factory.Handler.Calls);
        Assert.Equal(HttpMethod.Post, call.Method);
        Assert.Equal(
            "http://gateway.internal/v1/lambda-runtime/realtime/topics/battle%2F1/publish",
            call.Uri);
        Assert.Equal("application/json", call.ContentType);
        Assert.Equal("{\"winner\":\"a\",\"score\":3}", call.Body);
    }

    [Fact]
    public async Task RealtimePublishRefusesAnEmptyTopicBeforeTransport()
    {
        var provider = new FakeProvider();
        var context = LambdaContext.From(provider);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            context.Realtime.PublishAsync("  ", new { a = 1 }, TestContext.Current.CancellationToken));
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
            context.Realtime.PublishAsync("battle/1", new { a = 1 }, TestContext.Current.CancellationToken));

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
            context.Realtime.PublishAsync("battle/1", new { a = 1 }, TestContext.Current.CancellationToken));

        Assert.Equal(502, exception.StatusCode);
        Assert.Equal("request_failed", exception.Code);
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

        internal IReadOnlyList<RecordedCall> Calls => calls;

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
