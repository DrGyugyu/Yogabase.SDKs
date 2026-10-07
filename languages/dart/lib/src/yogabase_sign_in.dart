import 'dart:convert';
import 'dart:math';

import 'package:crypto/crypto.dart';
import 'package:http/http.dart' as http;
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';

import 'yogabase_gateway_support.dart';

const _timeout = Duration(seconds: 30);

/// A refused sign-in or token request: the gateway's [status] and [code] (an OAuth `error` for the token
/// endpoint) and the [correlationId] to quote when asking about it.
class YogabaseAuthException implements Exception {
  YogabaseAuthException(this.status, this.code, this.message, [this.correlationId]);

  final int status;
  final String code;
  final String message;
  final String? correlationId;

  @override
  String toString() => 'YogabaseAuthException($status $code): $message';
}

/// A signed-in user's tokens, refreshed when about to expire. It is the Kiota authentication provider
/// (`DefaultRequestAdapter(authProvider: session)`): the token goes to the gateway's origin and nowhere else.
class YogabaseSession implements AuthenticationProvider {
  YogabaseSession._(this._gateway, this.projectId, this._clientId, this._tokenEndpoint, this._http);

  /// The project's id from its client configuration.
  final String projectId;
  final Uri _gateway;
  final String _clientId;
  final Uri _tokenEndpoint;
  final http.Client? _http;
  String _accessToken = '';
  String? _refreshToken;
  DateTime _expiresAt = DateTime.fromMillisecondsSinceEpoch(0);
  Future<void>? _refreshing;

  @override
  Future<void> authenticateRequest(RequestInformation request, [Map<String, Object>? additionalAuthenticationContext]) async {
    if (_sameOrigin(request.uri, _gateway)) request.headers.put('Authorization', 'Bearer ${await getAccessToken()}');
  }

  /// The access token, refreshed first when it expires within a minute.
  Future<String> getAccessToken() async {
    if (_refreshToken != null && _expiresAt.difference(DateTime.now()) < const Duration(minutes: 1)) {
      // The refresh token rotates, so concurrent callers share one refresh.
      await (_refreshing ??= _exchange({'grant_type': 'refresh_token', 'refresh_token': _refreshToken!})
          .whenComplete(() => _refreshing = null));
    }
    return _accessToken;
  }

  Future<void> _exchange(Map<String, String> form) async {
    final post = _http?.post ?? http.post;
    final response = await post(_tokenEndpoint, body: {'client_id': _clientId, ...form}).timeout(_timeout);
    if (response.statusCode != 200) throw _refusal(response);
    if (_json(response.body) case {
      'access_token': final String token,
      'token_type': final String type,
      'expires_in': final num seconds,
    } && final Map<String, dynamic> body when token.isNotEmpty && type.toLowerCase() == 'bearer' && seconds > 0) {
      _accessToken = token;
      _refreshToken = body['refresh_token'] is String ? body['refresh_token'] as String : null;
      _expiresAt = DateTime.now().add(Duration(seconds: seconds.toInt()));
    } else {
      throw YogabaseAuthException(502, 'token_response_invalid', 'Yogabase returned an invalid token response.');
    }
  }
}

/// Signs in with no browser: logs in for the identity cookie, then runs the PKCE code flow with that cookie
/// answering the authorize endpoint, whose redirect is read and never followed. [gateway] is the project
/// gateway's https origin. Not for Flutter web: a browser hides the redirect and sets no `Cookie`.
///
/// Every refusal is a [YogabaseAuthException] with the gateway's code: `invalid_credentials` (401: a wrong
/// password and an unknown, unconfirmed or locked account alike), `mfa_enrollment_required` (403),
/// `authentication_rate_limited` (429), and `mfa_required` (202) for an account with MFA when no [mfaCode]
/// is given: call again with the authenticator code. Also `interactive_login_required` (401, no cookie),
/// an OAuth error such as `invalid_grant`, `oidc_discovery_invalid` (an endpoint on another origin, and no
/// password is sent), and `authorization_response_invalid` or `authorization_redirect_mismatch` for a
/// redirect that is not this call's, after which no token is requested.
Future<YogabaseSession> signInWithPassword(
  Uri gateway,
  String publishableKey,
  String email,
  String password, {
  String? mfaCode,
  http.Client? httpClient,
}) async {
  if (gateway.scheme != 'https' || gateway.host.isEmpty || gateway.userInfo.isNotEmpty || gateway.hasQuery ||
      gateway.hasFragment || gateway.path.length > 1) {
    throw ArgumentError.value(gateway, 'gateway', 'must be an origin-only https URL');
  }
  final client = httpClient ?? http.Client();
  try {
    // The gateway's cookies are kept here and go to the gateway's origin only (every URL below is checked).
    final cookies = <String, String>{};
    Future<http.Response> call(String method, Uri url, [Object? json]) async {
      final request = http.Request(method, url)..followRedirects = false;
      if (cookies.isNotEmpty) request.headers['cookie'] = cookies.entries.map((e) => '${e.key}=${e.value}').join('; ');
      if (json != null) {
        request.headers['content-type'] = 'application/json';
        request.body = jsonEncode(json);
      }
      final response = await http.Response.fromStream(await client.send(request).timeout(_timeout));
      for (final line in response.headersSplitValues['set-cookie'] ?? const <String>[]) {
        final cookie = RegExp(r'^\s*([^=;\s]+)=([^;]*)').firstMatch(line); // an empty value is a deletion
        if (cookie != null) cookie[2]!.isEmpty ? cookies.remove(cookie[1]) : cookies[cookie[1]!] = cookie[2]!;
      }
      return response;
    }

    Future<Object?> read(Uri url) async {
      final response = await call('GET', url);
      if (response.statusCode != 200) throw _refusal(response);
      return _json(response.body);
    }

    YogabaseAuthException unusable(String what) => YogabaseAuthException(502, 'oidc_discovery_invalid', 'Yogabase returned $what.');
    Uri parse(String url) => Uri.tryParse(url) ?? (throw unusable('$url, which is not a URL'));
    Uri own(String url) {
      final uri = parse(url);
      return _sameOrigin(uri, gateway) ? uri : throw unusable('$url, which is on another origin');
    }

    final (projectId, clientId, redirect, scope, discoveryUrl) = switch (await read(gateway.replace(pathSegments: ['v1', 'client-config', publishableKey]))) {
      {
        'projectId': final String id,
        'oidcPublicClientId': final String client,
        'oidcRedirectUris': [final String uri, ...],
        'oidcScopes': final List<dynamic> scopes,
        'oidcDiscoveryEndpoint': final String endpoint,
      } => (id, client, uri, scopes.join(' '), endpoint),
      _ => throw unusable('a client configuration without the sign-in members'),
    };
    final registered = parse(redirect);
    final (authorizeUrl, tokenUrl) = switch (await read(own(discoveryUrl))) {
      {'authorization_endpoint': final String authorize, 'token_endpoint': final String token} => (own(authorize), own(token)),
      _ => throw unusable('a discovery document without its endpoints'),
    };

    var login = await call('POST', gateway.replace(path: '/auth/login'), {'email': email, 'password': password});
    if (login.statusCode == 202) {
      if (mfaCode == null) throw YogabaseAuthException(202, 'mfa_required', 'Complete the second authentication factor: pass mfaCode.');
      login = await call('POST', gateway.replace(path: '/auth/mfa/verify'), {'code': mfaCode});
    }
    if (login.statusCode != 204) throw _refusal(login);

    final secure = Random.secure();
    String random(int count) => base64Url.encode(List.generate(count, (_) => secure.nextInt(256))).replaceAll('=', '');
    final verifier = random(48), state = random(32);
    final challenge = base64Url.encode(sha256.convert(ascii.encode(verifier)).bytes).replaceAll('=', '');
    final authorize = await call('GET', authorizeUrl.replace(queryParameters: {
      'response_type': 'code', 'client_id': clientId, 'redirect_uri': redirect, 'scope': scope,
      'code_challenge': challenge, 'code_challenge_method': 'S256', 'state': state,
    }));
    final location = authorize.headers['location'];
    if (authorize.statusCode != 302 || location == null) throw _refusal(authorize); // no cookie: 401 interactive_login_required
    final back = registered.resolveUri(parse(location));
    if ((back.scheme, back.authority, back.path) != (registered.scheme, registered.authority, registered.path)) {
      throw YogabaseAuthException(400, 'authorization_redirect_mismatch', 'The authorization response used another redirect URI.');
    }
    final query = back.queryParameters;
    if (query['error'] case final error?) throw YogabaseAuthException(400, error, 'The authorization server rejected sign-in.');
    if ((query['code'] ?? '').isEmpty || query['state'] != state) {
      throw YogabaseAuthException(400, 'authorization_response_invalid', 'The authorization response lacks a code or came back with another state.');
    }

    final session = YogabaseSession._(gateway, projectId, clientId, tokenUrl, httpClient);
    await session._exchange({'grant_type': 'authorization_code', 'code': query['code']!, 'redirect_uri': redirect, 'code_verifier': verifier});
    return session;
  } finally {
    if (httpClient == null) client.close();
  }
}

bool _sameOrigin(Uri a, Uri b) => a.scheme == b.scheme && a.host == b.host && a.port == b.port;

Object? _json(String body) {
  try {
    return jsonDecode(body);
  } on FormatException {
    return null;
  }
}

YogabaseAuthException _refusal(http.Response response) {
  final body = _json(response.body);
  final problem = body is Map<String, dynamic> ? body : const <String, dynamic>{};
  final oauth = problem['error'], code = problem['code'], detail = problem['detail'] ?? problem['title'];
  final correlation = response.headers['x-correlation-id'] ?? problem['correlationId'];
  return YogabaseAuthException(
    response.statusCode,
    oauth is String ? oauth : code is String ? code : YogabaseStatusErrorCodes.forStatus(response.statusCode),
    detail is String ? detail : 'Yogabase returned HTTP ${response.statusCode}.',
    correlation is String ? correlation : null,
  );
}
