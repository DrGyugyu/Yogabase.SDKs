using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace Yogabase.Lambda;

public sealed partial class LambdaContext
{
    private LambdaActors? actors;
    private LambdaWorkflows? workflows;
    private LambdaReminders? reminders;
    private LambdaMessaging? messaging;

    public LambdaActors Actors { get { lock (gate) return actors ??= new(Client("yogabase-actors")); } }
    public LambdaWorkflows Workflows { get { lock (gate) return workflows ??= new(Client("yogabase-workflows")); } }
    public LambdaReminders Reminders { get { lock (gate) return reminders ??= new(Client("yogabase-reminders")); } }
    public LambdaMessaging Messaging { get { lock (gate) return messaging ??= new(Client("yogabase-messaging")); } }

    /// <summary>The project's real Valkey client. The runtime owns it; do not dispose it.</summary>
    /// <remarks>Keys and pub/sub channels must start with p:&lt;project-id&gt;:. Valkey enforces the ACL.</remarks>
    public IConnectionMultiplexer Valkey => services.GetService(typeof(IConnectionMultiplexer)) as IConnectionMultiplexer
        ?? throw new InvalidOperationException("valkey-lambda-access: no project Valkey binding is available to this Lambda.");

    public IDatabase Cache => Valkey.GetDatabase();

    /// <summary>
    /// The runtime's <see cref="IDistributedCache"/>: the Valkey module's <c>cache</c> capability over
    /// this project's own ACL identity. Entries live under <c>p:&lt;project-id&gt;:cache:</c>, an entry
    /// written without an expiry lives one hour, and none outlives seven days. The runtime owns it.
    /// </summary>
    /// <remarks>
    /// Registered whenever the runtime binds Valkey. Until the installation accepts its
    /// <c>valkey-lambda-cache</c> evidence, every call on it throws <see cref="InvalidOperationException"/>
    /// naming that fact; store failures are <c>valkey_lambda_cache_unavailable</c>.
    /// </remarks>
    public IDistributedCache DistributedCache => services.GetService(typeof(IDistributedCache)) as IDistributedCache
        ?? throw new InvalidOperationException("valkey-lambda-cache: no project Valkey cache is available to this Lambda.");
}

/// <summary>Invokes the exact admitted version of an Actor Lambda through the runtime API.</summary>
public sealed class LambdaActors
{
    private readonly HttpClient client;
    internal LambdaActors(HttpClient client) => this.client = client;

    public Task<JsonElement> InvokeAsync(string functionId, string instanceKey, string functionVersion,
        object payload, string? idempotencyKey = null, CancellationToken cancellationToken = default) =>
        LambdaSubstrateHttp.PostAsync(client,
            "actors/" + LambdaSubstrateHttp.Segment(functionId) + "/instances/"
                + LambdaSubstrateHttp.Segment(instanceKey) + "/invoke",
            new { functionVersion, payload, idempotencyKey }, cancellationToken);
}

/// <summary>Starts, reads and cancels project Workflow Lambdas through the runtime API.</summary>
public sealed class LambdaWorkflows
{
    private readonly HttpClient client;
    internal LambdaWorkflows(HttpClient client) => this.client = client;

    public async Task<string> StartAsync(string workflowId, string definitionVersion, object input,
        string? idempotencyKey = null, CancellationToken cancellationToken = default)
    {
        var response = await LambdaSubstrateHttp.PostAsync(client,
            "workflows/" + LambdaSubstrateHttp.Segment(workflowId) + "/start",
            new { definitionVersion, input, idempotencyKey }, cancellationToken).ConfigureAwait(false);
        if (response.ValueKind != JsonValueKind.Object
            || !response.TryGetProperty("workflowInstanceId", out var value)
            || value.ValueKind != JsonValueKind.String
            || value.GetString() is not { } instanceId
            || !Guid.TryParseExact(instanceId, "D", out var parsed)
            || parsed == Guid.Empty
            || !string.Equals(instanceId, parsed.ToString("D"), StringComparison.Ordinal))
            throw new LambdaApiException(502, LambdaRefusalCodes.ResponseInvalid, "No canonical workflow instance was returned.");
        return instanceId;
    }

    public async Task CancelAsync(string instanceId, string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        using var request = LambdaSubstrateHttp.JsonRequest(HttpMethod.Post,
            Instance(instanceId) + "/cancel", new { idempotencyKey });
        using var response = await LambdaTransport.SendAsync(client, request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<JsonElement> GetAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, Instance(instanceId));
        using var response = await LambdaTransport.SendAsync(client, request, cancellationToken).ConfigureAwait(false);
        return await LambdaTransport.ReadJsonAsync<JsonElement>(response, cancellationToken).ConfigureAwait(false);
    }

    private static string Instance(string id) => "workflows/instances/" + LambdaSubstrateHttp.Segment(id);
}

/// <summary>Named reminders use the existing durable scheduler and its concurrency checks.</summary>
public sealed class LambdaReminders
{
    private readonly HttpClient client;
    internal LambdaReminders(HttpClient client) => this.client = client;

    /// <summary>Choose one dueAt or cron. Names belong to this calling Lambda within its project.</summary>
    public async Task SetAsync(string name, string functionId, string functionVersion, object payload,
        DateTimeOffset? dueAt = null, string? cron = null, string? timeZone = null,
        CancellationToken cancellationToken = default)
    {
        if ((dueAt is null) == string.IsNullOrWhiteSpace(cron))
            throw new ArgumentException("Choose exactly one of dueAt or cron.");
        var route = "reminders/" + LambdaSubstrateHttp.Segment(name);
        using var existing = await ReadAsync(route, cancellationToken).ConfigureAwait(false);
        using var request = LambdaSubstrateHttp.JsonRequest(HttpMethod.Put, route,
            new { functionId, functionVersion, payload, dueAt, cron, timeZone });
        if (existing.StatusCode == HttpStatusCode.NotFound)
            request.Headers.TryAddWithoutValidation("If-None-Match", "*");
        else
            request.Headers.TryAddWithoutValidation("If-Match", RequireETag(existing));
        using var response = await LambdaTransport.SendAsync(client, request, cancellationToken).ConfigureAwait(false);
    }

    public async Task ClearAsync(string name, CancellationToken cancellationToken = default)
    {
        var route = "reminders/" + LambdaSubstrateHttp.Segment(name);
        using var existing = await ReadAsync(route, cancellationToken).ConfigureAwait(false);
        if (existing.StatusCode == HttpStatusCode.NotFound) return;
        using var request = new HttpRequestMessage(HttpMethod.Delete, route);
        request.Headers.TryAddWithoutValidation("If-Match", RequireETag(existing));
        using var response = await LambdaTransport.SendAsync(client, request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> ReadAsync(string route, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, route);
        return await LambdaTransport.SendAsync(client, request, cancellationToken, allowNotFound: true).ConfigureAwait(false);
    }

    private static string RequireETag(HttpResponseMessage response) => response.Headers.ETag?.ToString()
        ?? throw new LambdaApiException(502, LambdaRefusalCodes.ResponseInvalid, "The reminder response carried no ETag.");
}

/// <summary>
/// Gateway-mediated, authenticated RobustMQ publication over MQTT, Kafka or RabbitMQ/AMQP. No broker
/// credential reaches the Lambda: the runtime API authenticates the invocation, and the broker's own
/// sidecar publishes under the project's prefix.
/// </summary>
/// <remarks>
/// None of these require Orleans. Kafka and AMQP are acknowledged publications: the call returns only
/// after the broker has persisted (Kafka) or confirmed (AMQP) the message, so both are QoS 1 and a
/// success that is not the runtime API's 202 acknowledgement is refused rather than trusted.
/// </remarks>
public sealed class LambdaMessaging
{
    public const string ProtocolUnqualified = "messaging_protocol_unqualified";
    public const string SubstrateUnavailable = "messaging_substrate_unavailable";

    /// <summary>
    /// 400: the record key is not base64, or is empty (omit the key to publish without one). An empty
    /// key is also refused before transport, with this status and code.
    /// </summary>
    public const string KeyInvalid = "messaging_key_invalid";

    /// <summary>413: the record key is longer than <see cref="MaximumKeyBytes"/>. Also refused before transport.</summary>
    public const string KeyTooLarge = "messaging_key_too_large";

    /// <summary>
    /// 400: a record key on an MQTT or AMQP publish, which cannot carry one (also refused before
    /// transport); or 503: this installation's Kafka publisher cannot carry a record key. A key is
    /// never dropped to make a publish succeed.
    /// </summary>
    public const string KeyUnsupported = "messaging_key_unsupported";

    /// <summary>The gateway's bound on one Kafka record key, in bytes once decoded.</summary>
    public const int MaximumKeyBytes = 1024;

    private readonly HttpClient client;
    internal LambdaMessaging(HttpClient client) => this.client = client;

    /// <summary>Publishes one message through the runtime API, without a record key.</summary>
    /// <param name="topic">An MQTT topic, or for Kafka and AMQP a logical topic or queue name.</param>
    /// <param name="payload">At most 32 KiB.</param>
    /// <param name="qos">0 or 1 for MQTT; Kafka and AMQP require 1.</param>
    /// <param name="protocol"><c>mqtt</c>, <c>kafka</c> or <c>amqp</c>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task PublishAsync(string topic, ReadOnlyMemory<byte> payload, int qos = 1,
        string protocol = "mqtt", CancellationToken cancellationToken = default) =>
        PublishCoreAsync(topic, payload, key: null, qos, protocol, cancellationToken);

    /// <summary>
    /// Publishes one message through the runtime API with an optional Kafka record key, which reaches
    /// the broker as the record key (sent as <c>keyBase64</c>); records with one key keep their order
    /// on one partition. A <see langword="null"/> key publishes without one, exactly as the keyless
    /// overload does.
    /// </summary>
    /// <param name="topic">A logical Kafka topic; for MQTT or AMQP the key must be null.</param>
    /// <param name="payload">At most 32 KiB.</param>
    /// <param name="key">1 to <see cref="MaximumKeyBytes"/> bytes, Kafka only; null for none.</param>
    /// <param name="qos">Kafka requires 1.</param>
    /// <param name="protocol"><c>kafka</c> by default, since only Kafka carries a key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="LambdaApiException">
    /// <see cref="KeyUnsupported"/> (400) for a key outside Kafka, <see cref="KeyInvalid"/> (400) for an
    /// empty key and <see cref="KeyTooLarge"/> (413) for a long one — each before transport, with the
    /// gateway's own status and code — and <see cref="KeyUnsupported"/> (503) from a gateway whose
    /// publisher cannot carry a key.
    /// </exception>
    public Task PublishAsync(string topic, ReadOnlyMemory<byte> payload, ReadOnlyMemory<byte>? key, int qos = 1,
        string protocol = "kafka", CancellationToken cancellationToken = default) =>
        PublishCoreAsync(topic, payload, key, qos, protocol, cancellationToken);

    /// <summary>
    /// Publishes to a Kafka topic on the bound RobustMQ cluster, with an optional record key, and
    /// returns once the broker has persisted it.
    /// </summary>
    public Task PublishKafkaAsync(string topic, ReadOnlyMemory<byte> payload, ReadOnlyMemory<byte>? key = null,
        CancellationToken cancellationToken = default) =>
        PublishCoreAsync(topic, payload, key, qos: 1, protocol: "kafka", cancellationToken);

    private async Task PublishCoreAsync(string topic, ReadOnlyMemory<byte> payload, ReadOnlyMemory<byte>? key, int qos,
        string protocol, CancellationToken cancellationToken)
    {
        if (protocol is not ("mqtt" or "kafka" or "amqp")) throw new ArgumentException("Use mqtt, kafka or amqp.", nameof(protocol));
        var acknowledged = protocol is "kafka" or "amqp";
        if (acknowledged)
        {
            RequireLogicalName(topic, protocol == "kafka" ? "Kafka topic" : "AMQP queue", nameof(topic));
        }
        else if (string.IsNullOrWhiteSpace(topic) || topic.Length > 512 || Encoding.UTF8.GetByteCount(topic) > 1024
                 || topic.Any(c => char.IsControl(c) || c is '+' or '#'))
        {
            throw new ArgumentException("A bounded MQTT topic without wildcards is required.", nameof(topic));
        }
        if (payload.Length > 32 * 1024) throw new ArgumentOutOfRangeException(nameof(payload));
        if (qos is < 0 or > 1 || acknowledged && qos != 1) throw new ArgumentOutOfRangeException(nameof(qos));
        // The gateway's own key rules, decided here with its status and code so one catch path covers
        // both: Kafka only, at least one byte, at most 1 KiB. A key is never truncated or dropped.
        if (key is { } recordKey)
        {
            if (protocol != "kafka")
                throw new LambdaApiException(400, KeyUnsupported, "A record key applies to Kafka publication only.");
            if (recordKey.Length == 0)
                throw new LambdaApiException(400, KeyInvalid, "A record key must be at least one byte; omit it to publish without a key.");
            if (recordKey.Length > MaximumKeyBytes)
                throw new LambdaApiException(413, KeyTooLarge, "A record key may be at most 1024 bytes.");
        }
        var route = "topics/" + string.Join("/", topic.Split('/').Select(LambdaSubstrateHttp.Segment))
            + "/messages?protocol=" + protocol;
        var payloadBase64 = Convert.ToBase64String(payload.ToArray());
        // No keyBase64 member at all for a keyless publish: the gateway reads any member as a key.
        using var request = key is { } sent
            ? LambdaSubstrateHttp.JsonRequest(HttpMethod.Post, route,
                new { payloadBase64, qos, keyBase64 = Convert.ToBase64String(sent.ToArray()) })
            : LambdaSubstrateHttp.JsonRequest(HttpMethod.Post, route, new { payloadBase64, qos });
        using var response = await LambdaTransport.SendAsync(client, request, cancellationToken).ConfigureAwait(false);
        if (acknowledged && response.StatusCode != System.Net.HttpStatusCode.Accepted)
            throw new LambdaApiException((int)response.StatusCode, LambdaRefusalCodes.ResponseInvalid,
                "The runtime API did not return the " + (protocol == "kafka" ? "Kafka publish" : "AMQP publisher-confirm")
                + " acknowledgement.");
    }

    /// <summary>
    /// Publishes to a RabbitMQ/AMQP queue on the bound RobustMQ cluster and returns once the broker has
    /// confirmed it. The queue name is logical; the broker stores it under this project's prefix.
    /// Refused with <see cref="SubstrateUnavailable"/> when no broker with AMQP is bound, and with
    /// <see cref="ProtocolUnqualified"/> on a build whose AMQP publish path is not qualified.
    /// </summary>
    public Task PublishAmqpAsync(string queue, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default) =>
        PublishAsync(queue, payload, qos: 1, protocol: "amqp", cancellationToken: cancellationToken);

    /// <summary>
    /// Reads a project stream (a Kafka topic) from an explicit position without joining or moving any
    /// consumer group. At least one record comes back whenever one exists, even past
    /// <see cref="LambdaStreamReadRequest.MaxBytes"/>, so a reader following <see cref="LambdaStreamReadResult.Next"/>
    /// always makes progress. Refused with <see cref="ProtocolUnqualified"/> until the installation
    /// accepts its event-replay evidence, and with <see cref="SubstrateUnavailable"/> where no broker is bound.
    /// </summary>
    public async Task<LambdaStreamReadResult> ReadStreamAsync(string topic, LambdaStreamReadRequest request,
        CancellationToken cancellationToken = default)
    {
        RequireLogicalName(topic, "stream topic", nameof(topic));
        if (request is null) throw new ArgumentNullException(nameof(request));
        request.Validate();
        var result = await LambdaSubstrateHttp.PostAsync<LambdaStreamReadResult>(client,
            "streams/" + LambdaSubstrateHttp.Segment(topic) + "/read", request, cancellationToken).ConfigureAwait(false);
        if (result.Records is null || result.Next is null
            || result.Records.Any(static record => record is null || !record.IsWellFormed())
            || result.Next.Any(static next => next is null || next.Partition is < 0 or > LambdaStreamReadRequest.MaximumPartitionIndex || next.Offset < 0))
            throw new LambdaApiException(502, LambdaRefusalCodes.ResponseInvalid, "The stream read answer is outside the runtime contract.");
        return result;
    }

    /// <summary>
    /// Ensures a project stream has at least <paramref name="partitions"/> partitions (1 to 32), so
    /// consumer triggers sharing one group can divide it. Partitions are never removed; the answer is
    /// the stream as it is afterwards.
    /// </summary>
    public async Task<LambdaStreamDescription> DeclareStreamPartitionsAsync(string topic, int partitions,
        CancellationToken cancellationToken = default)
    {
        RequireLogicalName(topic, "stream topic", nameof(topic));
        if (partitions is < 1 or > LambdaStreamDescription.MaximumDeclaredPartitions)
            throw new ArgumentOutOfRangeException(nameof(partitions), "A stream declares 1 to 32 partitions.");
        var description = await LambdaSubstrateHttp.PostAsync<LambdaStreamDescription>(client,
            "streams/" + LambdaSubstrateHttp.Segment(topic) + "/partitions", new { partitions }, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(description.Topic, topic, StringComparison.Ordinal) || description.Partitions < partitions)
            throw new LambdaApiException(502, LambdaRefusalCodes.ResponseInvalid, "The stream declaration answer is outside the runtime contract.");
        return description;
    }

    /// <summary>
    /// Leases up to <paramref name="maxMessages"/> (1 to 10) messages from a project durable queue for
    /// <paramref name="leaseSeconds"/> (5 to 300), waiting up to <paramref name="waitSeconds"/> (0 to 20)
    /// for the first. Settle each with <see cref="AckAsync"/> or <see cref="NackAsync"/> before its
    /// lease expires; an unsettled message is redelivered.
    /// </summary>
    public async Task<IReadOnlyList<LambdaQueueDelivery>> ReceiveAsync(string queue, int maxMessages = 1,
        int leaseSeconds = 30, int waitSeconds = 0, CancellationToken cancellationToken = default)
    {
        RequireLogicalName(queue, "AMQP queue", nameof(queue));
        if (maxMessages is < 1 or > LambdaQueueDelivery.MaximumMessagesPerReceive)
            throw new ArgumentOutOfRangeException(nameof(maxMessages), "A receive leases 1 to 10 messages.");
        if (leaseSeconds is < LambdaQueueDelivery.MinimumLeaseSeconds or > LambdaQueueDelivery.MaximumLeaseSeconds)
            throw new ArgumentOutOfRangeException(nameof(leaseSeconds), "A lease lasts 5 to 300 seconds.");
        if (waitSeconds is < 0 or > LambdaQueueDelivery.MaximumReceiveWaitSeconds)
            throw new ArgumentOutOfRangeException(nameof(waitSeconds), "A receive waits 0 to 20 seconds.");
        var answer = await LambdaSubstrateHttp.PostAsync<LambdaQueueReceiveAnswer>(client,
            "queues/" + LambdaSubstrateHttp.Segment(queue) + "/receive",
            new { maxMessages, leaseSeconds, waitSeconds }, cancellationToken).ConfigureAwait(false);
        if (answer.Deliveries is null || answer.Deliveries.Count > maxMessages
            || answer.Deliveries.Any(static delivery => delivery is null || !delivery.IsWellFormed()))
            throw new LambdaApiException(502, LambdaRefusalCodes.ResponseInvalid, "The queue receive answer is outside the runtime contract.");
        return answer.Deliveries;
    }

    /// <summary>
    /// Acknowledges a leased delivery, removing its message. <see langword="false"/> when the
    /// delivery is no longer leased — unknown, already settled, or its lease expired and the message
    /// was redelivered — in which case nothing changed.
    /// </summary>
    public Task<bool> AckAsync(string queue, string deliveryId, CancellationToken cancellationToken = default) =>
        SettleAsync(queue, deliveryId, "ack", null, cancellationToken);

    /// <summary>
    /// Negatively acknowledges a leased delivery: its message is redelivered when
    /// <paramref name="requeue"/> is true and discarded otherwise. <see langword="false"/> when the
    /// delivery is no longer leased.
    /// </summary>
    public Task<bool> NackAsync(string queue, string deliveryId, bool requeue = true, CancellationToken cancellationToken = default) =>
        SettleAsync(queue, deliveryId, "nack", new { requeue }, cancellationToken);

    private async Task<bool> SettleAsync(string queue, string deliveryId, string verb, object? body, CancellationToken cancellationToken)
    {
        RequireLogicalName(queue, "AMQP queue", nameof(queue));
        if (!LambdaQueueDelivery.IsDeliveryId(deliveryId))
            throw new ArgumentException("A delivery id is 32 lowercase hexadecimal characters.", nameof(deliveryId));
        var route = "queues/" + LambdaSubstrateHttp.Segment(queue) + "/deliveries/" + deliveryId + "/" + verb;
        using var request = body is null
            ? new HttpRequestMessage(HttpMethod.Post, route)
            : LambdaSubstrateHttp.JsonRequest(HttpMethod.Post, route, body);
        try
        {
            using var response = await LambdaTransport.SendAsync(client, request, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (LambdaApiException refusal) when (refusal.StatusCode == 404
            && string.Equals(refusal.Code, LambdaQueueDelivery.UnknownDelivery, StringComparison.Ordinal))
        {
            return false;
        }
    }

    // Kafka topics, streams and AMQP queues share one grammar, spelled exactly as the gateway checks
    // it, so a name the gateway would refuse never leaves the Lambda.
    private static void RequireLogicalName(string name, string noun, string parameterName)
    {
        if (string.IsNullOrEmpty(name) || name.Length > 200 || name is "." or ".."
            || name.StartsWith("p.", StringComparison.Ordinal)
            || !name.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '.' or '_' or '-'))
            throw new ArgumentException(
                "A logical " + noun
                + " of 1 to 200 ASCII letters, digits, dots, underscores or hyphens without the reserved p. prefix is required.",
                parameterName);
    }
}

internal static class LambdaSubstrateHttp
{
    internal static string Segment(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value is "." or ".."
            || value.Any(c => char.IsControl(c) || c is '/' or '\\' or '%'))
            throw new ArgumentException("A non-empty route segment without separators or percent encoding is required.", nameof(value));
        return Uri.EscapeDataString(value);
    }

    internal static HttpRequestMessage JsonRequest(HttpMethod method, string route, object body)
    {
        var json = JsonSerializer.Serialize(body, LambdaJson.Options);
        if (Encoding.UTF8.GetByteCount(json) > 64 * 1024)
            throw new ArgumentException("The runtime API request exceeds 64 KiB.", nameof(body));
        return new HttpRequestMessage(method, route) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    internal static Task<JsonElement> PostAsync(HttpClient client, string route, object body, CancellationToken cancellationToken) =>
        PostAsync<JsonElement>(client, route, body, cancellationToken);

    internal static async Task<T> PostAsync<T>(HttpClient client, string route, object body, CancellationToken cancellationToken)
    {
        using var request = JsonRequest(HttpMethod.Post, route, body);
        using var response = await LambdaTransport.SendAsync(client, request, cancellationToken).ConfigureAwait(false);
        return await LambdaTransport.ReadJsonAsync<T>(response, cancellationToken).ConfigureAwait(false);
    }
}
