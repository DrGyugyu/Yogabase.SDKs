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
  _requireOrigin(gateway);
  return _signIn(gateway, publishableKey, httpClient, {}, (call) async {
    var login = await call('POST', gateway.replace(path: '/auth/login'), {'email': email, 'password': password});
    if (login.statusCode == 202) login = await _verifyMfa(call, gateway, mfaCode);
    if (login.statusCode != 204) throw _refusal(login);
  });
}

/// A started sign-in with an external provider (Google, Microsoft or Apple): open [authorizationUrl] in the system
/// browser, then give the redirect it ends in to [completeExternalSignIn]. It holds the verifier, the state and the
/// gateway's cookie, so complete it once, with the redirect of its own start. It holds no HTTP client: the one passed to
/// [startExternalSignIn], if any, is the caller's to close; otherwise each call opens one and closes it before it returns.
class YogabaseExternalSignIn {
  YogabaseExternalSignIn._(this.authorizationUrl, this._gateway, this._publishableKey, this._provider, this._redirectUri,
      this._state, this._verifier, this._cookies, this._httpClient);

  /// The provider's address, always https.
  final Uri authorizationUrl;
  final Uri _gateway, _redirectUri;
  final String _publishableKey, _provider, _state, _verifier;
  final Map<String, String> _cookies;
  final http.Client? _httpClient;
  int? _status; // the callback's answer once the gateway accepted it: 204, or 202 while the authenticator code is owed
}

/// Starts a sign-in with [provider], the id the project gave it. [redirectUri] is one of the project's registered
/// redirect URIs, where the browser ends up. Only the S256 challenge of the verifier is sent. Every refusal is a
/// [YogabaseAuthException] with the gateway's code (`external_redirect_uri_invalid`, `external_provider_unknown`,
/// `external_identity_unavailable`, ...), or `external_start_invalid` for an answer that is not an https address and a state.
Future<YogabaseExternalSignIn> startExternalSignIn(
  Uri gateway,
  String publishableKey,
  String provider,
  String redirectUri, {
  http.Client? httpClient,
}) async {
  _requireOrigin(gateway);
  if (!RegExp(r'^(?!\.{1,2}$)[a-z0-9._-]{1,64}$').hasMatch(provider)) {
    throw ArgumentError.value(provider, 'provider', 'must be 1 to 64 lowercase letters, digits, ".", "_" or "-"');
  }
  final registered = Uri.tryParse(redirectUri) ?? (throw ArgumentError.value(redirectUri, 'redirectUri', 'must be a URI'));
  final verifier = _random(48), cookies = <String, String>{};
  final client = httpClient ?? http.Client();
  try {
    final url = gateway.replace(pathSegments: ['auth', 'external', provider, 'start']);
    final answer = await _send(client, cookies, 'POST', url, {'redirectUri': redirectUri, 'codeChallenge': _s256(verifier)});
    if (answer.statusCode != 200) throw _refusal(answer);
    if (_json(answer.body) case {'authorizationUrl': final String address, 'state': final String state} when state.isNotEmpty) {
      final authorization = Uri.tryParse(address);
      if (authorization != null && authorization.isScheme('https') && authorization.host.isNotEmpty) {
        return YogabaseExternalSignIn._(authorization, gateway, publishableKey, provider, registered, state, verifier, cookies, httpClient);
      }
    }
    throw YogabaseAuthException(502, 'external_start_invalid', 'Yogabase returned an invalid external sign-in start.');
  } finally {
    if (httpClient == null) client.close();
  }
}

/// Completes [pending] with the [redirect] the application received. Nothing is sent for a redirect with another
/// address, another state, a repeated parameter, an `error` (a refusal whose code is its value) or no code. Then the
/// callback and the same code flow as [signInWithPassword], whose session it returns. Refusals are
/// [YogabaseAuthException]s with the gateway's code (`external_identity_invalid`, `external_identity_email_in_use`, ...).
/// An account with MFA is `202 mfa_required`, the code already spent: call again with the same [pending] and the
/// authenticator code as [mfaCode], and the callback is not sent again.
Future<YogabaseSession> completeExternalSignIn(YogabaseExternalSignIn pending, Uri redirect, {String? mfaCode}) async {
  final code = pending._status == null ? _authorizationCode(pending._redirectUri, redirect, pending._state) : '';
  final gateway = pending._gateway;
  return _signIn(gateway, pending._publishableKey, pending._httpClient, pending._cookies, (call) async {
    if (pending._status == null) {
      final url = gateway.replace(pathSegments: ['auth', 'external', pending._provider, 'callback']);
      final answer = await call('POST', url, {'code': code, 'state': pending._state, 'codeVerifier': pending._verifier});
      if (answer.statusCode != 204 && answer.statusCode != 202) throw _refusal(answer);
      pending._status = answer.statusCode;
    }
    if (pending._status == 202) {
      final answer = await _verifyMfa(call, gateway, mfaCode);
      if (answer.statusCode != 204) throw _refusal(answer);
      pending._status = 204;
    }
  });
}

typedef _Call = Future<http.Response> Function(String method, Uri url, [Object? json]);

// What both sign-ins share. Discovery comes first, so a document naming another origin stops the flow before
// [authenticate] sends a password or spends a code; it leaves the identity cookie in [cookies]; then the PKCE code
// flow runs with that cookie answering the authorize endpoint, whose redirect is read and never followed.
Future<YogabaseSession> _signIn(
  Uri gateway,
  String publishableKey,
  http.Client? httpClient,
  Map<String, String> cookies,
  Future<void> Function(_Call call) authenticate,
) async {
  final client = httpClient ?? http.Client();
  try {
    Future<http.Response> call(String method, Uri url, [Object? json]) => _send(client, cookies, method, url, json);

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

    await authenticate(call);

    final verifier = _random(48), state = _random(32);
    final authorize = await call('GET', authorizeUrl.replace(queryParameters: {
      'response_type': 'code', 'client_id': clientId, 'redirect_uri': redirect, 'scope': scope,
      'code_challenge': _s256(verifier), 'code_challenge_method': 'S256', 'state': state,
    }));
    final location = authorize.headers['location'];
    if (authorize.statusCode != 302 || location == null) throw _refusal(authorize); // no cookie: 401 interactive_login_required
    final code = _authorizationCode(registered, registered.resolveUri(parse(location)), state);

    final session = YogabaseSession._(gateway, projectId, clientId, tokenUrl, httpClient);
    await session._exchange({'grant_type': 'authorization_code', 'code': code, 'redirect_uri': redirect, 'code_verifier': verifier});
    return session;
  } finally {
    if (httpClient == null) client.close();
  }
}

void _requireOrigin(Uri gateway) {
  if (gateway.scheme != 'https' || gateway.host.isEmpty || gateway.userInfo.isNotEmpty || gateway.hasQuery ||
      gateway.hasFragment || gateway.path.length > 1) {
    throw ArgumentError.value(gateway, 'gateway', 'must be an origin-only https URL');
  }
}

// One request with a JSON body, redirects never followed. The gateway's cookies are kept in [cookies] and go to the
// gateway's origin only (every URL used is checked).
Future<http.Response> _send(http.Client client, Map<String, String> cookies, String method, Uri url, [Object? json]) async {
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

// 202 from a route that sets the identity cookie: the account has MFA, so the authenticator code completes it.
Future<http.Response> _verifyMfa(_Call call, Uri gateway, String? mfaCode) {
  if (mfaCode == null) throw YogabaseAuthException(202, 'mfa_required', 'Complete the second authentication factor: pass mfaCode.');
  return call('POST', gateway.replace(path: '/auth/mfa/verify'), {'code': mfaCode});
}

String _random(int count) {
  final secure = Random.secure();
  return base64Url.encode(List.generate(count, (_) => secure.nextInt(256))).replaceAll('=', '');
}

String _s256(String verifier) => base64Url.encode(sha256.convert(ascii.encode(verifier)).bytes).replaceAll('=', '');

// The code a redirect carries, once it is held to the registered address (no userinfo or fragment), to the state sent
// and to one value per parameter. The `error` is only believed with the right state.
String _authorizationCode(Uri registered, Uri back, String state) {
  if ((back.scheme, back.host, back.port, back.path) != (registered.scheme, registered.host, registered.port, registered.path) ||
      back.userInfo.isNotEmpty || back.hasFragment) {
    throw YogabaseAuthException(400, 'authorization_redirect_mismatch', 'The authorization response used another redirect URI.');
  }
  final query = back.queryParametersAll;
  if (query.values.any((values) => values.length > 1) || query['state']?.first != state) {
    throw YogabaseAuthException(400, 'authorization_response_invalid', 'The authorization response repeats a parameter or came back with another state.');
  }
  if (query['error'] case [final error]) throw YogabaseAuthException(400, error, 'The authorization server rejected sign-in.');
  if (query['code'] case [final code] when code.isNotEmpty) return code;
  throw YogabaseAuthException(400, 'authorization_response_invalid', 'The authorization response lacks a code.');
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
