using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Yogabase.Yogabase;

public sealed class YogabaseClient : IDisposable, IAsyncDisposable
{
    public const string ClientConfigSchema = "yogabase-client-config.v1";
    private const int MaximumClientConfigBytes = 256 * 1024;

    private readonly YogabaseHttpTransport transport;
    private readonly YogabaseAuthClient auth;
    private readonly YogabaseRealtimeClient realtime;
    private bool disposed;

    private YogabaseClient(YogabaseClientConfiguration configuration, YogabaseClientOptions options)
    {
        Configuration = ValidateConfiguration(configuration, options.AllowInsecureLoopback);
        var sessionStore = options.SessionStore ?? new InMemoryYogabaseSessionStore();
        auth = new YogabaseAuthClient(Configuration, options, sessionStore);
        Auth = auth;
        transport = new YogabaseHttpTransport(
            new Uri(Configuration.GatewayEndpoint, UriKind.Absolute),
            options,
            auth.GetValidAccessTokenAsync);
        auth.AttachTransport(transport);
        Storage = new YogabaseStorageClient(Configuration, transport);
        Servers = new YogabaseServersClient(transport);
        Functions = new YogabaseFunctionsClient(Configuration, transport);
        Actors = new YogabaseActorsClient(Configuration, transport);
        Workflows = new YogabaseWorkflowsClient(Configuration, transport);
        realtime = new YogabaseRealtimeClient(Configuration, auth.GetValidAccessTokenAsync);
        Realtime = realtime;
    }

    public YogabaseClientConfiguration Configuration { get; }
    public IYogabaseAuth Auth { get; }
    public IYogabaseRealtime Realtime { get; }
    public IYogabaseStorage Storage { get; }
    public IYogabaseFunctions Functions { get; }
    public IYogabaseActors Actors { get; }
    public IYogabaseWorkflows Workflows { get; }

    /// <summary>
    /// Game-server allocation for the signed-in player: start, list, inspect, and stop instances of
    /// this project's hosted services. The bootstrap document carries no capability identifier for
    /// this surface yet, so the client does not pretend to know whether the installation enables
    /// it; an installation without it answers the gateway's own refusal.
    /// </summary>
    public IYogabaseServers Servers { get; }

    /// <summary>
    /// Creates a client for the Control Plane's build surface. It deliberately takes a separate
    /// origin and credential from the application client: the Control Plane API and a
    /// project-scoped developer token issued from the console's Settings page. A developer tool
    /// (the Unity Editor, a CLI, CI) calls this; a shipped game client never does.
    /// </summary>
    public static YogabaseBuildsClient Builds(
        Uri controlPlaneEndpoint,
        string developerToken,
        YogabaseBuildsClientOptions? options = null) =>
        new(controlPlaneEndpoint, developerToken, options);

    public static async Task<YogabaseClient> CreateAsync(
        Uri gatewayEndpoint,
        string publishableKey,
        YogabaseClientOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new YogabaseClientOptions();
        if (gatewayEndpoint is null)
        {
            throw new ArgumentNullException(nameof(gatewayEndpoint));
        }

        YogabaseHttpTransport.ValidateGateway(gatewayEndpoint, options.AllowInsecureLoopback);
        ValidatePublishableKey(publishableKey);

        using var client = YogabaseHttpClientFactory.Create(options);
        var path = YogabaseRoutes.ClientConfig(publishableKey);
        HttpResponseMessage? response = null;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(gatewayEndpoint, path));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode || !IsTransient(response.StatusCode) || attempt == 1)
            {
                break;
            }

            response.Dispose();
            await Task.Delay(TimeSpan.FromMilliseconds(125), cancellationToken).ConfigureAwait(false);
        }

        if (response is null)
        {
            throw new YogabaseApiException(503, "client_config_unavailable", "Yogabase client configuration is unavailable.");
        }
        if (!response.IsSuccessStatusCode)
        {
            await YogabaseHttpTransport.ThrowApiExceptionAsync(response, cancellationToken).ConfigureAwait(false);
        }

        using (response)
        {
            if (response.Content.Headers.ContentLength is > MaximumClientConfigBytes)
            {
                throw new YogabaseApiException(502, "client_config_invalid", "Yogabase client configuration exceeded its size limit.");
            }

            byte[] payload;
            try
            {
                payload = await YogabaseHttpContent.ReadBoundedBytesAsync(
                    response.Content,
                    MaximumClientConfigBytes,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (InvalidDataException)
            {
                throw new YogabaseApiException(
                    502,
                    "client_config_invalid",
                    "Yogabase client configuration exceeded its size limit.");
            }
            if (payload.Length is < 2 or > MaximumClientConfigBytes)
            {
                throw new YogabaseApiException(502, "client_config_invalid", "Yogabase client configuration is empty or oversized.");
            }

            YogabaseClientConfiguration? configuration;
            try
            {
                configuration = JsonSerializer.Deserialize<YogabaseClientConfiguration>(payload, YogabaseJson.Options);
            }
            catch (JsonException exception)
            {
                throw new YogabaseApiException(502, "client_config_invalid", "Yogabase client configuration is malformed.", exception.Path);
            }

            var validated = ValidateConfiguration(configuration, options.AllowInsecureLoopback);
            var configuredGateway = new Uri(validated.GatewayEndpoint, UriKind.Absolute);
            if (!SameOrigin(gatewayEndpoint, configuredGateway))
            {
                throw new YogabaseApiException(502, "client_config_origin_mismatch", "Yogabase client configuration named another gateway origin.");
            }

            return new YogabaseClient(validated, options);
        }
    }

    public static YogabaseClient FromConfiguration(
        YogabaseClientConfiguration configuration,
        YogabaseClientOptions? options = null) => new(configuration, options ?? new YogabaseClientOptions());

    /// <summary>
    /// Loads the persisted session and refreshes it when required. Applications can await this
    /// during startup instead of discovering an expired session on their first gateway request.
    /// </summary>
    public Task<YogabaseSession?> InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return auth.GetValidSessionAsync(cancellationToken);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        realtime.DisposeAsync().AsTask().GetAwaiter().GetResult();
        auth.Dispose();
        transport.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        await realtime.DisposeAsync().ConfigureAwait(false);
        auth.Dispose();
        transport.Dispose();
    }

    internal static void EnsureCapability(YogabaseClientConfiguration configuration, string capabilityId)
    {
        if (!configuration.IsAvailable(capabilityId))
        {
            throw new NotSupportedException($"Yogabase capability '{capabilityId}' is unavailable for this release.");
        }
    }

    private void ThrowIfDisposed()
    {
#if NET10_0_OR_GREATER
        ObjectDisposedException.ThrowIf(disposed, this);
#else
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(YogabaseClient));
        }
#endif
    }

    private static YogabaseClientConfiguration ValidateConfiguration(
        YogabaseClientConfiguration? configuration,
        bool allowInsecureLoopback)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }
        if (!string.Equals(configuration.Schema, ClientConfigSchema, StringComparison.Ordinal))
        {
            throw new NotSupportedException($"Unsupported Yogabase client configuration schema '{configuration.Schema}'.");
        }
        if (configuration.ProjectId == Guid.Empty || configuration.EnvironmentId == Guid.Empty)
        {
            throw new ArgumentException("Yogabase client configuration has an invalid project or environment identity.", nameof(configuration));
        }
        if (!Uri.TryCreate(configuration.GatewayEndpoint, UriKind.Absolute, out var gateway))
        {
            throw new ArgumentException("Yogabase client configuration has an invalid gateway.", nameof(configuration));
        }

        YogabaseHttpTransport.ValidateGateway(gateway, allowInsecureLoopback);
        if (!Uri.TryCreate(configuration.RealtimeEndpoint, UriKind.Absolute, out var realtime))
        {
            throw new ArgumentException("Yogabase client configuration has an invalid realtime origin.", nameof(configuration));
        }
        YogabaseHttpTransport.ValidateGateway(realtime, allowInsecureLoopback);
        if (!Uri.TryCreate(configuration.OidcAuthority, UriKind.Absolute, out var authority)
            || !string.Equals(authority.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal)
                && !(allowInsecureLoopback && authority.IsLoopback && authority.Scheme == Uri.UriSchemeHttp)
            || !SameOrigin(gateway, authority)
            || !string.IsNullOrEmpty(authority.UserInfo)
            || !string.IsNullOrEmpty(authority.Query)
            || !string.IsNullOrEmpty(authority.Fragment)
            || !string.Equals(
                authority.AbsolutePath.TrimEnd('/'),
                $"/p/{configuration.ProjectId:D}",
                StringComparison.Ordinal))
        {
            throw new ArgumentException("Yogabase client configuration has an invalid OIDC authority.", nameof(configuration));
        }
        if (!Uri.TryCreate(configuration.OidcDiscoveryEndpoint, UriKind.Absolute, out var discovery)
            || !SameOrigin(gateway, discovery)
            || !string.Equals(discovery.Scheme, gateway.Scheme, StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(discovery.UserInfo)
            || !string.IsNullOrEmpty(discovery.Query)
            || !string.IsNullOrEmpty(discovery.Fragment)
            || !string.Equals(
                discovery.AbsolutePath,
                YogabaseRoutes.OidcDiscoveryPath,
                StringComparison.Ordinal))
        {
            throw new ArgumentException("Yogabase client configuration has an invalid OIDC discovery endpoint.", nameof(configuration));
        }
        if (string.IsNullOrWhiteSpace(configuration.OidcPublicClientId)
            || configuration.OidcPublicClientId.Length > 256
            || configuration.OidcScopes is null
            || configuration.OidcScopes.Count is < 1 or > 64
            || configuration.OidcScopes.Any(static value => value is null)
            || configuration.OidcScopes.Distinct(StringComparer.Ordinal).Count() != configuration.OidcScopes.Count
            || configuration.OidcScopes.Any(value => string.IsNullOrWhiteSpace(value) || value.Length > 128)
            || configuration.OidcRedirectUris is null
            || configuration.OidcRedirectUris.Count is < 1 or > 64
            || configuration.OidcRedirectUris.Any(static value => value is null)
            || configuration.OidcRedirectUris.Distinct(StringComparer.Ordinal).Count() != configuration.OidcRedirectUris.Count
            || configuration.OidcRedirectUris.Any(value =>
                value.Length > 2048
                || !Uri.TryCreate(value, UriKind.Absolute, out var redirect)
                || !string.IsNullOrEmpty(redirect.UserInfo)
                || !string.IsNullOrEmpty(redirect.Query)
                || !string.IsNullOrEmpty(redirect.Fragment))
            || configuration.Capabilities is null
            // The Data API module is withdrawn, so an installation now publishes four capabilities
            // and one that predates the removal still publishes five. Accept both rather than
            // refusing to start against either side of that rollout.
            || configuration.Capabilities.Count is < 4 or > 5
            || configuration.Capabilities.Any(static item => item is null)
            || configuration.Capabilities.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count()
                != configuration.Capabilities.Count
            || configuration.Capabilities.Any(item =>
                item.Id is not ("auth" or "storage" or "functions-rest" or "realtime-subscribe"
                    or YogabaseCapability.WithdrawnDataRest)
                || item.UnavailableReason?.Length > 256)
            || string.IsNullOrWhiteSpace(configuration.RealtimePath)
            || !configuration.RealtimePath.StartsWith("/", StringComparison.Ordinal)
            || configuration.RealtimePath.Contains("//", StringComparison.Ordinal)
            || !string.Equals(configuration.RealtimePath, YogabaseRoutes.RealtimePath, StringComparison.Ordinal)
            || configuration.IssuedAt == default
            || configuration.IssuedAt.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Yogabase client configuration contains an invalid bounded field.", nameof(configuration));
        }

        return new YogabaseClientConfiguration
        {
            Schema = configuration.Schema,
            ProjectId = configuration.ProjectId,
            EnvironmentId = configuration.EnvironmentId,
            GatewayEndpoint = gateway.AbsoluteUri,
            RealtimeEndpoint = realtime.AbsoluteUri,
            OidcAuthority = authority.AbsoluteUri.TrimEnd('/'),
            OidcDiscoveryEndpoint = discovery.AbsoluteUri,
            OidcPublicClientId = configuration.OidcPublicClientId,
            OidcScopes = Array.AsReadOnly(configuration.OidcScopes.ToArray()),
            OidcRedirectUris = Array.AsReadOnly(configuration.OidcRedirectUris.ToArray()),
            RealtimePath = configuration.RealtimePath,
            Capabilities = Array.AsReadOnly(configuration.Capabilities.Select(item => new YogabaseCapability
            {
                Id = item.Id,
                Available = item.Available,
                UnavailableReason = item.UnavailableReason,
            }).ToArray()),
            IssuedAt = configuration.IssuedAt,
        };
    }

    private static void ValidatePublishableKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)
            || key.Length is < 16 or > 160
            || !key.StartsWith("sy_pub_", StringComparison.Ordinal)
            || !string.Equals(key, key.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException("A bounded Yogabase publishable key is required.", nameof(key));
        }
    }

    private static bool SameOrigin(Uri left, Uri right) =>
        string.Equals(left.Scheme, right.Scheme, StringComparison.OrdinalIgnoreCase)
        && string.Equals(left.Host, right.Host, StringComparison.OrdinalIgnoreCase)
        && left.Port == right.Port;

    private static bool IsTransient(HttpStatusCode code) =>
        code is HttpStatusCode.RequestTimeout
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout
            or (HttpStatusCode)429;
}
