package io.yogabase.client;

/**
 * A refused sign-in or token request, with the gateway's {@link #status()} and {@link #code()}:
 * {@code invalid_credentials} (401: a wrong password and an unknown, unconfirmed or locked account alike),
 * {@code mfa_enrollment_required} (403), {@code authentication_rate_limited} (429), {@code mfa_required} (202: call
 * again with the authenticator code), {@code interactive_login_required} (401), an OAuth error of the token endpoint
 * such as {@code invalid_grant}, or one this client names itself for an answer that is not this call's:
 * {@code authorization_response_invalid}, {@code authorization_redirect_mismatch}, {@code oidc_discovery_invalid},
 * {@code token_response_invalid}, {@code external_start_invalid}. An external sign-in adds the gateway's
 * {@code external_*} codes. {@link #correlationId()} is the gateway's {@code X-Correlation-ID} to quote.
 */
public final class YogabaseAuthException extends YogabaseGatewayException {
    private static final long serialVersionUID = 1L;

    YogabaseAuthException(String message, int status, String code, String correlationId) {
        super(message, status, code, correlationId, null, null);
    }
}
