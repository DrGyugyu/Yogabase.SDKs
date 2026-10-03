import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'yogabase_api_client.dart';
import 'yogabase_jobs_client.dart';

/// Application-plane facade with its own origin and request adapter.
///
/// Construct a separate instance and adapter for delivery traffic. This keeps
/// application credentials out of the Control Plane delivery client.
class YogabaseApplicationClient {
  YogabaseApplicationClient(RequestAdapter requestAdapter, Uri applicationOrigin)
      : origin = _requireOrigin(applicationOrigin, 'applicationOrigin'),
        requestAdapter = _configureAdapter(
          requestAdapter,
          _requireOrigin(applicationOrigin, 'applicationOrigin'),
        ),
        api = YogabaseApiClient(requestAdapter);

  final Uri origin;
  final RequestAdapter requestAdapter;
  final YogabaseApiClient api;

  /// Durable background jobs of one project for the signed-in user, beside the
  /// generated invoke routes. It shares this client's adapter, and so its
  /// authentication and origin.
  YogabaseJobsClient jobs(String projectId) =>
      YogabaseJobsClient(requestAdapter, origin, projectId);

  static RequestAdapter _configureAdapter(RequestAdapter adapter, Uri origin) {
    adapter.baseUrl = origin.toString();
    return adapter;
  }

  static Uri _requireOrigin(Uri value, String name) {
    if ((value.scheme.toLowerCase() != 'http' &&
            value.scheme.toLowerCase() != 'https') ||
        value.host.isEmpty ||
        value.userInfo.isNotEmpty ||
        value.query.isNotEmpty ||
        value.fragment.isNotEmpty ||
        (value.path.isNotEmpty && value.path != '/')) {
      throw ArgumentError('$name must be an origin-only HTTP(S) URL');
    }
    return Uri(
      scheme: value.scheme.toLowerCase(),
      host: value.host,
      port: value.hasPort ? value.port : null,
    );
  }
}
