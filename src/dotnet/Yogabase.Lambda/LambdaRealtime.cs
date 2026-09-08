using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Yogabase.Lambda;

/// <summary>
/// Realtime topic publication for the Lambda's own project, over the runtime's pre-authenticated
/// <c>yogabase-realtime</c> client. A Lambda publishes; it does not subscribe, hold a Hub, or
/// address another project's topics.
/// </summary>
public sealed class LambdaRealtime
{
    /// <summary>The outbox's own bound on one event, matched here so an oversized payload never leaves the Lambda.</summary>
    public const int MaximumPayloadBytes = 64 * 1024;

    private readonly HttpClient client;

    internal LambdaRealtime(HttpClient client)
    {
        this.client = client;
    }

    /// <summary>
    /// Publishes a payload to one topic, serialized as JSON. The gateway records the event in the
    /// durable Realtime outbox and answers once per invocation and topic; publishing the same
    /// event again from a retried invocation is acknowledged rather than duplicated.
    /// </summary>
    /// <param name="topic">A Realtime binding id: <c>[a-z][a-z0-9._-]{0,95}</c>.</param>
    /// <param name="payload">Serialized as JSON; at most 64 KiB on the wire.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task PublishAsync(string topic, object payload, CancellationToken cancellationToken = default)
    {
        if (payload is null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        return PublishJsonAsync(topic, JsonSerializer.Serialize(payload, LambdaJson.Options), cancellationToken);
    }

    /// <summary>
    /// Publishes an already-serialized JSON document to one topic. Use this when the Lambda has
    /// composed the payload itself and a second serialization pass would be wasteful or lossy.
    /// </summary>
    public async Task PublishJsonAsync(string topic, string json, CancellationToken cancellationToken = default)
    {
        if (!LambdaRoutes.IsTopic(topic))
        {
            throw new ArgumentException(
                "A realtime topic is a binding id: a lower-case letter followed by up to 95 lower-case "
                + "letters, digits, '.', '_' or '-'.",
                nameof(topic));
        }
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("A non-empty JSON payload is required.", nameof(json));
        }
        if (Encoding.UTF8.GetByteCount(json) > MaximumPayloadBytes)
        {
            throw new ArgumentException("A realtime payload is at most 64 KiB.", nameof(json));
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            LambdaRoutes.RealtimeTopicPublish(topic))
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        using var response = await LambdaTransport.SendAsync(client, request, cancellationToken)
            .ConfigureAwait(false);
    }
}
