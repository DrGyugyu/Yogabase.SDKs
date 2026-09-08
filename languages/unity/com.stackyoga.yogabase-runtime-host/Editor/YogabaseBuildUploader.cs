// C# 9 only. Braced namespace, no file-scoped namespaces, no records, no collection expressions.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Yogabase.Editor
{
    /// <summary>One dependency finding the Build check reported.</summary>
    [Serializable]
    public sealed class YogabaseBuildFinding
    {
        /// <summary>Advisory identifier, for example a CVE or GHSA id.</summary>
        public string id = string.Empty;

        /// <summary>Catalogued package the advisory applies to.</summary>
        public string package = string.Empty;

        /// <summary>Version found in the bundle.</summary>
        public string version = string.Empty;

        /// <summary>First fixed version, when the advisory names one.</summary>
        public string fixedIn = string.Empty;

        /// <summary>Advisory severity as recorded in the pinned snapshot.</summary>
        public string severity = string.Empty;

        /// <summary>Bundle entry the catalogued component came from.</summary>
        public string entry = string.Empty;
    }

    /// <summary>One reason the bundle itself was refused.</summary>
    [Serializable]
    public sealed class YogabaseBuildRefusal
    {
        /// <summary>Stable refusal code.</summary>
        public string code = string.Empty;

        /// <summary>First failing field or bundle path.</summary>
        public string path = string.Empty;

        /// <summary>Human-readable reason.</summary>
        public string message = string.Empty;
    }

    /// <summary>Result of Yogabase's own pass over the uploaded bundle.</summary>
    [Serializable]
    public sealed class YogabaseBuildCheck
    {
        /// <summary>
        /// Whether the check found no Critical/High finding among the catalogued components in the
        /// pinned snapshot on its recorded date. Never a claim that the build is secure.
        /// </summary>
        public bool passed;

        /// <summary>Vulnerability findings, empty when none were catalogued.</summary>
        public YogabaseBuildFinding[] findings = Array.Empty<YogabaseBuildFinding>();

        /// <summary>Structural refusals, empty when the bundle's layout was accepted.</summary>
        public YogabaseBuildRefusal[] refusals = Array.Empty<YogabaseBuildRefusal>();
    }

    /// <summary>One uploaded build and the state of its check.</summary>
    [Serializable]
    public sealed class YogabaseBuild
    {
        /// <summary>Opaque build identifier.</summary>
        public string buildId = string.Empty;

        /// <summary>Digest Yogabase recorded for the uploaded bundle.</summary>
        public string digest = string.Empty;

        /// <summary><c>checking</c>, <c>admitted</c>, or <c>refused</c>.</summary>
        public string state = string.Empty;

        /// <summary>Check result. Meaningless while the state is <c>checking</c>.</summary>
        public YogabaseBuildCheck check = new YogabaseBuildCheck();

        /// <summary>Whether the check has finished, either way.</summary>
        public bool IsTerminal
        {
            get
            {
                return state == YogabaseEditorRoutes.StateAdmitted
                    || state == YogabaseEditorRoutes.StateRefused;
            }
        }

        /// <summary>Whether the build may be released.</summary>
        public bool IsAdmitted
        {
            get { return state == YogabaseEditorRoutes.StateAdmitted; }
        }
    }

    /// <summary>Failure reported by the Control Plane's build surface.</summary>
    public sealed class YogabaseBuildException : Exception
    {
        /// <summary>Creates a failure for one build-surface response.</summary>
        public YogabaseBuildException(int statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }

        /// <summary>HTTP status the Control Plane answered, or 0 for a local failure.</summary>
        public int StatusCode { get; }
    }

    /// <summary>
    /// Zips a Unity server build and uploads it to the Control Plane's build surface.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This speaks the multipart request directly with <see cref="HttpClient"/> rather than
    /// referencing the <c>Yogabase.Yogabase.Client</c> NuGet package. That package targets
    /// <c>netstandard2.1</c> and would import cleanly as a managed plugin, but its dependency
    /// closure (SignalR and <c>System.Text.Json</c> and their transitive assemblies) would have to
    /// be vendored into the project alongside it, and none of that closure is needed to send one
    /// form. The trade is deliberate: this file duplicates a small amount of the .NET client's
    /// request shape, and <see cref="YogabaseEditorRoutes"/> keeps the routes in one place so the
    /// two stay alignable. Responses are read with <see cref="JsonUtility"/>, which Unity ships.
    /// </para>
    /// <para>
    /// Nothing here runs on Unity's main thread except the zip step, so the caller must marshal any
    /// UI update itself.
    /// </para>
    /// </remarks>
    public static class YogabaseBuildUploader
    {
        private const int MaximumResponseBytes = 4 * 1024 * 1024;

        /// <summary>
        /// Zips a build output directory. Entry names are the paths relative to
        /// <paramref name="directoryPath"/> with forward slashes, and the declared executable plus
        /// the usual native player files keep a <c>0755</c> mode.
        /// </summary>
        /// <remarks>
        /// The mode bits matter: a Unity Linux dedicated server will not start if its player binary
        /// loses the executable bit in transit, and a zip that only carries <c>0644</c> is the usual
        /// way that happens.
        /// </remarks>
        public static void CreateBundleZip(string directoryPath, string executable, string zipPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException("A build output directory is required.", nameof(directoryPath));
            }

            var root = new DirectoryInfo(directoryPath);
            if (!root.Exists)
            {
                throw new DirectoryNotFoundException("Build output directory not found: " + directoryPath);
            }

            var rootPath = root.FullName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var files = root.GetFiles("*", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                throw new ArgumentException("The build output directory is empty.", nameof(directoryPath));
            }

            Array.Sort(files, (left, right) => string.CompareOrdinal(left.FullName, right.FullName));
            var directory = Path.GetDirectoryName(zipPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            using (var output = new FileStream(zipPath, FileMode.CreateNew, FileAccess.Write))
            using (var archive = new ZipArchive(output, ZipArchiveMode.Create))
            {
                foreach (var file in files)
                {
                    var relative = file.FullName.Substring(rootPath.Length)
                        .Replace(Path.DirectorySeparatorChar, '/');
                    // Fully qualified: UnityEngine also declares a CompressionLevel, and an
                    // unqualified name is ambiguous inside an Editor assembly.
                    var entry = archive.CreateEntry(
                        relative, System.IO.Compression.CompressionLevel.Optimal);
                    // ZipArchive throws below 1980; a restored or generated file can carry an
                    // earlier timestamp, and failing the whole publish over it would be absurd.
                    var written = file.LastWriteTime;
                    entry.LastWriteTime = written.Year < 1980
                        ? new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero)
                        : new DateTimeOffset(written);
                    entry.ExternalAttributes = UnixMode(relative, executable) << 16;
                    using (var source = file.OpenRead())
                    using (var target = entry.Open())
                    {
                        source.CopyTo(target);
                    }
                }
            }
        }

        /// <summary>
        /// Uploads one bundle zip and returns the accepted build, whose check has started.
        /// </summary>
        public static async Task<YogabaseBuild> UploadAsync(
            YogabaseSettings settings,
            string zipPath,
            CancellationToken cancellationToken)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            if (!File.Exists(zipPath))
            {
                throw new FileNotFoundException("Bundle zip not found.", zipPath);
            }

            using (var client = CreateClient(settings))
            using (var form = new MultipartFormDataContent())
            using (var bundle = new FileStream(zipPath, FileMode.Open, FileAccess.Read))
            {
                // Metadata first, bundle last: a server that reads the form as a stream can then
                // refuse a malformed port or executable before it has buffered the whole archive.
                form.Add(new StringContent(settings.executableName), YogabaseEditorRoutes.FieldExecutable);
                form.Add(new StringContent(settings.serviceName), YogabaseEditorRoutes.FieldName);
                if (!string.IsNullOrWhiteSpace(settings.version))
                {
                    form.Add(new StringContent(settings.version.Trim()), YogabaseEditorRoutes.FieldVersion);
                }
                foreach (var declared in settings.ports)
                {
                    form.Add(new StringContent(declared.ToFieldValue()), YogabaseEditorRoutes.FieldPorts);
                }

                var bundleContent = new StreamContent(bundle);
                bundleContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
                form.Add(bundleContent, YogabaseEditorRoutes.FieldBundle, Path.GetFileName(zipPath));

                var route = YogabaseEditorRoutes.Builds(
                    settings.projectId.Trim(), settings.environmentId.Trim());
                using (var request = new HttpRequestMessage(HttpMethod.Post, route))
                {
                    request.Content = form;
                    return await SendAsync(client, request, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <summary>Reads one build and its current check result.</summary>
        public static async Task<YogabaseBuild> GetAsync(
            YogabaseSettings settings,
            string buildId,
            CancellationToken cancellationToken)
        {
            using (var client = CreateClient(settings))
            {
                var route = YogabaseEditorRoutes.Build(
                    settings.projectId.Trim(), settings.environmentId.Trim(), buildId);
                using (var request = new HttpRequestMessage(HttpMethod.Get, route))
                {
                    return await SendAsync(client, request, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Polls until the check reaches <c>admitted</c> or <c>refused</c>. A refused build is
        /// returned, not thrown: its refusals and findings are what the developer needs to see.
        /// </summary>
        public static async Task<YogabaseBuild> WaitForCheckAsync(
            YogabaseSettings settings,
            string buildId,
            Action<YogabaseBuild> onPoll,
            CancellationToken cancellationToken)
        {
            var delay = TimeSpan.FromSeconds(3);
            var deadline = DateTime.UtcNow + TimeSpan.FromMinutes(15);
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var build = await GetAsync(settings, buildId, cancellationToken).ConfigureAwait(false);
                if (onPoll != null)
                {
                    onPoll(build);
                }
                if (build.IsTerminal)
                {
                    return build;
                }
                if (DateTime.UtcNow >= deadline)
                {
                    throw new YogabaseBuildException(
                        0, "The Build check did not finish within fifteen minutes.");
                }

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                var doubled = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
                delay = doubled > TimeSpan.FromSeconds(30) ? TimeSpan.FromSeconds(30) : doubled;
            }
        }

        private static HttpClient CreateClient(YogabaseSettings settings)
        {
            var token = settings.GetDeveloperToken();
            if (string.IsNullOrEmpty(token))
            {
                throw new YogabaseBuildException(
                    0,
                    "No developer token on this machine. Paste the project-scoped token from the "
                    + "console's Settings page into the Yogabase settings asset.");
            }

            var origin = settings.controlPlaneUrl.Trim();
            if (!origin.EndsWith("/", StringComparison.Ordinal))
            {
                origin += "/";
            }

            var handler = new HttpClientHandler { AllowAutoRedirect = false };
            var client = new HttpClient(handler, true)
            {
                BaseAddress = new Uri(origin, UriKind.Absolute),
                // A build bundle is hundreds of megabytes on a slow link.
                Timeout = TimeSpan.FromMinutes(30),
            };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static async Task<YogabaseBuild> SendAsync(
            HttpClient client,
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            using (var response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false))
            {
                var body = await ReadBoundedAsync(response, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw new YogabaseBuildException(
                        (int)response.StatusCode,
                        "Yogabase returned HTTP " + (int)response.StatusCode
                        + (string.IsNullOrEmpty(body) ? "." : ": " + Summarize(body)));
                }

                YogabaseBuild build;
                try
                {
                    build = JsonUtility.FromJson<YogabaseBuild>(body);
                }
                catch (ArgumentException exception)
                {
                    throw new YogabaseBuildException(
                        (int)response.StatusCode,
                        "Yogabase returned an unreadable build response: " + exception.Message);
                }

                if (build == null || string.IsNullOrEmpty(build.buildId) || string.IsNullOrEmpty(build.state))
                {
                    throw new YogabaseBuildException(
                        (int)response.StatusCode, "Yogabase returned an invalid build.");
                }
                if (build.check == null)
                {
                    build.check = new YogabaseBuildCheck();
                }
                if (build.check.findings == null)
                {
                    build.check.findings = Array.Empty<YogabaseBuildFinding>();
                }
                if (build.check.refusals == null)
                {
                    build.check.refusals = Array.Empty<YogabaseBuildRefusal>();
                }

                return build;
            }
        }

        private static async Task<string> ReadBoundedAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            if (response.Content.Headers.ContentLength.HasValue
                && response.Content.Headers.ContentLength.Value > MaximumResponseBytes)
            {
                throw new YogabaseBuildException(
                    (int)response.StatusCode, "Yogabase returned an oversized response.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            using (var buffer = new MemoryStream())
            {
                var chunk = new byte[64 * 1024];
                while (true)
                {
                    var read = await stream.ReadAsync(chunk, 0, chunk.Length, cancellationToken)
                        .ConfigureAwait(false);
                    if (read == 0)
                    {
                        break;
                    }
                    if (buffer.Length + read > MaximumResponseBytes)
                    {
                        throw new YogabaseBuildException(
                            (int)response.StatusCode, "Yogabase returned an oversized response.");
                    }

                    buffer.Write(chunk, 0, read);
                }

                return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
            }
        }

        // An untrusted error body is shown to the developer, so keep it short and single-line.
        private static string Summarize(string body)
        {
            var flattened = body.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return flattened.Length <= 400 ? flattened : flattened.Substring(0, 400) + "…";
        }

        private static int UnixMode(string relative, string executable)
        {
            const int Regular = 0x8000; // S_IFREG
            var name = relative;
            var slash = relative.LastIndexOf('/');
            if (slash >= 0)
            {
                name = relative.Substring(slash + 1);
            }

            var isExecutable = string.Equals(relative, executable, StringComparison.Ordinal)
                || string.Equals(name, executable, StringComparison.Ordinal)
                || name.EndsWith(".x86_64", StringComparison.Ordinal)
                || name.EndsWith(".aarch64", StringComparison.Ordinal)
                || name.EndsWith(".so", StringComparison.Ordinal)
                || name.IndexOf(".so.", StringComparison.Ordinal) >= 0
                || name.IndexOf('.') < 0;
            return Regular | (isExecutable ? 0x1ED : 0x1A4); // 0755 : 0644
        }

        /// <summary>
        /// Reads the ELF machine type of one file, so the publish window can refuse a bundle whose
        /// binary does not match the architecture the settings declare.
        /// </summary>
        /// <returns>
        /// <c>62</c> for x86-64, <c>183</c> for AArch64, <c>0</c> when the file is not an ELF64
        /// little-endian object at all.
        /// </returns>
        public static int ReadElfMachine(string path)
        {
            var header = new byte[20];
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                var read = 0;
                while (read < header.Length)
                {
                    var got = stream.Read(header, read, header.Length - read);
                    if (got == 0)
                    {
                        return 0;
                    }

                    read += got;
                }
            }

            var isElf64LittleEndian = header[0] == 0x7F
                && header[1] == (byte)'E'
                && header[2] == (byte)'L'
                && header[3] == (byte)'F'
                && header[4] == 2
                && header[5] == 1;
            return isElf64LittleEndian ? header[18] | (header[19] << 8) : 0;
        }

        /// <summary>Human-readable name for an ELF <c>e_machine</c> value.</summary>
        public static string DescribeElfMachine(int machine)
        {
            switch (machine)
            {
                case 62: return "x86-64 (linux-x64)";
                case 183: return "AArch64 (linux-arm64)";
                case 0: return "not an ELF64 little-endian object";
                default: return "ELF e_machine " + machine.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        /// <summary>Every declared port, in the form the multipart <c>ports</c> field carries.</summary>
        public static List<string> DescribePorts(YogabaseSettings settings)
        {
            var values = new List<string>();
            foreach (var declared in settings.ports)
            {
                values.Add(declared.ToFieldValue());
            }

            return values;
        }
    }
}
