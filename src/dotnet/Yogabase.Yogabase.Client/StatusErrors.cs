namespace Yogabase.Yogabase;

/// <summary>
/// The code a refusal carries when the gateway named none: it is derived from the HTTP status, so a caller
/// can match on it without a status table. A refusal that names its own code (a Jobs
/// <c>job_not_found</c>, a schedule's <c>schedule_payload_too_large</c>) keeps it.
/// </summary>
public static class YogabaseStatusErrors
{
    /// <summary>401: no signed-in user, or a token the gateway cannot authenticate.</summary>
    public const string AuthenticationRequired = "authentication_required";

    /// <summary>403: the token may not call the route, or the caller may not do this to that resource.</summary>
    public const string Forbidden = "forbidden";

    /// <summary>
    /// 404 with no body: no such resource, another user's resource, or an installation that does not serve
    /// the route at all (the gateway answers all three the same).
    /// </summary>
    public const string NotFound = "not_found";

    /// <summary>409 with no body: the request conflicts with what the gateway holds.</summary>
    public const string Conflict = "conflict";

    /// <summary>412: the ETag sent is stale, or the precondition does not hold for what the gateway holds.</summary>
    public const string PreconditionFailed = "precondition_failed";

    /// <summary>413 with no body: a body over its listener's limit.</summary>
    public const string PayloadTooLarge = "payload_too_large";

    /// <summary>415: the request body is not JSON.</summary>
    public const string UnsupportedMediaType = "unsupported_media_type";

    /// <summary>428: the route needs an <c>If-Match</c> or <c>If-None-Match</c> precondition and the request sent none.</summary>
    public const string PreconditionRequired = "precondition_required";

    /// <summary>429: too many requests.</summary>
    public const string RateLimited = "rate_limited";

    /// <summary>Any other refusal, which a caller reads from <see cref="YogabaseApiException.StatusCode"/>.</summary>
    public const string RequestFailed = "request_failed";
}
