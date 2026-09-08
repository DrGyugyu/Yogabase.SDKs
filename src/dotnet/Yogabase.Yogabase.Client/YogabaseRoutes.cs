using System;
using System.Globalization;

namespace Yogabase.Yogabase;

/// <summary>
/// Every Yogabase wire route this package calls, in one place.
/// </summary>
/// <remarks>
/// <para>
/// The routes belong to three separately addressed hosts and three separate credentials, and the
/// constants are grouped that way so a route is never sent to the wrong origin with the wrong
/// token:
/// </para>
/// <list type="bullet">
/// <item><description>
/// <b>Application gateway</b> — publishable-key bootstrap, then the signed-in user's OIDC access
/// token. This is the origin a game client and an application talk to.
/// </description></item>
/// <item><description>
/// <b>Control Plane API</b> — a project-scoped developer token issued from the console's Settings
/// page. This is the origin a developer tool (the Unity Editor, a CLI, CI) talks to, and it is
/// never reachable with a player credential.
/// </description></item>
/// <item><description>
/// <b>Delivery host</b> — a short-lived workload <c>DeliveryBearer</c> token.
/// </description></item>
/// </list>
/// <para>
/// Every route is relative and must be combined with its origin through
/// <see cref="Uri(Uri, string)"/>; a leading slash would discard a base path. The same names exist
/// in each language package of this repository (TypeScript <c>routes.ts</c>, Python
/// <c>routes.py</c>, Java <c>YogabaseRoutes.java</c>, Dart <c>routes.dart</c>, Unity
/// <c>YogabaseEditorRoutes.cs</c>) so a rename on the server is a single, findable edit per
/// language.
/// </para>
/// </remarks>
public static class YogabaseRoutes
{
    // ---------------------------------------------------------------------------------------
    // Application gateway — publishable key, then the player's OIDC access token.
    // ---------------------------------------------------------------------------------------

    /// <summary>Non-secret bootstrap document for one publishable key.</summary>
    public const string ClientConfigTemplate = "v1/client-config/{publishableKey}";

    /// <summary>Interactive authentication routes, all relative to the gateway origin.</summary>
    public const string AuthSignUp = "auth/signup";
    /// <inheritdoc cref="AuthSignUp"/>
    public const string AuthLogin = "auth/login";
    /// <inheritdoc cref="AuthSignUp"/>
    public const string AuthLogout = "auth/logout";
    /// <inheritdoc cref="AuthSignUp"/>
    public const string AuthEmailConfirm = "auth/email/confirm";
    /// <inheritdoc cref="AuthSignUp"/>
    public const string AuthPasswordResetRequest = "auth/password/reset/request";
    /// <inheritdoc cref="AuthSignUp"/>
    public const string AuthPasswordResetComplete = "auth/password/reset/complete";
    /// <inheritdoc cref="AuthSignUp"/>
    public const string AuthMfaSetup = "auth/mfa/setup";
    /// <inheritdoc cref="AuthSignUp"/>
    public const string AuthMfaConfirm = "auth/mfa/confirm";
    /// <inheritdoc cref="AuthSignUp"/>
    public const string AuthMfaVerify = "auth/mfa/verify";
    /// <inheritdoc cref="AuthSignUp"/>
    public const string AuthMfaRecoveryRegenerate = "auth/mfa/recovery/regenerate";
    /// <inheritdoc cref="AuthSignUp"/>
    public const string AuthMfaRecoveryUse = "auth/mfa/recovery/use";
    /// <inheritdoc cref="AuthSignUp"/>
    public const string AuthExternalCallbackTemplate = "auth/external/{provider}/callback";

    /// <summary>OIDC discovery, relative to the gateway origin.</summary>
    public const string OidcDiscoveryPath = "/.well-known/openid-configuration";
    /// <summary>The only authorization endpoint path the bootstrap document may name.</summary>
    public const string OidcAuthorizePath = "/connect/authorize";
    /// <summary>The only token endpoint path the bootstrap document may name.</summary>
    public const string OidcTokenPath = "/connect/token";

    /// <summary>Mediated object storage.</summary>
    public const string StorageObjectTemplate = "v1/storage/objects/{key}";

    /// <summary>Stateless Function, Actor Lambda, and Workflow Lambda invocation.</summary>
    public const string FunctionInvokeTemplate =
        "v1/projects/{projectId}/functions/{functionId}/versions/{functionVersion}/invoke";
    /// <inheritdoc cref="FunctionInvokeTemplate"/>
    public const string ActorInvokeTemplate =
        "v1/projects/{projectId}/actors/{functionId}/versions/{functionVersion}/instances/{instanceKey}/invoke";
    /// <inheritdoc cref="FunctionInvokeTemplate"/>
    public const string WorkflowStartTemplate =
        "v1/projects/{projectId}/workflows/{workflowId}/versions/{definitionVersion}/start";
    /// <inheritdoc cref="FunctionInvokeTemplate"/>
    public const string WorkflowCancelTemplate =
        "v1/projects/{projectId}/workflows/instances/{workflowInstanceId}/cancel";

    /// <summary>SignalR realtime hub path, carried verbatim in the bootstrap document.</summary>
    public const string RealtimePath = "/v1/realtime";

    /// <summary>
    /// Game-server allocation. A player token starts, lists, inspects, and stops the hosted
    /// service instances of its own project.
    /// </summary>
    public const string Servers = "v1/servers";
    /// <inheritdoc cref="Servers"/>
    public const string ServersStart = "v1/servers/start";
    /// <inheritdoc cref="Servers"/>
    public const string ServerTemplate = "v1/servers/{serverId}";
    /// <inheritdoc cref="Servers"/>
    public const string ServerStopTemplate = "v1/servers/{serverId}/stop";

    // ---------------------------------------------------------------------------------------
    // Control Plane API — project-scoped developer token.
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// Build upload and listing. <c>POST</c> is a multipart form carrying the bundle zip plus the
    /// optional <c>executable</c>, repeated <c>ports</c>, <c>name</c>, and <c>version</c> fields.
    /// </summary>
    public const string BuildsTemplate =
        "api/v1/projects/{projectId}/environments/{environmentId}/builds";
    /// <summary>One build, carrying its state and its Build check result.</summary>
    public const string BuildTemplate =
        "api/v1/projects/{projectId}/environments/{environmentId}/builds/{buildId}";
    /// <summary>Runs one build's checks again for the same bytes. <c>POST</c>, answered with the build.</summary>
    public const string BuildRecheckTemplate =
        "api/v1/projects/{projectId}/environments/{environmentId}/builds/{buildId}/recheck";

    /// <summary>
    /// Multipart field names of the build upload form. <c>bundle</c> is the one required part;
    /// the Control Plane also accepts an optional pre-signed <c>manifest</c> part, which this
    /// package never sends because Yogabase derives the manifest from the bundle itself.
    /// </summary>
    public const string BuildFieldBundle = "bundle";
    /// <inheritdoc cref="BuildFieldBundle"/>
    public const string BuildFieldExecutable = "executable";
    /// <inheritdoc cref="BuildFieldBundle"/>
    public const string BuildFieldPorts = "ports";
    /// <inheritdoc cref="BuildFieldBundle"/>
    public const string BuildFieldName = "name";
    /// <inheritdoc cref="BuildFieldBundle"/>
    public const string BuildFieldVersion = "version";

    // ---------------------------------------------------------------------------------------
    // Delivery host — workload DeliveryBearer token.
    // ---------------------------------------------------------------------------------------

    /// <summary>Prefix every workload delivery route hangs off.</summary>
    public const string DeliveryProjectsPrefix = "api/v1/delivery/projects";

    // ---------------------------------------------------------------------------------------
    // Builders. Every caller goes through these so an escaping rule is defined exactly once.
    // ---------------------------------------------------------------------------------------

    /// <summary>Bootstrap document route for one publishable key.</summary>
    public static string ClientConfig(string publishableKey) =>
        "v1/client-config/" + Uri.EscapeDataString(publishableKey);

    /// <summary>External-identity callback route for one provider.</summary>
    public static string AuthExternalCallback(string provider) =>
        "auth/external/" + Uri.EscapeDataString(provider) + "/callback";

    /// <summary>Object route for one already-validated logical storage key.</summary>
    public static string StorageObject(string escapedLogicalKey) =>
        "v1/storage/objects/" + escapedLogicalKey;

    /// <summary>Invocation route for one exact registered Function version.</summary>
    public static string FunctionInvoke(
        Guid projectId,
        string escapedFunctionId,
        string escapedFunctionVersion) =>
        "v1/projects/" + Id(projectId) + "/functions/" + escapedFunctionId
        + "/versions/" + escapedFunctionVersion + "/invoke";

    /// <summary>Invocation route for one exact Actor Lambda instance.</summary>
    public static string ActorInvoke(
        Guid projectId,
        string escapedFunctionId,
        string escapedFunctionVersion,
        string escapedInstanceKey) =>
        "v1/projects/" + Id(projectId) + "/actors/" + escapedFunctionId
        + "/versions/" + escapedFunctionVersion + "/instances/" + escapedInstanceKey + "/invoke";

    /// <summary>Start route for one immutable Workflow Lambda definition version.</summary>
    public static string WorkflowStart(
        Guid projectId,
        string escapedWorkflowId,
        string escapedDefinitionVersion) =>
        "v1/projects/" + Id(projectId) + "/workflows/" + escapedWorkflowId
        + "/versions/" + escapedDefinitionVersion + "/start";

    /// <summary>Cancel route for one running Workflow Lambda instance.</summary>
    public static string WorkflowCancel(Guid projectId, string escapedWorkflowInstanceId) =>
        "v1/projects/" + Id(projectId) + "/workflows/instances/" + escapedWorkflowInstanceId + "/cancel";

    /// <summary>Route of one allocated game-server instance.</summary>
    public static string Server(string escapedServerId) => "v1/servers/" + escapedServerId;

    /// <summary>Stop route of one allocated game-server instance.</summary>
    public static string ServerStop(string escapedServerId) => "v1/servers/" + escapedServerId + "/stop";

    /// <summary>Build collection route of one environment.</summary>
    public static string Builds(Guid projectId, Guid environmentId) =>
        "api/v1/projects/" + Id(projectId) + "/environments/" + Id(environmentId) + "/builds";

    /// <summary>Route of one build. The id is a UUID in its canonical <c>D</c> form.</summary>
    public static string Build(Guid projectId, Guid environmentId, string escapedBuildId) =>
        Builds(projectId, environmentId) + "/" + escapedBuildId;

    /// <summary>Re-check route of one build.</summary>
    public static string BuildRecheck(Guid projectId, Guid environmentId, string escapedBuildId) =>
        Build(projectId, environmentId, escapedBuildId) + "/recheck";

    private static string Id(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("A non-empty identifier is required.", nameof(value));
        }

        return value.ToString("D", CultureInfo.InvariantCulture);
    }
}
