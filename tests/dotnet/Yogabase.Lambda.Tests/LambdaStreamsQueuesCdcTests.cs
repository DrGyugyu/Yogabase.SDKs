using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Yogabase.Lambda;

namespace Yogabase.Lambda.Tests;

/// <summary>
/// The L2 follow-ups (owner decision, 2026-09-17): the runtime's <see cref="IDistributedCache"/>,
/// stream read and partition declaration, durable-queue receive with acknowledgement, and the
/// <c>yogabase.cdc.v1</c> event and record-key parser, each spelled as the product's runtime API
/// and CDC publisher spell them.
/// </summary>
public sealed class LambdaStreamsQueuesCdcTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private const string Delivery = "0123456789abcdef0123456789abcdef";

    [Fact]
    public void DistributedCacheIsTheRuntimesOwnServiceAndItsAbsenceIsNamed()
    {
        using var provider = new Provider(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var context = LambdaContext.From(provider);
        var missing = Assert.Throws<InvalidOperationException>(() => context.DistributedCache);
        Assert.Contains("valkey-lambda-cache", missing.Message, StringComparison.Ordinal);

        var cache = new MemoryCacheStandIn();
        provider.Cache = cache;
        Assert.Same(cache, context.DistributedCache);
        Assert.Empty(provider.Calls);
    }

    [Fact]
    public async Task AStreamReadFromAnOffsetSpellsTheRuntimeRequestAndDecodesRecords()
    {
        using var provider = new Provider(_ => Json(HttpStatusCode.OK, """
            {"records":[
              {"partition":0,"offset":41,"timestampUnixMs":1790000000000,"keyBase64":"a2V5","valueBase64":"eyJhIjoxfQ==","headers":{"h":"v"}},
              {"partition":0,"offset":42,"timestampUnixMs":1790000000001,"keyBase64":null,"valueBase64":"","headers":{}}],
             "next":[{"partition":0,"offset":43}],"endOfTopic":true}
            """));
        var result = await LambdaContext.From(provider).Messaging.ReadStreamAsync(
            "orders.events", LambdaStreamReadRequest.StartingAt(41, partition: 0) with { Limit = 2, MaxBytes = 4096, WaitSeconds = 1 }, Token);

        var call = Assert.Single(provider.Calls);
        Assert.Equal("yogabase-messaging", Assert.Single(provider.Names));
        Assert.Equal(HttpMethod.Post, call.Method);
        Assert.Equal("/v1/lambda-runtime/streams/orders.events/read", call.Path);
        Assert.Equal("{\"partition\":0,\"fromOffset\":41,\"limit\":2,\"maxBytes\":4096,\"waitSeconds\":1}", call.Body);
        Assert.Equal(2, result.Records.Count);
        Assert.Equal("key", Encoding.UTF8.GetString(result.Records[0].Key!));
        Assert.Equal("{\"a\":1}", Encoding.UTF8.GetString(result.Records[0].Value));
        Assert.Equal("v", result.Records[0].Headers["h"]);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1790000000000), result.Records[0].Timestamp);
        Assert.Null(result.Records[1].Key);
        Assert.Empty(result.Records[1].Value);
        Assert.Equal((0, 43L), (result.Next[0].Partition, result.Next[0].Offset));
        Assert.True(result.EndOfTopic);
    }

    [Fact]
    public async Task AStreamReadFromATimestampSendsOnlyThatPosition()
    {
        using var provider = new Provider(_ => Json(HttpStatusCode.OK, "{\"records\":[],\"next\":[],\"endOfTopic\":true}"));
        await LambdaContext.From(provider).Messaging.ReadStreamAsync(
            "orders", LambdaStreamReadRequest.StartingAt(DateTimeOffset.Parse("2026-09-22T01:02:03Z", System.Globalization.CultureInfo.InvariantCulture)), Token);

        using var body = JsonDocument.Parse(Assert.Single(provider.Calls).Body);
        Assert.Equal("2026-09-22T01:02:03+00:00", body.RootElement.GetProperty("fromTimestamp").GetString());
        Assert.False(body.RootElement.TryGetProperty("fromOffset", out _));
        Assert.False(body.RootElement.TryGetProperty("partition", out _));
        Assert.Equal(16, body.RootElement.GetProperty("limit").GetInt32());
        Assert.Equal(256 * 1024, body.RootElement.GetProperty("maxBytes").GetInt32());
    }

    public static TheoryData<LambdaStreamReadRequest> OutOfBoundReads() => new()
    {
        new LambdaStreamReadRequest(),                                                   // no position
        LambdaStreamReadRequest.StartingAt(1) with { FromTimestamp = DateTimeOffset.UnixEpoch }, // both positions
        LambdaStreamReadRequest.StartingAt(-1),
        LambdaStreamReadRequest.StartingAt(0, partition: 1024),
        LambdaStreamReadRequest.StartingAt(0) with { Limit = 101 },
        LambdaStreamReadRequest.StartingAt(0) with { Limit = 0 },
        LambdaStreamReadRequest.StartingAt(0) with { MaxBytes = 1024 * 1024 + 1 },
        LambdaStreamReadRequest.StartingAt(0) with { WaitSeconds = 6 },
        LambdaStreamReadRequest.StartingAt(DateTimeOffset.UnixEpoch.AddTicks(-1)),
    };

    [Theory]
    [MemberData(nameof(OutOfBoundReads))]
    public async Task AReadOutsideTheRuntimeBoundsNeverLeavesTheLambda(LambdaStreamReadRequest request)
    {
        using var provider = new Provider(_ => Json(HttpStatusCode.OK, "{}"));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => LambdaContext.From(provider).Messaging.ReadStreamAsync("orders", request, Token));
        Assert.Empty(provider.Calls);
    }

    [Theory]
    [InlineData("p.orders")]
    [InlineData("..")]
    [InlineData("orders/eu")]
    [InlineData("")]
    public async Task AStreamOrQueueNameOutsideTheGrammarNeverLeavesTheLambda(string name)
    {
        using var provider = new Provider(_ => Json(HttpStatusCode.OK, "{}"));
        var messaging = LambdaContext.From(provider).Messaging;
        await Assert.ThrowsAsync<ArgumentException>(() => messaging.ReadStreamAsync(name, LambdaStreamReadRequest.StartingAt(0), Token));
        await Assert.ThrowsAsync<ArgumentException>(() => messaging.DeclareStreamPartitionsAsync(name, 4, Token));
        await Assert.ThrowsAsync<ArgumentException>(() => messaging.ReceiveAsync(name, cancellationToken: Token));
        await Assert.ThrowsAsync<ArgumentException>(() => messaging.AckAsync(name, Delivery, Token));
        Assert.Empty(provider.Calls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(33)]
    public async Task APartitionDeclarationOutsideOneToThirtyTwoIsRefused(int partitions)
    {
        using var provider = new Provider(_ => Json(HttpStatusCode.OK, "{}"));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            LambdaContext.From(provider).Messaging.DeclareStreamPartitionsAsync("orders", partitions, Token));
        Assert.Empty(provider.Calls);
    }

    [Fact]
    public async Task APartitionDeclarationReturnsTheStreamAfterIt()
    {
        using var provider = new Provider(_ => Json(HttpStatusCode.OK, "{\"topic\":\"orders\",\"partitions\":8}"));
        var stream = await LambdaContext.From(provider).Messaging.DeclareStreamPartitionsAsync("orders", 4, Token);

        var call = Assert.Single(provider.Calls);
        Assert.Equal("/v1/lambda-runtime/streams/orders/partitions", call.Path);
        Assert.Equal("{\"partitions\":4}", call.Body);
        Assert.Equal(("orders", 8), (stream.Topic, stream.Partitions));
    }

    [Fact]
    public async Task AQueueReceiveLeasesDeliveriesAndSettlesThem()
    {
        using var provider = new Provider(request => request.RequestUri!.AbsolutePath.EndsWith("/receive", StringComparison.Ordinal)
            ? Json(HttpStatusCode.OK, "{\"deliveries\":[{\"deliveryId\":\"" + Delivery + "\",\"bodyBase64\":\"aGk=\",\"redelivered\":true,\"leaseExpiresAt\":\"2026-09-22T00:00:30+00:00\"}]}")
            : new HttpResponseMessage(HttpStatusCode.NoContent));
        var messaging = LambdaContext.From(provider).Messaging;

        var deliveries = await messaging.ReceiveAsync("jobs", maxMessages: 5, leaseSeconds: 60, waitSeconds: 10, cancellationToken: Token);
        var delivery = Assert.Single(deliveries);
        Assert.Equal(Delivery, delivery.DeliveryId);
        Assert.Equal("hi", Encoding.UTF8.GetString(delivery.Body));
        Assert.True(delivery.Redelivered);
        Assert.Equal(DateTimeOffset.Parse("2026-09-22T00:00:30Z", System.Globalization.CultureInfo.InvariantCulture), delivery.LeaseExpiresAt);

        Assert.True(await messaging.AckAsync("jobs", delivery.DeliveryId, Token));
        Assert.True(await messaging.NackAsync("jobs", delivery.DeliveryId, requeue: false, Token));

        Assert.Equal("/v1/lambda-runtime/queues/jobs/receive", provider.Calls[0].Path);
        Assert.Equal("{\"maxMessages\":5,\"leaseSeconds\":60,\"waitSeconds\":10}", provider.Calls[0].Body);
        Assert.Equal("/v1/lambda-runtime/queues/jobs/deliveries/" + Delivery + "/ack", provider.Calls[1].Path);
        Assert.Equal("/v1/lambda-runtime/queues/jobs/deliveries/" + Delivery + "/nack", provider.Calls[2].Path);
        Assert.Equal("{\"requeue\":false}", provider.Calls[2].Body);
    }

    [Fact]
    public async Task AnAcknowledgementOfADeliveryNoLongerLeasedAnswersFalse()
    {
        using var provider = new Provider(_ => Json(HttpStatusCode.NotFound, "{\"code\":\"queue_delivery_unknown\"}"));
        Assert.False(await LambdaContext.From(provider).Messaging.AckAsync("jobs", Delivery, Token));
    }

    [Theory]
    [InlineData(0, 30, 0)]
    [InlineData(11, 30, 0)]
    [InlineData(1, 4, 0)]
    [InlineData(1, 301, 0)]
    [InlineData(1, 30, 21)]
    public async Task AReceiveOutsideTheRuntimeBoundsNeverLeavesTheLambda(int maxMessages, int leaseSeconds, int waitSeconds)
    {
        using var provider = new Provider(_ => Json(HttpStatusCode.OK, "{\"deliveries\":[]}"));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            LambdaContext.From(provider).Messaging.ReceiveAsync("jobs", maxMessages, leaseSeconds, waitSeconds, Token));
        Assert.Empty(provider.Calls);
    }

    [Theory]
    [InlineData("0123456789ABCDEF0123456789ABCDEF")]
    [InlineData("0123")]
    [InlineData("0123456789abcdef0123456789abcdeg")]
    public async Task ADeliveryIdOutsideThirtyTwoLowercaseHexIsRefused(string deliveryId)
    {
        using var provider = new Provider(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        await Assert.ThrowsAsync<ArgumentException>(() => LambdaContext.From(provider).Messaging.NackAsync("jobs", deliveryId, cancellationToken: Token));
        Assert.Empty(provider.Calls);
    }

    [Theory]
    [InlineData("{\"deliveries\":[{\"deliveryId\":\"short\",\"bodyBase64\":\"\",\"redelivered\":false,\"leaseExpiresAt\":\"2026-09-22T00:00:30+00:00\"}]}")]
    [InlineData("{\"deliveries\":[{\"deliveryId\":\"0123456789abcdef0123456789abcdef\",\"bodyBase64\":\"not base64!\",\"redelivered\":false,\"leaseExpiresAt\":\"2026-09-22T00:00:30+00:00\"}]}")]
    [InlineData("{\"deliveries\":null}")]
    public async Task AReceiveAnswerOutsideTheContractIsRefused(string body)
    {
        using var provider = new Provider(_ => Json(HttpStatusCode.OK, body));
        var error = await Assert.ThrowsAsync<LambdaApiException>(() => LambdaContext.From(provider).Messaging.ReceiveAsync("jobs", cancellationToken: Token));
        Assert.Equal(LambdaRefusalCodes.ResponseInvalid, error.Code);
    }

    [Fact]
    public async Task UnqualifiedStreamAndQueueRoutesKeepTheirStableCodes()
    {
        using var provider = new Provider(_ => Json(HttpStatusCode.ServiceUnavailable, "{\"code\":\"messaging_protocol_unqualified\"}"));
        var messaging = LambdaContext.From(provider).Messaging;
        var read = await Assert.ThrowsAsync<LambdaApiException>(() => messaging.ReadStreamAsync("orders", LambdaStreamReadRequest.StartingAt(0), Token));
        var receive = await Assert.ThrowsAsync<LambdaApiException>(() => messaging.ReceiveAsync("jobs", cancellationToken: Token));
        Assert.Equal((503, LambdaMessaging.ProtocolUnqualified), (read.StatusCode, read.Code));
        Assert.Equal((503, LambdaMessaging.ProtocolUnqualified), (receive.StatusCode, receive.Code));
    }

    // ---- yogabase.cdc.v1

    private const string UpdateEvent = """
        {"schema":"yogabase.cdc.v1","streamId":"orders-cdc","table":"public.orders","operation":"update",
         "lsn":"0/1A2B3C","commitLsn":"0/1A2B40","commitTimestamp":"2026-09-22T01:02:03.456789Z","transactionId":771,
         "sequence":2,"key":{"id":"42"},"oldKey":{"id":"41"},"newValues":{"id":"42","status":"paid","note":null},
         "unchangedColumns":["payload"]}
        """;

    [Fact]
    public void ACdcUpdateParsesIntoItsTypedEvent()
    {
        var change = LambdaCdcEvent.Parse(Encoding.UTF8.GetBytes(UpdateEvent));

        Assert.Equal("orders-cdc", change.StreamId);
        Assert.Equal("public.orders", change.Table);
        Assert.Equal(("public", "orders"), (change.TableSchema, change.TableName));
        Assert.Equal(LambdaCdcOperation.Update, change.Operation);
        Assert.Equal(("0/1A2B3C", "0/1A2B40"), (change.Lsn, change.CommitLsn));
        Assert.Equal(new DateTimeOffset(2026, 9, 22, 1, 2, 3, TimeSpan.Zero).AddTicks(4567890), change.CommitTimestamp);
        Assert.Equal((771L, 2), (change.TransactionId, change.Sequence));
        Assert.Equal("42", change.Key!["id"]);
        Assert.Equal("41", change.OldKey!["id"]);
        Assert.Equal("paid", change.NewValues!["status"]);
        Assert.True(change.NewValues.ContainsKey("note"));
        Assert.Null(change.NewValues["note"]);
        Assert.Equal(["payload"], change.UnchangedColumns);
        Assert.False(change.Oversized);
        Assert.False(change.KeyOmitted);
    }

    [Fact]
    public void AnOversizedDeleteAndATruncateParseWithoutTheirDroppedParts()
    {
        var oversized = LambdaCdcEvent.Parse("""
            {"schema":"yogabase.cdc.v1","streamId":"s","table":"app.big","operation":"insert","lsn":"1/0","commitLsn":"1/8",
             "commitTimestamp":"2026-09-22T00:00:00.000000Z","transactionId":1,"sequence":0,"key":{"id":"7"},"oversized":true}
            """);
        Assert.True(oversized.Oversized);
        Assert.Null(oversized.NewValues);
        Assert.Equal("7", oversized.Key!["id"]);

        var minimal = LambdaCdcEvent.Parse("""
            {"schema":"yogabase.cdc.v1","streamId":"s","table":"app.big","operation":"update","lsn":"1/0","commitLsn":"1/8",
             "commitTimestamp":"2026-09-22T00:00:00.000000Z","transactionId":1,"sequence":0,"oversized":true,"keyOmitted":true}
            """);
        Assert.True(minimal.KeyOmitted);
        Assert.Null(minimal.Key);

        var truncate = LambdaCdcEvent.Parse("""
            {"schema":"yogabase.cdc.v1","streamId":"s","table":"app.big","operation":"truncate","lsn":"1/0","commitLsn":"1/8",
             "commitTimestamp":"2026-09-22T00:00:00.000000Z","transactionId":1,"sequence":3}
            """);
        Assert.Equal(LambdaCdcOperation.Truncate, truncate.Operation);
        Assert.Null(truncate.Key);
    }

    [Theory]
    [InlineData("{\"schema\":\"yogabase.cdc.v2\",\"streamId\":\"s\",\"table\":\"app.t\",\"operation\":\"insert\",\"lsn\":\"0/1\",\"commitLsn\":\"0/1\",\"commitTimestamp\":\"2026-09-22T00:00:00Z\",\"transactionId\":1,\"sequence\":0,\"key\":{\"id\":\"1\"},\"newValues\":{}}")]
    [InlineData("{\"schema\":\"yogabase.cdc.v1\",\"streamId\":\"s\",\"table\":\"App.T\",\"operation\":\"insert\",\"lsn\":\"0/1\",\"commitLsn\":\"0/1\",\"commitTimestamp\":\"2026-09-22T00:00:00Z\",\"transactionId\":1,\"sequence\":0,\"key\":{\"id\":\"1\"},\"newValues\":{}}")]
    [InlineData("{\"schema\":\"yogabase.cdc.v1\",\"streamId\":\"s\",\"table\":\"app.t\",\"operation\":\"merge\",\"lsn\":\"0/1\",\"commitLsn\":\"0/1\",\"commitTimestamp\":\"2026-09-22T00:00:00Z\",\"transactionId\":1,\"sequence\":0}")]
    [InlineData("{\"schema\":\"yogabase.cdc.v1\",\"streamId\":\"s\",\"table\":\"app.t\",\"operation\":\"insert\",\"lsn\":\"zz\",\"commitLsn\":\"0/1\",\"commitTimestamp\":\"2026-09-22T00:00:00Z\",\"transactionId\":1,\"sequence\":0,\"key\":{\"id\":\"1\"},\"newValues\":{}}")]
    [InlineData("{\"schema\":\"yogabase.cdc.v1\",\"streamId\":\"s\",\"table\":\"app.t\",\"operation\":\"insert\",\"lsn\":\"0/1\",\"commitLsn\":\"0/1\",\"commitTimestamp\":\"2026-09-22T00:00:00Z\",\"transactionId\":1,\"sequence\":0,\"newValues\":{}}")]
    [InlineData("{\"schema\":\"yogabase.cdc.v1\",\"streamId\":\"s\",\"table\":\"app.t\",\"operation\":\"insert\",\"lsn\":\"0/1\",\"commitLsn\":\"0/1\",\"commitTimestamp\":\"2026-09-22T00:00:00Z\",\"transactionId\":1,\"sequence\":-1,\"key\":{\"id\":\"1\"},\"newValues\":{}}")]
    [InlineData("[]")]
    [InlineData("not json")]
    public void AnEventOutsideTheV1ShapeIsRefused(string json)
    {
        Assert.Throws<FormatException>(() => LambdaCdcEvent.Parse(json));
        Assert.False(LambdaCdcEvent.TryParse(Encoding.UTF8.GetBytes(json), out _));
    }

    [Fact]
    public void AnUnknownAdditivePropertyIsIgnored()
    {
        var change = LambdaCdcEvent.Parse(UpdateEvent.TrimEnd().TrimEnd('}') + ",\"futureField\":true}");
        Assert.Equal(LambdaCdcOperation.Update, change.Operation);
    }

    [Fact]
    public void ARecordKeyIsTheTableAndThirtyTwoHexCharacters()
    {
        var key = LambdaCdcRecordKey.Parse(Encoding.UTF8.GetBytes("public.orders:0123456789abcdef0123456789abcdef"));
        Assert.Equal(("public.orders", "0123456789abcdef0123456789abcdef"), (key.Table, key.RowHash));
        Assert.Equal("public.orders:0123456789abcdef0123456789abcdef", key.ToString());
    }

    [Theory]
    [InlineData("public.orders:0123456789ABCDEF0123456789ABCDEF")]
    [InlineData("public.orders:0123")]
    [InlineData("orders:0123456789abcdef0123456789abcdef")]
    [InlineData("public.orders0123456789abcdef0123456789abcdef")]
    [InlineData("Public.orders:0123456789abcdef0123456789abcdef")]
    [InlineData("")]
    public void ARecordKeyOutsideTheShapeIsRefused(string value)
    {
        Assert.Throws<FormatException>(() => LambdaCdcRecordKey.Parse(Encoding.UTF8.GetBytes(value)));
        Assert.False(LambdaCdcRecordKey.TryParse(Encoding.UTF8.GetBytes(value), out _));
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status)
        { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed record Call(HttpMethod Method, string Path, string Body);

    private sealed class MemoryCacheStandIn : IDistributedCache
    {
        public byte[]? Get(string key) => null;
        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult<byte[]?>(null);
        public void Refresh(string key) { }
        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
        public void Remove(string key) { }
        public Task RemoveAsync(string key, CancellationToken token = default) => Task.CompletedTask;
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) { }
        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) => Task.CompletedTask;
    }

    private sealed class Provider : HttpMessageHandler, IServiceProvider, IHttpClientFactory
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> respond;
        public List<Call> Calls { get; } = [];
        public List<string> Names { get; } = [];
        public IDistributedCache? Cache { get; set; }
        public Provider(Func<HttpRequestMessage, HttpResponseMessage> respond) => this.respond = respond;
        public object? GetService(Type serviceType) => serviceType == typeof(ILogger) ? NullLogger.Instance
            : serviceType == typeof(ClaimsPrincipal) ? new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "lambda:caller")], "runtime"))
            : serviceType == typeof(IHttpClientFactory) ? this
            : serviceType == typeof(IDistributedCache) ? Cache
            : serviceType == typeof(IReadOnlyDictionary<string, string>) ? new Dictionary<string, string>
            {
                ["yogabase.invocationId"] = "invocation", ["yogabase.functionId"] = "caller",
                ["yogabase.version"] = "1", ["yogabase.environmentId"] = "11111111-1111-4111-8111-111111111111",
            } : null;
        public HttpClient CreateClient(string name)
        {
            Names.Add(name);
            return new HttpClient(this, disposeHandler: false) { BaseAddress = new Uri("http://runtime/v1/lambda-runtime/") };
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add(new Call(request.Method, request.RequestUri!.GetComponents(UriComponents.PathAndQuery, UriFormat.UriEscaped),
                request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken)));
            return respond(request);
        }
    }
}
