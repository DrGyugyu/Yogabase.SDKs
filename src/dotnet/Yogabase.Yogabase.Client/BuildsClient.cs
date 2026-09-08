using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Yogabase.Yogabase;

/// <summary>Options for the developer-credentialled Control Plane build surface.</summary>
public sealed class YogabaseBuildsClientOptions
{
    /// <summary>Optional direct transport handler for tests or platform integration.</summary>
    public HttpMessageHandler? TransportHandler { get; set; }

    /// <summary>
    /// Per-request timeout. A build bundle is tens or hundreds of megabytes, so this defaults far
    /// above the application client's thirty seconds.
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Allows an <c>http://</c> loopback Control Plane, for a local installation.</summary>
    public bool AllowInsecureLoopback { get; set; }
}

/// <summary>Polling shape of <see cref="YogabaseBuildsClient.WaitForCheckAsync"/>.</summary>
public sealed class YogabaseBuildPollingOptions
{
    /// <summary>How long to wait for a terminal Build check before giving up.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>First poll delay. The contract's documented cadence is three seconds.</summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>Upper bound the doubling backoff settles at.</summary>
    public TimeSpan MaximumDelay { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>One declared network port of a build.</summary>
public sealed class YogabaseBuildPort
{
    /// <summary>Port name the client later looks up in an allocation, for example <c>game</c>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Port the build listens on.</summary>
    public int Port { get; set; }

    /// <summary>Transport protocol, <c>udp</c> or <c>tcp</c>.</summary>
    public string Protocol { get; set; } = "udp";

    /// <summary>The <c>name=port/protocol</c> form the multipart <c>ports</c> field carries.</summary>
    public string ToFieldValue()
    {
        if (string.IsNullOrWhiteSpace(Name)
            || Name.Length > 63
            || !string.Equals(Name, Name.Trim(), StringComparison.Ordinal)
            || Name.Contains('=')
            || Name.Contains('/'))
        {
            throw new InvalidOperationException("A bounded port name without '=' or '/' is required.");
        }
        if (Port is < 1 or > 65535)
        {
            throw new InvalidOperationException("A port must be between 1 and 65535.");
        }
        if (Protocol is not ("udp" or "tcp"))
        {
            throw new InvalidOperationException("A port protocol must be udp or tcp.");
        }

        return Name + "=" + Port.ToString(CultureInfo.InvariantCulture) + "/" + Protocol;
    }
}

/// <summary>Everything the multipart build upload carries besides the bundle itself.</summary>
/// <remarks>
/// Every field is a hint the Control Plane needs only where the archive is genuinely ambiguous.
/// The bundle is the one required part; Yogabase derives the layout, the inventory, and the scan
/// from the uploaded bytes, and this client never computes or sends a manifest, an SBOM, or a scan
/// result of its own.
/// </remarks>
public sealed class YogabaseBuildUploadRequest
{
    /// <summary>
    /// Path of the executable inside the zip, for example <c>Server.x86_64</c>. Optional; Yogabase
    /// derives it when the bundle carries exactly one candidate.
    /// </summary>
    public string? Executable { get; set; }

    /// <summary>Ports the build listens on. Sent as one repeated <c>ports</c> field each.</summary>
    public IReadOnlyList<YogabaseBuildPort> Ports { get; set; } = Array.Empty<YogabaseBuildPort>();

    /// <summary>Optional build name. Builds are versioned per name per project.</summary>
    public string? Name { get; set; }

    /// <summary>Optional build version label.</summary>
    public string? Version { get; set; }

    /// <summary>File name attached to the bundle part.</summary>
    public string BundleFileName { get; set; } = "build.zip";
}

/// <summary>One vulnerability the member's scan matched, in the six parts a person can act on.</summary>
public sealed class YogabaseBuildFinding
{
    /// <summary>Advisory identifier, for example a CVE or GHSA id.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Catalogued package the advisory applies to.</summary>
    public string Package { get; set; } = string.Empty;

    /// <summary>Version found in the bundle.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>First fixed version, or empty when the advisory names none.</summary>
    public string FixedIn { get; set; } = string.Empty;

    /// <summary>Advisory severity as the matcher classified it.</summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>Archive entry the catalogued component came from.</summary>
    public string Entry { get; set; } = string.Empty;
}

/// <summary>One refusal, with the field that failed named separately from the code.</summary>
public sealed class YogabaseBuildRefusal
{
    /// <summary>Stable machine code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>The field path the refusal names, or empty when it named none.</summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>A bounded cause token, never a message or a value; empty when none.</summary>
    public string Cause { get; set; } = string.Empty;
}

/// <summary>
/// What the checks found, in the one shape every Yogabase surface reads: Yogabase's own layout,
/// inventory, and vulnerability pass over the uploaded bundle.
/// </summary>
public sealed class YogabaseBuildCheck
{
    /// <summary>The inventory and scan came with the build, produced by whoever built it.</summary>
    public const string CustomerSupplied = "customer-supplied";

    /// <summary>The inventory and scan were derived from the bundle itself, with no producer input.</summary>
    public const string YogabaseDerived = "yogabase-derived";

    /// <summary>
    /// Whether the inventory and scan came with the build or were derived from it:
    /// <see cref="CustomerSupplied"/> or <see cref="YogabaseDerived"/>.
    /// </summary>
    public string EvidenceOrigin { get; set; } = string.Empty;

    /// <summary>Digest of the inventory the check verified, or empty before the member reported.</summary>
    public string InventoryDigest { get; set; } = string.Empty;

    /// <summary>Digest of the scan the check verified, or empty before the member reported.</summary>
    public string ScanDigest { get; set; } = string.Empty;

    /// <summary>Digest of the vulnerability snapshot the scan was matched against, when reported.</summary>
    public string DatabaseDigest { get; set; } = string.Empty;

    /// <summary>
    /// Whether the check found no Critical/High finding among the catalogued components in the
    /// pinned snapshot on its recorded date. False until the checks report: an absent result is
    /// never reported as a clean one, and a pass is never a claim that the build is secure.
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>When this check was observed.</summary>
    public DateTimeOffset ObservedAt { get; set; }

    /// <summary>Vulnerability findings, empty when none were catalogued.</summary>
    public IReadOnlyList<YogabaseBuildFinding> Findings { get; set; } = Array.Empty<YogabaseBuildFinding>();

    /// <summary>Refusals, empty when the bundle was accepted.</summary>
    public IReadOnlyList<YogabaseBuildRefusal> Refusals { get; set; } = Array.Empty<YogabaseBuildRefusal>();
}

/// <summary>One uploaded build and the state of its check.</summary>
public sealed class YogabaseBuild
{
    /// <summary>The build's identifier, a UUID in its canonical <c>D</c> form.</summary>
    public string BuildId { get; set; } = string.Empty;

    /// <summary>The archive's exact SHA-256, <c>sha256:</c> prefixed.</summary>
    public string Digest { get; set; } = string.Empty;

    /// <summary>
    /// <c>checking</c>, <c>admitted</c>, or <c>refused</c>, verbatim. An unrecognized state is
    /// carried rather than rejected, and simply is not terminal.
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>The build's own identity and version, as <c>name:version</c>.</summary>
    public string Identity { get; set; } = string.Empty;

    /// <summary>
    /// Which upload of this identity this is, starting at 1. A corrected re-upload takes the next
    /// attempt and supersedes the refused one.
    /// </summary>
    public int Attempt { get; set; }

    /// <summary>The artifact kind Yogabase classified the bundle as, verbatim.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>The check. Always present; before the member reports it says nothing has been observed.</summary>
    public YogabaseBuildCheck Check { get; set; } = new YogabaseBuildCheck();

    /// <summary>The state a Build check stops at.</summary>
    public const string StateChecking = "checking";
    /// <inheritdoc cref="StateChecking"/>
    public const string StateAdmitted = "admitted";
    /// <inheritdoc cref="StateChecking"/>
    public const string StateRefused = "refused";

    /// <summary>Whether the check has finished, either way.</summary>
    public bool IsTerminal => State is StateAdmitted or StateRefused;

    /// <summary>Whether the build may be released.</summary>
    public bool IsAdmitted => State == StateAdmitted;
}

/// <summary>
/// Client for the Control Plane's build-ingest surface. It is deliberately independent from
/// <see cref="YogabaseClient"/>: it addresses the Control Plane rather than the application
/// gateway, and it authenticates with a project-scoped developer token issued from the console's
/// Settings page — never with a player credential, and never with a cloud or Kubernetes credential.
/// </summary>
/// <remarks>
/// A developer token is a secret. Hold it in the host's credential store (the Unity package keeps
/// it in <c>EditorPrefs</c>, never in a committed asset), never in source control, and never in a
/// shipped client build.
/// </remarks>
public sealed class YogabaseBuildsClient : IDisposable
{
    /// <summary>The prefix every developer token carries, so one is recognisable without parsing.</summary>
    public const string DeveloperTokenPrefix = "ybt_";

    /// <summary>Longest developer token the Control Plane accepts.</summary>
    public const int MaximumDeveloperTokenLength = 128;

    private const int MaximumZipEntryPathLength = 1024;
    private static readonly TimeSpan MaximumUploadTimeout = TimeSpan.FromHours(2);
    private readonly Uri controlPlane;
    private readonly string developerToken;
    private readonly HttpClient client;
    private bool disposed;

    /// <summary>Creates a client for one Control Plane origin and one developer token.</summary>
    /// <param name="controlPlaneEndpoint">Origin-only absolute URI of the Control Plane API.</param>
    /// <param name="developerToken">Project-scoped developer token from the console's Settings page.</param>
    /// <param name="options">Optional transport, timeout, and loopback options.</param>
    public YogabaseBuildsClient(
        Uri controlPlaneEndpoint,
        string developerToken,
        YogabaseBuildsClientOptions? options = null)
    {
        if (controlPlaneEndpoint is null)
        {
            throw new ArgumentNullException(nameof(controlPlaneEndpoint));
        }

        options ??= new YogabaseBuildsClientOptions();
        YogabaseHttpTransport.ValidateGateway(controlPlaneEndpoint, options.AllowInsecureLoopback);
        ValidateDeveloperToken(developerToken);
        controlPlane = controlPlaneEndpoint;
        this.developerToken = developerToken;
        client = YogabaseHttpClientFactory.Create(
            new YogabaseClientOptions
            {
                TransportHandler = options.TransportHandler,
                RequestTimeout = options.RequestTimeout,
                AllowInsecureLoopback = options.AllowInsecureLoopback,
            },
            MaximumUploadTimeout);
    }

    /// <summary>
    /// Uploads an already-built bundle zip. The caller's stream is read but never disposed, and
    /// never seeks backwards, so a forward-only stream is acceptable.
    /// </summary>
    public async Task<YogabaseBuild> UploadAsync(
        Guid projectId,
        Guid environmentId,
        Stream bundle,
        YogabaseBuildUploadRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (bundle is null)
        {
            throw new ArgumentNullException(nameof(bundle));
        }
        if (!bundle.CanRead)
        {
            throw new ArgumentException("The build bundle stream must be readable.", nameof(bundle));
        }

        request ??= new YogabaseBuildUploadRequest();
        var fields = BuildFields(request);
        using var form = new MultipartFormDataContent();

        // Metadata first, bundle last. The Control Plane buffers the whole form before it reads
        // any part, so this is not a contract requirement; it is the order that stays cheapest if
        // that ever changes, and it costs nothing.
        foreach (var field in fields)
        {
            form.Add(new StringContent(field.Value), field.Key);
        }

        var bundleContent = new StreamContent(new NonDisposingReadStream(bundle));
        bundleContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        form.Add(bundleContent, YogabaseRoutes.BuildFieldBundle, request.BundleFileName);

        using var response = await SendAsync(
            HttpMethod.Post,
            YogabaseRoutes.Builds(projectId, environmentId),
            message => message.Content = form,
            cancellationToken).ConfigureAwait(false);
        return Validate(await YogabaseHttpTransport.ReadJsonAsync<YogabaseBuild>(response, cancellationToken)
            .ConfigureAwait(false));
    }

    /// <summary>
    /// Zips a build output directory in memory and uploads it. Entry names are the paths relative
    /// to <paramref name="directoryPath"/> with forward slashes, and the declared executable plus
    /// the usual native player files keep a <c>0755</c> mode so the Linux server can actually run.
    /// </summary>
    public async Task<YogabaseBuild> UploadDirectoryAsync(
        Guid projectId,
        Guid environmentId,
        string directoryPath,
        YogabaseBuildUploadRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        request ??= new YogabaseBuildUploadRequest();
        using var bundle = CreateBundleZip(directoryPath, request.Executable);
        return await UploadAsync(projectId, environmentId, bundle, request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Reads one build and its current check result.</summary>
    public async Task<YogabaseBuild> GetAsync(
        Guid projectId,
        Guid environmentId,
        string buildId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        using var response = await SendAsync(
            HttpMethod.Get,
            YogabaseRoutes.Build(projectId, environmentId, BuildId(buildId)),
            configure: null,
            cancellationToken).ConfigureAwait(false);
        return Validate(await YogabaseHttpTransport.ReadJsonAsync<YogabaseBuild>(response, cancellationToken)
            .ConfigureAwait(false));
    }

    /// <summary>
    /// Runs the checks again for the same bytes. The digest never changes, so this is a re-check
    /// of one build rather than a new one: the case it exists for is a build refused by a
    /// vulnerability database that has since moved on, or by a dependency that was briefly
    /// unavailable. The returned build is usually <c>checking</c> again; poll it with
    /// <see cref="WaitForCheckAsync"/>.
    /// </summary>
    public async Task<YogabaseBuild> RecheckAsync(
        Guid projectId,
        Guid environmentId,
        string buildId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        using var response = await SendAsync(
            HttpMethod.Post,
            YogabaseRoutes.BuildRecheck(projectId, environmentId, BuildId(buildId)),
            configure: null,
            cancellationToken).ConfigureAwait(false);
        return Validate(await YogabaseHttpTransport.ReadJsonAsync<YogabaseBuild>(response, cancellationToken)
            .ConfigureAwait(false));
    }

    /// <summary>
    /// Polls the build until its check reaches <c>admitted</c> or <c>refused</c>. A refused build
    /// is returned, not thrown: its refusals and findings are the answer the caller wants.
    /// </summary>
    /// <param name="projectId">Project the build belongs to.</param>
    /// <param name="environmentId">Environment the build belongs to.</param>
    /// <param name="buildId">Build identifier returned by the upload.</param>
    /// <param name="options">Optional polling cadence and timeout.</param>
    /// <param name="progress">
    /// Optional observer notified after every poll, so a UI can show progress without polling
    /// twice.
    /// </param>
    /// <param name="cancellationToken">Cancels the wait without cancelling the check itself.</param>
    public async Task<YogabaseBuild> WaitForCheckAsync(
        Guid projectId,
        Guid environmentId,
        string buildId,
        YogabaseBuildPollingOptions? options = null,
        IProgress<YogabaseBuild>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        options ??= new YogabaseBuildPollingOptions();
        if (options.Timeout <= TimeSpan.Zero
            || options.Timeout > TimeSpan.FromHours(2)
            || options.InitialDelay <= TimeSpan.Zero
            || options.MaximumDelay < options.InitialDelay)
        {
            throw new ArgumentOutOfRangeException(nameof(options));
        }

        var id = BuildId(buildId);
        using var timeoutSource = new CancellationTokenSource(options.Timeout);
        using var pollingSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
        var pollingToken = pollingSource.Token;
        var deadline = DateTimeOffset.UtcNow + options.Timeout;
        var delay = options.InitialDelay;
        try
        {
            while (true)
            {
                try
                {
                    var build = await GetAsync(projectId, environmentId, id, pollingToken).ConfigureAwait(false);
                    progress?.Report(build);
                    if (build.IsTerminal)
                    {
                        return build;
                    }
                }
                catch (YogabaseApiException exception) when (IsTransient(exception.StatusCode))
                {
                    // Reading a build is safe to retry; the upload that created it is not retried.
                }

                if (DateTimeOffset.UtcNow >= deadline)
                {
                    throw CheckTimeout();
                }

                var remaining = deadline - DateTimeOffset.UtcNow;
                await Task.Delay(delay <= remaining ? delay : remaining, pollingToken).ConfigureAwait(false);
                delay = TimeSpan.FromMilliseconds(Math.Min(
                    options.MaximumDelay.TotalMilliseconds,
                    delay.TotalMilliseconds * 2));
            }
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw CheckTimeout();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        client.Dispose();
    }

    public static MemoryStream CreateBundleZip(string directoryPath, string? executable = null)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException("A build output directory is required.", nameof(directoryPath));
        }

        var root = new DirectoryInfo(directoryPath);
        if (!root.Exists)
        {
            throw new DirectoryNotFoundException($"Build output directory not found: {directoryPath}");
        }

        var rootPath = root.FullName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var files = root.GetFiles("*", SearchOption.AllDirectories)
            .Select(file => file.FullName)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        if (files.Length == 0)
        {
            throw new ArgumentException("The build output directory is empty.", nameof(directoryPath));
        }

        var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in files)
            {
                var relative = file.Substring(rootPath.Length).Replace(Path.DirectorySeparatorChar, '/');
                if (relative.Length > MaximumZipEntryPathLength)
                {
                    throw new ArgumentException($"Bundle entry path is too long: {relative}", nameof(directoryPath));
                }

                var entry = archive.CreateEntry(relative, CompressionLevel.Optimal);
                // ZipArchive throws below 1980; a restored or generated file can carry an earlier
                // timestamp, and failing the whole upload over it would be absurd.
                var written = File.GetLastWriteTime(file);
                entry.LastWriteTime = written.Year < 1980
                    ? new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero)
                    : new DateTimeOffset(written);
                entry.ExternalAttributes = UnixMode(relative, executable) << 16;
                using var source = File.OpenRead(file);
                using var target = entry.Open();
                source.CopyTo(target);
            }
        }

        buffer.Position = 0;
        return buffer;
    }

    // A Unity Linux dedicated server will not start if its player binary loses the executable bit
    // in transit, and a zip that only carries 0644 is the usual way that happens.
    private static int UnixMode(string relative, string? executable)
    {
        const int Regular = 0b1000 << 12;
        var name = relative;
        var slash = relative.LastIndexOf('/');
        if (slash >= 0)
        {
            name = relative.Substring(slash + 1);
        }

        var isExecutable = string.Equals(relative, executable, StringComparison.Ordinal)
            || name.EndsWith(".x86_64", StringComparison.Ordinal)
            || name.EndsWith(".so", StringComparison.Ordinal)
            || name.Contains(".so.", StringComparison.Ordinal)
            || !name.Contains('.');
        return Regular | (isExecutable ? 0x1ED : 0x1A4); // 0755 : 0644
    }

    private static List<KeyValuePair<string, string>> BuildFields(YogabaseBuildUploadRequest request)
    {
        var fields = new List<KeyValuePair<string, string>>();
        if (request.Executable is not null)
        {
            ValidateBoundedField(request.Executable, MaximumZipEntryPathLength, nameof(request.Executable));
            fields.Add(new KeyValuePair<string, string>(YogabaseRoutes.BuildFieldExecutable, request.Executable));
        }
        if (request.Name is not null)
        {
            ValidateBoundedField(request.Name, 253, nameof(request.Name));
            fields.Add(new KeyValuePair<string, string>(YogabaseRoutes.BuildFieldName, request.Name));
        }
        if (request.Version is not null)
        {
            ValidateBoundedField(request.Version, 64, nameof(request.Version));
            fields.Add(new KeyValuePair<string, string>(YogabaseRoutes.BuildFieldVersion, request.Version));
        }

        var ports = request.Ports ?? Array.Empty<YogabaseBuildPort>();
        if (ports.Count > 32)
        {
            throw new ArgumentException("A build declares at most 32 ports.", nameof(request));
        }

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var port in ports)
        {
            if (port is null)
            {
                throw new ArgumentException("A declared port must not be null.", nameof(request));
            }
            if (!names.Add(port.Name))
            {
                throw new ArgumentException($"Duplicate declared port name: {port.Name}", nameof(request));
            }

            fields.Add(new KeyValuePair<string, string>(YogabaseRoutes.BuildFieldPorts, port.ToFieldValue()));
        }

        return fields;
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        Action<HttpRequestMessage>? configure,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, new Uri(controlPlane, path));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", developerToken);
        configure?.Invoke(request);
        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            await YogabaseHttpTransport.ThrowApiExceptionAsync(response, cancellationToken).ConfigureAwait(false);
        }

        return response;
    }

    /// <summary>
    /// A build id is a UUID: the Control Plane's route constrains it to one, so anything else is
    /// answered as an unroutable path rather than as an unknown build. Refusing it here names the
    /// real problem, and the canonical <c>D</c> form is what the route and the response both use.
    /// </summary>
    private static string BuildId(string buildId)
    {
        if (buildId is null)
        {
            throw new ArgumentNullException(nameof(buildId));
        }
        if (!Guid.TryParse(buildId, out var parsed) || parsed == Guid.Empty)
        {
            throw new ArgumentException("A Yogabase build id is a non-empty UUID.", nameof(buildId));
        }

        return parsed.ToString("D", CultureInfo.InvariantCulture);
    }

    private static YogabaseBuild Validate(YogabaseBuild build)
    {
        if (!Guid.TryParse(build.BuildId, out var id)
            || id == Guid.Empty
            || string.IsNullOrWhiteSpace(build.State)
            || build.Check is null)
        {
            throw new YogabaseApiException(502, "build_response_invalid", "Yogabase returned an invalid build.");
        }

        return build;
    }

    private static void ValidateDeveloperToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)
            || token.Length is < 8 or > MaximumDeveloperTokenLength
            || !token.StartsWith(DeveloperTokenPrefix, StringComparison.Ordinal)
            || !string.Equals(token, token.Trim(), StringComparison.Ordinal)
            || token.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException(
                "A Yogabase developer token starts with 'ybt_' and is at most 128 characters.",
                nameof(token));
        }
    }

    private static void ValidateBoundedField(string value, int maximumLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length > maximumLength
            || !string.Equals(value, value.Trim(), StringComparison.Ordinal)
            || value.Any(char.IsControl))
        {
            throw new ArgumentException("A bounded build field is required.", parameterName);
        }
    }

    private static bool IsTransient(int statusCode) => statusCode is 408 or 429 or 502 or 503 or 504;

    private static TimeoutException CheckTimeout() =>
        new("The Yogabase Build check did not reach a terminal state before the polling timeout.");

    private void ThrowIfDisposed()
    {
#if NET10_0_OR_GREATER
        ObjectDisposedException.ThrowIf(disposed, this);
#else
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(YogabaseBuildsClient));
        }
#endif
    }
}
