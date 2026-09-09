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

/// <summary>Gateway-mediated, authenticated RobustMQ publication. No broker credential reaches the Lambda.</summary>
public sealed class LambdaMessaging
{
    public const string ProtocolUnqualified = "messaging_protocol_unqualified";
    private readonly HttpClient client;
    internal LambdaMessaging(HttpClient client) => this.client = client;

    public async Task PublishAsync(string topic, ReadOnlyMemory<byte> payload, int qos = 1,
        string protocol = "mqtt", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(topic) || topic.Length > 512 || Encoding.UTF8.GetByteCount(topic) > 1024
            || topic.Any(c => char.IsControl(c) || c is '+' or '#'))
            throw new ArgumentException("A bounded MQTT topic without wildcards is required.", nameof(topic));
        if (payload.Length > 32 * 1024) throw new ArgumentOutOfRangeException(nameof(payload));
        if (qos is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(qos));
        if (protocol is not ("mqtt" or "kafka")) throw new ArgumentException("Use mqtt or kafka.", nameof(protocol));
        var route = "topics/" + string.Join("/", topic.Split('/').Select(LambdaSubstrateHttp.Segment))
            + "/messages?protocol=" + protocol;
        using var request = LambdaSubstrateHttp.JsonRequest(HttpMethod.Post, route,
            new { payloadBase64 = Convert.ToBase64String(payload.ToArray()), qos });
        using var response = await LambdaTransport.SendAsync(client, request, cancellationToken).ConfigureAwait(false);
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

    internal static async Task<JsonElement> PostAsync(HttpClient client, string route, object body, CancellationToken cancellationToken)
    {
        using var request = JsonRequest(HttpMethod.Post, route, body);
        using var response = await LambdaTransport.SendAsync(client, request, cancellationToken).ConfigureAwait(false);
        return await LambdaTransport.ReadJsonAsync<JsonElement>(response, cancellationToken).ConfigureAwait(false);
    }
}
