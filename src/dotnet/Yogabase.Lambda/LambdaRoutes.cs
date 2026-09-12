using System;
using System.Globalization;
using System.Text;

namespace Yogabase.Lambda;

/// <summary>
/// Every internal Lambda runtime API route this package calls, in one place, together with the
/// gateway's own spelling of what it accepts on each.
/// </summary>
/// <remarks>
/// <para>
/// These routes are relative to the <see cref="System.Net.Http.HttpClient.BaseAddress"/> of the
/// named clients the runtime hands the Lambda, which already ends with
/// <c>/v1/lambda-runtime/</c> and already carries the per-invocation
/// <c>Authorization</c> header. Nothing here is a public product contract: the sandbox reaches the
/// gateway over an egress path that opens only to the project's own database and gateway, and a
/// Lambda never receives a cloud, Kubernetes, fleet, or Control Plane credential.
/// </para>
/// <para>
/// Every route is relative and must never start with a slash: a leading slash would discard the
/// base address's <c>/v1/lambda-runtime/</c> path and send the request to the gateway root.
/// </para>
/// </remarks>
public static class LambdaRoutes
{
    /// <summary>Named client for the mediated object store.</summary>
    public const string StorageClientName = "yogabase-storage";

    /// <summary>Named client for the project's application-user directory.</summary>
    public const string UsersClientName = "yogabase-users";

    /// <summary>Named client for realtime topic publication.</summary>
    public const string RealtimeClientName = "yogabase-realtime";

    /// <summary>
    /// Object collection. <see cref="StorageObjectListing"/> spells its paged, prefix-filtered
    /// listing; <see cref="StorageObject"/> spells one object under it.
    /// </summary>
    public const string StorageObjects = "objects";

    /// <summary>Application-user collection.</summary>
    public const string Users = "users";

    /// <summary>
    /// The first logical key segment the product reserves for its own per-environment artifact
    /// layout (<c>projects/{project}/environments/…</c> in the self-hosted store). The gateway
    /// refuses a key or listing prefix entering it for every verb with <c>key_reserved_prefix</c>
    /// and filters it out of every listing page; this wrapper refuses it before transport with the
    /// same code.
    /// </summary>
    public const string ReservedStorageSegment = "environments";

    /// <summary>The gateway's bound on one logical object key.</summary>
    public const int MaximumStorageKeyLength = 1024;

    /// <summary>The gateway's bound on a listing cursor; a longer one is refused as <c>cursor_invalid</c>.</summary>
    public const int MaximumStorageCursorLength = 2048;

    /// <summary>
    /// One object, by logical key, spelled as the gateway's <c>objects/{**key}</c> catch-all route
    /// receives it: each segment percent-encoded on its own and the separators kept. Kestrel leaves
    /// an encoded separator (<c>%2F</c>) encoded all the way into the route value, and the gateway
    /// refuses any key containing <c>%</c>, so encoding the whole key as one segment would turn
    /// every nested key into a <c>key_percent_encoded</c> refusal.
    /// </summary>
    /// <param name="key">A key that passed <see cref="StorageKey"/>.</param>
    public static string StorageObject(string key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        var segments = key.Split('/');
        var route = new StringBuilder(StorageObjects).Append('/');
        for (var index = 0; index < segments.Length; index++)
        {
            if (index > 0)
            {
                route.Append('/');
            }

            route.Append(Uri.EscapeDataString(segments[index]));
        }

        return route.ToString();
    }

    /// <summary>
    /// The paged object listing, <c>objects?prefix=&amp;cursor=&amp;limit=</c>, with each
    /// parameter present only when given. The gateway lists the whole project namespace without a
    /// prefix, starts from the beginning without a cursor, and pages by 100 without a limit.
    /// </summary>
    public static string StorageObjectListing(string? prefix, string? cursor, int? limit)
    {
        var route = new StringBuilder(StorageObjects);
        var separator = '?';
        if (!string.IsNullOrEmpty(prefix))
        {
            route.Append(separator).Append("prefix=").Append(Uri.EscapeDataString(prefix));
            separator = '&';
        }
        if (!string.IsNullOrEmpty(cursor))
        {
            route.Append(separator).Append("cursor=").Append(Uri.EscapeDataString(cursor));
            separator = '&';
        }
        if (limit is { } pageSize)
        {
            route.Append(separator).Append("limit=").Append(pageSize.ToString(CultureInfo.InvariantCulture));
        }

        return route.ToString();
    }

    /// <summary>One application user, by canonical subject (a lower-case <c>D</c>-form UUID).</summary>
    public static string User(string canonicalSubject) => "users/" + canonicalSubject;

    /// <summary>
    /// Access-token issuance for one application user, through the project realm's own first-party
    /// Lambda grant.
    /// </summary>
    public static string UserTokens(string canonicalSubject) => "users/" + canonicalSubject + "/tokens";

    /// <summary>Publication route of one realtime topic (a binding id; see <see cref="IsTopic"/>).</summary>
    public static string RealtimeTopicPublish(string topic) =>
        "realtime/topics/" + topic + "/publish";

    /// <summary>
    /// Publication route of one realtime topic addressed to a single application user. The gateway
    /// commits the event to the same durable outbox with the recipient stored beside it, and the
    /// realtime gateway later delivers it to that subject's own connections instead of to everyone
    /// subscribed to the topic.
    /// </summary>
    /// <param name="topic">A topic that passed <see cref="IsTopic"/>.</param>
    /// <param name="canonicalSubject">A subject that passed <see cref="CanonicalSubject"/>.</param>
    public static string RealtimeTopicPublishToUser(string topic, string canonicalSubject) =>
        "realtime/topics/" + topic + "/users/" + canonicalSubject + "/publish";

    /// <summary>
    /// The grammar of a realtime topic, which is a Realtime binding id: a lower-case letter, then
    /// up to 95 lower-case letters, digits, <c>.</c>, <c>_</c> or <c>-</c>. It is spelled here
    /// exactly as the gateway checks it, so a topic the gateway would refuse never leaves the
    /// Lambda.
    /// </summary>
    public static bool IsTopic(string? value)
    {
        if (value is null || value.Length is 0 or > 96 || value[0] < 'a' || value[0] > 'z')
        {
            return false;
        }

        foreach (var character in value)
        {
            var allowed = (character >= 'a' && character <= 'z')
                || (character >= '0' && character <= '9')
                || character == '.'
                || character == '_'
                || character == '-';
            if (!allowed)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// A user subject as the gateway accepts it: a non-empty UUID in lower-case <c>D</c> form.
    /// Any other spelling of the same UUID is normalised; anything that is not a UUID is refused.
    /// </summary>
    public static string CanonicalSubject(string subject, string parameterName)
    {
        if (subject is null)
        {
            throw new ArgumentNullException(parameterName);
        }
        if (!Guid.TryParse(subject.Trim(), out var parsed) || parsed == Guid.Empty)
        {
            throw new ArgumentException("An application-user subject is a non-empty UUID.", parameterName);
        }

        return parsed.ToString("D", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// A logical object key as the gateway accepts it, checked here with the gateway's own rules so
    /// a key it would refuse never leaves the Lambda. A key is project-local: it may not be empty,
    /// longer than <see cref="MaximumStorageKeyLength"/>, or absolute; may not contain a backslash,
    /// a percent sign (the gateway refuses percent-encoding outright rather than guessing how many
    /// times a key will be decoded), a NUL, an empty segment, or a <c>.</c> or <c>..</c> segment;
    /// may not already be a physical <c>projects/</c> path or carry a scheme; and may not enter the
    /// reserved <see cref="ReservedStorageSegment"/>. Shape problems are argument errors; the
    /// reserved segment is refused as the gateway refuses it, with a <see cref="LambdaApiException"/>
    /// carrying <see cref="LambdaRefusalCodes.KeyReservedPrefix"/>.
    /// </summary>
    /// <returns>The key, unchanged.</returns>
    public static string StorageKey(string key, string parameterName)
    {
        if (key is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        var problem = KeyShapeProblem(key) ?? LogicalKeyProblem(key);
        if (problem is not null)
        {
            throw new ArgumentException(problem, parameterName);
        }
        if (IsReservedStorageKey(key))
        {
            throw Reserved("The object key starts with a segment the product reserves for its own artifacts.");
        }

        return key;
    }

    /// <summary>
    /// A logical listing prefix as the gateway accepts it: <see langword="null"/> or empty for the
    /// whole project namespace, otherwise a key-shaped value that may additionally end in a
    /// separator. A prefix entering the reserved segment is refused before transport with
    /// <see cref="LambdaRefusalCodes.KeyReservedPrefix"/> — including one without a separator that
    /// merely spells the start of it (<c>environment</c>), because a listing under it would
    /// enumerate the reserved layout by plain string match. <c>environment/</c>, a directory of the
    /// application's own, is an ordinary prefix.
    /// </summary>
    /// <returns>The prefix, or <see langword="null"/> when none was given.</returns>
    public static string? StoragePrefix(string? prefix, string parameterName)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            return null;
        }

        var shape = prefix!.EndsWith("/", StringComparison.Ordinal)
            ? prefix.Substring(0, prefix.Length - 1)
            : prefix;
        var problem = KeyShapeProblem(shape) ?? LogicalKeyProblem(prefix);
        if (problem is not null)
        {
            throw new ArgumentException(problem, parameterName);
        }
        if (IsReservedStoragePrefix(prefix))
        {
            throw Reserved("The listing prefix enters a segment the product reserves for its own artifacts.");
        }

        return prefix;
    }

    /// <summary>Whether a key's first segment is the reserved <see cref="ReservedStorageSegment"/>.</summary>
    public static bool IsReservedStorageKey(string key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return string.Equals(FirstSegment(key), ReservedStorageSegment, StringComparison.Ordinal);
    }

    /// <summary>
    /// Whether a listing prefix would enumerate the reserved layout: its first segment is the
    /// reserved segment, or it has no separator and the reserved segment starts with it.
    /// </summary>
    public static bool IsReservedStoragePrefix(string prefix)
    {
        if (prefix is null)
        {
            throw new ArgumentNullException(nameof(prefix));
        }

        var first = FirstSegment(prefix);
        return first.Length > 0
            && (string.Equals(first, ReservedStorageSegment, StringComparison.Ordinal)
                || (prefix.IndexOf('/') < 0
                    && ReservedStorageSegment.StartsWith(first, StringComparison.Ordinal)));
    }

    /// <summary>
    /// Keys named in the <c>IReadOnlyDictionary&lt;string,string&gt;</c> the runtime resolves for
    /// per-invocation facts.
    /// </summary>
    public const string InvocationIdKey = "yogabase.invocationId";
    /// <inheritdoc cref="InvocationIdKey"/>
    public const string FunctionIdKey = "yogabase.functionId";
    /// <inheritdoc cref="InvocationIdKey"/>
    public const string VersionKey = "yogabase.version";
    /// <inheritdoc cref="InvocationIdKey"/>
    public const string EnvironmentIdKey = "yogabase.environmentId";
    /// <inheritdoc cref="InvocationIdKey"/>
    public const string ProjectIdKey = "yogabase.projectId";

    /// <summary>
    /// Subject prefix the runtime gives an invocation that no end user initiated — a schedule, an
    /// actor tick, or a workflow step. The remainder is the function id.
    /// </summary>
    public const string SystemSubjectPrefix = "lambda:";

    /// <summary>
    /// The claim naming what triggered the invocation (<c>Caller</c>, <c>Scheduled</c>,
    /// <c>Actor</c>, <c>Workflow</c> or <c>Consumer</c>). Present on every principal the runtime builds.
    /// </summary>
    public const string InvocationKindClaimType = "yogabase:invocation-kind";

    /// <summary>The kind a managed broker consumer trigger carries: the input is one Kafka record's value.</summary>
    public const string ConsumerInvocationKind = "Consumer";

    private static string FirstSegment(string value)
    {
        var separator = value.IndexOf('/');
        return separator < 0 ? value : value.Substring(0, separator);
    }

    /// <summary>The gateway's key-shape rules, spelled in its order; null when the key passes.</summary>
    private static string? KeyShapeProblem(string key)
    {
        if (key.Length == 0)
        {
            return "An object key is required.";
        }
        if (key.Length > MaximumStorageKeyLength)
        {
            return "An object key is at most 1024 characters.";
        }
        if (key[0] == '/')
        {
            return "An object key may not be absolute.";
        }
        if (key.Contains('\\'))
        {
            return "An object key may not contain a backslash.";
        }
        if (key.Contains('%'))
        {
            return "An object key may not contain percent-encoding.";
        }
        if (key.Contains('\0'))
        {
            return "An object key may not contain a NUL.";
        }

        foreach (var segment in key.Split('/'))
        {
            if (segment.Length == 0)
            {
                return "An object key may not contain an empty segment.";
            }
            if (segment == "." || segment == "..")
            {
                return "An object key may not contain a relative path segment.";
            }
        }

        return null;
    }

    private static string? LogicalKeyProblem(string keyOrPrefix) =>
        keyOrPrefix.StartsWith("projects/", StringComparison.Ordinal)
            || keyOrPrefix.Contains("://", StringComparison.Ordinal)
            ? "A project-local logical key is required, not a physical path or a URL."
            : null;

    private static LambdaApiException Reserved(string message) =>
        new(403, LambdaRefusalCodes.KeyReservedPrefix, message);
}
