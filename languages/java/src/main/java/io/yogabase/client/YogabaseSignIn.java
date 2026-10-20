package io.yogabase.client;

import com.google.gson.JsonArray;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParseException;
import com.google.gson.JsonParser;
import java.io.IOException;
import java.io.InputStream;
import java.net.CookieManager;
import java.net.ProxySelector;
import java.net.URI;
import java.net.URISyntaxException;
import java.net.URLDecoder;
import java.net.URLEncoder;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.security.SecureRandom;
import java.time.Clock;
import java.time.Duration;
import java.util.Base64;
import java.util.HashMap;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.Objects;
import java.util.Set;
import java.util.StringJoiner;
import java.util.UUID;
import java.util.regex.Pattern;

/**
 * Headless sign-in of a project user, with a password or with an external provider, for a JVM application. It signs
 * in at the gateway for the identity cookie ({@code POST /auth/login}, or the external provider's callback), then
 * runs the OpenID Connect authorization-code flow with PKCE with that cookie in place of a browser: it reads the
 * authorization endpoint's redirect without following it and exchanges the code for tokens at the token endpoint.
 * The result is a {@link YogabaseSession}, the Kiota token provider of the application client.
 *
 * <p>The publishable key identifies the project and authenticates nothing; no confidential credential is held.
 * The e-mail address and password go to the gateway origin given and to nothing else, and the identity cookie only
 * to the endpoints the gateway's own discovery document names on that origin. Plain HTTP is accepted only for a
 * loopback gateway with {@link Options#allowInsecureLoopback(boolean)}.
 */
public final class YogabaseSignIn {
    static final int MAXIMUM_DOCUMENT_BYTES = 1_048_576;
    private static final Duration DEFAULT_TIMEOUT = Duration.ofSeconds(30);
    private static final Pattern PROVIDER = Pattern.compile("[a-z0-9._-]{1,64}");
    private static final Set<String> LOOPBACK = Set.of("localhost", "127.0.0.1", "[::1]", "::1");
    private static final SecureRandom RANDOM = new SecureRandom();

    private YogabaseSignIn() {
    }

    /** How one sign-in reaches the gateway. */
    public static final class Options {
        private String mfaCode;
        private boolean allowInsecureLoopback;
        private Duration timeout = DEFAULT_TIMEOUT;
        private HttpClient httpClient;
        private Clock clock = Clock.systemUTC();

        /** The authenticator code for an account with MFA, after a first call refused with {@code mfa_required}. */
        public Options mfaCode(String value) {
            this.mfaCode = value;
            return this;
        }

        /** Accepts a plain {@code http://} gateway on a loopback host, for a local installation only. */
        public Options allowInsecureLoopback(boolean value) {
            this.allowInsecureLoopback = value;
            return this;
        }

        /** The longest one request may take; 30 seconds by default. */
        public Options timeout(Duration value) {
            Objects.requireNonNull(value, "timeout");
            if (value.isZero() || value.isNegative() || value.compareTo(Duration.ofMinutes(15)) > 0) {
                throw new IllegalArgumentException("timeout must be greater than zero and at most 15 minutes");
            }
            this.timeout = value;
            return this;
        }

        /**
         * The HTTP client to sign in with instead of one made for the call. It must keep cookies (a cookie handler)
         * and must not follow redirects, because the identity cookie and the authorization redirect are what the
         * flow reads.
         */
        public Options httpClient(HttpClient value) {
            Objects.requireNonNull(value, "httpClient");
            if (value.cookieHandler().isEmpty() || value.followRedirects() != HttpClient.Redirect.NEVER) {
                throw new IllegalArgumentException("httpClient must have a cookie handler and must not follow redirects");
            }
            this.httpClient = value;
            return this;
        }

        Options clock(Clock value) {
            this.clock = Objects.requireNonNull(value, "clock");
            return this;
        }
    }

    /**
     * Signs the user in with no browser. A refusal is a {@link YogabaseAuthException} with the gateway's code:
     * {@code invalid_credentials} (401: a wrong password, an unknown, unconfirmed or locked account, never told
     * apart), {@code mfa_enrollment_required} (403), {@code authentication_rate_limited} (429), and
     * {@code mfa_required} (202) for an account with MFA and no {@link Options#mfaCode(String)}: call again with it.
     */
    public static YogabaseSession signInWithPassword(URI gateway, String publishableKey, String email, String password) {
        return signInWithPassword(gateway, publishableKey, email, password, new Options());
    }

    /** {@link #signInWithPassword(URI, String, String, String)} with options. */
    public static YogabaseSession signInWithPassword(
        URI gateway, String publishableKey, String email, String password, Options options) {
        Objects.requireNonNull(options, "options");
        final URI origin = requireGateway(gateway, options.allowInsecureLoopback);
        requireKey(publishableKey);
        Objects.requireNonNull(email, "email");
        Objects.requireNonNull(password, "password");
        final Transport transport = transport(options);
        final Discovery found = discover(transport, origin, publishableKey);
        final JsonObject credentials = new JsonObject();
        credentials.addProperty("email", email);
        credentials.addProperty("password", password);
        Response login = transport.send("POST", origin.resolve("/auth/login"), json(credentials));
        if (login.status() == 202 && options.mfaCode != null) {
            verifyMfa(transport, origin, options.mfaCode);
        } else if (login.status() != 204) {
            throw refusal(login);
        }
        return authorize(transport, origin, found, options.clock);
    }

    /**
     * Starts signing a user in with a provider the project configured, under the id it gave it ({@code google},
     * {@code microsoft}, {@code apple}); {@code redirectUri} must be one the project registered. Only the S256
     * challenge of a random verifier is sent. Open {@link YogabaseExternalSignIn#authorizationUrl()} in the system
     * browser, then pass the address it is redirected to to {@link #completeExternalSignIn}. This opens no browser
     * and listens on no port. A refusal is a {@link YogabaseAuthException} with the gateway's code, or
     * {@code external_start_invalid} for an answer that is not an https address and a state.
     */
    public static YogabaseExternalSignIn startExternalSignIn(
        URI gateway, String publishableKey, String provider, String redirectUri, Options options) {
        Objects.requireNonNull(options, "options");
        final URI origin = requireGateway(gateway, options.allowInsecureLoopback);
        requireKey(publishableKey);
        if (provider == null || !PROVIDER.matcher(provider).matches()) {
            throw new IllegalArgumentException("provider must be 1 to 64 lowercase letters, digits, '.', '_' or '-'");
        }
        final URI registered = parse(Objects.requireNonNull(redirectUri, "redirectUri"), "redirectUri");
        final String verifier = random(48);
        final Transport transport = transport(options);
        final JsonObject body = new JsonObject();
        body.addProperty("redirectUri", redirectUri);
        body.addProperty("codeChallenge", s256(verifier));
        final Response response = transport.send("POST", origin.resolve("/auth/external/" + provider + "/start"), json(body));
        if (response.status() != 200) {
            throw refusal(response);
        }
        final JsonObject answer = object(response);
        final String url = string(answer, "authorizationUrl");
        final String state = string(answer, "state");
        final URI authorization = url == null ? null : tryParse(url);
        if (authorization == null
            || !"https".equalsIgnoreCase(authorization.getScheme())
            || authorization.getHost() == null
            || state == null
            || state.isEmpty()) {
            throw new YogabaseAuthException("Yogabase returned an invalid external sign-in start.", 502, "external_start_invalid", null);
        }
        return new YogabaseExternalSignIn(
            url, origin, publishableKey, provider, registered, state, verifier, transport, options.clock);
    }

    /** {@link #startExternalSignIn(URI, String, String, String, Options)} with default options. */
    public static YogabaseExternalSignIn startExternalSignIn(URI gateway, String publishableKey, String provider, String redirectUri) {
        return startExternalSignIn(gateway, publishableKey, provider, redirectUri, new Options());
    }

    /**
     * Completes {@code pending} from {@code redirect}, the full address the browser was redirected to, then signs
     * in as {@link #signInWithPassword} does. A redirect with another state, an {@code error}, no code or another
     * address ends the call before any request. An account with MFA refuses with {@code mfa_required} (202): call
     * again with the same {@code pending} and the authenticator code, and the code already spent is not sent again.
     */
    public static YogabaseSession completeExternalSignIn(YogabaseExternalSignIn pending, URI redirect, String mfaCode) {
        Objects.requireNonNull(pending, "pending");
        synchronized (pending) {
            final String code = pending.step == null ? readRedirect(Objects.requireNonNull(redirect, "redirect"), pending.redirectUri, pending.state) : null;
            // A discovery document that is not the gateway's ends the call before the one-time code is sent.
            final Discovery found = discover(pending.transport, pending.gateway, pending.publishableKey);
            if (pending.step == null) {
                final JsonObject body = new JsonObject();
                body.addProperty("code", code);
                body.addProperty("state", pending.state);
                body.addProperty("codeVerifier", pending.verifier);
                final Response response = pending.transport.send(
                    "POST", pending.gateway.resolve("/auth/external/" + pending.provider + "/callback"), json(body));
                if (response.status() != 204 && response.status() != 202) {
                    throw refusal(response);
                }
                pending.step = response.status() == 204 ? YogabaseExternalSignIn.Step.SIGNED_IN : YogabaseExternalSignIn.Step.MFA;
            }
            if (pending.step == YogabaseExternalSignIn.Step.MFA) {
                if (mfaCode == null) {
                    throw new YogabaseAuthException(
                        "Complete the second authentication factor: call again with the authenticator code.", 202, "mfa_required", null);
                }
                verifyMfa(pending.transport, pending.gateway, mfaCode);
                pending.step = YogabaseExternalSignIn.Step.SIGNED_IN;
            }
            return authorize(pending.transport, pending.gateway, found, pending.clock);
        }
    }

    // ------------------------------------------------------------------------------------------------- the flow

    record Discovery(UUID projectId, String clientId, URI redirectUri, String scope, URI authorizeEndpoint, URI tokenEndpoint) {
    }

    record TokenSet(String accessToken, String refreshToken, long expiresInSeconds) {
    }

    private static Discovery discover(Transport transport, URI gateway, String publishableKey) {
        final JsonObject config = read(transport, gateway.resolve(
            "/v1/client-config/" + URLEncoder.encode(publishableKey, StandardCharsets.UTF_8).replace("+", "%20")));
        final String projectId = string(config, "projectId");
        final String clientId = string(config, "oidcPublicClientId");
        final JsonArray redirects = config.get("oidcRedirectUris") instanceof JsonArray array ? array : null;
        final JsonArray scopes = config.get("oidcScopes") instanceof JsonArray array ? array : null;
        final String redirect = redirects == null || redirects.isEmpty() || !isString(redirects.get(0)) ? null : redirects.get(0).getAsString();
        final StringJoiner scope = new StringJoiner(" ");
        if (scopes != null) {
            for (final JsonElement value : scopes) {
                if (!isString(value)) {
                    throw discoveryInvalid("oidcScopes");
                }
                scope.add(value.getAsString());
            }
        }
        if (clientId == null || clientId.isEmpty()) {
            throw discoveryInvalid("oidcPublicClientId");
        }
        if (redirect == null || tryParse(redirect) == null) {
            throw discoveryInvalid("oidcRedirectUris");
        }
        if (scope.length() == 0) {
            throw discoveryInvalid("oidcScopes");
        }
        final UUID project;
        try {
            project = UUID.fromString(Objects.requireNonNull(projectId));
        } catch (IllegalArgumentException | NullPointerException exception) {
            throw discoveryInvalid("projectId");
        }
        final JsonObject discovery = read(transport, own(gateway, string(config, "oidcDiscoveryEndpoint"), "oidcDiscoveryEndpoint"));
        return new Discovery(
            project,
            clientId,
            tryParse(redirect),
            scope.toString(),
            own(gateway, string(discovery, "authorization_endpoint"), "authorization_endpoint"),
            own(gateway, string(discovery, "token_endpoint"), "token_endpoint"));
    }

    // The identity cookie goes to the endpoints the gateway names, so they must be the gateway's own.
    private static URI own(URI gateway, String value, String name) {
        final URI uri = value == null || value.isEmpty() ? null : tryParse(value);
        if (uri == null || !uri.isAbsolute() || !YogabaseClientOrigin.sameOrigin(uri, gateway)) {
            throw new YogabaseAuthException("Yogabase named " + name + " on another origin, or none.", 502, "oidc_discovery_invalid", null);
        }
        return uri;
    }

    private static YogabaseAuthException discoveryInvalid(String name) {
        return new YogabaseAuthException("Yogabase returned no usable " + name + ".", 502, "oidc_discovery_invalid", null);
    }

    private static void verifyMfa(Transport transport, URI gateway, String code) {
        final JsonObject body = new JsonObject();
        body.addProperty("code", code);
        final Response verify = transport.send("POST", gateway.resolve("/auth/mfa/verify"), json(body));
        if (verify.status() != 204) {
            throw refusal(verify);
        }
    }

    // The code flow with the identity cookie answering the authorization endpoint, whose redirect is read, never followed.
    private static YogabaseSession authorize(Transport transport, URI gateway, Discovery found, Clock clock) {
        final String verifier = random(48);
        final String state = random(32);
        final Map<String, String> query = new LinkedHashMap<>();
        query.put("response_type", "code");
        query.put("client_id", found.clientId());
        query.put("redirect_uri", found.redirectUri().toString());
        query.put("scope", found.scope());
        query.put("code_challenge", s256(verifier));
        query.put("code_challenge_method", "S256");
        query.put("state", state);
        final URI authorizeUri = URI.create(found.authorizeEndpoint() + (found.authorizeEndpoint().getRawQuery() == null ? "?" : "&") + form(query));
        final Response response = transport.send("GET", authorizeUri, null);
        final String location = response.header("Location");
        if (response.status() != 302 || location == null) {
            throw refusal(response); // without the identity cookie this is 401 interactive_login_required
        }
        final URI back = tryParse(location);
        if (back == null) {
            throw new YogabaseAuthException("The authorization response used another redirect URI.", 400, "authorization_redirect_mismatch", null);
        }
        final String code = readRedirect(back.isAbsolute() ? back : found.redirectUri().resolve(back), found.redirectUri(), state);
        final Map<String, String> exchange = new LinkedHashMap<>();
        exchange.put("grant_type", "authorization_code");
        exchange.put("client_id", found.clientId());
        exchange.put("code", code);
        exchange.put("redirect_uri", found.redirectUri().toString());
        exchange.put("code_verifier", verifier);
        final TokenSet tokens = exchange(transport, found.tokenEndpoint(), exchange);
        return new YogabaseSession(gateway, found.projectId(), found.clientId(), found.tokenEndpoint(), transport, clock, tokens);
    }

    /** One token request; also the session's refresh. */
    static TokenSet exchange(Transport transport, URI tokenEndpoint, Map<String, String> fields) {
        final Response response = transport.send("POST", tokenEndpoint, new Body("application/x-www-form-urlencoded", form(fields).getBytes(StandardCharsets.US_ASCII)));
        if (response.status() != 200) {
            throw refusal(response);
        }
        final JsonObject token;
        try {
            token = object(response);
        } catch (YogabaseAuthException exception) {
            throw tokenInvalid();
        }
        final String accessToken = string(token, "access_token");
        final String tokenType = string(token, "token_type");
        final JsonElement expiresIn = token.get("expires_in");
        if (accessToken == null
            || accessToken.isEmpty()
            || tokenType == null
            || !tokenType.equalsIgnoreCase("bearer")
            || expiresIn == null
            || !expiresIn.isJsonPrimitive()
            || !expiresIn.getAsJsonPrimitive().isNumber()
            || expiresIn.getAsDouble() <= 0) {
            throw tokenInvalid();
        }
        final String refreshToken = string(token, "refresh_token");
        return new TokenSet(accessToken, refreshToken == null || refreshToken.isEmpty() ? null : refreshToken, (long) Math.floor(expiresIn.getAsDouble()));
    }

    private static YogabaseAuthException tokenInvalid() {
        return new YogabaseAuthException("Yogabase returned an invalid token response.", 502, "token_response_invalid", null);
    }

    /** The code in what came back to {@code registered}, held to that address and to the {@code state} sent. */
    static String readRedirect(URI back, URI registered, String state) {
        if (!back.isAbsolute()
            || !registered.getScheme().equalsIgnoreCase(back.getScheme())
            || !Objects.equals(lower(registered.getRawAuthority()), lower(back.getRawAuthority()))
            || !Objects.equals(registered.getRawPath(), back.getRawPath())
            || back.getRawUserInfo() != null
            || back.getRawFragment() != null) {
            throw new YogabaseAuthException("The authorization response used another redirect URI.", 400, "authorization_redirect_mismatch", null);
        }
        final Map<String, String> query = new HashMap<>();
        final String raw = back.getRawQuery();
        if (raw != null && !raw.isEmpty()) {
            for (final String pair : raw.split("&", -1)) {
                final int equals = pair.indexOf('=');
                final String name = decode(equals < 0 ? pair : pair.substring(0, equals));
                final String value = equals < 0 ? "" : decode(pair.substring(equals + 1));
                if (query.put(name, value) != null) {
                    throw responseInvalid();
                }
            }
        }
        final String returned = query.getOrDefault("state", "");
        if (!MessageDigest.isEqual(returned.getBytes(StandardCharsets.UTF_8), state.getBytes(StandardCharsets.UTF_8))) {
            throw responseInvalid();
        }
        final String error = query.get("error");
        if (error != null) {
            throw new YogabaseAuthException("The authorization server rejected sign-in.", 400, error.isEmpty() ? "authorization_response_invalid" : error, null);
        }
        final String code = query.get("code");
        if (code == null || code.isEmpty()) {
            throw new YogabaseAuthException("The authorization response lacks a code.", 400, "authorization_response_invalid", null);
        }
        return code;
    }

    private static YogabaseAuthException responseInvalid() {
        return new YogabaseAuthException(
            "The authorization response repeats a parameter or came back with another state.", 400, "authorization_response_invalid", null);
    }

    // ------------------------------------------------------------------------------------------------- reading

    private static JsonObject read(Transport transport, URI uri) {
        final Response response = transport.send("GET", uri, null);
        if (response.status() != 200) {
            throw refusal(response);
        }
        return object(response);
    }

    private static JsonObject object(Response response) {
        try {
            final JsonElement parsed = JsonParser.parseString(new String(response.body(), StandardCharsets.UTF_8));
            if (parsed instanceof JsonObject value) {
                return value;
            }
        } catch (JsonParseException | IllegalStateException exception) {
            // fall through to the refusal below
        }
        throw new YogabaseAuthException("Yogabase returned a document that is not one JSON object.", 502, "response_invalid", response.header("X-Correlation-ID"));
    }

    /** A refusal as the gateway or the token endpoint wrote it: the token endpoint names its code {@code error}, the gateway {@code code}. */
    static YogabaseAuthException refusal(Response response) {
        JsonObject problem = null;
        try {
            if (JsonParser.parseString(new String(response.body(), StandardCharsets.UTF_8)) instanceof JsonObject value) {
                problem = value;
            }
        } catch (JsonParseException | IllegalStateException exception) {
            problem = null;
        }
        final String error = problem == null ? null : string(problem, "error");
        final String code = problem == null ? null : string(problem, "code");
        final String detail = problem == null ? null : string(problem, "detail");
        final String title = problem == null ? null : string(problem, "title");
        final String description = problem == null ? null : string(problem, "error_description");
        String correlation = response.header("X-Correlation-ID");
        if (correlation == null && problem != null) {
            correlation = string(problem, "correlationId") != null ? string(problem, "correlationId") : string(problem, "traceId");
        }
        final String message = detail != null ? detail
            : title != null ? title
            : description != null ? description
            : "Yogabase returned HTTP " + response.status() + ".";
        return new YogabaseAuthException(
            message,
            response.status(),
            error != null ? error : code != null ? code : statusCode(response.status()),
            correlation);
    }

    private static String statusCode(int status) {
        return switch (status) {
            case 401 -> YogabaseStatusErrorCodes.AUTHENTICATION_REQUIRED;
            case 403 -> YogabaseStatusErrorCodes.FORBIDDEN;
            case 404 -> YogabaseStatusErrorCodes.NOT_FOUND;
            case 409 -> YogabaseStatusErrorCodes.CONFLICT;
            case 429 -> YogabaseStatusErrorCodes.RATE_LIMITED;
            default -> YogabaseStatusErrorCodes.REQUEST_FAILED;
        };
    }

    private static String string(JsonObject object, String name) {
        final JsonElement value = object.get(name);
        return isString(value) ? value.getAsString() : null;
    }

    private static boolean isString(JsonElement value) {
        return value != null && value.isJsonPrimitive() && value.getAsJsonPrimitive().isString();
    }

    // ------------------------------------------------------------------------------------------------- helpers

    private static URI requireGateway(URI gateway, boolean allowInsecureLoopback) {
        final URI origin = YogabaseClientOrigin.requireOrigin(gateway, "gateway");
        final boolean secure = origin.getScheme().equalsIgnoreCase("https");
        final boolean loopback = allowInsecureLoopback
            && origin.getScheme().equalsIgnoreCase("http")
            && LOOPBACK.contains(origin.getHost().toLowerCase(Locale.ROOT));
        if (!secure && !loopback) {
            throw new IllegalArgumentException("gateway must use HTTPS (plain http only for a loopback host with allowInsecureLoopback)");
        }
        return URI.create(YogabaseClientOrigin.baseUrl(origin));
    }

    private static void requireKey(String publishableKey) {
        if (publishableKey == null
            || publishableKey.length() < 16
            || publishableKey.length() > 160
            || !publishableKey.startsWith("sy_pub_")
            || !publishableKey.equals(publishableKey.strip())) {
            throw new IllegalArgumentException("publishableKey must be the project's sy_pub_ publishable key");
        }
    }

    private static URI parse(String value, String name) {
        final URI uri = tryParse(value);
        if (uri == null || !uri.isAbsolute()) {
            throw new IllegalArgumentException(name + " must be an absolute URI");
        }
        return uri;
    }

    private static URI tryParse(String value) {
        try {
            return new URI(value);
        } catch (URISyntaxException exception) {
            return null;
        }
    }

    private static String lower(String value) {
        return value == null ? null : value.toLowerCase(Locale.ROOT);
    }

    private static String decode(String value) {
        try {
            return URLDecoder.decode(value, StandardCharsets.UTF_8);
        } catch (IllegalArgumentException exception) {
            throw responseInvalid();
        }
    }

    private static String form(Map<String, String> fields) {
        final StringJoiner joined = new StringJoiner("&");
        for (final Map.Entry<String, String> field : fields.entrySet()) {
            joined.add(URLEncoder.encode(field.getKey(), StandardCharsets.UTF_8) + "=" + URLEncoder.encode(field.getValue(), StandardCharsets.UTF_8));
        }
        return joined.toString();
    }

    private static Body json(JsonObject value) {
        return new Body("application/json", value.toString().getBytes(StandardCharsets.UTF_8));
    }

    static String random(int bytes) {
        final byte[] value = new byte[bytes];
        RANDOM.nextBytes(value);
        return Base64.getUrlEncoder().withoutPadding().encodeToString(value);
    }

    static String s256(String verifier) {
        try {
            return Base64.getUrlEncoder().withoutPadding().encodeToString(
                MessageDigest.getInstance("SHA-256").digest(verifier.getBytes(StandardCharsets.US_ASCII)));
        } catch (NoSuchAlgorithmException exception) {
            throw new IllegalStateException("SHA-256 is unavailable", exception);
        }
    }

    // ------------------------------------------------------------------------------------------------- transport

    record Body(String contentType, byte[] bytes) {
    }

    record Response(int status, Map<String, List<String>> headers, byte[] body) {
        String header(String name) {
            for (final Map.Entry<String, List<String>> header : headers.entrySet()) {
                if (header.getKey() != null && header.getKey().equalsIgnoreCase(name) && !header.getValue().isEmpty()) {
                    return header.getValue().get(0);
                }
            }
            return null;
        }
    }

    /** One request and its whole bounded answer; redirects are never followed and cookies are kept between calls. */
    @FunctionalInterface
    interface Transport {
        Response send(String method, URI uri, Body body);
    }

    private static Transport transport(Options options) {
        final HttpClient client = options.httpClient != null
            ? options.httpClient
            : HttpClient.newBuilder()
                .cookieHandler(new CookieManager())
                .followRedirects(HttpClient.Redirect.NEVER)
                .proxy(ProxySelector.getDefault())
                .connectTimeout(options.timeout)
                .build();
        final Duration timeout = options.timeout;
        return (method, uri, body) -> {
            final HttpRequest.Builder request = HttpRequest.newBuilder(uri).timeout(timeout).header("Accept", "application/json");
            if (body == null) {
                request.method(method, HttpRequest.BodyPublishers.noBody());
            } else {
                request.header("Content-Type", body.contentType()).method(method, HttpRequest.BodyPublishers.ofByteArray(body.bytes()));
            }
            try {
                final HttpResponse<InputStream> response = client.send(request.build(), HttpResponse.BodyHandlers.ofInputStream());
                try (InputStream stream = response.body()) {
                    final byte[] bytes = stream.readNBytes(MAXIMUM_DOCUMENT_BYTES + 1);
                    if (bytes.length > MAXIMUM_DOCUMENT_BYTES) {
                        throw new YogabaseAuthException("Yogabase returned an oversized document.", 502, "response_invalid", null);
                    }
                    return new Response(response.statusCode(), response.headers().map(), bytes);
                }
            } catch (IOException exception) {
                throw new YogabaseAuthException("The gateway could not be reached: " + exception.getMessage(), 503, "gateway_unreachable", null);
            } catch (InterruptedException exception) {
                Thread.currentThread().interrupt();
                throw new YogabaseAuthException("The sign-in was interrupted.", 503, "gateway_unreachable", null);
            }
        };
    }
}
