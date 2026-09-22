// C# 9 only. Braced namespace, no file-scoped namespaces, no records.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Yogabase.Editor
{
    /// <summary>Architectures a Yogabase hosted service may declare.</summary>
    public enum YogabaseTargetArchitecture
    {
        /// <summary>x86-64. Runs under emulation on an Apple Silicon cluster.</summary>
        LinuxX64 = 0,

        /// <summary>AArch64. Native on an Apple Silicon cluster.</summary>
        LinuxArm64 = 1,
    }

    /// <summary>One network port the built server listens on.</summary>
    [Serializable]
    public sealed class YogabasePortDeclaration
    {
        [Tooltip("Port name a client later looks up in an allocation, for example \"game\".")]
        public string name = "game";

        [Tooltip("Port the server listens on.")]
        public int port = 7777;

        [Tooltip("Transport protocol.")]
        public YogabasePortProtocol protocol = YogabasePortProtocol.Udp;

        /// <summary>The <c>name=port/protocol</c> form the multipart <c>ports</c> field carries.</summary>
        public string ToFieldValue()
        {
            return name + "=" + port.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + "/" + (protocol == YogabasePortProtocol.Tcp ? "tcp" : "udp");
        }
    }

    /// <summary>Transport protocol of a declared port.</summary>
    /// <summary>What the publish window uploads.</summary>
    public enum YogabasePublishKind
    {
        /// <summary>A Unity Linux dedicated server the window builds, zips, and uploads.</summary>
        Server = 0,

        /// <summary>A Lambda bundle zip built outside Unity, uploaded with its execution declaration.</summary>
        Lambda = 1,
    }

    /// <summary>How a Lambda keeps state, spelled exactly as the Builds contract's <c>executionKind</c>.</summary>
    public enum YogabaseLambdaExecutionKind
    {
        /// <summary>Keeps no state between calls.</summary>
        Stateless = 0,

        /// <summary>Keeps durable state per actor.</summary>
        Stateful = 1,
    }

    public enum YogabasePortProtocol
    {
        /// <summary>UDP. The usual choice for a realtime game server.</summary>
        Udp = 0,

        /// <summary>TCP.</summary>
        Tcp = 1,
    }

    /// <summary>
    /// Where this project publishes its dedicated server, and what it declares about it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Create one with <c>Assets ▸ Create ▸ Yogabase ▸ Settings</c> and commit it: everything on it
    /// is project configuration, not a credential.
    /// </para>
    /// <para>
    /// <b>The developer token is deliberately not a field on this asset.</b> It is a secret that
    /// speaks for the project's builds, and a serialized field would put it in a committed
    /// <c>.asset</c> file and in every clone of the repository. It lives in
    /// <see cref="EditorPrefs"/> instead, keyed by project id, so it stays on the machine that
    /// pasted it. Paste it in this asset's inspector or in <c>Yogabase ▸ Publish Server</c>.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(fileName = "YogabaseSettings", menuName = "Yogabase/Settings", order = 400)]
    public sealed class YogabaseSettings : ScriptableObject
    {
        [Header("Installation")]
        [Tooltip("The installation's Control Plane origin, for example https://control-plane.example.com. "
            + "This is not the application gateway a game client talks to.")]
        public string controlPlaneUrl = string.Empty;

        [Tooltip("Project id, from the console's Settings page.")]
        public string projectId = string.Empty;

        [Tooltip("Environment id to publish into.")]
        public string environmentId = string.Empty;

        [Header("What to publish")]
        [Tooltip("Server builds, zips, and uploads a Unity Linux dedicated server. Lambda uploads a Lambda "
            + "bundle zip built outside Unity, with the execution declaration below.")]
        public YogabasePublishKind publishKind = YogabasePublishKind.Server;

        [Header("Build")]
        [Tooltip("Hosted-service name, or a Lambda's function id. Builds are versioned per name per "
            + "project. For a Lambda it may be left empty to use the bundle's own function id.")]
        public string serviceName = string.Empty;

        [Tooltip("File name of the Linux server executable Unity produces, and the path inside the "
            + "uploaded zip. Never a .sh wrapper: a script entrypoint is refused as a foreign runtime.")]
        public string executableName = "Server.x86_64";

        [Tooltip("Architecture to build and declare. The declared value must match the binary Unity "
            + "actually emits; the publish window verifies the ELF before uploading.")]
        public YogabaseTargetArchitecture targetArchitecture = YogabaseTargetArchitecture.LinuxX64;

        [Tooltip("Ports the server listens on. A client looks these up by name in the allocation it "
            + "receives from /v1/servers/start.")]
        public List<YogabasePortDeclaration> ports = new List<YogabasePortDeclaration>
        {
            new YogabasePortDeclaration { name = "game", port = 7777, protocol = YogabasePortProtocol.Udp },
        };

        [Tooltip("Optional version label recorded with the build. Leave empty to send none.")]
        public string version = string.Empty;

        [Tooltip("Build output directory, relative to the project folder. Its whole contents are zipped.")]
        public string buildOutputDirectory = "Builds/LinuxServer";

        [Header("Lambda archive")]
        [Tooltip("The Lambda bundle zip to upload, relative to the project folder. Used only when "
            + "publishing a Lambda; the window uploads it as it is.")]
        public string lambdaArchivePath = string.Empty;

        [Tooltip("Stateless keeps no state between calls; Stateful keeps durable state per actor. "
            + "Yogabase never works this out from the build.")]
        public YogabaseLambdaExecutionKind lambdaExecutionKind = YogabaseLambdaExecutionKind.Stateless;

        [Tooltip("Realtime binding ids this Lambda receives events from. Leave empty to declare none.")]
        public List<string> realtimeIngressBindings = new List<string>();

        [Tooltip("Realtime binding ids this Lambda publishes events to. It can publish only to these. "
            + "Leave empty to declare none.")]
        public List<string> realtimeEgressBindings = new List<string>();

        [Tooltip("For a bundle whose entry is RegisterLambdas: every Lambda id it registers. Yogabase never "
            + "works these out from the build, and the sandbox refuses a bundle that registers anything "
            + "else. Leave empty for a single-function (InvokeAsync) bundle.")]
        public List<string> registeredLambdaIds = new List<string>();

        /// <summary>The <c>executionKind</c> field value: exactly <c>Stateless</c> or <c>Stateful</c>.</summary>
        public string LambdaExecutionKindToken
        {
            get { return lambdaExecutionKind == YogabaseLambdaExecutionKind.Stateful ? "Stateful" : "Stateless"; }
        }

        /// <summary>The architecture token the product contract uses.</summary>
        public string TargetArchitectureToken
        {
            get
            {
                return targetArchitecture == YogabaseTargetArchitecture.LinuxArm64
                    ? "linux-arm64"
                    : "linux-x64";
            }
        }

        /// <summary>
        /// <c>UnityEditor.Build.OSArchitecture</c> value for the declared architecture: 0 x64,
        /// 1 ARM64.
        /// </summary>
        public int OsArchitecture
        {
            get { return targetArchitecture == YogabaseTargetArchitecture.LinuxArm64 ? 1 : 0; }
        }

        /// <summary>Reads this project's developer token from EditorPrefs, or an empty string.</summary>
        public string GetDeveloperToken()
        {
            if (string.IsNullOrEmpty(projectId))
            {
                return string.Empty;
            }

            return EditorPrefs.GetString(
                YogabaseEditorRoutes.DeveloperTokenPreferenceKey(projectId), string.Empty);
        }

        /// <summary>Stores this project's developer token in EditorPrefs, never in this asset.</summary>
        public void SetDeveloperToken(string token)
        {
            if (string.IsNullOrEmpty(projectId))
            {
                throw new InvalidOperationException(
                    "Set the project id before pasting a developer token: the token is stored per project.");
            }

            var key = YogabaseEditorRoutes.DeveloperTokenPreferenceKey(projectId);
            if (string.IsNullOrEmpty(token))
            {
                EditorPrefs.DeleteKey(key);
                return;
            }

            EditorPrefs.SetString(key, token.Trim());
        }

        /// <summary>Forgets this project's developer token on this machine.</summary>
        public void ClearDeveloperToken()
        {
            if (!string.IsNullOrEmpty(projectId))
            {
                EditorPrefs.DeleteKey(YogabaseEditorRoutes.DeveloperTokenPreferenceKey(projectId));
            }
        }

        /// <summary>
        /// Everything wrong with these settings, in the order a developer should fix it. An empty
        /// list means the publish flow may run.
        /// </summary>
        public List<string> Validate()
        {
            var problems = new List<string>();
            Uri parsed;
            if (string.IsNullOrWhiteSpace(controlPlaneUrl))
            {
                problems.Add("Control Plane URL is empty.");
            }
            else if (!Uri.TryCreate(controlPlaneUrl.Trim(), UriKind.Absolute, out parsed))
            {
                problems.Add("Control Plane URL is not an absolute URI.");
            }
            else if (parsed.Scheme != Uri.UriSchemeHttps && !parsed.IsLoopback)
            {
                problems.Add("Control Plane URL must use HTTPS, or be a loopback address for a local installation.");
            }

            Guid ignored;
            if (!Guid.TryParse(projectId, out ignored))
            {
                problems.Add("Project id is not a GUID.");
            }
            if (!Guid.TryParse(environmentId, out ignored))
            {
                problems.Add("Environment id is not a GUID.");
            }
            if (publishKind == YogabasePublishKind.Lambda)
            {
                ValidateLambda(problems);
            }
            else
            {
                ValidateServer(problems);
            }

            var token = GetDeveloperToken();
            if (string.IsNullOrEmpty(token))
            {
                problems.Add(
                    "No developer token on this machine. Paste the project-scoped token from the "
                    + "console's Settings page; it is stored in EditorPrefs, never in this asset.");
            }
            else if (!token.StartsWith(YogabaseEditorRoutes.DeveloperTokenPrefix, StringComparison.Ordinal)
                || token.Length > YogabaseEditorRoutes.MaximumDeveloperTokenLength)
            {
                problems.Add(
                    "The stored token is not a Yogabase developer token: one starts with '"
                    + YogabaseEditorRoutes.DeveloperTokenPrefix + "' and is at most "
                    + YogabaseEditorRoutes.MaximumDeveloperTokenLength + " characters. A console "
                    + "session cookie, a player token, or a Delivery bearer is refused by the build routes.");
            }

            return problems;
        }

        private void ValidateServer(List<string> problems)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                problems.Add("Service name is empty.");
            }
            if (string.IsNullOrWhiteSpace(executableName))
            {
                problems.Add("Executable name is empty.");
            }
            else if (executableName.EndsWith(".sh", StringComparison.OrdinalIgnoreCase)
                || executableName.EndsWith(".bash", StringComparison.OrdinalIgnoreCase))
            {
                problems.Add(
                    "Executable name points at a shell script. Yogabase refuses a script entrypoint as a "
                    + "foreign runtime; point it at the ELF binary Unity produces.");
            }

            if (ports == null || ports.Count == 0)
            {
                problems.Add("Declare at least one port; a client has nothing to connect to otherwise.");
            }
            else
            {
                var seen = new HashSet<string>(StringComparer.Ordinal);
                for (var index = 0; index < ports.Count; index++)
                {
                    var declared = ports[index];
                    if (declared == null || string.IsNullOrWhiteSpace(declared.name))
                    {
                        problems.Add("Port " + index + " has no name.");
                        continue;
                    }
                    if (declared.name.IndexOf('=') >= 0 || declared.name.IndexOf('/') >= 0)
                    {
                        problems.Add("Port name '" + declared.name + "' may not contain '=' or '/'.");
                    }
                    if (!seen.Add(declared.name))
                    {
                        problems.Add("Port name '" + declared.name + "' is declared twice.");
                    }
                    if (declared.port < 1 || declared.port > 65535)
                    {
                        problems.Add("Port '" + declared.name + "' is outside 1-65535.");
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(buildOutputDirectory))
            {
                problems.Add("Build output directory is empty.");
            }
        }

        private void ValidateLambda(List<string> problems)
        {
            if (!string.IsNullOrWhiteSpace(serviceName) && !IsFunctionId(serviceName.Trim()))
            {
                problems.Add(
                    "A Lambda's function id uses lowercase letters, digits, '.', '_' and '-', starting with a "
                    + "letter. Leave the name empty to use the bundle's own.");
            }
            if (string.IsNullOrWhiteSpace(lambdaArchivePath))
            {
                problems.Add("Lambda archive path is empty.");
            }
            else if (!lambdaArchivePath.Trim().EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                problems.Add("The Lambda archive is a .zip bundle.");
            }

            ValidateBindings(realtimeIngressBindings, YogabaseEditorRoutes.FieldRealtimeIngressBindings, problems);
            ValidateBindings(realtimeEgressBindings, YogabaseEditorRoutes.FieldRealtimeEgressBindings, problems);
            ValidateRegisteredLambdaIds(problems);
        }

        // The Control Plane's own registration rules, so a registration it would refuse never leaves
        // the Editor: count, each id, repeats, the reserved build name, and every ingress binding
        // being a registered id.
        private void ValidateRegisteredLambdaIds(List<string> problems)
        {
            if (registeredLambdaIds == null || registeredLambdaIds.Count == 0)
            {
                return;
            }
            const string Field = YogabaseEditorRoutes.FieldRegisteredLambdaIds;
            if (registeredLambdaIds.Count > YogabaseEditorRoutes.MaximumRegisteredLambdaIds)
            {
                problems.Add("At most " + YogabaseEditorRoutes.MaximumRegisteredLambdaIds
                    + " Lambda ids may be listed in " + Field + ".");
            }

            var buildName = string.IsNullOrWhiteSpace(serviceName) ? null : serviceName.Trim();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < registeredLambdaIds.Count; index++)
            {
                var id = registeredLambdaIds[index] == null ? null : registeredLambdaIds[index].Trim();
                if (!YogabaseEditorRoutes.IsRegisteredLambdaId(id))
                {
                    problems.Add(Field + "[" + index + "] is not a Lambda id: a lowercase letter, then lowercase "
                        + "letters, digits, '.', '_' or '-', at most "
                        + YogabaseEditorRoutes.MaximumRegisteredLambdaIdLength + " characters.");
                }
                else if (!seen.Add(id))
                {
                    problems.Add(Field + "[" + index + "] repeats an id listed before it.");
                }
                else if (buildName != null && string.Equals(id, buildName, StringComparison.Ordinal))
                {
                    problems.Add(Field + "[" + index + "] is the build's own name, which stays reserved for "
                        + "the build itself.");
                }
            }

            if (realtimeIngressBindings == null)
            {
                return;
            }
            for (var index = 0; index < realtimeIngressBindings.Count; index++)
            {
                var binding = realtimeIngressBindings[index] == null ? null : realtimeIngressBindings[index].Trim();
                if (binding != null && !seen.Contains(binding))
                {
                    problems.Add(YogabaseEditorRoutes.FieldRealtimeIngressBindings + "[" + index + "] is not one "
                        + "of the registered Lambda ids: a registering build receives realtime events only on a "
                        + "registered id.");
                }
            }
        }

        // The admission Job's own rules, so a declaration a member would refuse never leaves the Editor.
        private static void ValidateBindings(List<string> bindings, string field, List<string> problems)
        {
            if (bindings == null)
            {
                return;
            }
            if (bindings.Count > YogabaseEditorRoutes.MaximumBindingsPerDirection)
            {
                problems.Add("At most " + YogabaseEditorRoutes.MaximumBindingsPerDirection
                    + " realtime binding ids may be listed in " + field + ".");
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < bindings.Count; index++)
            {
                var binding = bindings[index];
                if (!YogabaseEditorRoutes.IsRealtimeBindingId(binding))
                {
                    problems.Add(field + "[" + index + "] is not a realtime binding id: a lowercase letter, then "
                        + "lowercase letters, digits, '.', '_' or '-', at most "
                        + YogabaseEditorRoutes.MaximumBindingIdLength + " characters.");
                }
                else if (!seen.Add(binding))
                {
                    problems.Add(field + "[" + index + "] repeats a binding listed before it.");
                }
            }
        }

        private static bool IsFunctionId(string value)
        {
            if (value.Length == 0 || value[0] < 'a' || value[0] > 'z')
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
        /// The settings asset in this project, or <see langword="null"/> when there is none. Refuses
        /// to guess when a project carries more than one.
        /// </summary>
        public static YogabaseSettings Find()
        {
            var guids = AssetDatabase.FindAssets("t:YogabaseSettings");
            if (guids == null || guids.Length == 0)
            {
                return null;
            }
            if (guids.Length > 1)
            {
                Debug.LogWarning(
                    "[Yogabase] This project carries " + guids.Length + " YogabaseSettings assets. Using "
                    + AssetDatabase.GUIDToAssetPath(guids[0]) + ".");
            }

            return AssetDatabase.LoadAssetAtPath<YogabaseSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
