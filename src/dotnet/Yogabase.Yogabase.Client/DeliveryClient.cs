using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Yogabase.Yogabase;

/// <summary>Options for the separately-hosted, workload-only delivery API.</summary>
public sealed class YogabaseDeliveryClientOptions
{
    public HttpMessageHandler? TransportHandler { get; set; }
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool AllowInsecureLoopback { get; set; }
}

public sealed class YogabaseDeliveryPollingOptions
{
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaximumDelay { get; set; } = TimeSpan.FromSeconds(30);
}

public sealed class YogabaseDeliveryPage<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public string? NextCursor { get; set; }
}

public sealed class YogabaseDeliveryVersioned<T>
{
    public YogabaseDeliveryVersioned(T value, string eTag) { Value = value; ETag = eTag; }
    public T Value { get; }
    public string ETag { get; }
}

public sealed class YogabaseDeliveryPreviewRequest
{
    public Guid PreviewId { get; set; }
    public string PreviewDigest { get; set; } = string.Empty;
}

public sealed class YogabaseDeliveryPreview
{
    public Guid PreviewId { get; set; }
    public string PreviewDigest { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public long ExpectedRevision { get; set; }
    public bool ApprovalRequired { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

public sealed class YogabaseDeliveryOperation
{
    public Guid OperationId { get; set; }
    public string State { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string? ResourceType { get; set; }
    public Guid? ResourceId { get; set; }
    public long Revision { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class YogabaseDeliveryAcceptedOperation
{
    public YogabaseDeliveryOperation Operation { get; set; } = new();
    public Uri Location { get; set; } = null!;
    public TimeSpan RetryAfter { get; set; }
}

public sealed class YogabaseDeliveryOperationEvent
{
    /// <summary>Opaque SSE cursor to send as Last-Event-ID after reconnecting.</summary>
    [JsonIgnore]
    public string? ResumeToken { get; set; }
    public Guid OperationId { get; set; }
    public string State { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
}

public sealed class YogabaseDeliveryTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
    public string State { get; set; } = string.Empty;
    public Guid TargetId { get; set; }
    public string TemplateDigest { get; set; } = string.Empty;
    public long Revision { get; set; }
}

public sealed class YogabaseDeliveryEnvironment
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Lifecycle { get; set; } = string.Empty;
    public bool IsProduction { get; set; }
    public Guid TemplateId { get; set; }
    public int TemplateVersion { get; set; }
    public Guid? CurrentReleaseId { get; set; }
    public long RuntimeRevision { get; set; }
    public long Revision { get; set; }
}

public sealed class YogabaseDeliveryArtifact
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ArtifactVersion { get; set; }
    public string BundleDigest { get; set; } = string.Empty;
    public string ManifestDigest { get; set; } = string.Empty;
    public string SbomDigest { get; set; } = string.Empty;
    public string ScanResultDigest { get; set; } = string.Empty;
    public string SignerId { get; set; } = string.Empty;
    public string KeyId { get; set; } = string.Empty;
    public long Revision { get; set; }
}

public sealed class YogabaseDeliveryRelease
{
    public Guid Id { get; set; }
    public string ReleaseType { get; set; } = string.Empty;
    public Guid? SourceReleaseId { get; set; }
    public long DesiredRuntimeRevision { get; set; }
    public string ArtifactSetDigest { get; set; } = string.Empty;
    public string ConfigurationDigest { get; set; } = string.Empty;
    public string MigrationEvidenceDigest { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long Revision { get; set; }
}

public sealed class YogabaseDeliveryEnvironmentPreviewRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid TemplateId { get; set; }
    public int TemplateVersion { get; set; }
}

public sealed class YogabaseDeliveryArtifactPreviewRequest
{
    public string SourceLocator { get; set; } = string.Empty;
    public string BundleDigest { get; set; } = string.Empty;
    public string ManifestDigest { get; set; } = string.Empty;
    public string SbomDigest { get; set; } = string.Empty;
    public string ScanEvidenceDigest { get; set; } = string.Empty;
    public int ArtifactVersion { get; set; }
    public long SourceSizeBytes { get; set; }
    public string SourceMediaType { get; set; } = string.Empty;
    public string ManifestUri { get; set; } = string.Empty;
    public string SbomUri { get; set; } = string.Empty;
    public string SbomFormat { get; set; } = string.Empty;
    public string ScanUri { get; set; } = string.Empty;
    public string Scanner { get; set; } = string.Empty;
    public DateTimeOffset ScannedAt { get; set; }
    public int CriticalFindings { get; set; }
    public int HighFindings { get; set; }
}

public sealed class YogabaseDeliveryReleaseConfiguration
{
    public bool StatelessFunctions { get; set; }
    public bool Scheduler { get; set; }
    public bool Jobs { get; set; }
    public int? MaxConcurrentExecutions { get; set; }
    public int? MaxCatchUp { get; set; }
    public IReadOnlyList<string>? AllowedTimeZones { get; set; }
}

public sealed class YogabaseDeliveryReleasePreviewRequest
{
    public IReadOnlyList<Guid> ArtifactAdmissionIds { get; set; } = Array.Empty<Guid>();
    public YogabaseDeliveryReleaseConfiguration Configuration { get; set; } = new();
    public string MigrationEvidenceDigest { get; set; } = string.Empty;
}

public sealed class YogabaseDeliveryPromotionPreviewRequest
{
    public Guid SourceEnvironmentId { get; set; }
    public Guid SourceReleaseId { get; set; }
    public string MigrationEvidenceDigest { get; set; } = string.Empty;
}

public sealed class YogabaseDeliveryRollbackPreviewRequest
{
    public Guid ReleaseId { get; set; }
    public string MigrationEvidenceDigest { get; set; } = string.Empty;
}

/// <summary>
/// Client for the delivery-only Control Plane host. It is intentionally independent from
/// <see cref="YogabaseClient"/> and accepts only a workload DeliveryBearer token provider.
/// </summary>
public sealed class YogabaseDeliveryClient : IDisposable
{
    private const string DeliveryPrefix = "api/v1/delivery/projects";
    private readonly Uri deliveryOrigin;
    private readonly Func<CancellationToken, Task<string?>> bearerToken;
    private readonly HttpClient client;
    private bool disposed;

    public YogabaseDeliveryClient(
        Uri deliveryOrigin,
        Func<CancellationToken, Task<string?>> bearerToken,
        YogabaseDeliveryClientOptions? options = null)
    {
        if (deliveryOrigin is null) throw new ArgumentNullException(nameof(deliveryOrigin));
        if (bearerToken is null) throw new ArgumentNullException(nameof(bearerToken));
        options ??= new YogabaseDeliveryClientOptions();
        YogabaseHttpTransport.ValidateGateway(deliveryOrigin, options.AllowInsecureLoopback);
        this.deliveryOrigin = deliveryOrigin;
        this.bearerToken = bearerToken;
        client = YogabaseHttpClientFactory.Create(new YogabaseClientOptions
        {
            TransportHandler = options.TransportHandler,
            RequestTimeout = options.RequestTimeout,
            AllowInsecureLoopback = options.AllowInsecureLoopback,
        });
    }

    public Task<YogabaseDeliveryPage<YogabaseDeliveryTemplate>> ListTemplatesAsync(Guid projectId, int limit = 50, string? cursor = null, CancellationToken cancellationToken = default) =>
        GetPageAsync<YogabaseDeliveryTemplate>(Path(projectId, "templates") + Query(limit, cursor), cancellationToken);
    public Task<YogabaseDeliveryVersioned<YogabaseDeliveryTemplate>> GetTemplateAsync(Guid projectId, Guid templateId, CancellationToken cancellationToken = default) =>
        GetVersionedAsync<YogabaseDeliveryTemplate>(Path(projectId, "templates", templateId), cancellationToken);
    public Task<YogabaseDeliveryPage<YogabaseDeliveryEnvironment>> ListEnvironmentsAsync(Guid projectId, int limit = 50, string? cursor = null, CancellationToken cancellationToken = default) =>
        GetPageAsync<YogabaseDeliveryEnvironment>(Path(projectId, "environments") + Query(limit, cursor), cancellationToken);
    public Task<YogabaseDeliveryVersioned<YogabaseDeliveryEnvironment>> GetEnvironmentAsync(Guid projectId, Guid environmentId, CancellationToken cancellationToken = default) =>
        GetVersionedAsync<YogabaseDeliveryEnvironment>(Path(projectId, "environments", environmentId), cancellationToken);
    public Task<YogabaseDeliveryPage<YogabaseDeliveryArtifact>> ListArtifactsAsync(Guid projectId, Guid environmentId, int limit = 50, string? cursor = null, CancellationToken cancellationToken = default) =>
        GetPageAsync<YogabaseDeliveryArtifact>(Path(projectId, "environments", environmentId, "artifacts") + Query(limit, cursor), cancellationToken);
    public Task<YogabaseDeliveryVersioned<YogabaseDeliveryArtifact>> GetArtifactAsync(Guid projectId, Guid environmentId, Guid admissionId, CancellationToken cancellationToken = default) =>
        GetVersionedAsync<YogabaseDeliveryArtifact>(Path(projectId, "environments", environmentId, "artifacts", admissionId), cancellationToken);
    public Task<YogabaseDeliveryPage<YogabaseDeliveryRelease>> ListReleasesAsync(Guid projectId, Guid environmentId, int limit = 50, string? cursor = null, CancellationToken cancellationToken = default) =>
        GetPageAsync<YogabaseDeliveryRelease>(Path(projectId, "environments", environmentId, "releases") + Query(limit, cursor), cancellationToken);
    public Task<YogabaseDeliveryVersioned<YogabaseDeliveryRelease>> GetReleaseAsync(Guid projectId, Guid environmentId, Guid releaseId, CancellationToken cancellationToken = default) =>
        GetVersionedAsync<YogabaseDeliveryRelease>(Path(projectId, "environments", environmentId, "releases", releaseId), cancellationToken);
    public Task<YogabaseDeliveryPage<YogabaseDeliveryOperation>> ListOperationsAsync(Guid projectId, int limit = 50, string? cursor = null, CancellationToken cancellationToken = default) =>
        GetPageAsync<YogabaseDeliveryOperation>(Path(projectId, "operations") + Query(limit, cursor), cancellationToken);
    public Task<YogabaseDeliveryVersioned<YogabaseDeliveryOperation>> GetOperationAsync(Guid projectId, Guid operationId, CancellationToken cancellationToken = default) =>
        GetVersionedAsync<YogabaseDeliveryOperation>(Path(projectId, "operations", operationId), cancellationToken);

    public Task<YogabaseDeliveryPreview> PreviewEnvironmentAsync(Guid projectId, YogabaseDeliveryEnvironmentPreviewRequest request, string eTag, CancellationToken cancellationToken = default) =>
        PreviewAsync(projectId, "environments/previews", request, eTag, cancellationToken);
    public Task<YogabaseDeliveryPreview> PreviewArtifactAsync(Guid projectId, Guid environmentId, YogabaseDeliveryArtifactPreviewRequest request, string eTag, CancellationToken cancellationToken = default) =>
        PreviewAsync(projectId, $"environments/{Id(environmentId)}/artifacts/previews", request, eTag, cancellationToken);
    public Task<YogabaseDeliveryPreview> PreviewReleaseAsync(Guid projectId, Guid environmentId, YogabaseDeliveryReleasePreviewRequest request, string eTag, CancellationToken cancellationToken = default) =>
        PreviewAsync(projectId, $"environments/{Id(environmentId)}/releases/previews", request, eTag, cancellationToken);
    public Task<YogabaseDeliveryPreview> PreviewPromotionAsync(Guid projectId, Guid environmentId, YogabaseDeliveryPromotionPreviewRequest request, string eTag, CancellationToken cancellationToken = default) =>
        PreviewAsync(projectId, $"environments/{Id(environmentId)}/promotions/previews", request, eTag, cancellationToken);
    public Task<YogabaseDeliveryPreview> PreviewRollbackAsync(Guid projectId, Guid environmentId, YogabaseDeliveryRollbackPreviewRequest request, string eTag, CancellationToken cancellationToken = default) =>
        PreviewAsync(projectId, $"environments/{Id(environmentId)}/rollbacks/previews", request, eTag, cancellationToken);
    public Task<YogabaseDeliveryPreview> PreviewDecommissionAsync(Guid projectId, Guid environmentId, string eTag, CancellationToken cancellationToken = default) =>
        PreviewAsync<object?>(projectId, $"environments/{Id(environmentId)}/decommissions/previews", null, eTag, cancellationToken);
    public Task<YogabaseDeliveryPreview> PreviewAbandonAsync(Guid projectId, Guid operationId, string eTag, CancellationToken cancellationToken = default) =>
        PreviewAsync<object?>(projectId, $"operations/{Id(operationId)}/abandon/previews", null, eTag, cancellationToken);

    public Task<YogabaseDeliveryAcceptedOperation> SubmitEnvironmentAsync(Guid projectId, YogabaseDeliveryPreviewRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        SubmitAsync(projectId, "environments", request, idempotencyKey, cancellationToken);
    public Task<YogabaseDeliveryAcceptedOperation> SubmitArtifactAsync(Guid projectId, Guid environmentId, YogabaseDeliveryPreviewRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        SubmitAsync(projectId, $"environments/{Id(environmentId)}/artifacts", request, idempotencyKey, cancellationToken);
    public Task<YogabaseDeliveryAcceptedOperation> SubmitReleaseAsync(Guid projectId, Guid environmentId, YogabaseDeliveryPreviewRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        SubmitAsync(projectId, $"environments/{Id(environmentId)}/releases", request, idempotencyKey, cancellationToken);
    public Task<YogabaseDeliveryAcceptedOperation> SubmitPromotionAsync(Guid projectId, Guid environmentId, YogabaseDeliveryPreviewRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        SubmitAsync(projectId, $"environments/{Id(environmentId)}/promotions", request, idempotencyKey, cancellationToken);
    public Task<YogabaseDeliveryAcceptedOperation> SubmitRollbackAsync(Guid projectId, Guid environmentId, YogabaseDeliveryPreviewRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        SubmitAsync(projectId, $"environments/{Id(environmentId)}/rollbacks", request, idempotencyKey, cancellationToken);
    public Task<YogabaseDeliveryAcceptedOperation> SubmitDecommissionAsync(Guid projectId, Guid environmentId, YogabaseDeliveryPreviewRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        SubmitAsync(projectId, $"environments/{Id(environmentId)}/decommissions", request, idempotencyKey, cancellationToken);
    public Task<YogabaseDeliveryAcceptedOperation> SubmitAbandonAsync(Guid projectId, Guid operationId, YogabaseDeliveryPreviewRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        SubmitAsync(projectId, $"operations/{Id(operationId)}/abandon", request, idempotencyKey, cancellationToken);

    public async Task<YogabaseDeliveryOperation> WaitForOperationAsync(Guid projectId, Guid operationId, YogabaseDeliveryPollingOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new YogabaseDeliveryPollingOptions();
        if (options.Timeout <= TimeSpan.Zero || options.Timeout > TimeSpan.FromMinutes(30) || options.InitialDelay <= TimeSpan.Zero || options.MaximumDelay < options.InitialDelay)
            throw new ArgumentOutOfRangeException(nameof(options));
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
                    var operation = (await GetOperationAsync(projectId, operationId, pollingToken).ConfigureAwait(false)).Value;
                    if (IsTerminal(operation.State)) return operation;
                }
                catch (YogabaseApiException exception) when (IsTransientDeliveryFailure(exception.StatusCode))
                {
                    // GET polling is safe to retry. Mutating delivery calls never receive implicit retries.
                }
                if (DateTimeOffset.UtcNow >= deadline) throw DeliveryPollingTimeout();
                var remaining = deadline - DateTimeOffset.UtcNow;
                await Task.Delay(delay <= remaining ? delay : remaining, pollingToken).ConfigureAwait(false);
                delay = TimeSpan.FromMilliseconds(Math.Min(options.MaximumDelay.TotalMilliseconds, delay.TotalMilliseconds * 2));
            }
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw DeliveryPollingTimeout();
        }
    }

    public async IAsyncEnumerable<YogabaseDeliveryOperationEvent> StreamOperationEventsAsync(Guid projectId, Guid? environmentId = null, Guid? operationId = null, string? lastEventId = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (lastEventId is not null && (lastEventId.Length is < 1 or > 4096)) throw new ArgumentException("Last event ID is outside the contract bounds.", nameof(lastEventId));
        var query = new List<string>();
        if (environmentId.HasValue) query.Add("environmentId=" + Id(environmentId.Value));
        if (operationId.HasValue) query.Add("operationId=" + Id(operationId.Value));
        using var response = await SendAsync(HttpMethod.Get, Path(projectId, "operations", "events") + (query.Count == 0 ? string.Empty : "?" + string.Join("&", query)), null, request =>
        {
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
            if (lastEventId is not null) request.Headers.TryAddWithoutValidation("Last-Event-ID", lastEventId);
        }, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(response.Content.Headers.ContentType?.MediaType, "text/event-stream", StringComparison.OrdinalIgnoreCase))
            throw new YogabaseApiException(502, "delivery_events_invalid", "Yogabase returned an invalid delivery event stream.");
        using var stream = await YogabaseHttpContent.ReadAsStreamAsync(response.Content, cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.UTF8, true, 16 * 1024, leaveOpen: false);
        var data = new StringBuilder();
        string? resumeToken = null;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await ReadLineAsync(reader, cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                break;
            }
            if (string.IsNullOrEmpty(line))
            {
                if (data.Length == 0)
                {
                    resumeToken = null;
                    continue;
                }
                YogabaseDeliveryOperationEvent? item;
                try { item = JsonSerializer.Deserialize<YogabaseDeliveryOperationEvent>(data.ToString(), YogabaseJson.Options); }
                catch (JsonException) { throw new YogabaseApiException(502, "delivery_event_invalid", "Yogabase returned an invalid delivery event."); }
                data.Clear();
                if (item is null || item.OperationId == Guid.Empty || string.IsNullOrWhiteSpace(item.State) || string.IsNullOrWhiteSpace(item.Detail) || item.OccurredAt == default)
                    throw new YogabaseApiException(502, "delivery_event_invalid", "Yogabase returned an invalid delivery event.");
                item.ResumeToken = resumeToken;
                resumeToken = null;
                yield return item;
                continue;
            }
            if (line.StartsWith("id:", StringComparison.Ordinal))
            {
                var start = line.Length > 3 && line[3] == ' ' ? 4 : 3;
                resumeToken = line.AsSpan(start).ToString();
                if (resumeToken.Length is < 1 or > 4096)
                    throw new YogabaseApiException(502, "delivery_event_invalid", "Yogabase returned an invalid delivery event cursor.");
                continue;
            }
            if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                if (data.Length > 0) data.Append('\n');
                data.Append(line.Length > 5 && line[5] == ' ' ? line.Substring(6) : line.Substring(5));
            }
        }
    }

    public void Dispose() { if (!disposed) { disposed = true; client.Dispose(); } }

    private async Task<YogabaseDeliveryPage<T>> GetPageAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Get, path, null, null, cancellationToken).ConfigureAwait(false);
        return await ReadJsonAsync<YogabaseDeliveryPage<T>>(response, cancellationToken).ConfigureAwait(false);
    }

    private async Task<YogabaseDeliveryVersioned<T>> GetVersionedAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Get, path, null, null, cancellationToken).ConfigureAwait(false);
        var eTag = RequiredHeader(response, "ETag");
        ValidateETag(eTag, nameof(eTag));
        return new YogabaseDeliveryVersioned<T>(await ReadJsonAsync<T>(response, cancellationToken).ConfigureAwait(false), eTag);
    }

    private async Task<YogabaseDeliveryPreview> PreviewAsync<T>(Guid projectId, string suffix, T request, string eTag, CancellationToken cancellationToken)
    {
        ValidateETag(eTag, nameof(eTag));
        using var response = await SendAsync(HttpMethod.Post, Path(projectId, suffix), request, message => message.Headers.TryAddWithoutValidation("If-Match", eTag), cancellationToken).ConfigureAwait(false);
        return await ReadJsonAsync<YogabaseDeliveryPreview>(response, cancellationToken).ConfigureAwait(false);
    }

    private async Task<YogabaseDeliveryAcceptedOperation> SubmitAsync(Guid projectId, string suffix, YogabaseDeliveryPreviewRequest request, string idempotencyKey, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (request.PreviewId == Guid.Empty) throw new ArgumentException("A preview ID is required.", nameof(request));
        ValidateDigest(request.PreviewDigest, nameof(request.PreviewDigest));
        ValidateIdempotencyKey(idempotencyKey);
        using var response = await SendAsync(HttpMethod.Post, Path(projectId, suffix), request, message => message.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey), cancellationToken).ConfigureAwait(false);
        var location = RequiredHeader(response, "Location");
        var retryAfter = ParsePositiveIntHeader(response, "Retry-After");
        var operation = await ReadJsonAsync<YogabaseDeliveryOperation>(response, cancellationToken).ConfigureAwait(false);
        var operationLocation = new Uri(deliveryOrigin, location);
        if (!SameOrigin(deliveryOrigin, operationLocation))
            throw new YogabaseApiException(502, "delivery_response_invalid", "Yogabase returned an operation location on another origin.");
        return new YogabaseDeliveryAcceptedOperation
        {
            Operation = operation,
            Location = operationLocation,
            RetryAfter = TimeSpan.FromSeconds(retryAfter),
        };
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body, Action<HttpRequestMessage>? configure, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        var token = await bearerToken(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(token)) throw new YogabaseApiException(401, "delivery_authentication_required", "A DeliveryBearer token is required.");
        using var request = new HttpRequestMessage(method, new Uri(deliveryOrigin, path));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = new StringContent(JsonSerializer.Serialize(body, YogabaseJson.Options), Encoding.UTF8, "application/json");
        configure?.Invoke(request);
        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) await YogabaseHttpTransport.ThrowApiExceptionAsync(response, cancellationToken).ConfigureAwait(false);
        return response;
    }

    private static Task<T> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken) =>
        YogabaseHttpTransport.ReadJsonAsync<T>(response, cancellationToken);

    private static string Path(Guid projectId, params object[] parts)
    {
        if (projectId == Guid.Empty) throw new ArgumentException("A project ID is required.", nameof(projectId));
        return DeliveryPrefix + "/" + Id(projectId) + "/" + string.Join("/", parts.Select(part => part is Guid id ? Id(id) : (string)part));
    }
    private static string Id(Guid value) { if (value == Guid.Empty) throw new ArgumentException("A non-empty identifier is required."); return value.ToString("D"); }
    private static string Query(int limit, string? cursor)
    {
        if (limit is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(limit));
        if (cursor is null) return "?limit=" + limit.ToString(CultureInfo.InvariantCulture);
        if (cursor.Length is < 1 or > 4096) throw new ArgumentException("Cursor is outside the contract bounds.", nameof(cursor));
        return "?limit=" + limit.ToString(CultureInfo.InvariantCulture) + "&cursor=" + Uri.EscapeDataString(cursor);
    }
    private static string RequiredHeader(HttpResponseMessage response, string name) => response.Headers.TryGetValues(name, out var values) && values.SingleOrDefault() is { Length: > 0 } value ? value : throw new YogabaseApiException(502, "delivery_response_invalid", "Yogabase omitted a required delivery response header.");
    private static int ParsePositiveIntHeader(HttpResponseMessage response, string name) => int.TryParse(RequiredHeader(response, name), NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value > 0 ? value : throw new YogabaseApiException(502, "delivery_response_invalid", "Yogabase returned an invalid delivery response header.");
    private static void ValidateETag(string value, string parameterName) { if (string.IsNullOrWhiteSpace(value) || value.Length > 32 || !value.StartsWith("W/\"", StringComparison.Ordinal) || !value.EndsWith("\"", StringComparison.Ordinal) || !long.TryParse(value.AsSpan(3, value.Length - 4), NumberStyles.None, CultureInfo.InvariantCulture, out _)) throw new ArgumentException("A canonical weak ETag is required.", parameterName); }
    private static void ValidateDigest(string value, string parameterName) { if (value is null || value.Length != 71 || !value.StartsWith("sha256:", StringComparison.Ordinal) || value.Substring(7).Any(character => !(character is >= '0' and <= '9' or >= 'a' and <= 'f'))) throw new ArgumentException("A canonical lowercase sha256: digest is required.", parameterName); }
    private static void ValidateIdempotencyKey(string value) { if (string.IsNullOrWhiteSpace(value) || value.Length > 128) throw new ArgumentException("An idempotency key within the contract bounds is required.", nameof(value)); }
    private static bool IsTerminal(string state) => state is "Succeeded" or "Failed" or "Abandoned" or "Cancelled" or "Blocked";
    private static TimeoutException DeliveryPollingTimeout() => new("The Yogabase delivery operation did not reach a terminal state before the polling timeout.");
    private static bool IsTransientDeliveryFailure(int statusCode) => statusCode is 408 or 429 or 502 or 503 or 504;
    private static bool SameOrigin(Uri left, Uri right) => string.Equals(left.Scheme, right.Scheme, StringComparison.OrdinalIgnoreCase)
        && string.Equals(left.Host, right.Host, StringComparison.OrdinalIgnoreCase) && left.Port == right.Port;
    private static Task<string?> ReadLineAsync(StreamReader reader, CancellationToken cancellationToken)
    {
#if NET10_0_OR_GREATER
        return reader.ReadLineAsync(cancellationToken).AsTask();
#else
        cancellationToken.ThrowIfCancellationRequested();
        return reader.ReadLineAsync();
#endif
    }

    private void ThrowIfDisposed()
    {
#if NET10_0_OR_GREATER
        ObjectDisposedException.ThrowIf(disposed, this);
#else
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(YogabaseDeliveryClient));
        }
#endif
    }
}
