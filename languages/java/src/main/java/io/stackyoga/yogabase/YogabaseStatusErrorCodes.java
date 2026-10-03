package io.stackyoga.yogabase;

/**
 * The code a refusal carries when the gateway named none, derived from the HTTP status so a caller can match on
 * it without a status table. A refusal that names its own code (a Jobs {@code job_not_found}, a schedule's
 * {@code schedule_payload_too_large}) keeps it. See {@link YogabaseGatewayException#code()}.
 */
public final class YogabaseStatusErrorCodes {
    private YogabaseStatusErrorCodes() {
    }

    /** 401: no signed-in user, or a token the gateway cannot authenticate. */
    public static final String AUTHENTICATION_REQUIRED = "authentication_required";

    /** 403: the token may not call the route, or the caller may not do this to that resource. */
    public static final String FORBIDDEN = "forbidden";

    /** 404 with no body: no such resource, another user's resource, or an installation that does not serve the route. */
    public static final String NOT_FOUND = "not_found";

    /** 409 with no body: the request conflicts with what the gateway holds. */
    public static final String CONFLICT = "conflict";

    /** 412: the ETag sent is stale, or the precondition does not hold for what the gateway holds. */
    public static final String PRECONDITION_FAILED = "precondition_failed";

    /** 413 with no body: a body over its listener's limit. */
    public static final String PAYLOAD_TOO_LARGE = "payload_too_large";

    /** 415: the request body is not JSON. */
    public static final String UNSUPPORTED_MEDIA_TYPE = "unsupported_media_type";

    /** 428: the route needs an {@code If-Match} or {@code If-None-Match} precondition and the request sent none. */
    public static final String PRECONDITION_REQUIRED = "precondition_required";

    /** 429: too many requests. */
    public static final String RATE_LIMITED = "rate_limited";

    /** Any other refusal, which a caller reads from the status. */
    public static final String REQUEST_FAILED = "request_failed";
}
