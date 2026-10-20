package io.yogabase.client;

import java.net.URI;
import java.time.Clock;

/**
 * A started external sign-in, from {@link YogabaseSignIn#startExternalSignIn}: open {@link #authorizationUrl()} in
 * the system browser, then pass the address the browser is redirected to, with this object, to
 * {@link YogabaseSignIn#completeExternalSignIn}. It holds the PKCE verifier, the state and the cookies the gateway
 * set, none of which is readable from outside, and it is spent once the completion has signed the user in.
 */
public final class YogabaseExternalSignIn {
    enum Step { MFA, SIGNED_IN }

    private final String authorizationUrl;
    final URI gateway;
    final String publishableKey;
    final String provider;
    final URI redirectUri;
    final String state;
    final String verifier;
    final YogabaseSignIn.Transport transport;
    final Clock clock;
    Step step;

    YogabaseExternalSignIn(
        String authorizationUrl,
        URI gateway,
        String publishableKey,
        String provider,
        URI redirectUri,
        String state,
        String verifier,
        YogabaseSignIn.Transport transport,
        Clock clock) {
        this.authorizationUrl = authorizationUrl;
        this.gateway = gateway;
        this.publishableKey = publishableKey;
        this.provider = provider;
        this.redirectUri = redirectUri;
        this.state = state;
        this.verifier = verifier;
        this.transport = transport;
        this.clock = clock;
    }

    /** The provider's address to open in the system browser. */
    public String authorizationUrl() {
        return authorizationUrl;
    }

    @Override
    public String toString() {
        return "YogabaseExternalSignIn[" + provider + "]";
    }
}
