package io.yogabase.client;

import com.microsoft.kiota.authentication.AccessTokenProvider;
import com.microsoft.kiota.authentication.AllowedHostsValidator;
import com.microsoft.kiota.authentication.AuthenticationProvider;
import com.microsoft.kiota.authentication.BaseBearerTokenAuthenticationProvider;
import java.net.URI;
import java.time.Clock;
import java.time.Duration;
import java.time.Instant;
import java.util.Map;
import java.util.Objects;
import java.util.UUID;

/**
 * A signed-in project user's tokens, from {@link YogabaseSignIn}. It is the Kiota {@link AccessTokenProvider} of the
 * application client: wrap it with {@link #authenticationProvider()} for an {@code OkHttpRequestAdapter}, and the
 * bearer goes to the gateway origin the user signed in at and to no other. The access token is refreshed with the
 * rotating refresh token when it expires within a minute, once for concurrent callers.
 */
public final class YogabaseSession implements AccessTokenProvider {
    private static final Duration REFRESH_MARGIN = Duration.ofSeconds(60);

    private final URI gatewayOrigin;
    private final UUID projectId;
    private final String clientId;
    private final URI tokenEndpoint;
    private final YogabaseSignIn.Transport transport;
    private final Clock clock;
    private final AllowedHostsValidator allowedHosts;
    private String accessToken;
    private String refreshToken;
    private Instant expiresAt;

    YogabaseSession(
        URI gatewayOrigin,
        UUID projectId,
        String clientId,
        URI tokenEndpoint,
        YogabaseSignIn.Transport transport,
        Clock clock,
        YogabaseSignIn.TokenSet tokens) {
        this.gatewayOrigin = gatewayOrigin;
        this.projectId = projectId;
        this.clientId = clientId;
        this.tokenEndpoint = tokenEndpoint;
        this.transport = transport;
        this.clock = clock;
        this.allowedHosts = new AllowedHostsValidator(gatewayOrigin.getHost());
        apply(tokens);
    }

    /** The gateway origin the user signed in at, the only origin the bearer is sent to. */
    public URI gatewayOrigin() {
        return gatewayOrigin;
    }

    /** The project's id from its client configuration, which the Jobs and schedules clients take. */
    public UUID projectId() {
        return projectId;
    }

    /** The access token, refreshed first when it expires within a minute and a refresh token is held. */
    public synchronized String accessToken() {
        if (refreshToken != null && clock.instant().isAfter(expiresAt.minus(REFRESH_MARGIN))) {
            apply(YogabaseSignIn.exchange(transport, tokenEndpoint, Map.of(
                "grant_type", "refresh_token",
                "client_id", clientId,
                "refresh_token", refreshToken)));
        }
        return accessToken;
    }

    /** Whether a refresh token is held, so the session outlives its access token. */
    public synchronized boolean canRefresh() {
        return refreshToken != null;
    }

    /** When the access token held now expires. */
    public synchronized Instant expiresAt() {
        return expiresAt;
    }

    /** The bearer for a request to the gateway origin, and an empty string for any other, which Kiota then sends without one. */
    @Override
    public String getAuthorizationToken(URI uri, Map<String, Object> additionalAuthenticationContext) {
        return uri != null && YogabaseClientOrigin.sameOrigin(uri, gatewayOrigin) ? accessToken() : "";
    }

    @Override
    public AllowedHostsValidator getAllowedHostsValidator() {
        return allowedHosts;
    }

    /** This session as the Kiota authentication provider an {@code OkHttpRequestAdapter} takes. */
    public AuthenticationProvider authenticationProvider() {
        return new BaseBearerTokenAuthenticationProvider(this);
    }

    private void apply(YogabaseSignIn.TokenSet tokens) {
        Objects.requireNonNull(tokens, "tokens");
        accessToken = tokens.accessToken();
        // The refresh token rotates: a refresh spends the one held, and the answer carries its successor or none.
        refreshToken = tokens.refreshToken();
        expiresAt = clock.instant().plusSeconds(tokens.expiresInSeconds());
    }
}
