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

        [Header("Build")]
        [Tooltip("Hosted-service name. Builds are versioned per name per project.")]
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
