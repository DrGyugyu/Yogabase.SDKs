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
    private readonly HttpClient client;

    internal LambdaRealtime(HttpClient client)
    {
        this.client = client;
    }

    /// <summary>Publishes a payload to one topic, serialized as JSON.</summary>
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
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("A non-empty JSON payload is required.", nameof(json));
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            LambdaRoutes.RealtimeTopicPublish(LambdaTransport.EscapeSegment(topic, nameof(topic))))
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        using var response = await LambdaTransport.SendAsync(client, request, cancellationToken)
            .ConfigureAwait(false);
    }
}
