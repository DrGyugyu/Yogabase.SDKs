package io.yogabase.client;

import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import com.microsoft.kiota.RequestInformation;
import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpServer;
import java.io.IOException;
import java.net.InetAddress;
import java.net.InetSocketAddress;
import java.net.URI;
import java.net.URLDecoder;
import java.net.http.HttpClient;
import java.nio.charset.StandardCharsets;
import java.time.Clock;
import java.time.Duration;
import java.time.Instant;
import java.time.ZoneOffset;
import java.util.ArrayList;
import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import java.util.concurrent.atomic.AtomicReference;

/**
 * Executable checks for the authored Java sign-in ({@link YogabaseSignIn}, {@link YogabaseSession}) against a fake
 * gateway on a loopback port, so the real JDK HTTP client, its cookie handling and its refusal to follow a redirect
 * are what is exercised. The fake answers as the gateway's routes do (the bootstrap document, the discovery
 * document, {@code /auth/login} and {@code /auth/mfa/verify} setting the identity cookie, {@code /connect/authorize}
 * redirecting with the code only when the cookie is sent, {@code /connect/token} checking the PKCE verifier against
 * the challenge, and the external provider's start and callback), and says nothing about the gateway itself.
 */
public final class YogabaseSignInTest {
    private static final String KEY = "sy_pub_0123456789abcdef";
    private static final UUID PROJECT = UUID.fromString("11111111-1111-4111-8111-111111111111");
    private static final String REDIRECT = "io.yogabase.app:/oauth/callback";
    private static final String COOKIE = "yb_identity";
    private static int checks;

    private YogabaseSignInTest() {
    }

    public static void main(String[] args) throws Exception {
        aPasswordSignInRunsTheCodeFlowWithTheIdentityCookieAndPkce();
        theBearerGoesToTheGatewayOriginAndToNoOther();
        aRefusedLoginIsReadByItsCodeAndCorrelation();
        anAccountWithMfaNeedsTheCodeAndThenSignsIn();
        anAuthorizationWithoutTheCookieIsRefusedByTheGatewaysCode();
        aRedirectWithAnotherStateOrAddressOrAnErrorIsRefusedAndNoTokenIsAsked();
        aDiscoveryDocumentOnAnotherOriginEndsTheSignInBeforeTheCredentialsAreSent();
        aTokenEndpointRefusalAndAnInvalidTokenAreNamed();
        anExpiringAccessTokenIsRefreshedWithTheRotatingRefreshToken();
        theGatewayMustBeHttpsUnlessLoopbackIsAllowed();
        anExternalSignInSendsOnlyTheChallengeAndCompletesWithTheVerifier();
        anExternalRedirectWithAnotherStateIsRefusedBeforeTheCodeIsSent();
        anExternalSignInWithMfaDoesNotSendTheSpentCodeAgain();
        anExternalStartThatIsNotAnHttpsAddressAndAStateIsRefused();
        badArgumentsAndAnUnfitHttpClientAreRefused();
        System.out.println("java sign-in tests: PASS (" + checks + " checks)");
    }

    // ----------------------------------------------------------------------------------------------- password

    private static void aPasswordSignInRunsTheCodeFlowWithTheIdentityCookieAndPkce() throws Exception {
        try (FakeGateway gateway = FakeGateway.start()) {
            final YogabaseSession session = YogabaseSignIn.signInWithPassword(
                gateway.origin(), KEY, "user@example.test", "correct horse", loopback());

            check(session.projectId().equals(PROJECT), "the session names the bootstrap's project");
            check(session.accessToken().equals("access-1"), "the session holds the access token");
            check(session.canRefresh(), "the session holds the refresh token");
            check(session.gatewayOrigin().equals(gateway.origin()), "the session is bound to the gateway origin");
            check(gateway.paths().equals(List.of(
                "GET /v1/client-config/" + KEY,
                "GET /.well-known/openid-configuration",
                "POST /auth/login",
                "GET /connect/authorize",
                "POST /connect/token")), "the calls are the bootstrap, discovery, login, authorize and token: " + gateway.paths());
            final JsonObject login = JsonParser.parseString(gateway.body("POST /auth/login")).getAsJsonObject();
            check(login.get("email").getAsString().equals("user@example.test") && login.get("password").getAsString().equals("correct horse")
                && login.size() == 2, "the login body is the e-mail and the password");
            final Map<String, String> authorize = parseQuery(gateway.query("GET /connect/authorize"));
            check(authorize.get("response_type").equals("code"), "authorize asks for a code");
            check(authorize.get("client_id").equals("yogabase-native-preview"), "authorize names the public client");
            check(authorize.get("redirect_uri").equals(REDIRECT), "authorize names the first registered redirect");
            check(authorize.get("scope").equals("openid offline_access"), "authorize asks for the bootstrap's scopes");
            check(authorize.get("code_challenge_method").equals("S256"), "authorize uses S256");
            check(authorize.get("state").length() >= 43, "the state is random");
            check(gateway.cookieAt("GET /connect/authorize").contains(COOKIE + "=identity-1"), "the identity cookie answers authorize");
            final Map<String, String> token = parseQuery(gateway.body("POST /connect/token"));
            check(token.get("grant_type").equals("authorization_code"), "the code is exchanged");
            check(token.get("code").equals("code-1"), "the code from the redirect is exchanged");
            check(token.get("redirect_uri").equals(REDIRECT), "the exchange names the same redirect");
            check(YogabaseSignIn.s256(token.get("code_verifier")).equals(authorize.get("code_challenge")), "the verifier hashes to the challenge");
            check(gateway.contentType("POST /connect/token").startsWith("application/x-www-form-urlencoded"), "the token request is a form");
        }
    }

    private static void theBearerGoesToTheGatewayOriginAndToNoOther() throws Exception {
        try (FakeGateway gateway = FakeGateway.start()) {
            final YogabaseSession session = YogabaseSignIn.signInWithPassword(gateway.origin(), KEY, "u@example.test", "pw", loopback());
            check(session.getAuthorizationToken(URI.create(gateway.origin() + "/v1/data/notes/select"), null).equals("access-1"), "the gateway gets the bearer");
            check(session.getAuthorizationToken(URI.create("http://127.0.0.2:" + gateway.origin().getPort() + "/v1"), null).isEmpty(), "another host does not");
            check(session.getAuthorizationToken(URI.create("https://127.0.0.1:" + gateway.origin().getPort() + "/v1"), null).isEmpty(), "another scheme does not");
            check(session.getAuthorizationToken(URI.create("http://127.0.0.1:1/v1"), null).isEmpty(), "another port does not");
            final RequestInformation information = new RequestInformation();
            information.urlTemplate = gateway.origin() + "/v1/servers";
            session.authenticationProvider().authenticateRequest(information, null);
            check(information.headers.get("Authorization").contains("Bearer access-1"), "the Kiota provider adds the bearer for the gateway");
            final YogabaseApplicationClient application = YogabaseApplicationClient.forSession(session);
            check(application.origin().equals(gateway.origin()), "the session's application client is bound to its gateway");
            check(application.requestAdapter().getBaseUrl().equals(gateway.origin().toString()), "and its adapter's base URL is the gateway");
        }
    }

    private static void aRefusedLoginIsReadByItsCodeAndCorrelation() throws Exception {
        try (FakeGateway gateway = FakeGateway.start()) {
            gateway.loginStatus = 401;
            final YogabaseAuthException refused = expect(() -> YogabaseSignIn.signInWithPassword(gateway.origin(), KEY, "u@example.test", "wrong", loopback()));
            check(refused.status() == 401 && refused.code().equals("invalid_credentials"), "a refused login is 401 invalid_credentials");
            check("corr-401".equals(refused.correlationId()), "the correlation id is the gateway's header");
            check(refused.getMessage().equals("The credentials were not accepted."), "the message is the problem's detail");
            check(!gateway.paths().contains("GET /connect/authorize"), "nothing is authorized after a refused login");
        }
    }

    private static void anAccountWithMfaNeedsTheCodeAndThenSignsIn() throws Exception {
        try (FakeGateway gateway = FakeGateway.start()) {
            gateway.mfa = true;
            final YogabaseAuthException pending = expect(() -> YogabaseSignIn.signInWithPassword(gateway.origin(), KEY, "u@example.test", "pw", loopback()));
            check(pending.status() == 202 && pending.code().equals("mfa_required"), "an account with MFA is 202 mfa_required");
            final YogabaseSession session = YogabaseSignIn.signInWithPassword(gateway.origin(), KEY, "u@example.test", "pw", loopback().mfaCode("123456"));
            check(session.accessToken().equals("access-1"), "with the code the sign-in completes");
            check(gateway.body("POST /auth/mfa/verify").equals("{\"code\":\"123456\"}"), "the code goes to /auth/mfa/verify");
            check(gateway.cookieAt("POST /auth/mfa/verify").contains("yb_two_factor=pending"), "the second factor carries the login's cookie");
        }
    }

    private static void anAuthorizationWithoutTheCookieIsRefusedByTheGatewaysCode() throws Exception {
        try (FakeGateway gateway = FakeGateway.start()) {
            gateway.setCookie = false;
            final YogabaseAuthException refused = expect(() -> YogabaseSignIn.signInWithPassword(gateway.origin(), KEY, "u@example.test", "pw", loopback()));
            check(refused.status() == 401 && refused.code().equals("interactive_login_required"), "authorize without the cookie is interactive_login_required");
            check(!gateway.paths().contains("POST /connect/token"), "no token is asked without a code");
        }
    }

    private static void aRedirectWithAnotherStateOrAddressOrAnErrorIsRefusedAndNoTokenIsAsked() throws Exception {
        final Object[][] cases = {
            {"state", "authorization_response_invalid"},
            {"address", "authorization_redirect_mismatch"},
            {"error", "access_denied"},
            {"repeat", "authorization_response_invalid"},
            {"nocode", "authorization_response_invalid"},
            {"fragment", "authorization_redirect_mismatch"},
        };
        for (final Object[] item : cases) {
            try (FakeGateway gateway = FakeGateway.start()) {
                gateway.redirectFault = (String) item[0];
                final YogabaseAuthException refused = expect(() -> YogabaseSignIn.signInWithPassword(gateway.origin(), KEY, "u@example.test", "pw", loopback()));
                check(refused.code().equals(item[1]), "a redirect with " + item[0] + " is " + item[1] + ", was " + refused.code());
                check(!gateway.paths().contains("POST /connect/token"), "no token is asked after a redirect with " + item[0]);
            }
        }
    }

    private static void aDiscoveryDocumentOnAnotherOriginEndsTheSignInBeforeTheCredentialsAreSent() throws Exception {
        for (final String field : List.of("oidcDiscoveryEndpoint", "authorization_endpoint", "token_endpoint")) {
            try (FakeGateway gateway = FakeGateway.start()) {
                gateway.foreignField = field;
                final YogabaseAuthException refused = expect(() -> YogabaseSignIn.signInWithPassword(gateway.origin(), KEY, "u@example.test", "pw", loopback()));
                check(refused.status() == 502 && refused.code().equals("oidc_discovery_invalid"), field + " on another origin is oidc_discovery_invalid");
                check(!gateway.paths().contains("POST /auth/login"), "the credentials are not sent when " + field + " is foreign");
            }
        }
    }

    private static void aTokenEndpointRefusalAndAnInvalidTokenAreNamed() throws Exception {
        try (FakeGateway gateway = FakeGateway.start()) {
            gateway.tokenFault = "refused";
            final YogabaseAuthException refused = expect(() -> YogabaseSignIn.signInWithPassword(gateway.origin(), KEY, "u@example.test", "pw", loopback()));
            check(refused.status() == 400 && refused.code().equals("invalid_grant"), "the token endpoint's error is the code");
        }
        for (final String fault : List.of("type", "expiry", "token", "html")) {
            try (FakeGateway gateway = FakeGateway.start()) {
                gateway.tokenFault = fault;
                final YogabaseAuthException refused = expect(() -> YogabaseSignIn.signInWithPassword(gateway.origin(), KEY, "u@example.test", "pw", loopback()));
                check(refused.status() == 502 && refused.code().equals("token_response_invalid"), "a token answer with a bad " + fault + " is token_response_invalid");
            }
        }
    }

    private static void anExpiringAccessTokenIsRefreshedWithTheRotatingRefreshToken() throws Exception {
        try (FakeGateway gateway = FakeGateway.start()) {
            final MovableClock clock = new MovableClock(Instant.parse("2026-10-20T00:00:00Z"));
            final YogabaseSession session = YogabaseSignIn.signInWithPassword(gateway.origin(), KEY, "u@example.test", "pw", loopback().clock(clock));
            check(session.accessToken().equals("access-1"), "a fresh token is used as it is");
            check(gateway.count("POST /connect/token") == 1, "no refresh while the token is fresh");
            clock.advance(Duration.ofSeconds(3600 - 59));
            check(session.accessToken().equals("access-2"), "a token within a minute of expiry is refreshed");
            final Map<String, String> refresh = parseQuery(gateway.bodies("POST /connect/token").get(1));
            check(refresh.get("grant_type").equals("refresh_token") && refresh.get("refresh_token").equals("refresh-1")
                && refresh.get("client_id").equals("yogabase-native-preview"), "the refresh spends the refresh token held");
            clock.advance(Duration.ofSeconds(3600));
            check(session.accessToken().equals("access-3"), "the next refresh spends the rotated token");
            check(parseQuery(gateway.bodies("POST /connect/token").get(2)).get("refresh_token").equals("refresh-2"), "the rotated refresh token is the one spent");
        }
    }

    private static void theGatewayMustBeHttpsUnlessLoopbackIsAllowed() {
        expectArgument(() -> YogabaseSignIn.signInWithPassword(URI.create("http://127.0.0.1:1"), KEY, "u", "p"), "plain http needs the loopback option");
        expectArgument(() -> YogabaseSignIn.signInWithPassword(URI.create("http://example.test"), KEY, "u", "p", loopback()), "plain http to another host is refused");
        expectArgument(() -> YogabaseSignIn.signInWithPassword(URI.create("https://example.test/base"), KEY, "u", "p"), "a gateway with a path is refused");
        expectArgument(() -> YogabaseSignIn.signInWithPassword(URI.create("https://example.test"), "pk_live_1234567890", "u", "p"), "a key that is not sy_pub_ is refused");
    }

    // ----------------------------------------------------------------------------------------------- external

    private static void anExternalSignInSendsOnlyTheChallengeAndCompletesWithTheVerifier() throws Exception {
        try (FakeGateway gateway = FakeGateway.start()) {
            final YogabaseExternalSignIn pending = YogabaseSignIn.startExternalSignIn(gateway.origin(), KEY, "google", REDIRECT, loopback());
            check(pending.authorizationUrl().equals("https://accounts.example.test/o/oauth2/auth?state=external-state"), "the start answers the provider's address");
            final JsonObject start = JsonParser.parseString(gateway.body("POST /auth/external/google/start")).getAsJsonObject();
            check(start.get("redirectUri").getAsString().equals(REDIRECT) && start.size() == 2, "the start sends the redirect and the challenge only");
            final String challenge = start.get("codeChallenge").getAsString();
            check(challenge.length() == 43, "the challenge is an S256 challenge");

            final YogabaseSession session = YogabaseSignIn.completeExternalSignIn(pending, URI.create(REDIRECT + "?code=provider-code&state=external-state"), null);

            final JsonObject callback = JsonParser.parseString(gateway.body("POST /auth/external/google/callback")).getAsJsonObject();
            check(callback.get("code").getAsString().equals("provider-code"), "the callback carries the provider's code");
            check(callback.get("state").getAsString().equals("external-state"), "the callback carries the state");
            check(YogabaseSignIn.s256(callback.get("codeVerifier").getAsString()).equals(challenge), "the callback carries the verifier of the challenge");
            check(gateway.cookieAt("POST /auth/external/google/callback").contains("yb_external=correlation"), "the start's cookie goes back with the callback");
            check(gateway.cookieAt("GET /connect/authorize").contains(COOKIE + "=identity-1"), "the callback's identity cookie answers authorize");
            check(session.accessToken().equals("access-1"), "the external sign-in ends with a session");
        }
    }

    private static void anExternalRedirectWithAnotherStateIsRefusedBeforeTheCodeIsSent() throws Exception {
        try (FakeGateway gateway = FakeGateway.start()) {
            final YogabaseExternalSignIn pending = YogabaseSignIn.startExternalSignIn(gateway.origin(), KEY, "apple", REDIRECT, loopback());
            final int before = gateway.paths().size();
            final YogabaseAuthException state = expect(() -> YogabaseSignIn.completeExternalSignIn(pending, URI.create(REDIRECT + "?code=c&state=other"), null));
            check(state.code().equals("authorization_response_invalid"), "another state is refused");
            final YogabaseAuthException error = expect(() -> YogabaseSignIn.completeExternalSignIn(pending, URI.create(REDIRECT + "?error=access_denied&state=external-state"), null));
            check(error.code().equals("access_denied"), "an error is refused by its name");
            final YogabaseAuthException address = expect(() -> YogabaseSignIn.completeExternalSignIn(pending, URI.create("https://evil.example.test/oauth/callback?code=c&state=external-state"), null));
            check(address.code().equals("authorization_redirect_mismatch"), "another address is refused");
            check(gateway.paths().size() == before, "nothing is sent for a redirect that is not this sign-in's");
        }
    }

    private static void anExternalSignInWithMfaDoesNotSendTheSpentCodeAgain() throws Exception {
        try (FakeGateway gateway = FakeGateway.start()) {
            gateway.mfa = true;
            final YogabaseExternalSignIn pending = YogabaseSignIn.startExternalSignIn(gateway.origin(), KEY, "microsoft", REDIRECT, loopback());
            final URI back = URI.create(REDIRECT + "?code=provider-code&state=external-state");
            final YogabaseAuthException mfa = expect(() -> YogabaseSignIn.completeExternalSignIn(pending, back, null));
            check(mfa.status() == 202 && mfa.code().equals("mfa_required"), "an external account with MFA is mfa_required");
            final YogabaseSession session = YogabaseSignIn.completeExternalSignIn(pending, back, "654321");
            check(gateway.count("POST /auth/external/microsoft/callback") == 1, "the spent code is not sent again");
            check(gateway.body("POST /auth/mfa/verify").equals("{\"code\":\"654321\"}"), "the code goes to /auth/mfa/verify");
            check(gateway.cookieAt("POST /auth/mfa/verify").contains("yb_two_factor=pending"), "the callback's two-factor cookie answers the verify");
            check(session.accessToken().equals("access-1"), "the second call completes the sign-in");
        }
    }

    private static void anExternalStartThatIsNotAnHttpsAddressAndAStateIsRefused() throws Exception {
        for (final String fault : List.of("http", "nostate", "relative")) {
            try (FakeGateway gateway = FakeGateway.start()) {
                gateway.startFault = fault;
                final YogabaseAuthException refused = expect(() -> YogabaseSignIn.startExternalSignIn(gateway.origin(), KEY, "google", REDIRECT, loopback()));
                check(refused.status() == 502 && refused.code().equals("external_start_invalid"), "a start with " + fault + " is external_start_invalid");
            }
        }
        try (FakeGateway gateway = FakeGateway.start()) {
            gateway.startFault = "unregistered";
            final YogabaseAuthException refused = expect(() -> YogabaseSignIn.startExternalSignIn(gateway.origin(), KEY, "google", "https://app.example.test/cb", loopback()));
            check(refused.status() == 400 && refused.code().equals("external_redirect_uri_invalid"), "an unregistered redirect is the gateway's refusal");
        }
    }

    private static void badArgumentsAndAnUnfitHttpClientAreRefused() {
        expectArgument(() -> YogabaseSignIn.startExternalSignIn(URI.create("https://example.test"), KEY, "Google", REDIRECT), "an uppercase provider id is refused");
        expectArgument(() -> YogabaseSignIn.startExternalSignIn(URI.create("https://example.test"), KEY, "a/b", REDIRECT), "a provider id with a slash is refused");
        expectArgument(() -> new YogabaseSignIn.Options().httpClient(HttpClient.newBuilder().followRedirects(HttpClient.Redirect.NORMAL).build()), "a client that follows redirects is refused");
        expectArgument(() -> new YogabaseSignIn.Options().httpClient(HttpClient.newHttpClient()), "a client without cookies is refused");
        expectArgument(() -> new YogabaseSignIn.Options().timeout(Duration.ZERO), "a zero timeout is refused");
    }

    // ----------------------------------------------------------------------------------------------- fake gateway

    private static YogabaseSignIn.Options loopback() {
        return new YogabaseSignIn.Options().allowInsecureLoopback(true);
    }

    private static final class FakeGateway implements AutoCloseable {
        private final HttpServer server;
        private final URI origin;
        private final List<String[]> calls = Collections.synchronizedList(new ArrayList<>());
        private final AtomicReference<String> challenge = new AtomicReference<>();
        private final AtomicReference<String> externalChallenge = new AtomicReference<>();
        private int tokens;
        int loginStatus = 204;
        boolean mfa;
        boolean setCookie = true;
        String redirectFault = "";
        String foreignField = "";
        String tokenFault = "";
        String startFault = "";

        private FakeGateway(HttpServer server) {
            this.server = server;
            this.origin = URI.create("http://127.0.0.1:" + server.getAddress().getPort());
        }

        static FakeGateway start() throws IOException {
            final HttpServer server = HttpServer.create(new InetSocketAddress(InetAddress.getLoopbackAddress(), 0), 0);
            final FakeGateway gateway = new FakeGateway(server);
            server.createContext("/", gateway::handle);
            server.start();
            return gateway;
        }

        URI origin() {
            return origin;
        }

        List<String> paths() {
            synchronized (calls) {
                return calls.stream().map(call -> call[0]).toList();
            }
        }

        int count(String path) {
            return (int) paths().stream().filter(path::equals).count();
        }

        private String[] last(String path) {
            synchronized (calls) {
                for (int i = calls.size() - 1; i >= 0; i--) {
                    if (calls.get(i)[0].equals(path)) {
                        return calls.get(i);
                    }
                }
            }
            throw new AssertionError("no call to " + path + " in " + paths());
        }

        List<String> bodies(String path) {
            synchronized (calls) {
                return calls.stream().filter(call -> call[0].equals(path)).map(call -> call[2]).toList();
            }
        }

        String body(String path) {
            return last(path)[2];
        }

        String query(String path) {
            return last(path)[1];
        }

        String cookieAt(String path) {
            return last(path)[3];
        }

        String contentType(String path) {
            return last(path)[4];
        }

        private void handle(HttpExchange exchange) throws IOException {
            final String method = exchange.getRequestMethod();
            final String path = exchange.getRequestURI().getRawPath();
            final String query = exchange.getRequestURI().getRawQuery();
            final String body = new String(exchange.getRequestBody().readAllBytes(), StandardCharsets.UTF_8);
            final String cookie = String.join("; ", exchange.getRequestHeaders().getOrDefault("Cookie", List.of()));
            final String contentType = exchange.getRequestHeaders().getFirst("Content-Type");
            calls.add(new String[] {method + " " + path, query == null ? "" : query, body, cookie, contentType == null ? "" : contentType});
            try {
                route(exchange, method + " " + path, query, body, cookie);
            } finally {
                exchange.close();
            }
        }

        private void route(HttpExchange exchange, String call, String query, String body, String cookie) throws IOException {
            final String self = origin.toString();
            switch (call) {
                case "GET /v1/client-config/" + KEY -> json(exchange, 200, "{\"schema\":\"yogabase-client-config.v1\",\"projectId\":\"" + PROJECT + "\","
                    + "\"environmentId\":\"22222222-2222-4222-8222-222222222222\",\"gatewayEndpoint\":\"" + self + "/\","
                    + "\"oidcAuthority\":\"" + self + "/p/" + PROJECT + "\","
                    + "\"oidcDiscoveryEndpoint\":\"" + ("oidcDiscoveryEndpoint".equals(foreignField) ? "http://127.0.0.2:1" : self) + "/.well-known/openid-configuration\","
                    + "\"oidcPublicClientId\":\"yogabase-native-preview\",\"oidcScopes\":[\"openid\",\"offline_access\"],"
                    + "\"oidcRedirectUris\":[\"" + REDIRECT + "\",\"https://app.example.test/cb\"],\"capabilities\":[]}");
                case "GET /.well-known/openid-configuration" -> json(exchange, 200, "{\"issuer\":\"" + self + "/p/" + PROJECT + "\","
                    + "\"authorization_endpoint\":\"" + ("authorization_endpoint".equals(foreignField) ? "https://evil.example.test" : self) + "/connect/authorize\","
                    + "\"token_endpoint\":\"" + ("token_endpoint".equals(foreignField) ? "http://127.0.0.1:1" : self) + "/connect/token\"}");
                case "POST /auth/login" -> {
                    if (loginStatus != 204) {
                        exchange.getResponseHeaders().add("X-Correlation-ID", "corr-401");
                        problem(exchange, loginStatus, "invalid_credentials", "Invalid credentials", "The credentials were not accepted.");
                    } else if (mfa) {
                        exchange.getResponseHeaders().add("Set-Cookie", "yb_two_factor=pending; path=/; httponly");
                        problem(exchange, 202, "mfa_required", "MFA required", "Complete the second authentication factor.");
                    } else {
                        identify(exchange);
                    }
                }
                case "POST /auth/mfa/verify" -> {
                    if (!cookie.contains("yb_two_factor=pending")) {
                        problem(exchange, 400, "mfa_challenge_missing", "MFA failed", "No second factor is pending.");
                    } else {
                        identify(exchange);
                    }
                }
                case "GET /connect/authorize" -> authorize(exchange, query, cookie);
                case "POST /connect/token" -> token(exchange, body);
                case "POST /auth/external/google/start", "POST /auth/external/apple/start", "POST /auth/external/microsoft/start" -> {
                    final JsonObject start = JsonParser.parseString(body).getAsJsonObject();
                    if ("unregistered".equals(startFault)) {
                        problem(exchange, 400, "external_redirect_uri_invalid", "External sign-in failed", "The redirect URI is not registered for this project.");
                        return;
                    }
                    externalChallenge.set(start.get("codeChallenge").getAsString());
                    exchange.getResponseHeaders().add("Set-Cookie", "yb_external=correlation; path=/; httponly");
                    switch (startFault) {
                        case "http" -> json(exchange, 200, "{\"authorizationUrl\":\"http://accounts.example.test/auth\",\"state\":\"external-state\"}");
                        case "nostate" -> json(exchange, 200, "{\"authorizationUrl\":\"https://accounts.example.test/auth\",\"state\":\"\"}");
                        case "relative" -> json(exchange, 200, "{\"authorizationUrl\":\"/auth\",\"state\":\"external-state\"}");
                        default -> json(exchange, 200, "{\"authorizationUrl\":\"https://accounts.example.test/o/oauth2/auth?state=external-state\",\"state\":\"external-state\"}");
                    }
                }
                case "POST /auth/external/google/callback", "POST /auth/external/apple/callback", "POST /auth/external/microsoft/callback" -> {
                    final JsonObject callback = JsonParser.parseString(body).getAsJsonObject();
                    if (!YogabaseSignIn.s256(callback.get("codeVerifier").getAsString()).equals(externalChallenge.get())
                        || !cookie.contains("yb_external=correlation")) {
                        problem(exchange, 401, "external_identity_invalid", "External sign-in failed", "The external sign-in response is invalid.");
                    } else if (mfa) {
                        exchange.getResponseHeaders().add("Set-Cookie", "yb_two_factor=pending; path=/; httponly");
                        problem(exchange, 202, "mfa_required", "MFA required", "Complete the second authentication factor.");
                    } else {
                        identify(exchange);
                    }
                }
                default -> problem(exchange, 404, "not_found", "Not found", "No such route: " + call);
            }
        }

        private void identify(HttpExchange exchange) throws IOException {
            if (setCookie) {
                exchange.getResponseHeaders().add("Set-Cookie", COOKIE + "=identity-1; path=/; samesite=lax; httponly");
            }
            exchange.sendResponseHeaders(204, -1);
        }

        private void authorize(HttpExchange exchange, String rawQuery, String cookie) throws IOException {
            if (!cookie.contains(COOKIE + "=identity-1")) {
                problem(exchange, 401, "interactive_login_required", "Sign in required", "Sign in before authorizing.");
                return;
            }
            final Map<String, String> query = parseQuery(rawQuery);
            challenge.set(query.get("code_challenge"));
            final String state = query.get("state");
            final String location = switch (redirectFault) {
                case "state" -> REDIRECT + "?code=code-1&state=another";
                case "address" -> "io.yogabase.other:/oauth/callback?code=code-1&state=" + state;
                case "error" -> REDIRECT + "?error=access_denied&state=" + state;
                case "repeat" -> REDIRECT + "?code=code-1&code=code-2&state=" + state;
                case "nocode" -> REDIRECT + "?state=" + state;
                case "fragment" -> REDIRECT + "?code=code-1&state=" + state + "#x";
                default -> REDIRECT + "?code=code-1&state=" + state;
            };
            exchange.getResponseHeaders().add("Location", location);
            exchange.sendResponseHeaders(302, -1);
        }

        private synchronized void token(HttpExchange exchange, String body) throws IOException {
            final Map<String, String> form = parseQuery(body);
            if ("refused".equals(tokenFault)) {
                json(exchange, 400, "{\"error\":\"invalid_grant\",\"error_description\":\"The authorization code is no longer valid.\"}");
                return;
            }
            if (form.get("grant_type").equals("authorization_code")
                && !YogabaseSignIn.s256(form.get("code_verifier")).equals(challenge.get())) {
                json(exchange, 400, "{\"error\":\"invalid_grant\"}");
                return;
            }
            tokens++;
            switch (tokenFault) {
                case "type" -> json(exchange, 200, "{\"access_token\":\"a\",\"token_type\":\"mac\",\"expires_in\":3600}");
                case "expiry" -> json(exchange, 200, "{\"access_token\":\"a\",\"token_type\":\"Bearer\",\"expires_in\":0}");
                case "token" -> json(exchange, 200, "{\"access_token\":\"\",\"token_type\":\"Bearer\",\"expires_in\":3600}");
                case "html" -> json(exchange, 200, "<html></html>");
                default -> json(exchange, 200, "{\"access_token\":\"access-" + tokens + "\",\"token_type\":\"Bearer\",\"expires_in\":3600,"
                    + "\"refresh_token\":\"refresh-" + tokens + "\",\"scope\":\"openid offline_access\"}");
            }
        }

        private static void problem(HttpExchange exchange, int status, String code, String title, String detail) throws IOException {
            exchange.getResponseHeaders().add("Content-Type", "application/problem+json");
            final byte[] bytes = ("{\"type\":\"https://yogabase.io/problems/" + code.replace('_', '-') + "\",\"title\":\"" + title + "\",\"status\":" + status
                + ",\"detail\":\"" + detail + "\",\"code\":\"" + code + "\",\"correlationId\":\"trace-1\"}").getBytes(StandardCharsets.UTF_8);
            exchange.sendResponseHeaders(status, bytes.length);
            exchange.getResponseBody().write(bytes);
        }

        private static void json(HttpExchange exchange, int status, String json) throws IOException {
            exchange.getResponseHeaders().add("Content-Type", "application/json");
            final byte[] bytes = json.getBytes(StandardCharsets.UTF_8);
            exchange.sendResponseHeaders(status, bytes.length);
            exchange.getResponseBody().write(bytes);
        }

        @Override
        public void close() {
            server.stop(0);
        }
    }

    private static final class MovableClock extends Clock {
        private Instant now;

        MovableClock(Instant now) {
            this.now = now;
        }

        void advance(Duration by) {
            now = now.plus(by);
        }

        @Override
        public ZoneOffset getZone() {
            return ZoneOffset.UTC;
        }

        @Override
        public Clock withZone(java.time.ZoneId zone) {
            return this;
        }

        @Override
        public Instant instant() {
            return now;
        }
    }

    private static Map<String, String> parseQuery(String raw) {
        final Map<String, String> values = new HashMap<>();
        if (raw == null || raw.isEmpty()) {
            return values;
        }
        for (final String pair : raw.split("&")) {
            final int equals = pair.indexOf('=');
            values.put(URLDecoder.decode(pair.substring(0, equals), StandardCharsets.UTF_8), URLDecoder.decode(pair.substring(equals + 1), StandardCharsets.UTF_8));
        }
        return values;
    }

    @FunctionalInterface
    private interface Call {
        Object run() throws Exception;
    }

    private static YogabaseAuthException expect(Call call) throws Exception {
        try {
            call.run();
        } catch (YogabaseAuthException expected) {
            checks++;
            return expected;
        }
        throw new AssertionError("expected a YogabaseAuthException");
    }

    private static void expectArgument(Call call, String name) {
        try {
            call.run();
        } catch (IllegalArgumentException expected) {
            checks++;
            return;
        } catch (Exception other) {
            throw new AssertionError(name + ": expected IllegalArgumentException, got " + other, other);
        }
        throw new AssertionError(name + ": expected IllegalArgumentException");
    }

    private static void check(boolean condition, String name) {
        if (!condition) {
            throw new AssertionError(name);
        }
        checks++;
    }
}
