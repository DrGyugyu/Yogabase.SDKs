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

    /// <summary>Object collection; <c>?prefix=</c> filters a listing.</summary>
    public const string StorageObjects = "objects";

    /// <summary>Application-user collection.</summary>
    public const string Users = "users";

    /// <summary>One object, by logical key.</summary>
    public static string StorageObject(string escapedKey) => "objects/" + escapedKey;

    /// <summary>A prefix-filtered object listing.</summary>
    public static string StorageObjectsWithPrefix(string prefix) =>
        "objects?prefix=" + Uri.EscapeDataString(prefix);

    /// <summary>One application user, by subject.</summary>
    public static string User(string escapedSubject) => "users/" + escapedSubject;

    /// <summary>Access-token issuance for one application user.</summary>
    public static string UserTokens(string escapedSubject) => "users/" + escapedSubject + "/tokens";

    /// <summary>Publication route of one realtime topic.</summary>
    public static string RealtimeTopicPublish(string escapedTopic) =>
        "realtime/topics/" + escapedTopic + "/publish";

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
