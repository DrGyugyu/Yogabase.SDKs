// C# 9 only. Braced namespace, no file-scoped namespaces, no records, no collection expressions:
// Unity compiles this file itself, and the Editor's language version is fixed.

namespace Yogabase.Editor
{
    /// <summary>
    /// Every Yogabase route this package's Editor tooling calls, in one place.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These are the Control Plane's build-ingest routes, reached with a project-scoped developer
    /// token. They are a different origin and a different credential from the application gateway
    /// that a shipped game client talks to, and from the Pod-local runtime-host relay that the
    /// dedicated server itself talks to.
    /// </para>
    /// <para>
    /// The same names exist in <c>src/dotnet/Yogabase.Yogabase.Client/YogabaseRoutes.cs</c>. This
    /// file is a deliberate second copy rather than a reference to that assembly: the Editor tool
    /// speaks the multipart request itself with <c>HttpClient</c>, so the package carries no NuGet
    /// dependency closure into Unity. Keeping the routes in exactly one file per side means a
    /// rename on the server is one findable edit here.
    /// </para>
    /// <para>
    /// Every route is relative and must be combined with the Control Plane origin; a leading slash
    /// would discard a base path.
    /// </para>
    /// </remarks>
    public static class YogabaseEditorRoutes
    {
        /// <summary>Build collection route of one environment.</summary>
        public static string Builds(string projectId, string environmentId)
        {
            return "api/v1/projects/" + projectId + "/environments/" + environmentId + "/builds";
        }

        /// <summary>
        /// Route of one build, carrying its state and its Build check result. The id is a UUID,
        /// which the Control Plane's route constrains, so it is sent in its canonical form rather
        /// than escaped.
        /// </summary>
        public static string Build(string projectId, string environmentId, string buildId)
        {
            return Builds(projectId, environmentId) + "/" + CanonicalBuildId(buildId);
        }

        /// <summary>
        /// The canonical <c>D</c> form of a build id, or <see langword="null"/> when the value is
        /// not a UUID at all — the Control Plane would answer such a path as unroutable rather
        /// than as an unknown build.
        /// </summary>
        public static string CanonicalBuildId(string buildId)
        {
            System.Guid parsed;
            if (string.IsNullOrEmpty(buildId) || !System.Guid.TryParse(buildId, out parsed)
                || parsed == System.Guid.Empty)
            {
                return null;
            }

            return parsed.ToString("D", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// The prefix every developer token carries. A bearer without it is not a developer token
        /// to the Control Plane at all — its handler yields no result rather than a refusal — so
        /// the settings asset refuses one before a single request is sent.
        /// </summary>
        public const string DeveloperTokenPrefix = "ybt_";

        /// <summary>Longest developer token the Control Plane accepts.</summary>
        public const int MaximumDeveloperTokenLength = 128;

        /// <summary>Multipart field names of the build upload form.</summary>
        public const string FieldBundle = "bundle";

        /// <inheritdoc cref="FieldBundle"/>
        public const string FieldExecutable = "executable";

        /// <inheritdoc cref="FieldBundle"/>
        public const string FieldPorts = "ports";

        /// <inheritdoc cref="FieldBundle"/>
        public const string FieldName = "name";

        /// <inheritdoc cref="FieldBundle"/>
        public const string FieldVersion = "version";

        /// <summary>
        /// A Lambda archive's execution declaration: exactly <c>Stateless</c> or <c>Stateful</c>, and the
        /// comma-separated realtime binding ids it receives from and publishes to. An empty binding
        /// field declares none; an absent one is not a declaration, and the Control Plane refuses a
        /// Lambda upload without all three (<c>BUILD_LAMBDA_DECLARATION_REQUIRED</c>).
        /// </summary>
        public const string FieldExecutionKind = "executionKind";

        /// <inheritdoc cref="FieldExecutionKind"/>
        public const string FieldRealtimeIngressBindings = "realtimeIngressBindings";

        /// <inheritdoc cref="FieldExecutionKind"/>
        public const string FieldRealtimeEgressBindings = "realtimeEgressBindings";

        /// <summary>The admission contract's bound on binding ids in one direction.</summary>
        public const int MaximumBindingsPerDirection = 16;

        /// <summary>The admission contract's bound on one binding id's length.</summary>
        public const int MaximumBindingIdLength = 96;

        /// <summary>
        /// A realtime binding id as the admission Job accepts it: a lowercase letter, then lowercase
        /// letters, digits, <c>.</c>, <c>_</c> or <c>-</c>, at most 96 characters.
        /// </summary>
        public static bool IsRealtimeBindingId(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > MaximumBindingIdLength
                || value[0] < 'a' || value[0] > 'z')
            {
                return false;
            }
            foreach (var character in value)
            {
                var allowed = (character >= 'a' && character <= 'z') || (character >= '0' && character <= '9')
                    || character == '.' || character == '_' || character == '-';
                if (!allowed)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// The comma-separated Lambda ids a <c>RegisterLambdas</c> archive registers. Sent only for a
        /// registering archive: the Control Plane refuses a registering archive without it
        /// (<c>BUILD_LAMBDA_REGISTRATION_REQUIRED</c>) and a single-function archive with it, even
        /// empty (<c>BUILD_LAMBDA_REGISTRATION_UNEXPECTED</c>).
        /// </summary>
        public const string FieldRegisteredLambdaIds = "registeredLambdaIds";

        /// <summary>The product's bound on the Lambda ids one registering archive states.</summary>
        public const int MaximumRegisteredLambdaIds = 64;

        /// <summary>The product's bound on one registered Lambda id's length.</summary>
        public const int MaximumRegisteredLambdaIdLength = 128;

        /// <summary>
        /// A registered Lambda id as the Control Plane accepts it: a lowercase letter, then lowercase
        /// letters, digits, <c>.</c>, <c>_</c> or <c>-</c>, at most 128 characters, never case-folded.
        /// </summary>
        public static bool IsRegisteredLambdaId(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > MaximumRegisteredLambdaIdLength
                || value[0] < 'a' || value[0] > 'z')
            {
                return false;
            }
            foreach (var character in value)
            {
                var allowed = (character >= 'a' && character <= 'z') || (character >= '0' && character <= '9')
                    || character == '.' || character == '_' || character == '-';
                if (!allowed)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>The states a Build check stops at.</summary>
        public const string StateChecking = "checking";

        /// <inheritdoc cref="StateChecking"/>
        public const string StateAdmitted = "admitted";

        /// <inheritdoc cref="StateChecking"/>
        public const string StateRefused = "refused";

        /// <summary>
        /// EditorPrefs key holding the developer token for one project. The token is a secret and
        /// never enters the settings asset, so it is never committed with the project.
        /// </summary>
        public static string DeveloperTokenPreferenceKey(string projectId)
        {
            return "Yogabase.DeveloperToken." + projectId;
        }
    }
}
