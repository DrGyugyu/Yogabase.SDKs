using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;
using Yogabase.Lambda;

namespace Yogabase.Lambda.Tests;

public sealed class LambdaSubstratesTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private const string Instance = "11111111-1111-4111-8111-111111111111";

    [Fact]
    public async Task ActorWrapperUsesNamedClientExplicitVersionAndEscapedInstanceSegment()
    {
        using var provider = new Provider(_ => Json(HttpStatusCode.OK, "{\"status\":0}"));
        var result = await LambdaContext.From(provider).Actors.InvokeAsync("counter", "customer:42", "7",
            new { amount = 2 }, "request-1", Token);
        Assert.Equal(0, result.GetProperty("status").GetInt32());
        Assert.Equal("yogabase-actors", Assert.Single(provider.Names));
        var call = Assert.Single(provider.Calls);
        Assert.Equal("/v1/lambda-runtime/actors/counter/instances/customer%3A42/invoke", call.Path);
        using var body = JsonDocument.Parse(call.Body);
        Assert.Equal("7", body.RootElement.GetProperty("functionVersion").GetString());
        Assert.Equal("request-1", body.RootElement.GetProperty("idempotencyKey").GetString());
        Assert.Equal(2, body.RootElement.GetProperty("payload").GetProperty("amount").GetInt32());
        Assert.DoesNotContain("projectId", call.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WorkflowsUseVersionedStartAndExactReadAndCancelRoutes()
    {
        using var provider = new Provider(request => request.Method == HttpMethod.Get
            ? Json(HttpStatusCode.OK, "{\"status\":\"running\"}")
            : request.RequestUri!.AbsolutePath.EndsWith("/cancel", StringComparison.Ordinal)
                ? new HttpResponseMessage(HttpStatusCode.NoContent)
                : Json(HttpStatusCode.OK, "{\"workflowInstanceId\":\"" + Instance + "\"}"));
        var context = LambdaContext.From(provider);
        Assert.Equal(Instance, await context.Workflows.StartAsync("onboard", "3", new { customer = 1 }, "start", Token));
        Assert.Equal("running", (await context.Workflows.GetAsync(Instance, Token)).GetProperty("status").GetString());
        await context.Workflows.CancelAsync(Instance, "cancel", Token);
        Assert.Equal("yogabase-workflows", Assert.Single(provider.Names));
        Assert.Equal("/v1/lambda-runtime/workflows/onboard/start", provider.Calls[0].Path);
        Assert.Equal("/v1/lambda-runtime/workflows/instances/" + Instance, provider.Calls[1].Path);
        Assert.Equal("/v1/lambda-runtime/workflows/instances/" + Instance + "/cancel", provider.Calls[2].Path);
        using var body = JsonDocument.Parse(provider.Calls[0].Body);
        Assert.Equal("3", body.RootElement.GetProperty("definitionVersion").GetString());
        Assert.Equal(1, body.RootElement.GetProperty("input").GetProperty("customer").GetInt32());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{\"workflowInstanceId\":1}")]
    [InlineData("{\"workflowInstanceId\":\"not-a-guid\"}")]
    public async Task MalformedWorkflowSuccessKeepsTheTypedResponseRefusal(string body)
    {
        using var provider = new Provider(_ => Json(HttpStatusCode.OK, body));
        var error = await Assert.ThrowsAsync<LambdaApiException>(() =>
            LambdaContext.From(provider).Workflows.StartAsync("onboard", "1", new { }, cancellationToken: Token));
        Assert.Equal(502, error.StatusCode);
        Assert.Equal(LambdaRefusalCodes.ResponseInvalid, error.Code);
    }

    [Fact]
    public async Task NewReminderUsesCreatePreconditionAndExistingReminderUsesCurrentETag()
    {
        var exists = false;
        using var provider = new Provider(request =>
        {
            if (request.Method == HttpMethod.Get)
                return exists ? Tagged("\"schedule-v2-11111111111141118111111111111111\"") : Json(HttpStatusCode.NotFound, "{}");
            exists = true;
            return Json(HttpStatusCode.Created, "{}");
        });
        var reminders = LambdaContext.From(provider).Reminders;
        var due = new DateTimeOffset(2026, 9, 9, 8, 0, 0, TimeSpan.Zero);
        await reminders.SetAsync("later", "plain", "2", new { order = 3 }, dueAt: due, cancellationToken: Token);
        Assert.Equal("*", provider.Calls[1].IfNoneMatch);
        using var body = JsonDocument.Parse(provider.Calls[1].Body);
        Assert.Equal("plain", body.RootElement.GetProperty("functionId").GetString());
        Assert.Equal("2", body.RootElement.GetProperty("functionVersion").GetString());
        Assert.Equal(due, body.RootElement.GetProperty("dueAt").GetDateTimeOffset());
        await reminders.SetAsync("later", "plain", "2", new { order = 4 }, dueAt: due, cancellationToken: Token);
        Assert.Equal("\"schedule-v2-11111111111141118111111111111111\"", provider.Calls[3].IfMatch);
        Assert.Equal("yogabase-reminders", Assert.Single(provider.Names));
    }

    [Fact]
    public async Task ReminderRaceIsReturnedWithoutOverwritingNewerStateOrRetrying()
    {
        using var provider = new Provider(request => request.Method == HttpMethod.Get
            ? Tagged("\"schedule-v2-11111111111141118111111111111111\"")
            : Json(HttpStatusCode.PreconditionFailed, "{\"code\":\"reminder_changed\"}"));
        var error = await Assert.ThrowsAsync<LambdaApiException>(() => LambdaContext.From(provider).Reminders.SetAsync(
            "recurring", "plain", "1", new { }, cron: "*/5 * * * *", cancellationToken: Token));
        Assert.Equal(412, error.StatusCode);
        Assert.Equal("reminder_changed", error.Code);
        Assert.Equal(2, provider.Calls.Count);
    }

    [Fact]
    public async Task ReminderClearRequiresCurrentETagAndMissingReminderIsAlreadyClear()
    {
        var exists = true;
        using var provider = new Provider(request => request.Method == HttpMethod.Get
            ? exists ? Tagged("\"schedule-v2-11111111111141118111111111111111\"") : Json(HttpStatusCode.NotFound, "{}")
            : new HttpResponseMessage(HttpStatusCode.NoContent));
        var reminders = LambdaContext.From(provider).Reminders;
        await reminders.ClearAsync("later", Token);
        Assert.Equal(HttpMethod.Delete, provider.Calls[1].Method);
        Assert.Equal("\"schedule-v2-11111111111141118111111111111111\"", provider.Calls[1].IfMatch);
        exists = false;
        await reminders.ClearAsync("later", Token);
        Assert.Equal(3, provider.Calls.Count);
    }

    [Fact]
    public async Task ReminderWithoutETagOrWithoutExactlyOneScheduleRefuses()
    {
        using var provider = new Provider(_ => Json(HttpStatusCode.OK, "{}"));
        var reminders = LambdaContext.From(provider).Reminders;
        var error = await Assert.ThrowsAsync<LambdaApiException>(() => reminders.ClearAsync("later", Token));
        Assert.Equal(LambdaRefusalCodes.ResponseInvalid, error.Code);
        await Assert.ThrowsAsync<ArgumentException>(() => reminders.SetAsync("later", "plain", "1", new { }, cancellationToken: Token));
        await Assert.ThrowsAsync<ArgumentException>(() => reminders.SetAsync("later", "plain", "1", new { }, cron: " ", cancellationToken: Token));
        Assert.Single(provider.Calls);
    }

    [Fact]
    public async Task MessagingEscapesEachTopicSegmentAndPropagatesUnboundKafkaRefusal()
    {
        using var provider = new Provider(request => request.RequestUri!.Query.Contains("kafka", StringComparison.Ordinal)
            ? Json(HttpStatusCode.ServiceUnavailable, "{\"code\":\"messaging_substrate_unavailable\"}")
            : new HttpResponseMessage(HttpStatusCode.Accepted));
        var messaging = LambdaContext.From(provider).Messaging;
        await messaging.PublishAsync("orders/player 42", "hello"u8.ToArray(), qos: 1, cancellationToken: Token);
        Assert.Equal("/v1/lambda-runtime/topics/orders/player%2042/messages?protocol=mqtt", provider.Calls[0].Path);
        using var body = JsonDocument.Parse(provider.Calls[0].Body);
        Assert.Equal("aGVsbG8=", body.RootElement.GetProperty("payloadBase64").GetString());
        Assert.Equal(1, body.RootElement.GetProperty("qos").GetInt32());
        var error = await Assert.ThrowsAsync<LambdaApiException>(() => messaging.PublishAsync("orders",
            "hello"u8.ToArray(), protocol: "kafka", cancellationToken: Token));
        Assert.Equal(LambdaMessaging.SubstrateUnavailable, error.Code);
        Assert.Equal(503, error.StatusCode);
        Assert.Equal("yogabase-messaging", Assert.Single(provider.Names));
    }

    [Fact]
    public async Task KafkaPublicationUsesTheSameNamedHttpClientAndWaitsForAcknowledgement()
    {
        using var provider = new Provider(_ => new HttpResponseMessage(HttpStatusCode.Accepted));
        await LambdaContext.From(provider).Messaging.PublishAsync("orders.created-v1", "hello"u8.ToArray(),
            protocol: "kafka", cancellationToken: Token);
        var call = Assert.Single(provider.Calls);
        Assert.Equal("/v1/lambda-runtime/topics/orders.created-v1/messages?protocol=kafka", call.Path);
        Assert.Equal("yogabase-messaging", Assert.Single(provider.Names));
        using var body = JsonDocument.Parse(call.Body);
        Assert.Equal(1, body.RootElement.GetProperty("qos").GetInt32());
        Assert.Equal("aGVsbG8=", body.RootElement.GetProperty("payloadBase64").GetString());
        Assert.DoesNotContain("projectId", call.Body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("orders/created")]
    [InlineData("p.foreign.orders")]
    [InlineData("orders%2Fcreated")]
    [InlineData("orders+#")]
    [InlineData("orders created")]
    [InlineData("é")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("")]
    public async Task InvalidKafkaTopicsNeverReachTheRuntimeApi(string topic)
    {
        using var provider = new Provider(_ => new HttpResponseMessage(HttpStatusCode.Accepted));
        await Assert.ThrowsAsync<ArgumentException>(() => LambdaContext.From(provider).Messaging.PublishAsync(
            topic, ReadOnlyMemory<byte>.Empty, protocol: "kafka", cancellationToken: Token));
        Assert.Empty(provider.Calls);
    }

    [Fact]
    public async Task KafkaRequiresBoundedPayloadTopicAndAcknowledgedPublication()
    {
        using var provider = new Provider(_ => new HttpResponseMessage(HttpStatusCode.Accepted));
        var messaging = LambdaContext.From(provider).Messaging;
        await Assert.ThrowsAsync<ArgumentException>(() => messaging.PublishAsync(new string('a', 201),
            ReadOnlyMemory<byte>.Empty, protocol: "kafka", cancellationToken: Token));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => messaging.PublishAsync("orders",
            new byte[32769], protocol: "kafka", cancellationToken: Token));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => messaging.PublishAsync("orders",
            ReadOnlyMemory<byte>.Empty, qos: 0, protocol: "kafka", cancellationToken: Token));
        Assert.Empty(provider.Calls);
        await messaging.PublishAsync(new string('a', 200), new byte[32768], protocol: "kafka", cancellationToken: Token);
        Assert.Single(provider.Calls);
    }

    [Fact]
    public async Task AFailedKafkaPublishIsTypedAndNeverRetried()
    {
        using var provider = new Provider(_ => Json(HttpStatusCode.ServiceUnavailable, "{\"code\":\"messaging_publish_failed\"}"));
        var error = await Assert.ThrowsAsync<LambdaApiException>(() => LambdaContext.From(provider).Messaging.PublishAsync(
            "orders", ReadOnlyMemory<byte>.Empty, protocol: "kafka", cancellationToken: Token));
        Assert.Equal("messaging_publish_failed", error.Code);
        Assert.Equal(503, error.StatusCode);
        Assert.Single(provider.Calls);
    }

    [Fact]
    public async Task KafkaDoesNotTreatAnUnrecognizedSuccessAsPersistedAcknowledgement()
    {
        using var provider = new Provider(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var error = await Assert.ThrowsAsync<LambdaApiException>(() => LambdaContext.From(provider).Messaging.PublishAsync(
            "orders", ReadOnlyMemory<byte>.Empty, protocol: "kafka", cancellationToken: Token));
        Assert.Equal(LambdaRefusalCodes.ResponseInvalid, error.Code);
        Assert.Single(provider.Calls);
    }

    [Fact]
    public async Task InvalidRoutesWildcardsAndOversizedMessagesNeverReachTransport()
    {
        using var provider = new Provider(_ => new HttpResponseMessage(HttpStatusCode.Accepted));
        var context = LambdaContext.From(provider);
        await Assert.ThrowsAsync<ArgumentException>(() => context.Actors.InvokeAsync("counter", "a/b", "1", new { }, cancellationToken: Token));
        await Assert.ThrowsAsync<ArgumentException>(() => context.Messaging.PublishAsync("orders/#", ReadOnlyMemory<byte>.Empty, cancellationToken: Token));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => context.Messaging.PublishAsync("orders", new byte[32 * 1024 + 1], cancellationToken: Token));
        Assert.Empty(provider.Calls);
    }

    [Fact]
    public void ValkeyIsTheActualRuntimeMultiplexerAndItsDatabaseAndSubscriberAreDirect()
    {
        using var provider = new Provider(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var context = LambdaContext.From(provider);
        Assert.Contains("valkey-lambda-access", Assert.Throws<InvalidOperationException>(() => context.Valkey).Message, StringComparison.Ordinal);
        var database = DispatchProxy.Create<IDatabase, RedisProxy>();
        var subscriber = DispatchProxy.Create<ISubscriber, RedisProxy>();
        var multiplexer = DispatchProxy.Create<IConnectionMultiplexer, RedisProxy>();
        ((RedisProxy)(object)multiplexer).Database = database;
        ((RedisProxy)(object)multiplexer).Subscriber = subscriber;
        provider.Valkey = multiplexer;
        Assert.Same(multiplexer, context.Valkey);
        Assert.Same(database, context.Cache);
        Assert.Same(subscriber, context.Valkey.GetSubscriber());
        Assert.Empty(provider.Calls);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status)
        { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private static HttpResponseMessage Tagged(string tag)
    {
        var response = Json(HttpStatusCode.OK, "{}");
        response.Headers.ETag = EntityTagHeaderValue.Parse(tag);
        return response;
    }

    private sealed record Call(HttpMethod Method, string Path, string Body, string IfMatch, string IfNoneMatch);

    private sealed class Provider : HttpMessageHandler, IServiceProvider, IHttpClientFactory
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> respond;
        public List<Call> Calls { get; } = [];
        public List<string> Names { get; } = [];
        public IConnectionMultiplexer? Valkey { get; set; }
        public Provider(Func<HttpRequestMessage, HttpResponseMessage> respond) => this.respond = respond;
        public object? GetService(Type serviceType) => serviceType == typeof(ILogger) ? NullLogger.Instance
            : serviceType == typeof(ClaimsPrincipal) ? new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "lambda:caller")], "runtime"))
            : serviceType == typeof(IHttpClientFactory) ? this
            : serviceType == typeof(IConnectionMultiplexer) ? Valkey
            : serviceType == typeof(IReadOnlyDictionary<string, string>) ? new Dictionary<string, string>
            {
                ["yogabase.invocationId"] = "invocation", ["yogabase.functionId"] = "caller",
                ["yogabase.version"] = "1", ["yogabase.environmentId"] = Instance,
            } : null;
        public HttpClient CreateClient(string name)
        {
            Names.Add(name);
            return new HttpClient(this, disposeHandler: false) { BaseAddress = new Uri("http://runtime/v1/lambda-runtime/") };
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add(new Call(request.Method, request.RequestUri!.GetComponents(UriComponents.PathAndQuery, UriFormat.UriEscaped),
                request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken),
                request.Headers.TryGetValues("If-Match", out var match) ? string.Join(",", match) : string.Empty,
                request.Headers.TryGetValues("If-None-Match", out var none) ? string.Join(",", none) : string.Empty));
            return respond(request);
        }
    }

    public class RedisProxy : DispatchProxy
    {
        public IDatabase? Database { get; set; }
        public ISubscriber? Subscriber { get; set; }
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "GetDatabase" => Database,
            "GetSubscriber" => Subscriber,
            _ => throw new NotSupportedException(targetMethod?.Name),
        };
    }
}
