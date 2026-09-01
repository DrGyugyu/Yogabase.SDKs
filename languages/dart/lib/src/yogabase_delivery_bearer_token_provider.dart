import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';

/// Lazy delivery-only bearer-token provider.
///
/// The current token is obtained on demand, so callers can rotate short-lived
/// workload credentials without storing them in the SDK.
class YogabaseDeliveryBearerTokenProvider implements AccessTokenProvider {
  YogabaseDeliveryBearerTokenProvider({
    required Uri deliveryOrigin,
    required Future<String> Function(Uri uri, Map<String, Object>? context)
        tokenProvider,
  })  : deliveryOrigin = _requireOrigin(deliveryOrigin),
        _tokenProvider = tokenProvider,
        _allowedHostsValidator = AllowedHostsValidator(<String>[
          _requireOrigin(deliveryOrigin).host,
        ]);

  final Uri deliveryOrigin;
  final Future<String> Function(Uri uri, Map<String, Object>? context)
      _tokenProvider;
  final AllowedHostsValidator _allowedHostsValidator;

  @override
  AllowedHostsValidator get allowedHostsValidator => _allowedHostsValidator;

  @override
  Future<String> getAuthorizationToken(
    Uri uri, [
    Map<String, Object>? additionalAuthenticationContext,
  ]) async {
    if (!_allowedHostsValidator.isUrlHostValid(uri) ||
        !_sameOrigin(deliveryOrigin, uri)) {
      return '';
    }
    final token = await _tokenProvider(uri, additionalAuthenticationContext);
    if (token.trim().isEmpty) {
      throw StateError('delivery bearer token provider returned no token');
    }
    return token;
  }

  static Uri _requireOrigin(Uri value) {
    final scheme = value.scheme.toLowerCase();
    if (scheme != 'https' ||
        value.host.isEmpty ||
        value.userInfo.isNotEmpty ||
        value.query.isNotEmpty ||
        value.fragment.isNotEmpty ||
        (value.path.isNotEmpty && value.path != '/')) {
      throw ArgumentError('deliveryOrigin must be an origin-only HTTPS URL');
    }
    return Uri(
      scheme: scheme,
      host: value.host,
      port: value.hasPort ? value.port : null,
    );
  }

  static bool _sameOrigin(Uri left, Uri right) {
    return left.scheme.toLowerCase() == right.scheme.toLowerCase() &&
        left.host.toLowerCase() == right.host.toLowerCase() &&
        _effectivePort(left) == _effectivePort(right);
  }

  static int _effectivePort(Uri value) {
    if (value.hasPort) {
      return value.port;
    }
    return value.scheme.toLowerCase() == 'https' ? 443 : 80;
  }
}
