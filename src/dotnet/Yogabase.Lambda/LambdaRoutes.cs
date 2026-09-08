using System;

namespace Yogabase.Lambda;

/// <summary>
/// Every internal Lambda runtime API route this package calls, in one place.
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
    /// Object collection; <c>?prefix=</c> filters a listing. The mediated store this release ships
    /// exposes no enumeration, so the gateway answers <c>501 storage_list_unavailable</c> here.
    /// </summary>
    public const string StorageObjects = "objects";

    /// <summary>Application-user collection.</summary>
    public const string Users = "users";

    /// <summary>One object, by logical key.</summary>
    public static string StorageObject(string escapedKey) => "objects/" + escapedKey;

    /// <summary>A prefix-filtered object listing.</summary>
    public static string StorageObjectsWithPrefix(string prefix) =>
        "objects?prefix=" + Uri.EscapeDataString(prefix);

    /// <summary>One application user, by canonical subject (a lower-case <c>D</c>-form UUID).</summary>
    public static string User(string canonicalSubject) => "users/" + canonicalSubject;

    /// <summary>
    /// Access-token issuance for one application user. The project realm exposes no first-party
    /// issuance grant yet, so this release answers <c>501 user_token_issuance_unavailable</c>.
    /// </summary>
    public static string UserTokens(string canonicalSubject) => "users/" + canonicalSubject + "/tokens";

    /// <summary>Publication route of one realtime topic (a binding id; see <see cref="IsTopic"/>).</summary>
    public static string RealtimeTopicPublish(string topic) =>
        "realtime/topics/" + topic + "/publish";

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

        return parsed.ToString("D", System.Globalization.CultureInfo.InvariantCulture);
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

    /// <summary>
    /// Subject prefix the runtime gives an invocation that no end user initiated — a schedule, an
    /// actor tick, or a workflow step. The remainder is the function id.
    /// </summary>
    public const string SystemSubjectPrefix = "lambda:";
}
