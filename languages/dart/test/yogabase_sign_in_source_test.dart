import 'dart:convert';

import 'package:crypto/crypto.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:microsoft_kiota_bundle/microsoft_kiota_bundle.dart';
import 'package:yogabase_client/yogabase_client.dart';

/// Headless password sign-in against a fake gateway that answers the routes the product's sign-in uses; run with
/// `dart run test/yogabase_sign_in_source_test.dart`. Nothing here reaches a network.
Future<void> main() async {
  await _signsInWithTheCookieAndPkceAndKeepsTheTokens();
  await _aLoginRefusalIsNamedAndStopsBeforeAuthorize();
  await _anInvalidTokenResponseIsRefused();
  await _withoutTheCookieThereIsNoTokenRequest();
  await _anAuthorizationResponseThatDoesNotCheckOutMakesNoTokenRequest();
  await _mfaRequiredIsReportedByNameAndCompletesWithTheCode();
  await _onlyAnHttpsGatewayAndDiscoveryInsideItAreAccepted();
  await _theAuthoredClientsSendTheSessionsTokenToTheGatewayOnly();
  await _anExpiringTokenIsRefreshedOnceWithTheRotatingRefreshToken();
  await _externalSignInSendsOnlyTheChallengeAndKeepsTheTokens();
  await _anExternalRedirectThatDoesNotCheckOutMakesNoRequestAfterTheStart();
  await _externalRefusalsKeepTheGatewaysCode();
  await _externalMfaCompletesWithTheCodeAndNeverResendsTheCallback();
  print('dart sign-in source checks: PASS');
}

final _gateway = Uri.parse('https://apps.example.test');
const _redirect = 'https://app.example.test/callback';
const _project = '11111111-1111-4111-8111-111111111111';
const _cookie = 'yogabase.project.identity=cookie-1';
const _secure = '; path=/; secure; httponly';

http.Response _reply(int status, [Object? body, Map<String, String> headers = const {}]) =>
    http.Response(body == null ? '' : jsonEncode(body), status, headers: {'content-type': 'application/json', ...headers});

http.Response _problem(int status, String code) => _reply(status, {'status': status, 'code': code}, {'x-correlation-id': 'corr-1'});

String _s256(String verifier) => base64Url.encode(sha256.convert(ascii.encode(verifier)).bytes).replaceAll('=', '');

/// The sign-in routes. [login] is the (status, code) that /auth/login answers, [redirect] the authorize Location.
class _Gateway {
  _Gateway({this.login = (204, null), this.callback = (204, null), this.redirect = '$_redirect?code=code-1&state={state}', this.cookie = true, this.expiresIn = 3600, Map<String, String>? discovery})
      : discovery = discovery ?? {'authorization_endpoint': '$_gateway/connect/authorize', 'token_endpoint': '$_gateway/connect/token'};

  final (int, String?) login, callback; // what /auth/login and the external callback answer once their input checks out
  final String redirect;
  final bool cookie;
  final Map<String, String> discovery;
  final int expiresIn;
  bool rejectRefresh = false;
  Map<String, Object?>? badToken; // a token response the endpoint should answer instead
  Map<String, Object?>? badStart; // a start response the external route should answer instead
  String discoveryUrl = '$_gateway/.well-known/openid-configuration';
  int issued = 0;
  String challenge = '';
  final startChallenges = <String, String>{}; // state -> the challenge its start received
  final requests = <http.Request>[];

  http.Response _signedIn((int, String?) outcome) {
    if (outcome.$2 == null) return _reply(204, null, cookie ? {'set-cookie': '$_cookie$_secure'} : {});
    return outcome.$1 == 202 ? _reply(202, {'status': 202, 'code': 'mfa_required'}, {'set-cookie': 'yogabase.project.identity.2fa=tfa-1$_secure'}) : _problem(outcome.$1, outcome.$2!);
  }

  List<String> get paths => [for (final request in requests) request.url.path];
  http.Request path(String path) => requests.firstWhere((request) => request.url.path == path);

  Future<http.Response> handle(http.Request request) async {
    await Future<void>.delayed(Duration.zero); // yield as real I/O does, so concurrent callers interleave
    requests.add(request);
    switch (request.url.path) {
      case '/v1/client-config/sy_pub_key':
        return _reply(200, {'projectId': _project, 'oidcPublicClientId': 'client-1', 'oidcRedirectUris': [_redirect], 'oidcScopes': ['openid', 'offline_access'], 'oidcDiscoveryEndpoint': discoveryUrl});
      case '/.well-known/openid-configuration':
        return _reply(200, discovery);
      case '/auth/login':
        return _signedIn(login);
      case '/auth/external/google/start':
        final state = 'state-${startChallenges.length + 1}';
        startChallenges[state] = (jsonDecode(request.body) as Map<String, dynamic>)['codeChallenge'] as String;
        return _reply(200, badStart ?? {'authorizationUrl': 'https://accounts.example.test/auth?state=$state', 'state': state});
      case '/auth/external/other/start':
        return _problem(404, 'external_provider_unknown');
      case '/auth/external/google/callback':
        final form = jsonDecode(request.body) as Map<String, dynamic>; // the verifier must be the one whose challenge the start received
        if (form['code'] != 'ext-code' || startChallenges[form['state']] != _s256('${form['codeVerifier']}')) return _problem(401, 'external_identity_invalid');
        return _signedIn(callback);
      case '/auth/mfa/verify':
        if (!request.headers['cookie']!.contains('tfa-1') || !request.body.contains('123456')) return _problem(400, 'mfa_invalid');
        return _reply(204, null, {'set-cookie': '$_cookie$_secure,yogabase.project.identity.2fa=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/'});
      case '/connect/authorize':
        if (request.headers['cookie'] != _cookie) return _problem(401, 'interactive_login_required');
        challenge = request.url.queryParameters['code_challenge']!;
        return _reply(302, null, {'location': redirect.replaceAll('{state}', request.url.queryParameters['state']!)});
      case '/connect/token':
        final form = Uri.splitQueryString(request.body);
        final valid = form['client_id'] == 'client-1' && (form['grant_type'] == 'authorization_code'
            ? form['code'] == 'code-1' && form['redirect_uri'] == _redirect && _s256(form['code_verifier']!) == challenge
            : !rejectRefresh && form['refresh_token'] == 'refresh-$issued'); // the refresh token rotates: only the latest is accepted
        if (!valid) return _reply(400, {'error': 'invalid_grant'});
        issued++;
        if (badToken != null) return _reply(200, badToken);
        return _reply(200, {'access_token': 'access-$issued', 'token_type': 'Bearer', 'expires_in': expiresIn, 'refresh_token': 'refresh-$issued'});
      default:
        return request.url.path.startsWith('/v1/projects/$_project/jobs/') ? _problem(404, 'job_not_found') : _reply(404);
    }
  }
}

Future<YogabaseSession> _signIn(_Gateway gateway, {String? mfaCode}) =>
    signInWithPassword(_gateway, 'sy_pub_key', 'user@example.test', 'secret-pw', mfaCode: mfaCode, httpClient: MockClient(gateway.handle));

Future<YogabaseAuthException> _refusal(_Gateway gateway, {String? mfaCode}) async {
  try {
    await _signIn(gateway, mfaCode: mfaCode);
  } on YogabaseAuthException catch (error) {
    return error;
  }
  throw StateError('sign-in was not refused');
}

Future<void> _signsInWithTheCookieAndPkceAndKeepsTheTokens() async {
  final gateway = _Gateway(); // the token route itself rejects a wrong code, redirect URI or verifier
  final session = await _signIn(gateway);
  _expect(gateway.paths.join(' ') == '/v1/client-config/sy_pub_key /.well-known/openid-configuration /auth/login /connect/authorize /connect/token', 'routes: ${gateway.paths}');
  final authorize = gateway.path('/connect/authorize'), token = gateway.path('/connect/token');
  final query = authorize.url.queryParameters;
  _expect('${query['response_type']} ${query['client_id']} ${query['redirect_uri']} ${query['scope']} ${query['code_challenge_method']}' == 'code client-1 $_redirect openid offline_access S256', 'authorize query: $query');
  final verifier = Uri.splitQueryString(token.body)['code_verifier']!;
  _expect(verifier.length >= 43 && _s256(verifier) == query['code_challenge'], 'the challenge is S256 of the verifier');
  _expect(!authorize.url.toString().contains(verifier) && !authorize.headers.toString().contains(verifier), 'the verifier never appears in the authorize request');
  _expect(gateway.path('/auth/login').body.contains('secret-pw') && !authorize.url.toString().contains('secret-pw'), 'the password goes to /auth/login only');
  _expect(!authorize.followRedirects && authorize.headers['cookie'] == _cookie, 'the redirect is not followed and the cookie is sent');
  _expect(session.projectId == _project && await session.getAccessToken() == 'access-1', 'the tokens are stored');
  final other = _Gateway();
  await _signIn(other);
  _expect(other.path('/connect/authorize').url.queryParameters['state'] != query['state'], 'each sign-in has its own state');
}

Future<void> _anInvalidTokenResponseIsRefused() async {
  for (final bad in <Map<String, Object?>>[{}, {'access_token': '', 'token_type': 'Bearer', 'expires_in': 60}, {'access_token': 'a', 'token_type': 'mac', 'expires_in': 60}, {'access_token': 'a', 'token_type': 'Bearer', 'expires_in': 0}]) {
    final error = await _refusal(_Gateway()..badToken = bad);
    _expect(error.status == 502 && error.code == 'token_response_invalid', 'token response $bad: ${error.code}');
  }
}

Future<void> _aLoginRefusalIsNamedAndStopsBeforeAuthorize() async {
  for (final (status, code) in [(401, 'invalid_credentials'), (403, 'mfa_enrollment_required'), (429, 'authentication_rate_limited')]) {
    final gateway = _Gateway(login: (status, code));
    final error = await _refusal(gateway);
    _expect('${error.status} ${error.code} ${error.correlationId} ${gateway.paths.last}' == '$status $code corr-1 /auth/login', 'login $status: ${error.code}');
  }
}

Future<void> _withoutTheCookieThereIsNoTokenRequest() async {
  final gateway = _Gateway(cookie: false);
  final error = await _refusal(gateway);
  _expect('${error.status} ${error.code}' == '401 interactive_login_required' && !gateway.paths.contains('/connect/token'), 'no cookie: ${error.code}');
}

Future<void> _anAuthorizationResponseThatDoesNotCheckOutMakesNoTokenRequest() async {
  final cases = {
    'authorization_response_invalid': ['$_redirect?code=code-1&state=other', '$_redirect?code=code-1', '$_redirect?state={state}', '$_redirect?code=&state={state}'],
    'authorization_redirect_mismatch': ['https://evil.example.test/callback?code=code-1&state={state}', '$_redirect/x?code=code-1&state={state}'],
    'access_denied': ['$_redirect?error=access_denied&state={state}'],
  };
  for (final MapEntry(key: code, value: redirects) in cases.entries) {
    for (final redirect in redirects) {
      final gateway = _Gateway(redirect: redirect);
      final error = await _refusal(gateway);
      _expect(error.status == 400 && error.code == code && !gateway.paths.contains('/connect/token'), '$redirect: ${error.status} ${error.code}');
    }
  }
}

Future<void> _mfaRequiredIsReportedByNameAndCompletesWithTheCode() async {
  final gateway = _Gateway(login: (202, 'mfa_required'));
  final error = await _refusal(gateway);
  _expect('${error.status} ${error.code} ${gateway.paths.last}' == '202 mfa_required /auth/login', 'mfa required: ${error.code}');
  _expect((await _refusal(gateway, mfaCode: '000000')).code == 'mfa_invalid' && !gateway.paths.contains('/connect/authorize'), 'a wrong code stops before authorize');
  final session = await _signIn(gateway, mfaCode: '123456'); // the 2fa cookie is deleted by the verify answer, so authorize sends one cookie
  _expect(await session.getAccessToken() == 'access-1', 'mfa sign-in');
}

Future<void> _onlyAnHttpsGatewayAndDiscoveryInsideItAreAccepted() async {
  try {
    await signInWithPassword(Uri.parse('http://localhost:5080'), 'sy_pub_key', 'user@example.test', 'pw', httpClient: MockClient(_Gateway().handle));
    _expect(false, 'an http gateway must be refused');
  } on ArgumentError {
    // expected
  }
  final elsewhere = _Gateway()..discoveryUrl = 'https://evil.example.test/.well-known/openid-configuration';
  _expect((await _refusal(elsewhere)).code == 'oidc_discovery_invalid' && elsewhere.requests.length == 1, 'a discovery document on another origin is not fetched');
  for (final endpoint in ['authorization_endpoint', 'token_endpoint']) {
    final gateway = _Gateway(discovery: {'authorization_endpoint': '$_gateway/connect/authorize', 'token_endpoint': '$_gateway/connect/token', endpoint: 'https://evil.example.test/connect'});
    _expect((await _refusal(gateway)).code == 'oidc_discovery_invalid' && !gateway.paths.contains('/auth/login'), '$endpoint on another origin is refused before the password is sent');
  }
}

Future<void> _theAuthoredClientsSendTheSessionsTokenToTheGatewayOnly() async {
  final gateway = _Gateway();
  final session = await _signIn(gateway);
  final adapter = DefaultRequestAdapter(authProvider: session, client: MockClient(gateway.handle));
  try {
    await YogabaseApplicationClient(adapter, _gateway).jobs(_project).get('5d0a8f0e-3c63-4d5e-9a0b-7c1f3a9e2b41');
    _expect(false, 'the fake gateway has no such job');
  } on YogabaseJobsException catch (error) {
    _expect(error.code == 'job_not_found' && gateway.requests.last.headers['Authorization'] == 'Bearer access-1', 'jobs call: ${error.code}');
  }
  for (final elsewhere in ['https://other.example.test/v1/servers', 'http://apps.example.test:443/v1/servers']) {
    final request = RequestInformation(httpMethod: HttpMethod.get, urlTemplate: elsewhere);
    await session.authenticateRequest(request);
    _expect(!request.headers.containsKey('Authorization'), 'no token for $elsewhere');
  }
}

Future<void> _anExpiringTokenIsRefreshedOnceWithTheRotatingRefreshToken() async {
  final gateway = _Gateway(expiresIn: 30); // inside the one-minute margin, so always due
  final session = await _signIn(gateway);
  final tokens = await Future.wait([session.getAccessToken(), session.getAccessToken()]);
  _expect(tokens.join(' ') == 'access-2 access-2' && gateway.paths.where((path) => path == '/connect/token').length == 2, 'two callers share one refresh: $tokens');
  _expect(await session.getAccessToken() == 'access-3', 'the next refresh spends the rotated refresh token');
  final due = _Gateway(expiresIn: 30);
  final stale = await _signIn(due);
  due.rejectRefresh = true;
  try {
    await stale.getAccessToken();
    _expect(false, 'a refused refresh must surface');
  } on YogabaseAuthException catch (error) {
    _expect(error.status == 400 && error.code == 'invalid_grant', 'refresh refusal: ${error.code}');
  }
}

Future<YogabaseExternalSignIn> _start(_Gateway gateway, [String provider = 'google']) =>
    startExternalSignIn(_gateway, 'sy_pub_key', provider, _redirect, httpClient: MockClient(gateway.handle));

/// What the browser is redirected to: [query] with `{state}` replaced by the state of [pending]'s own start.
Uri _back(YogabaseExternalSignIn pending, [String query = 'code=ext-code&state={state}', String address = _redirect]) =>
    Uri.parse('$address?${query.replaceAll('{state}', pending.authorizationUrl.queryParameters['state']!)}');

Future<YogabaseAuthException> _externalRefusal(Future<Object?> attempt) async {
  try {
    await attempt;
  } on YogabaseAuthException catch (error) {
    return error;
  }
  throw StateError('external sign-in was not refused');
}

Future<void> _externalSignInSendsOnlyTheChallengeAndKeepsTheTokens() async {
  final gateway = _Gateway();
  final pending = await _start(gateway);
  _expect(pending.authorizationUrl.scheme == 'https', 'the provider address is https');
  final start = gateway.path('/auth/external/google/start'), sent = jsonDecode(start.body) as Map<String, dynamic>;
  _expect(sent.length == 2 && sent['redirectUri'] == _redirect && !start.headers.containsKey('authorization') && !start.headers.containsKey('cookie'), 'start body: $sent');
  final session = await completeExternalSignIn(pending, _back(pending)); // the fake callback rejects any verifier but the one whose challenge start got
  _expect(gateway.paths.join(' ') == '/auth/external/google/start /v1/client-config/sy_pub_key /.well-known/openid-configuration /auth/external/google/callback /connect/authorize /connect/token', 'routes: ${gateway.paths}');
  final callback = gateway.path('/auth/external/google/callback'), body = jsonDecode(callback.body) as Map<String, dynamic>, verifier = body['codeVerifier'] as String;
  _expect(RegExp(r'^[A-Za-z0-9_-]{43,128}$').hasMatch(verifier) && sent['codeChallenge'] == _s256(verifier), 'the challenge is S256 of the verifier');
  _expect(!start.body.contains(verifier) && !start.url.toString().contains(verifier) && !start.headers.toString().contains(verifier), 'the verifier never reaches start');
  _expect(body['code'] == 'ext-code' && body['state'] == pending.authorizationUrl.queryParameters['state'] && !callback.headers.containsKey('authorization'), 'callback body: $body');
  final authorize = gateway.path('/connect/authorize');
  _expect(!authorize.followRedirects && authorize.headers['cookie'] == _cookie, 'the redirect is not followed and the cookie is sent');
  _expect(session.projectId == _project && await session.getAccessToken() == 'access-1', 'the tokens are stored');
  _expect((await _start(gateway)).authorizationUrl != pending.authorizationUrl, 'each start has its own state');
}

Future<void> _anExternalRedirectThatDoesNotCheckOutMakesNoRequestAfterTheStart() async {
  final cases = {
    'authorization_response_invalid': ['code=ext-code&state=other', 'error=access_denied&state=other', 'code=ext-code', 'state={state}', 'code=&state={state}', 'code=ext-code&code=ext-code&state={state}', 'code=ext-code&state={state}&state={state}'],
    'access_denied': ['error=access_denied&state={state}'],
  };
  for (final MapEntry(key: code, value: queries) in cases.entries) {
    for (final query in queries) {
      final gateway = _Gateway(), pending = await _start(gateway);
      final error = await _externalRefusal(completeExternalSignIn(pending, _back(pending, query)));
      _expect(error.status == 400 && error.code == code && gateway.paths.length == 1, '$query: ${error.status} ${error.code} ${gateway.paths}');
    }
  }
  for (final address in ['https://evil.example.test/callback', '$_redirect/x', 'http://app.example.test/callback', 'https://app.example.test:8443/callback', 'https://user@app.example.test/callback', '$_redirect#fragment']) {
    final gateway = _Gateway(), pending = await _start(gateway);
    final error = await _externalRefusal(completeExternalSignIn(pending, _back(pending, 'code=ext-code&state={state}', address)));
    _expect(error.code == 'authorization_redirect_mismatch' && gateway.paths.length == 1, '$address: ${error.code} ${gateway.paths}');
  }
  final gateway = _Gateway(), first = await _start(gateway), second = await _start(gateway); // a redirect belongs to the start that made it
  final error = await _externalRefusal(completeExternalSignIn(first, _back(second)));
  _expect(error.code == 'authorization_response_invalid' && gateway.paths.length == 2, 'another start\'s redirect: ${error.code} ${gateway.paths}');
}

Future<void> _externalRefusalsKeepTheGatewaysCode() async {
  for (final (status, code) in [(401, 'external_identity_invalid'), (409, 'external_identity_email_in_use'), (403, 'external_email_unverified')]) {
    final gateway = _Gateway(callback: (status, code)), pending = await _start(gateway);
    final error = await _externalRefusal(completeExternalSignIn(pending, _back(pending)));
    _expect('${error.status} ${error.code} ${error.correlationId}' == '$status $code corr-1' && !gateway.paths.contains('/connect/authorize'), 'callback $status: ${error.code}');
  }
  _expect((await _externalRefusal(_start(_Gateway(), 'other'))).code == 'external_provider_unknown', 'an unknown provider is named');
  for (final bad in <Map<String, Object?>>[{}, {'authorizationUrl': 'http://accounts.example.test/auth', 'state': 's'}, {'authorizationUrl': 'https://accounts.example.test/auth', 'state': ''}, {'authorizationUrl': 'not a url', 'state': 's'}]) {
    final error = await _externalRefusal(_start(_Gateway()..badStart = bad));
    _expect(error.status == 502 && error.code == 'external_start_invalid', 'start answer $bad: ${error.code}');
  }
  for (final provider in ['Google', '..', 'a/b', '']) {
    final gateway = _Gateway();
    try {
      await _start(gateway, provider);
      _expect(false, 'provider "$provider" must be refused');
    } on ArgumentError {
      _expect(gateway.requests.isEmpty, 'nothing is sent for provider "$provider"');
    }
  }
  try {
    await startExternalSignIn(Uri.parse('http://localhost:5080'), 'sy_pub_key', 'google', _redirect, httpClient: MockClient(_Gateway().handle));
    _expect(false, 'an http gateway must be refused');
  } on ArgumentError {
    // expected
  }
}

Future<void> _externalMfaCompletesWithTheCodeAndNeverResendsTheCallback() async {
  final gateway = _Gateway(callback: (202, 'mfa_required')), pending = await _start(gateway), redirect = _back(pending);
  final error = await _externalRefusal(completeExternalSignIn(pending, redirect));
  _expect('${error.status} ${error.code} ${gateway.paths.last}' == '202 mfa_required /auth/external/google/callback', 'mfa required: ${error.code}');
  _expect((await _externalRefusal(completeExternalSignIn(pending, redirect, mfaCode: '000000'))).code == 'mfa_invalid' && !gateway.paths.contains('/connect/authorize'), 'a wrong code stops before authorize');
  final session = await completeExternalSignIn(pending, redirect, mfaCode: '123456'); // the 2fa cookie rides along from the callback's answer
  _expect(await session.getAccessToken() == 'access-1', 'mfa sign-in');
  _expect(gateway.paths.where((path) => path == '/auth/external/google/callback').length == 1, 'the spent code is sent once');
}

void _expect(bool condition, String message) {
  if (!condition) throw StateError('FAILED: $message');
}
