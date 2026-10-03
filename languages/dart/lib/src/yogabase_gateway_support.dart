import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';

import 'models/problem.dart';

/// What the authored application-gateway clients ([YogabaseJobsClient] and
/// [YogabaseSchedulesClient]) share: the code a refusal carries when the
/// gateway named none, and the way a Kiota refusal is read for its status.
///
/// The code a refusal carries when the gateway named none is derived from the
/// HTTP status, so a caller can match on it without a status table. A refusal
/// that names its own code (a Jobs `job_not_found`, a schedule's
/// `schedule_payload_too_large`) keeps it.
class YogabaseStatusErrorCodes {
  YogabaseStatusErrorCodes._();

  /// 401: no signed-in user, or a token the gateway cannot authenticate.
  static const String authenticationRequired = 'authentication_required';

  /// 403: the token may not call the route, or the caller may not do this to
  /// that resource.
  static const String forbidden = 'forbidden';

  /// 404 with no body: no such resource, another user's resource, or an
  /// installation that does not serve the route.
  static const String notFound = 'not_found';

  /// 409 with no body: the request conflicts with what the gateway holds.
  static const String conflict = 'conflict';

  /// 412: the ETag sent is stale, or the precondition does not hold for what
  /// the gateway holds.
  static const String preconditionFailed = 'precondition_failed';

  /// 413 with no body: a body over its listener's limit.
  static const String payloadTooLarge = 'payload_too_large';

  /// 415: the request body is not JSON.
  static const String unsupportedMediaType = 'unsupported_media_type';

  /// 428: the route needs an `If-Match` or `If-None-Match` precondition and
  /// the request sent none.
  static const String preconditionRequired = 'precondition_required';

  /// 429: too many requests.
  static const String rateLimited = 'rate_limited';

  /// Any other refusal, which a caller reads from the status.
  static const String requestFailed = 'request_failed';

  /// The code a refusal of [status] carries when the gateway named none.
  static String forStatus(int status) {
    switch (status) {
      case 401:
        return authenticationRequired;
      case 403:
        return forbidden;
      case 404:
        return notFound;
      case 409:
        return conflict;
      case 412:
        return preconditionFailed;
      // A gateway that predates the named refusal answers a bare 413 for a
      // body over its listener's limit.
      case 413:
        return payloadTooLarge;
      case 415:
        return unsupportedMediaType;
      case 428:
        return preconditionRequired;
      case 429:
        return rateLimited;
      default:
        return requestFailed;
    }
  }
}

/// The HTTP status a Kiota [ApiException] carries: its response status code,
/// or for a [Problem] the status member of the document, or 0 when it has
/// neither.
int yogabaseStatusOf(ApiException exception) {
  final Object? code = exception.statusCode;
  if (code is int && code > 0) {
    return code;
  }
  if (exception is Problem) {
    final Object? member = exception.status;
    if (member is int) {
      return member;
    }
  }
  return 0;
}

/// [value] when it is a positive whole number, otherwise null: a limit a
/// refusal states badly is not reported.
int? yogabasePositiveInt(Object? value) =>
    value is int && value > 0 ? value : null;
