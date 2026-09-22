// C# 9 only. Braced namespace, no file-scoped namespaces, no records, no collection expressions.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Yogabase.Editor
{
    /// <summary>
    /// One window that takes a Unity project from source to an admitted Yogabase build: build the
    /// Linux dedicated server, zip it, upload it, and show what the Build check said.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This replaces the former hand-packaging flow. A developer no longer writes a service layout
    /// document, generates an SBOM, runs a scanner, or produces a detached signature: Yogabase
    /// derives the manifest and inventory from the uploaded bundle, scans it, and signs all three
    /// with the installation admission key.
    /// </para>
    /// <para>
    /// The build runs on Unity's main thread; the upload and the poll run on a background task and
    /// report through <see cref="log"/>, which <see cref="Update"/> drains into a repaint.
    /// </para>
    /// </remarks>
    public sealed class YogabasePublishServerWindow : EditorWindow
    {
        private const string StandalonePlatform = "Standalone";
        private const string ArchitectureSetting = "Architecture";
        private const string LinuxUserBuildSettingsTypeName = "UnityEditor.LinuxStandalone.UserBuildSettings";

        private readonly List<string> log = new List<string>();
        private readonly object gate = new object();

        private YogabaseSettings settings;
        private Vector2 scroll;
        private bool skipBuild;
        private bool busy;
        private bool dirty;
        private string phase = "Idle.";
        private YogabaseBuild result;
        private CancellationTokenSource cancellation;

        /// <summary>Opens the publish window.</summary>
        [MenuItem("Yogabase/Publish Server", false, 100)]
        public static void Open()
        {
            var window = GetWindow<YogabasePublishServerWindow>(false, "Publish Server", true);
            window.minSize = new Vector2(520f, 460f);
            window.Show();
        }

        private void OnEnable()
        {
            if (settings == null)
            {
                settings = YogabaseSettings.Find();
            }
        }

        private void OnDisable()
        {
            if (cancellation != null)
            {
                cancellation.Cancel();
                cancellation.Dispose();
                cancellation = null;
            }
        }

        private void Update()
        {
            if (dirty)
            {
                dirty = false;
                Repaint();
            }
        }

        private void OnGUI()
        {
            using (new EditorGUI.DisabledScope(busy))
            {
                settings = (YogabaseSettings)EditorGUILayout.ObjectField(
                    "Settings", settings, typeof(YogabaseSettings), false);
            }

            if (settings == null)
            {
                EditorGUILayout.HelpBox(
                    "Create a Yogabase settings asset (Assets ▸ Create ▸ Yogabase ▸ Settings) and assign "
                    + "it here. It holds the Control Plane URL, the project and environment ids, the "
                    + "service name, the executable name, the architecture, and the declared ports.",
                    MessageType.Info);
                return;
            }

            var publishingLambda = settings.publishKind == YogabasePublishKind.Lambda;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("This publish", EditorStyles.boldLabel);
            if (publishingLambda)
            {
                EditorGUILayout.LabelField("Kind", "Lambda archive");
                EditorGUILayout.LabelField("Function id", string.IsNullOrWhiteSpace(settings.serviceName)
                    ? "(the bundle's own)"
                    : settings.serviceName);
                EditorGUILayout.LabelField("Archive", settings.lambdaArchivePath);
                EditorGUILayout.LabelField("Execution kind", settings.LambdaExecutionKindToken);
                EditorGUILayout.LabelField("Receives from", DescribeBindings(settings.realtimeIngressBindings));
                EditorGUILayout.LabelField("Publishes to", DescribeBindings(settings.realtimeEgressBindings));
            }
            else
            {
                EditorGUILayout.LabelField("Service", settings.serviceName);
                EditorGUILayout.LabelField("Architecture", settings.TargetArchitectureToken);
                EditorGUILayout.LabelField("Executable", settings.executableName);
                EditorGUILayout.LabelField("Ports", string.Join(", ", YogabaseBuildUploader.DescribePorts(settings)));
                EditorGUILayout.LabelField("Output", settings.buildOutputDirectory);
            }

            var problems = settings.Validate();
            if (problems.Count > 0)
            {
                EditorGUILayout.HelpBox("• " + string.Join("\n• ", problems), MessageType.Warning);
            }

            if (!publishingLambda)
            {
                EditorGUILayout.Space();
                using (new EditorGUI.DisabledScope(busy))
                {
                    skipBuild = EditorGUILayout.ToggleLeft(
                        "Upload the existing build output without rebuilding", skipBuild);
                }
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(busy || problems.Count > 0))
                {
                    var label = publishingLambda
                        ? "Publish Lambda archive"
                        : skipBuild ? "Zip and publish" : "Build and publish";
                    if (GUILayout.Button(label, GUILayout.Height(28f)))
                    {
                        StartPublish();
                    }
                }

                using (new EditorGUI.DisabledScope(!busy))
                {
                    if (GUILayout.Button("Cancel", GUILayout.Height(28f), GUILayout.Width(90f)))
                    {
                        if (cancellation != null)
                        {
                            cancellation.Cancel();
                        }
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Status", phase);
            DrawResult();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
            using (var view = new EditorGUILayout.ScrollViewScope(scroll, GUILayout.MinHeight(140f)))
            {
                scroll = view.scrollPosition;
                string[] lines;
                lock (gate)
                {
                    lines = log.ToArray();
                }

                EditorGUILayout.TextArea(string.Join("\n", lines), GUILayout.ExpandHeight(true));
            }
        }

        private void DrawResult()
        {
            if (result == null)
            {
                return;
            }

            EditorGUILayout.LabelField("Build id", result.buildId);
            EditorGUILayout.LabelField("Digest", result.digest);
            EditorGUILayout.LabelField("Identity", result.identity + "  (attempt " + result.attempt + ")");
            EditorGUILayout.LabelField("State", result.state);
            if (!result.IsTerminal)
            {
                return;
            }

            if (result.IsAdmitted)
            {
                EditorGUILayout.HelpBox(
                    "Build check passed. It found no Critical or High finding among the components "
                    + "Yogabase could catalogue, in the pinned advisory snapshot on its recorded date. "
                    + "That is not a claim that the build is secure: nothing is executed, statically "
                    + "linked libraries are opaque, and anything uncatalogued is reported as such.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Build check refused this bundle. Fix what it names below and publish again.",
                    MessageType.Error);
            }

            if (result.check.refusals.Length > 0)
            {
                EditorGUILayout.LabelField("Refusals", EditorStyles.boldLabel);
                foreach (var refusal in result.check.refusals)
                {
                    // The Control Plane names a code, the field it is about, and a bounded cause
                    // token; it deliberately never sends prose or a value from the bundle.
                    EditorGUILayout.HelpBox(
                        refusal.code
                        + (string.IsNullOrEmpty(refusal.field) ? string.Empty : "\nfield: " + refusal.field)
                        + (string.IsNullOrEmpty(refusal.cause) ? string.Empty : "\ncause: " + refusal.cause),
                        MessageType.Error);
                }
            }

            if (result.check.findings.Length > 0)
            {
                EditorGUILayout.LabelField("Findings", EditorStyles.boldLabel);
                foreach (var finding in result.check.findings)
                {
                    var fixedIn = string.IsNullOrEmpty(finding.fixedIn)
                        ? "no fixed version recorded"
                        : "fixed in " + finding.fixedIn;
                    EditorGUILayout.HelpBox(
                        finding.severity + "  " + finding.id + "\n"
                        + finding.package + " " + finding.version + " — " + fixedIn
                        + (string.IsNullOrEmpty(finding.entry) ? string.Empty : "\nfrom " + finding.entry),
                        finding.severity == "Critical" || finding.severity == "High"
                            ? MessageType.Error
                            : MessageType.Warning);
                }
            }
        }

        private void StartPublish()
        {
            lock (gate)
            {
                log.Clear();
            }

            result = null;
            busy = true;
            cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            var projectFolder = Path.GetDirectoryName(Application.dataPath) ?? ".";
            var buildOutput = Path.GetFullPath(Path.Combine(projectFolder, settings.buildOutputDirectory));
            var zipPath = Path.Combine(Path.GetTempPath(), "yogabase-" + settings.serviceName + ".zip");

            try
            {
                if (settings.publishKind == YogabasePublishKind.Lambda)
                {
                    // A Lambda bundle is built outside Unity; it is uploaded exactly as it is, with the
                    // execution declaration the settings state. Nothing is built, zipped, or ELF-checked.
                    zipPath = Path.GetFullPath(Path.Combine(projectFolder, settings.lambdaArchivePath.Trim()));
                    if (!File.Exists(zipPath))
                    {
                        throw new FileNotFoundException("The Lambda archive is not where the settings say.", zipPath);
                    }
                    Report("Publishing the Lambda archive " + zipPath + " as " + settings.LambdaExecutionKindToken
                        + ", receiving from " + DescribeBindings(settings.realtimeIngressBindings)
                        + " and publishing to " + DescribeBindings(settings.realtimeEgressBindings) + ".");
                }
                else if (!skipBuild)
                {
                    Report("Building the Linux dedicated server (" + settings.TargetArchitectureToken + ")…");
                    BuildDedicatedServer(buildOutput);
                }
                else
                {
                    Report("Skipping the build; using the existing output at " + buildOutput + ".");
                }

                if (settings.publishKind != YogabasePublishKind.Lambda)
                {
                    VerifyEntrypoint(buildOutput);

                    Report("Zipping the build output…");
                    EditorUtility.DisplayProgressBar("Yogabase", "Zipping the build output…", 0.5f);
                    YogabaseBuildUploader.CreateBundleZip(buildOutput, settings.executableName, zipPath);
                }

                var megabytes = new FileInfo(zipPath).Length / (1024d * 1024d);
                Report("Bundle is " + megabytes.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
                    + " MB at " + zipPath + ".");
            }
            catch (Exception exception)
            {
                Fail(exception);
                return;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            var captured = settings;
            Task.Run(async () =>
            {
                try
                {
                    Report("Uploading to " + captured.controlPlaneUrl.Trim() + "…");
                    var accepted = await YogabaseBuildUploader.UploadAsync(captured, zipPath, token)
                        .ConfigureAwait(false);
                    Report("Accepted as build " + accepted.buildId + " (" + accepted.digest + ").");
                    SetResult(accepted);

                    Report("Waiting for the Build check…");
                    var checkedBuild = await YogabaseBuildUploader
                        .WaitForCheckAsync(captured, accepted.buildId, SetResult, token)
                        .ConfigureAwait(false);
                    SetResult(checkedBuild);
                    Report(checkedBuild.IsAdmitted
                        ? "Build check passed. The build is admitted."
                        : "Build check refused the bundle.");
                }
                catch (OperationCanceledException)
                {
                    Report("Cancelled. The upload may still be running server-side.");
                }
                catch (Exception exception)
                {
                    Report("FAILED: " + exception.Message);
                }
                finally
                {
                    busy = false;
                    phase = "Idle.";
                    dirty = true;
                }
            });
        }

        /// <summary>
        /// Builds the Linux Dedicated Server subtarget with IL2CPP at the declared architecture.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The subtarget must be <c>Server</c>, not a Player build run with <c>-nographics</c>: a
        /// Player build still links the desktop graphics and audio stack, and those libraries are
        /// not in Yogabase's base-image allowlist, so its dependency closure will not resolve.
        /// </para>
        /// <para>
        /// The architecture is written through
        /// <c>UnityEditor.LinuxStandalone.UserBuildSettings.architecture</c> by reflection. That
        /// property is the only input the Linux player build reads when it chooses an architecture:
        /// <c>PlayerSettings.SetArchitecture</c> and
        /// <c>EditorUserBuildSettings.SetPlatformSettings("Standalone", "Architecture", …)</c> are
        /// not in that chain at all, and a build that sets only those reports success while emitting
        /// an x86-64 binary under an ARM64 name. Reflection is used because the property lives in the
        /// Linux module's extension assembly, which Editor assemblies do not reference and which
        /// only exists when that module is installed. All three are written anyway so nothing else
        /// keying off the legacy values disagrees with what was actually built.
        /// </para>
        /// </remarks>
        private void BuildDedicatedServer(string buildOutput)
        {
            var scenes = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    scenes.Add(scene.path);
                }
            }
            if (scenes.Count == 0)
            {
                throw new InvalidOperationException(
                    "No scenes are enabled in Build Settings, so the server would start with nothing loaded.");
            }

            var previousSubtarget = EditorUserBuildSettings.standaloneBuildSubtarget;
            var previousServerBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Server);
            var previousServerArchitecture = PlayerSettings.GetArchitecture(NamedBuildTarget.Server);
            var previousStandaloneArchitecture = PlayerSettings.GetArchitecture(NamedBuildTarget.Standalone);
            var previousPlatformSetting =
                EditorUserBuildSettings.GetPlatformSettings(StandalonePlatform, ArchitectureSetting);
            int? previousLinuxArchitecture = null;

            try
            {
                EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;
                previousLinuxArchitecture = LinuxPlayerArchitecture.Get();
                LinuxPlayerArchitecture.Set(settings.OsArchitecture);
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Server, ScriptingImplementation.IL2CPP);
                PlayerSettings.SetArchitecture(NamedBuildTarget.Server, settings.OsArchitecture);
                PlayerSettings.SetArchitecture(NamedBuildTarget.Standalone, settings.OsArchitecture);
                EditorUserBuildSettings.SetPlatformSettings(
                    StandalonePlatform,
                    ArchitectureSetting,
                    settings.targetArchitecture == YogabaseTargetArchitecture.LinuxArm64 ? "ARM64" : "x64");
                // Persist before anything can reload settings from disk: these setters are in-memory
                // only, and a reload silently reverts the architecture while the build carries on.
                AssetDatabase.SaveAssets();

                Directory.CreateDirectory(buildOutput);
                var options = new BuildPlayerOptions
                {
                    scenes = scenes.ToArray(),
                    locationPathName = Path.Combine(buildOutput, settings.executableName),
                    target = BuildTarget.StandaloneLinux64,
                    targetGroup = BuildTargetGroup.Standalone,
                    subtarget = (int)StandaloneBuildSubtarget.Server,
                    options = BuildOptions.None,
                };

                var report = BuildPipeline.BuildPlayer(options);
                if (report == null || report.summary.result != BuildResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Unity's build did not succeed: "
                        + (report == null ? "no report" : report.summary.result.ToString())
                        + ". See the Console for the build errors.");
                }

                Report("Built " + report.summary.outputPath + " ("
                    + (report.summary.totalSize / (1024d * 1024d)).ToString(
                        "0.0", System.Globalization.CultureInfo.InvariantCulture)
                    + " MB).");
            }
            finally
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Server, previousServerBackend);
                PlayerSettings.SetArchitecture(NamedBuildTarget.Server, previousServerArchitecture);
                PlayerSettings.SetArchitecture(NamedBuildTarget.Standalone, previousStandaloneArchitecture);
                EditorUserBuildSettings.SetPlatformSettings(
                    StandalonePlatform, ArchitectureSetting, previousPlatformSetting);
                if (previousLinuxArchitecture.HasValue)
                {
                    // Before restoring the subtarget, not after: this property writes to whichever
                    // profile the current subtarget selects.
                    LinuxPlayerArchitecture.Set(previousLinuxArchitecture.Value);
                }

                EditorUserBuildSettings.standaloneBuildSubtarget = previousSubtarget;
                AssetDatabase.SaveAssets();
            }
        }

        // Catching a mismatch here is worth a few milliseconds: uploading a binary whose e_machine
        // disagrees with the declared architecture wastes a whole round trip to learn it.
        private void VerifyEntrypoint(string buildOutput)
        {
            var entrypoint = Path.Combine(buildOutput, settings.executableName);
            if (!File.Exists(entrypoint))
            {
                throw new FileNotFoundException(
                    "The declared executable is not in the build output. Check the executable name in "
                    + "the settings asset against what Unity actually wrote.",
                    entrypoint);
            }

            var machine = YogabaseBuildUploader.ReadElfMachine(entrypoint);
            var expected = settings.targetArchitecture == YogabaseTargetArchitecture.LinuxArm64 ? 183 : 62;
            if (machine != expected)
            {
                throw new InvalidOperationException(
                    "The built binary is " + YogabaseBuildUploader.DescribeElfMachine(machine)
                    + " but the settings declare " + settings.TargetArchitectureToken
                    + ". Yogabase refuses a bundle whose ELF architecture disagrees with its declared "
                    + "target, so this would fail admission. Fix the architecture and rebuild.");
            }

            Report("Entrypoint is " + YogabaseBuildUploader.DescribeElfMachine(machine) + ".");
        }

        private static string DescribeBindings(List<string> bindings)
        {
            return bindings == null || bindings.Count == 0 ? "(none)" : string.Join(", ", bindings);
        }

        private void SetResult(YogabaseBuild build)
        {
            result = build;
            phase = "Build " + build.buildId + " is " + build.state + ".";
            dirty = true;
        }

        private void Fail(Exception exception)
        {
            Report("FAILED: " + exception.Message);
            busy = false;
            phase = "Idle.";
            dirty = true;
        }

        private void Report(string message)
        {
            lock (gate)
            {
                log.Add(DateTime.Now.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
                    + "  " + message);
            }

            phase = message;
            dirty = true;
        }

        /// <summary>
        /// The one setting the Linux player build reads when it chooses an architecture, reached by
        /// reflection because it lives in the Linux module's extension assembly.
        /// </summary>
        private static class LinuxPlayerArchitecture
        {
            public static int Get()
            {
                return Convert.ToInt32(
                    ResolveProperty().GetValue(null), System.Globalization.CultureInfo.InvariantCulture);
            }

            /// <param name="osArchitecture">
            /// A <c>UnityEditor.Build.OSArchitecture</c> value: 0 x64, 1 ARM64.
            /// </param>
            public static void Set(int osArchitecture)
            {
                var property = ResolveProperty();
                property.SetValue(null, Enum.ToObject(property.PropertyType, osArchitecture));
            }

            private static PropertyInfo ResolveProperty()
            {
                var type = ResolveType();
                var property = type.GetProperty(
                    "architecture", BindingFlags.Public | BindingFlags.Static);
                if (property == null || !property.CanRead || !property.CanWrite)
                {
                    throw new InvalidOperationException(
                        "'" + LinuxUserBuildSettingsTypeName + "' has no readable/writable static "
                        + "'architecture' property in this Unity version. That property is the only "
                        + "input the Linux player build reads when choosing an architecture, so without "
                        + "it this build cannot tell Unity which architecture to emit.");
                }

                return property;
            }

            private static Type ResolveType()
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var candidate = assembly.GetType(LinuxUserBuildSettingsTypeName, false);
                    if (candidate != null)
                    {
                        return candidate;
                    }
                }

                throw new InvalidOperationException(
                    "Could not find '" + LinuxUserBuildSettingsTypeName + "' in any loaded assembly. It "
                    + "ships in the Linux Dedicated Server module's UnityEditor.LinuxStandalone."
                    + "Extensions.dll, so that module is not installed. Install it from Unity Hub "
                    + "(module id linux-server) before publishing.");
            }
        }
    }
}
