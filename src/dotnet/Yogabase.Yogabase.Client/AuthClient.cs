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

namespace Yogabase.Yogabase;

internal sealed class YogabaseAuthClient : IYogabaseAuth, IDisposable
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(1);
    private static readonly char[] EqualsSeparator = { '=' };
    private readonly YogabaseClientConfiguration configuration;
    private readonly YogabaseClientOptions options;
    private readonly IYogabaseSessionStore store;
    private readonly SemaphoreSlim sessionMutationGate = new(1, 1);
    private YogabaseHttpTransport? transport;
    private OidcEndpoints? endpoints;
    private long sessionGeneration;

    internal YogabaseAuthClient(
        YogabaseClientConfiguration configuration,
        YogabaseClientOptions options,
        IYogabaseSessionStore store)
    {
        this.configuration = configuration;
        this.options = options;
        this.store = store;
    }

    internal void AttachTransport(YogabaseHttpTransport value) => transport = value;

    public async Task<YogabaseSession?> GetSessionAsync(CancellationToken cancellationToken = default)
    {
        var tokens = await store.LoadAsync(cancellationToken).ConfigureAwait(false);
        return tokens is null ? null : YogabaseSession.FromTokenSet(tokens);
    }

    public async Task SignUpAsync(
        string email,
        string password,
        string? invitationToken = null,
        CancellationToken cancellationToken = default)
    {
        YogabaseClient.EnsureCapability(configuration, "auth");
        ValidateEmail(email);
        ValidatePassword(password);
        ValidateOptionalBoundedValue(invitationToken, 8192, nameof(invitationToken));

        using var response = await RequireTransport().SendAsync(
            HttpMethod.Post,
            "auth/signup",
            new { email, password, invitationToken },
            authenticated: false,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<YogabaseInteractiveSignInOutcome> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        YogabaseClient.EnsureCapability(configuration, "auth");
        ValidateEmail(email);
        ValidatePassword(password);
        var response = await RequireTransport().SendAsync(
            HttpMethod.Post,
            "auth/login",
            new { email, password },
            authenticated: false,
            cancellationToken,
            ensureSuccess: false).ConfigureAwait(false);
        if ((int)response.StatusCode == 204)
        {
            response.Dispose();
            return YogabaseInteractiveSignInOutcome.SignedIn;
        }
        if ((int)response.StatusCode == 202)
        {
            response.Dispose();
            return YogabaseInteractiveSignInOutcome.MfaRequired;
        }

        await YogabaseHttpTransport.ThrowApiExceptionAsync(response, cancellationToken).ConfigureAwait(false);
        throw new InvalidOperationException("The authentication response was not handled.");
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        YogabaseClient.EnsureCapability(configuration, "auth");
        using var response = await RequireTransport().SendAsync(
            HttpMethod.Post,
            "auth/logout",
            body: null,
            authenticated: false,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task ConfirmEmailAsync(
        Guid userId,
        string token,
        CancellationToken cancellationToken = default)
    {
        YogabaseClient.EnsureCapability(configuration, "auth");
        ValidateUserId(userId);
        ValidateRequiredBoundedValue(token, 8192, nameof(token));
        using var response = await RequireTransport().SendAsync(
            HttpMethod.Post,
            "auth/email/confirm",
            new { userId, token },
            authenticated: false,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task RequestPasswordResetAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        YogabaseClient.EnsureCapability(configuration, "auth");
        ValidateEmail(email);
        using var response = await RequireTransport().SendAsync(
            HttpMethod.Post,
            "auth/password/reset/request",
            new { email },
            authenticated: false,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task CompletePasswordResetAsync(
        Guid userId,
        string token,
        string password,
        CancellationToken cancellationToken = default)
    {
        YogabaseClient.EnsureCapability(configuration, "auth");
        ValidateUserId(userId);
        ValidateRequiredBoundedValue(token, 8192, nameof(token));
        ValidatePassword(password);
        using var response = await RequireTransport().SendAsync(
            HttpMethod.Post,
            "auth/password/reset/complete",
            new { userId, token, password },
            authenticated: false,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<YogabaseMfaSetupResult> SetupMfaAsync(
        CancellationToken cancellationToken = default)
    {
        YogabaseClient.EnsureCapability(configuration, "auth");
        var response = await RequireTransport().SendAsync(
            HttpMethod.Post,
            "auth/mfa/setup",
            body: null,
            authenticated: false,
            cancellationToken).ConfigureAwait(false);
        var result = await YogabaseHttpTransport.ReadJsonAsync<YogabaseMfaSetupResult>(response, cancellationToken)
            .ConfigureAwait(false);
        ValidateRequiredBoundedValue(result.SharedKey, 512, nameof(result.SharedKey));
        return result;
    }

    public Task ConfirmMfaAsync(string code, CancellationToken cancellationToken = default) =>
        SendMfaCodeAsync("auth/mfa/confirm", code, cancellationToken);

    public Task VerifyMfaAsync(string code, CancellationToken cancellationToken = default) =>
        SendMfaCodeAsync("auth/mfa/verify", code, cancellationToken);

    public async Task<IReadOnlyList<string>> RegenerateRecoveryCodesAsync(
        CancellationToken cancellationToken = default)
    {
        YogabaseClient.EnsureCapability(configuration, "auth");
        var response = await RequireTransport().SendAsync(
            HttpMethod.Post,
            "auth/mfa/recovery/regenerate",
            body: null,
            authenticated: false,
            cancellationToken).ConfigureAwait(false);
        var result = await YogabaseHttpTransport.ReadJsonAsync<RecoveryCodesResponse>(response, cancellationToken)
            .ConfigureAwait(false);
        if (result.RecoveryCodes is null
            || result.RecoveryCodes.Count is < 1 or > 10
            || result.RecoveryCodes.Any(value => string.IsNullOrWhiteSpace(value) || value.Length > 256))
        {
            throw new YogabaseApiException(502, "recovery_codes_invalid", "Yogabase returned invalid recovery codes.");
        }

        return result.RecoveryCodes.ToArray();
    }

    public async Task UseRecoveryCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        YogabaseClient.EnsureCapability(configuration, "auth");
        ValidateRequiredBoundedValue(code, 256, nameof(code));
        using var response = await RequireTransport().SendAsync(
            HttpMethod.Post,
            "auth/mfa/recovery/use",
            new { code },
            authenticated: false,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<YogabaseInteractiveSignInOutcome> CompleteExternalLoginAsync(
        string provider,
        string code,
        string state,
        CancellationToken cancellationToken = default)
    {
        YogabaseClient.EnsureCapability(configuration, "auth");
        ValidateProvider(provider);
        ValidateRequiredBoundedValue(code, 8192, nameof(code));
        ValidateRequiredBoundedValue(state, 2048, nameof(state));
        var response = await RequireTransport().SendAsync(
            HttpMethod.Post,
            $"auth/external/{Uri.EscapeDataString(provider)}/callback",
            new { code, state },
            authenticated: false,
            cancellationToken,
            ensureSuccess: false).ConfigureAwait(false);
        if ((int)response.StatusCode == 204)
        {
            response.Dispose();
            return YogabaseInteractiveSignInOutcome.SignedIn;
        }
        if ((int)response.StatusCode == 202)
        {
            response.Dispose();
            return YogabaseInteractiveSignInOutcome.MfaRequired;
        }

        await YogabaseHttpTransport.ThrowApiExceptionAsync(response, cancellationToken).ConfigureAwait(false);
        throw new InvalidOperationException("The external authentication response was not handled.");
    }

    public async Task<YogabaseSession> SignInWithPkceAsync(
        YogabasePkceOptions options,
        CancellationToken cancellationToken = default)
    {
        YogabaseClient.EnsureCapability(configuration, "auth");
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
            throw new ArgumentException("The redirect URI is not registered for this Yogabase client.", nameof(options));
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
            throw new YogabaseApiException(400, "authorization_response_invalid", "The authorization response is malformed.");
        }
        if (!query.TryGetValue("state", out var returnedState) || !FixedTimeEquals(state, returnedState))
        {
            throw new YogabaseApiException(400, "authorization_response_invalid", "The authorization response failed state validation.");
        }
        if (query.TryGetValue("error", out var error))
        {
            throw new YogabaseApiException(400, error, "The authorization server rejected sign-in.");
        }
        if (!query.TryGetValue("code", out var code) || string.IsNullOrWhiteSpace(code))
        {
            throw new YogabaseApiException(400, "authorization_response_invalid", "The authorization response did not contain a code.");
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

    public Task<YogabaseSession> RefreshAsync(CancellationToken cancellationToken = default) =>
        RefreshCoreAsync(force: true, cancellationToken);

    private async Task<YogabaseSession> RefreshCoreAsync(
        bool force,
        CancellationToken cancellationToken)
    {
        await sessionMutationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var current = await store.LoadAsync(cancellationToken).ConfigureAwait(false);
            if (!force && current is not null && current.ExpiresAt > DateTimeOffset.UtcNow + RefreshSkew)
            {
                return YogabaseSession.FromTokenSet(current);
            }
            if (current is null || string.IsNullOrWhiteSpace(current.RefreshToken))
            {
                throw new YogabaseApiException(401, "refresh_token_missing", "This session cannot be refreshed.");
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

    private async Task<YogabaseSession> ExchangeTokenAsync(
        Uri endpoint,
        IReadOnlyDictionary<string, string> fields,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new FormUrlEncodedContent(fields),
        };
        using var response = await RequireTransport().SendExternalAsync(request, cancellationToken).ConfigureAwait(false);
        var payload = await YogabaseHttpContent.ReadBoundedStringAsync(
            response.Content,
            256 * 1024,
            cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var code = "token_request_failed";
            var message = "The Yogabase authorization server rejected the token request.";
            TryReadOAuthError(payload, ref code, ref message);
            throw new YogabaseApiException((int)response.StatusCode, code, message);
        }

        YogabaseTokenSet tokens;
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

            tokens = new YogabaseTokenSet
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
            throw new YogabaseApiException(502, "token_response_invalid", "The Yogabase authorization server returned an invalid token response.");
        }

        await store.SaveAsync(tokens, cancellationToken).ConfigureAwait(false);
        return YogabaseSession.FromTokenSet(tokens);
    }

    private async Task<YogabaseSession> ExchangeNewSessionAsync(
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
                throw new YogabaseApiException(
                    409,
                    "authentication_state_changed",
                    "The local Yogabase session changed while sign-in was in progress.");
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
            throw new YogabaseApiException((int)response.StatusCode, "oidc_discovery_unavailable", "Yogabase OIDC discovery is unavailable.");
        }
        var payload = await YogabaseHttpContent.ReadBoundedStringAsync(
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
            throw new YogabaseApiException(502, "oidc_discovery_invalid", "Yogabase OIDC discovery returned an invalid document.");
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

    private async Task SendMfaCodeAsync(
        string path,
        string code,
        CancellationToken cancellationToken)
    {
        YogabaseClient.EnsureCapability(configuration, "auth");
        ValidateRequiredBoundedValue(code, 128, nameof(code));
        using var response = await RequireTransport().SendAsync(
            HttpMethod.Post,
            path,
            new { code },
            authenticated: false,
            cancellationToken).ConfigureAwait(false);
    }

    private static void ValidateEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length > 320
            || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException("A bounded email address is required.", nameof(value));
        }
    }

    private static void ValidatePassword(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 1024)
        {
            throw new ArgumentException("A non-empty password within the gateway limit is required.", nameof(value));
        }
    }

    private static void ValidateUserId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("A non-empty user id is required.", nameof(value));
        }
    }

    private static void ValidateRequiredBoundedValue(string value, int maximumLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length > maximumLength
            || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException("A bounded non-empty value is required.", parameterName);
        }
    }

    private static void ValidateOptionalBoundedValue(string? value, int maximumLength, string parameterName)
    {
        if (value is not null
            && (string.IsNullOrWhiteSpace(value)
                || value.Length > maximumLength
                || !string.Equals(value, value.Trim(), StringComparison.Ordinal)))
        {
            throw new ArgumentException("The optional value must be bounded and non-empty when supplied.", parameterName);
        }
    }

    private static void ValidateProvider(string value)
    {
        if (string.IsNullOrEmpty(value)
            || value.Length > 64
            || value.Any(static character => !(character is >= 'a' and <= 'z'
                or >= '0' and <= '9'
                or '.' or '_' or '-')))
        {
            throw new ArgumentException("External identity providers use 1..64 lowercase [a-z0-9._-] characters.", nameof(value));
        }
    }

    private YogabaseHttpTransport RequireTransport() => transport
        ?? throw new InvalidOperationException("The Yogabase client is not initialized.");

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
                throw new YogabaseApiException(400, "authorization_response_invalid", "The authorization response contains duplicate parameters.");
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
            throw new YogabaseApiException(400, "authorization_redirect_mismatch", "The authorization response used another redirect URI.");
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

    private sealed class RecoveryCodesResponse
    {
        public IReadOnlyList<string>? RecoveryCodes { get; set; }
    }
}
