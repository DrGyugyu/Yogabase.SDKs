using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace StackYoga.YogaBase;

public sealed class YogaBaseClient : IDisposable, IAsyncDisposable
{
    public const string ClientConfigSchema = "yogabase-client-config.v1";
    private const int MaximumClientConfigBytes = 256 * 1024;

    private readonly YogaBaseHttpTransport transport;
    private readonly YogaBaseAuthClient auth;
    private readonly YogaBaseRealtimeClient realtime;
    private bool disposed;

    private YogaBaseClient(YogaBaseClientConfiguration configuration, YogaBaseClientOptions options)
    {
        Configuration = ValidateConfiguration(configuration, options.AllowInsecureLoopback);
        var sessionStore = options.SessionStore ?? new InMemoryYogaBaseSessionStore();
        auth = new YogaBaseAuthClient(Configuration, options, sessionStore);
        Auth = auth;
        transport = new YogaBaseHttpTransport(
            new Uri(Configuration.GatewayEndpoint, UriKind.Absolute),
            options,
            auth.GetValidAccessTokenAsync);
        auth.AttachTransport(transport);
        Data = new YogaBaseDataClient(Configuration, transport);
        Storage = new YogaBaseStorageClient(Configuration, transport);
        Functions = new YogaBaseFunctionsClient(Configuration, transport);
        realtime = new YogaBaseRealtimeClient(Configuration, auth.GetValidAccessTokenAsync);
        Realtime = realtime;
    }

    public YogaBaseClientConfiguration Configuration { get; }
    public IYogaBaseAuth Auth { get; }
    public IYogaBaseData Data { get; }
    public IYogaBaseRealtime Realtime { get; }
    public IYogaBaseStorage Storage { get; }
    public IYogaBaseFunctions Functions { get; }

    public static async Task<YogaBaseClient> CreateAsync(
        Uri gatewayEndpoint,
        string publishableKey,
        YogaBaseClientOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new YogaBaseClientOptions();
        if (gatewayEndpoint is null)
        {
            throw new ArgumentNullException(nameof(gatewayEndpoint));
        }

        YogaBaseHttpTransport.ValidateGateway(gatewayEndpoint, options.AllowInsecureLoopback);
        ValidatePublishableKey(publishableKey);

        using var client = YogaBaseHttpClientFactory.Create(options);
        var path = $"v1/client-config/{Uri.EscapeDataString(publishableKey)}";
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
            throw new YogaBaseApiException(503, "client_config_unavailable", "YogaBase client configuration is unavailable.");
        }
        if (!response.IsSuccessStatusCode)
        {
            await YogaBaseHttpTransport.ThrowApiExceptionAsync(response, cancellationToken).ConfigureAwait(false);
        }

        using (response)
        {
            if (response.Content.Headers.ContentLength is > MaximumClientConfigBytes)
            {
                throw new YogaBaseApiException(502, "client_config_invalid", "YogaBase client configuration exceeded its size limit.");
            }

            byte[] payload;
            try
            {
                payload = await YogaBaseHttpContent.ReadBoundedBytesAsync(
                    response.Content,
                    MaximumClientConfigBytes,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (InvalidDataException)
            {
                throw new YogaBaseApiException(
                    502,
                    "client_config_invalid",
                    "YogaBase client configuration exceeded its size limit.");
            }
            if (payload.Length is < 2 or > MaximumClientConfigBytes)
            {
                throw new YogaBaseApiException(502, "client_config_invalid", "YogaBase client configuration is empty or oversized.");
            }

            YogaBaseClientConfiguration? configuration;
            try
            {
                configuration = JsonSerializer.Deserialize<YogaBaseClientConfiguration>(payload, YogaBaseJson.Options);
            }
            catch (JsonException exception)
            {
                throw new YogaBaseApiException(502, "client_config_invalid", "YogaBase client configuration is malformed.", exception.Path);
            }

            var validated = ValidateConfiguration(configuration, options.AllowInsecureLoopback);
            var configuredGateway = new Uri(validated.GatewayEndpoint, UriKind.Absolute);
            if (!SameOrigin(gatewayEndpoint, configuredGateway))
            {
                throw new YogaBaseApiException(502, "client_config_origin_mismatch", "YogaBase client configuration named another gateway origin.");
            }

            return new YogaBaseClient(validated, options);
        }
    }

    public static YogaBaseClient FromConfiguration(
        YogaBaseClientConfiguration configuration,
        YogaBaseClientOptions? options = null) => new(configuration, options ?? new YogaBaseClientOptions());

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

    internal static void EnsureCapability(YogaBaseClientConfiguration configuration, string capabilityId)
    {
        if (!configuration.IsAvailable(capabilityId))
        {
            throw new NotSupportedException($"YogaBase capability '{capabilityId}' is unavailable for this release.");
        }
    }

    private static YogaBaseClientConfiguration ValidateConfiguration(
        YogaBaseClientConfiguration? configuration,
        bool allowInsecureLoopback)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }
        if (!string.Equals(configuration.Schema, ClientConfigSchema, StringComparison.Ordinal))
        {
            throw new NotSupportedException($"Unsupported YogaBase client configuration schema '{configuration.Schema}'.");
        }
        if (configuration.ProjectId == Guid.Empty || configuration.EnvironmentId == Guid.Empty)
        {
            throw new ArgumentException("YogaBase client configuration has an invalid project or environment identity.", nameof(configuration));
        }
        if (!Uri.TryCreate(configuration.GatewayEndpoint, UriKind.Absolute, out var gateway))
        {
            throw new ArgumentException("YogaBase client configuration has an invalid gateway.", nameof(configuration));
        }

        YogaBaseHttpTransport.ValidateGateway(gateway, allowInsecureLoopback);
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
            throw new ArgumentException("YogaBase client configuration has an invalid OIDC authority.", nameof(configuration));
        }
        if (!Uri.TryCreate(configuration.OidcDiscoveryEndpoint, UriKind.Absolute, out var discovery)
            || !SameOrigin(gateway, discovery)
            || !string.Equals(discovery.Scheme, gateway.Scheme, StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(discovery.UserInfo)
            || !string.IsNullOrEmpty(discovery.Query)
            || !string.IsNullOrEmpty(discovery.Fragment)
            || !string.Equals(
                discovery.AbsolutePath,
                "/.well-known/openid-configuration",
                StringComparison.Ordinal))
        {
            throw new ArgumentException("YogaBase client configuration has an invalid OIDC discovery endpoint.", nameof(configuration));
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
            || configuration.Capabilities.Count != 5
            || configuration.Capabilities.Any(static item => item is null)
            || configuration.Capabilities.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count()
                != configuration.Capabilities.Count
            || configuration.Capabilities.Any(item =>
                item.Id is not ("auth" or "data-rest" or "storage" or "functions-rest" or "realtime-subscribe")
                || item.UnavailableReason?.Length > 256)
            || string.IsNullOrWhiteSpace(configuration.RealtimePath)
            || !configuration.RealtimePath.StartsWith("/", StringComparison.Ordinal)
            || configuration.RealtimePath.Contains("//", StringComparison.Ordinal)
            || !string.Equals(configuration.RealtimePath, "/v1/realtime", StringComparison.Ordinal)
            || configuration.IssuedAt == default
            || configuration.IssuedAt.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("YogaBase client configuration contains an invalid bounded field.", nameof(configuration));
        }

        return new YogaBaseClientConfiguration
        {
            Schema = configuration.Schema,
            ProjectId = configuration.ProjectId,
            EnvironmentId = configuration.EnvironmentId,
            GatewayEndpoint = gateway.AbsoluteUri,
            OidcAuthority = authority.AbsoluteUri.TrimEnd('/'),
            OidcDiscoveryEndpoint = discovery.AbsoluteUri,
            OidcPublicClientId = configuration.OidcPublicClientId,
            OidcScopes = Array.AsReadOnly(configuration.OidcScopes.ToArray()),
            OidcRedirectUris = Array.AsReadOnly(configuration.OidcRedirectUris.ToArray()),
            RealtimePath = configuration.RealtimePath,
            Capabilities = Array.AsReadOnly(configuration.Capabilities.Select(item => new YogaBaseCapability
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
            throw new ArgumentException("A bounded StackYoga publishable key is required.", nameof(key));
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
