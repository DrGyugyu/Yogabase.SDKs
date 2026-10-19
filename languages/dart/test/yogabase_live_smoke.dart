import 'dart:convert';
import 'dart:io';

import 'package:crypto/crypto.dart';
import 'package:microsoft_kiota_bundle/microsoft_kiota_bundle.dart';
import 'package:yogabase_client/src/models/invocation_request.dart';
import 'package:yogabase_client/yogabase_client.dart';
import 'package:uuid/uuid.dart';

/// Signs in to a real installation, invokes a function, then puts and gets an object; run with
/// `dart run test/yogabase_live_smoke.dart`. It does nothing unless these are set: YOGABASE_SMOKE_GATEWAY (the
/// gateway origin), _PUBLISHABLE_KEY, _EMAIL and _PASSWORD (an existing, confirmed user without MFA), _FUNCTION_ID
/// and _FUNCTION_VERSION (a deployed stateless function that accepts `{}`).
Future<void> main() async {
  const names = ['GATEWAY', 'PUBLISHABLE_KEY', 'EMAIL', 'PASSWORD', 'FUNCTION_ID', 'FUNCTION_VERSION'];
  final env = {for (final name in names) name: Platform.environment['YOGABASE_SMOKE_$name'] ?? ''};
  final missing = names.where((name) => env[name]!.isEmpty);
  if (missing.isNotEmpty) {
    print('dart live smoke: SKIPPED (set YOGABASE_SMOKE_*; missing: ${missing.join(', ')})');
    return;
  }
  final gateway = Uri.parse(env['GATEWAY']!);
  final session = await signInWithPassword(gateway, env['PUBLISHABLE_KEY']!, env['EMAIL']!, env['PASSWORD']!);
  final api = YogabaseApplicationClient(DefaultRequestAdapter(authProvider: session), gateway).api;

  final payload = utf8.encode('{}');
  final invoked = await api.v1.projects
      .byProjectId(UuidValue.fromString(session.projectId))
      .functions
      .byFunctionId(env['FUNCTION_ID']!)
      .versions
      .byFunctionVersion(env['FUNCTION_VERSION']!)
      .invoke
      .postAsync(InvocationRequest()
        ..idempotencyKey = const Uuid().v4()
        ..correlationId = 'live-smoke'
        ..deadline = DateTime.now().toUtc().add(const Duration(seconds: 20))
        ..mode = 0 // synchronous
        ..payloadDigest = 'sha256:${sha256.convert(payload)}'
        ..inlinePayloadBase64 = base64.encode(payload));
  if (invoked?.outcome != 4) throw StateError('invoke outcome ${invoked?.outcome}, expected 4 (Completed)');

  final bytes = Uint8List.fromList(utf8.encode('live smoke ${const Uuid().v4()}'));
  final object = storageObjectForLogicalKey(api.v1.storage.objects, gateway.origin, 'smoke/${const Uuid().v4()}.txt');
  await object.putAsync(bytes, (request) {
    request.headers.put('X-Yogabase-Content-Sha256', 'sha256:${sha256.convert(bytes)}');
    request.headers.put('Idempotency-Key', const Uuid().v4());
  });
  if (utf8.decode((await object.getAsync())!.toList()) != utf8.decode(bytes)) throw StateError('the stored object differs');

  print('dart live smoke: PASS');
}
