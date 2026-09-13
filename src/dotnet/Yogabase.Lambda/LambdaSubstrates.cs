using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
    private readonly HttpClient client;
    internal LambdaMessaging(HttpClient client) => this.client = client;

    /// <summary>Publishes one message through the runtime API.</summary>
    /// <param name="topic">An MQTT topic, or for Kafka and AMQP a logical topic or queue name.</param>
    /// <param name="payload">At most 32 KiB.</param>
    /// <param name="qos">0 or 1 for MQTT; Kafka and AMQP require 1.</param>
    /// <param name="protocol"><c>mqtt</c>, <c>kafka</c> or <c>amqp</c>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task PublishAsync(string topic, ReadOnlyMemory<byte> payload, int qos = 1,
        string protocol = "mqtt", CancellationToken cancellationToken = default)
    {
        if (protocol is not ("mqtt" or "kafka" or "amqp")) throw new ArgumentException("Use mqtt, kafka or amqp.", nameof(protocol));
        var acknowledged = protocol is "kafka" or "amqp";
        if (acknowledged)
        {
            // Kafka topics and AMQP queues share one grammar, spelled exactly as the gateway checks it,
            // so a name the gateway would refuse never leaves the Lambda.
            if (string.IsNullOrEmpty(topic) || topic.Length > 200 || topic is "." or ".."
                || topic.StartsWith("p.", StringComparison.Ordinal)
                || !topic.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '.' or '_' or '-'))
                throw new ArgumentException(
                    "A logical " + (protocol == "kafka" ? "Kafka topic" : "AMQP queue")
                    + " of 1 to 200 ASCII letters, digits, dots, underscores or hyphens without the reserved p. prefix is required.",
                    nameof(topic));
        }
        else if (string.IsNullOrWhiteSpace(topic) || topic.Length > 512 || Encoding.UTF8.GetByteCount(topic) > 1024
                 || topic.Any(c => char.IsControl(c) || c is '+' or '#'))
        {
            throw new ArgumentException("A bounded MQTT topic without wildcards is required.", nameof(topic));
        }
        if (payload.Length > 32 * 1024) throw new ArgumentOutOfRangeException(nameof(payload));
        if (qos is < 0 or > 1 || acknowledged && qos != 1) throw new ArgumentOutOfRangeException(nameof(qos));
        var route = "topics/" + string.Join("/", topic.Split('/').Select(LambdaSubstrateHttp.Segment))
            + "/messages?protocol=" + protocol;
        using var request = LambdaSubstrateHttp.JsonRequest(HttpMethod.Post, route,
            new { payloadBase64 = Convert.ToBase64String(payload.ToArray()), qos });
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

    internal static async Task<JsonElement> PostAsync(HttpClient client, string route, object body, CancellationToken cancellationToken)
    {
        using var request = JsonRequest(HttpMethod.Post, route, body);
        using var response = await LambdaTransport.SendAsync(client, request, cancellationToken).ConfigureAwait(false);
        return await LambdaTransport.ReadJsonAsync<JsonElement>(response, cancellationToken).ConfigureAwait(false);
    }
}
