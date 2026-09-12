using System;
using System.Globalization;
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
///
/// <para>
/// Two shapes, and the difference matters. <see cref="PublishAsync(string, object, CancellationToken)"/>
/// broadcasts to everyone subscribed to the topic, which is right for "the boss spawned" or "the
/// match ended". <see cref="PublishToUserAsync(string, string, object, CancellationToken)"/>
/// addresses one player, which is right for anything about that player's own state -- their
/// inventory, their currency, their quest -- because any signed-in client may subscribe to any
/// topic, so a broadcast of one player's inventory is readable by all of them.
/// </para>
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
    /// Publishes a payload to one topic, serialized as JSON, for every client subscribed to it. The
    /// gateway records the event in the durable Realtime outbox and answers once per invocation and
    /// topic; publishing the same event again from a retried invocation is acknowledged rather than
    /// duplicated.
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
    /// Publishes a payload to one topic for a single application user, serialized as JSON. Only
    /// that user's own connections receive it; other subscribers of the topic do not.
    ///
    /// <para>
    /// Use this for anything scoped to one player. A Lambda that validated an item use and wrote the
    /// database finishes by telling that player what changed:
    /// <code>
    /// var player = context.Caller.RequireSubjectId();
    /// await context.Realtime.PublishToUserAsync(
    ///     "inventory.updated",
    ///     player,
    ///     new { itemId, remaining });
    /// </code>
    /// </para>
    ///
    /// <para>
    /// The subject is a delivery address, not a permission: it grants the Lambda nothing about that
    /// user, and a subject with no live connection simply receives nothing. Delivery is
    /// at-least-once and the client de-duplicates on the event id.
    /// </para>
    /// </summary>
    /// <param name="topic">A Realtime binding id: <c>[a-z][a-z0-9._-]{0,95}</c>.</param>
    /// <param name="subject">The recipient's application-user subject; any UUID spelling is normalised.</param>
    /// <param name="payload">Serialized as JSON; at most 64 KiB on the wire.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task PublishToUserAsync(
        string topic,
        string subject,
        object payload,
        CancellationToken cancellationToken = default)
    {
        if (payload is null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        return PublishJsonToUserAsync(
            topic,
            subject,
            JsonSerializer.Serialize(payload, LambdaJson.Options),
            cancellationToken);
    }

    /// <summary>
    /// The <see cref="Guid"/> overload of
    /// <see cref="PublishToUserAsync(string, string, object, CancellationToken)"/>, which is the
    /// shape <c>context.Caller.RequireSubjectId()</c> and <c>TryGetSubjectId</c> hand back, so the
    /// common case needs no conversion at the call site.
    /// </summary>
    public Task PublishToUserAsync(
        string topic,
        Guid subject,
        object payload,
        CancellationToken cancellationToken = default) =>
        PublishToUserAsync(
            topic,
            subject.ToString("D", CultureInfo.InvariantCulture),
            payload,
            cancellationToken);

    /// <summary>
    /// Publishes an already-serialized JSON document to one topic. Use this when the Lambda has
    /// composed the payload itself and a second serialization pass would be wasteful or lossy.
    /// </summary>
    public Task PublishJsonAsync(string topic, string json, CancellationToken cancellationToken = default) =>
        SendAsync(LambdaRoutes.RealtimeTopicPublish(ValidateTopic(topic)), ValidateJson(json), cancellationToken);

    /// <summary>
    /// Publishes an already-serialized JSON document to one topic for a single application user.
    /// The addressed counterpart of <see cref="PublishJsonAsync"/>.
    /// </summary>
    public Task PublishJsonToUserAsync(
        string topic,
        string subject,
        string json,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            LambdaRoutes.RealtimeTopicPublishToUser(
                ValidateTopic(topic),
                LambdaRoutes.CanonicalSubject(subject, nameof(subject))),
            ValidateJson(json),
            cancellationToken);

    private async Task SendAsync(string route, string json, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, route)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        using var response = await LambdaTransport.SendAsync(client, request, cancellationToken)
            .ConfigureAwait(false);
    }

    private static string ValidateTopic(string topic)
    {
        if (!LambdaRoutes.IsTopic(topic))
        {
            throw new ArgumentException(
                "A realtime topic is a binding id: a lower-case letter followed by up to 95 lower-case "
                + "letters, digits, '.', '_' or '-'.",
                nameof(topic));
        }

        return topic;
    }

    private static string ValidateJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("A non-empty JSON payload is required.", nameof(json));
        }
        if (Encoding.UTF8.GetByteCount(json) > MaximumPayloadBytes)
        {
            throw new ArgumentException("A realtime payload is at most 64 KiB.", nameof(json));
        }

        return json;
    }
}
