using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace StackYoga.YogaBase;

internal sealed class YogaBaseAuthClient : IYogaBaseAuth, IDisposable
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(1);
    private static readonly char[] EqualsSeparator = { '=' };
    private readonly YogaBaseClientConfiguration configuration;
    private readonly YogaBaseClientOptions options;
    private readonly IYogaBaseSessionStore store;
    private readonly SemaphoreSlim sessionMutationGate = new(1, 1);
    private YogaBaseHttpTransport? transport;
    private OidcEndpoints? endpoints;
    private long sessionGeneration;

    internal YogaBaseAuthClient(
        YogaBaseClientConfiguration configuration,
        YogaBaseClientOptions options,
        IYogaBaseSessionStore store)
    {
        this.configuration = configuration;
        this.options = options;
        this.store = store;
    }

    internal void AttachTransport(YogaBaseHttpTransport value) => transport = value;

    public async Task<YogaBaseSession?> GetSessionAsync(CancellationToken cancellationToken = default)
    {
        var tokens = await store.LoadAsync(cancellationToken).ConfigureAwait(false);
        return tokens is null ? null : YogaBaseSession.FromTokenSet(tokens);
    }

    public async Task SignUpAsync(
        string email,
        string password,
        string? invitationToken = null,
        CancellationToken cancellationToken = default)
    {
        YogaBaseClient.EnsureCapability(configuration, "auth");
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Email and password are required.");
        }

        using var response = await RequireTransport().SendAsync(
            HttpMethod.Post,
            "auth/signup",
            new { email, password, invitationToken },
            authenticated: false,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<YogaBaseSession> SignInWithPkceAsync(
        YogaBasePkceOptions options,
        CancellationToken cancellationToken = default)
    {
        YogaBaseClient.EnsureCapability(configuration, "auth");
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }
        if (options.RedirectUri is null || !options.RedirectUri.IsAbsoluteUri)
        {
            throw new ArgumentException("An absolute redirect URI is required.", nameof(options));
        }
        if (!configuration.OidcRedirectUris.Contains(options.RedirectUri.AbsoluteUri, StringComparer.Ordinal))
        {
            throw new ArgumentException("The redirect URI is not registered for this YogaBase client.", nameof(options));
        }
        if (options.OpenBrowserAsync is null)
        {
            throw new ArgumentException("A browser callback is required for PKCE sign-in.", nameof(options));
        }

        var expectedSessionGeneration = Volatile.Read(ref sessionGeneration);
        var oidc = await GetEndpointsAsync(cancellationToken).ConfigureAwait(false);
        var verifier = Base64Url(RandomBytes(48));
        string challenge;
        using (var sha = SHA256.Create())
        {
            challenge = Base64Url(sha.ComputeHash(Encoding.ASCII.GetBytes(verifier)));
        }
        var state = Base64Url(RandomBytes(32));
        var scopes = NormalizeScopes(options.Scopes);
        var authorize = Query(oidc.AuthorizationEndpoint, new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["client_id"] = configuration.OidcPublicClientId,
            ["redirect_uri"] = options.RedirectUri.AbsoluteUri,
            ["scope"] = string.Join(" ", scopes),
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
            ["state"] = state,
        });
        var callback = await options.OpenBrowserAsync(authorize, cancellationToken).ConfigureAwait(false);
        ValidateRedirect(options.RedirectUri, callback);
        Dictionary<string, string> query;
        try
        {
            query = ParseQuery(callback.Query);
        }
        catch (UriFormatException)
        {
            throw new YogaBaseApiException(400, "authorization_response_invalid", "The authorization response is malformed.");
        }
        if (!query.TryGetValue("state", out var returnedState) || !FixedTimeEquals(state, returnedState))
        {
            throw new YogaBaseApiException(400, "authorization_response_invalid", "The authorization response failed state validation.");
        }
        if (query.TryGetValue("error", out var error))
        {
            throw new YogaBaseApiException(400, error, "The authorization server rejected sign-in.");
        }
        if (!query.TryGetValue("code", out var code) || string.IsNullOrWhiteSpace(code))
        {
            throw new YogaBaseApiException(400, "authorization_response_invalid", "The authorization response did not contain a code.");
        }

        return await ExchangeNewSessionAsync(
            oidc.TokenEndpoint,
            new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = configuration.OidcPublicClientId,
                ["code"] = code,
                ["redirect_uri"] = options.RedirectUri.AbsoluteUri,
                ["code_verifier"] = verifier,
            },
            expectedSessionGeneration,
            cancellationToken).ConfigureAwait(false);
    }

    public Task<YogaBaseSession> RefreshAsync(CancellationToken cancellationToken = default) =>
        RefreshCoreAsync(force: true, cancellationToken);

    private async Task<YogaBaseSession> RefreshCoreAsync(
        bool force,
        CancellationToken cancellationToken)
    {
        await sessionMutationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var current = await store.LoadAsync(cancellationToken).ConfigureAwait(false);
            if (!force && current is not null && current.ExpiresAt > DateTimeOffset.UtcNow + RefreshSkew)
            {
                return YogaBaseSession.FromTokenSet(current);
            }
            if (current is null || string.IsNullOrWhiteSpace(current.RefreshToken))
            {
                throw new YogaBaseApiException(401, "refresh_token_missing", "This session cannot be refreshed.");
            }

            var oidc = await GetEndpointsAsync(cancellationToken).ConfigureAwait(false);
            return await ExchangeTokenAsync(
                oidc.TokenEndpoint,
                new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["client_id"] = configuration.OidcPublicClientId,
                    ["refresh_token"] = current.RefreshToken!,
                },
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            sessionMutationGate.Release();
        }
    }

    public async Task ClearLocalSessionAsync(CancellationToken cancellationToken = default)
    {
        await sessionMutationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await store.ClearAsync(cancellationToken).ConfigureAwait(false);
            Interlocked.Increment(ref sessionGeneration);
        }
        finally
        {
            sessionMutationGate.Release();
        }
    }

    internal async Task<string?> GetValidAccessTokenAsync(CancellationToken cancellationToken)
    {
        var current = await store.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (current is null)
        {
            return null;
        }
        if (current.ExpiresAt > DateTimeOffset.UtcNow + RefreshSkew)
        {
            return current.AccessToken;
        }
        if (string.IsNullOrEmpty(current.RefreshToken))
        {
            return current.ExpiresAt > DateTimeOffset.UtcNow ? current.AccessToken : null;
        }

        var refreshed = await RefreshCoreAsync(force: false, cancellationToken).ConfigureAwait(false);
        return refreshed.AccessToken;
    }

    private async Task<YogaBaseSession> ExchangeTokenAsync(
        Uri endpoint,
        IReadOnlyDictionary<string, string> fields,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new FormUrlEncodedContent(fields),
        };
        using var response = await RequireTransport().SendExternalAsync(request, cancellationToken).ConfigureAwait(false);
        var payload = await YogaBaseHttpContent.ReadBoundedStringAsync(
            response.Content,
            256 * 1024,
            cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var code = "token_request_failed";
            var message = "The YogaBase authorization server rejected the token request.";
            TryReadOAuthError(payload, ref code, ref message);
            throw new YogaBaseApiException((int)response.StatusCode, code, message);
        }

        YogaBaseTokenSet tokens;
        try
        {
            using var document = JsonDocument.Parse(payload, new JsonDocumentOptions { MaxDepth = 16 });
            var root = document.RootElement;
            var accessToken = RequiredString(root, "access_token");
            var tokenType = RequiredString(root, "token_type");
            if (!string.Equals(tokenType, "Bearer", StringComparison.OrdinalIgnoreCase)
                || !root.TryGetProperty("expires_in", out var expiresElement)
                || !expiresElement.TryGetInt32(out var expiresIn)
                || expiresIn is < 1 or > 86_400)
            {
                throw new JsonException();
            }

            tokens = new YogaBaseTokenSet
            {
                AccessToken = accessToken,
                RefreshToken = OptionalString(root, "refresh_token"),
                TokenType = "Bearer",
                ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn),
                Subject = TryReadJwtSubject(accessToken),
            };
        }
        catch (Exception exception) when (exception is JsonException or FormatException)
        {
            throw new YogaBaseApiException(502, "token_response_invalid", "The YogaBase authorization server returned an invalid token response.");
        }

        await store.SaveAsync(tokens, cancellationToken).ConfigureAwait(false);
        return YogaBaseSession.FromTokenSet(tokens);
    }

    private async Task<YogaBaseSession> ExchangeNewSessionAsync(
        Uri endpoint,
        IReadOnlyDictionary<string, string> fields,
        long expectedSessionGeneration,
        CancellationToken cancellationToken)
    {
        await sessionMutationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (Volatile.Read(ref sessionGeneration) != expectedSessionGeneration)
            {
                throw new YogaBaseApiException(
                    409,
                    "authentication_state_changed",
                    "The local YogaBase session changed while sign-in was in progress.");
            }

            var session = await ExchangeTokenAsync(endpoint, fields, cancellationToken).ConfigureAwait(false);
            Interlocked.Increment(ref sessionGeneration);
            return session;
        }
        finally
        {
            sessionMutationGate.Release();
        }
    }

    private async Task<OidcEndpoints> GetEndpointsAsync(CancellationToken cancellationToken)
    {
        if (endpoints is not null)
        {
            return endpoints;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, configuration.OidcDiscoveryEndpoint);
        using var response = await RequireTransport().SendExternalAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new YogaBaseApiException((int)response.StatusCode, "oidc_discovery_unavailable", "YogaBase OIDC discovery is unavailable.");
        }
        var payload = await YogaBaseHttpContent.ReadBoundedStringAsync(
            response.Content,
            256 * 1024,
            cancellationToken).ConfigureAwait(false);
        try
        {
            using var document = JsonDocument.Parse(payload, new JsonDocumentOptions { MaxDepth = 32 });
            var root = document.RootElement;
            var issuer = new Uri(RequiredString(root, "issuer"), UriKind.Absolute);
            var authorization = new Uri(RequiredString(root, "authorization_endpoint"), UriKind.Absolute);
            var token = new Uri(RequiredString(root, "token_endpoint"), UriKind.Absolute);
            if (!string.Equals(issuer.AbsoluteUri.TrimEnd('/'), configuration.OidcAuthority.TrimEnd('/'), StringComparison.Ordinal)
                || !SameOrigin(issuer, authorization)
                || !SameOrigin(issuer, token)
                || !string.IsNullOrEmpty(authorization.UserInfo)
                || !string.IsNullOrEmpty(authorization.Query)
                || !string.IsNullOrEmpty(authorization.Fragment)
                || !string.Equals(authorization.AbsolutePath, "/connect/authorize", StringComparison.Ordinal)
                || !string.IsNullOrEmpty(token.UserInfo)
                || !string.IsNullOrEmpty(token.Query)
                || !string.IsNullOrEmpty(token.Fragment)
                || !string.Equals(token.AbsolutePath, "/connect/token", StringComparison.Ordinal)
                || issuer.Scheme != Uri.UriSchemeHttps && !(options.AllowInsecureLoopback && issuer.IsLoopback))
            {
                throw new JsonException();
            }

            endpoints = new OidcEndpoints(authorization, token);
            return endpoints;
        }
        catch (Exception exception) when (exception is JsonException or UriFormatException)
        {
            throw new YogaBaseApiException(502, "oidc_discovery_invalid", "YogaBase OIDC discovery returned an invalid document.");
        }
    }

    private string[] NormalizeScopes(IReadOnlyList<string>? requested)
    {
        var allowed = new HashSet<string>(configuration.OidcScopes, StringComparer.Ordinal);
        var scopes = requested is null || requested.Count == 0
            ? configuration.OidcScopes.ToArray()
            : requested.Distinct(StringComparer.Ordinal).ToArray();
        if (scopes.Length is < 1 or > 64
            || scopes.Any(scope => string.IsNullOrWhiteSpace(scope) || scope.Length > 128 || !allowed.Contains(scope)))
        {
            throw new ArgumentException("One or more requested OIDC scopes are not allowed.", nameof(requested));
        }

        return scopes;
    }

    private YogaBaseHttpTransport RequireTransport() => transport
        ?? throw new InvalidOperationException("The YogaBase client is not initialized.");

    private static byte[] RandomBytes(int count)
    {
        var bytes = new byte[count];
        using var random = RandomNumberGenerator.Create();
        random.GetBytes(bytes);
        return bytes;
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes)
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');

    private static Uri Query(Uri endpoint, IReadOnlyDictionary<string, string> fields)
    {
        var builder = new UriBuilder(endpoint)
        {
            Query = string.Join("&", fields.Select(pair =>
                $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}")),
        };
        return builder.Uri;
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var segment in query.TrimStart('?').Split('&'))
        {
            if (string.IsNullOrEmpty(segment))
            {
                continue;
            }
            var parts = segment.Split(EqualsSeparator, 2);
            var key = Uri.UnescapeDataString(parts[0].Replace('+', ' '));
            var value = parts.Length == 2 ? Uri.UnescapeDataString(parts[1].Replace('+', ' ')) : string.Empty;
            if (!values.TryAdd(key, value))
            {
                throw new YogaBaseApiException(400, "authorization_response_invalid", "The authorization response contains duplicate parameters.");
            }
        }
        return values;
    }

    private static void ValidateRedirect(Uri expected, Uri actual)
    {
        if (actual is null
            || !actual.IsAbsoluteUri
            || !string.Equals(expected.Scheme, actual.Scheme, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(expected.Host, actual.Host, StringComparison.OrdinalIgnoreCase)
            || expected.Port != actual.Port
            || !string.Equals(expected.AbsolutePath, actual.AbsolutePath, StringComparison.Ordinal)
            || !string.IsNullOrEmpty(actual.UserInfo)
            || !string.IsNullOrEmpty(actual.Fragment))
        {
            throw new YogaBaseApiException(400, "authorization_redirect_mismatch", "The authorization response used another redirect URI.");
        }
    }

    private static bool FixedTimeEquals(string expected, string actual)
    {
        var left = Encoding.UTF8.GetBytes(expected);
        var right = Encoding.UTF8.GetBytes(actual);
        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }

    private static string RequiredString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value)
            || value.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw new JsonException();
        }
        return value.GetString()!;
    }

    private static string? OptionalString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static void TryReadOAuthError(string payload, ref string code, ref string message)
    {
        try
        {
            using var document = JsonDocument.Parse(payload, new JsonDocumentOptions { MaxDepth = 8 });
            code = OptionalString(document.RootElement, "error") ?? code;
            message = OptionalString(document.RootElement, "error_description") ?? message;
        }
        catch (JsonException)
        {
        }
    }

    private static string? TryReadJwtSubject(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3)
            {
                return null;
            }
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
            return OptionalString(document.RootElement, "sub");
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            return null;
        }
    }

    private static bool SameOrigin(Uri left, Uri right) =>
        string.Equals(left.Scheme, right.Scheme, StringComparison.OrdinalIgnoreCase)
        && string.Equals(left.Host, right.Host, StringComparison.OrdinalIgnoreCase)
        && left.Port == right.Port;

    public void Dispose()
    {
        sessionMutationGate.Dispose();
    }

    private sealed class OidcEndpoints
    {
        internal OidcEndpoints(Uri authorizationEndpoint, Uri tokenEndpoint)
        {
            AuthorizationEndpoint = authorizationEndpoint;
            TokenEndpoint = tokenEndpoint;
        }

        internal Uri AuthorizationEndpoint { get; }
        internal Uri TokenEndpoint { get; }
    }
}
