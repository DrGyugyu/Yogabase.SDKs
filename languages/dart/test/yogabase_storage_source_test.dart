import 'dart:convert';

import 'package:crypto/crypto.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:microsoft_kiota_bundle/microsoft_kiota_bundle.dart';
import 'package:yogabase_client/src/models/invocation_request.dart';
import 'package:yogabase_client/yogabase_client.dart';
import 'package:uuid/uuid.dart';

/// A Storage object put and get of raw bytes, and a JSON body, through a real adapter against an in-memory
/// gateway; run with `dart run test/yogabase_storage_source_test.dart`. Nothing here reaches a network.
Future<void> main() async {
  final gateway = Uri.parse('https://apps.example.test');
  final seen = <http.Request>[];
  var stored = Uint8List(0);
  final client = MockClient((request) async {
    seen.add(request);
    if (request.url.path.endsWith('/invoke')) {
      return http.Response(jsonEncode({'outcome': 4, 'invocationId': const Uuid().v4()}), 200, headers: {'content-type': 'application/json'});
    }
    if (request.method == 'PUT') {
      stored = request.bodyBytes;
      return http.Response(jsonEncode({'key': 'a/b.bin', 'sizeBytes': stored.length, 'alreadyExisted': false}), 201, headers: {'content-type': 'application/json'});
    }
    return http.Response.bytes(stored, 200, headers: {'content-type': 'application/octet-stream'});
  });
  final api = YogabaseApplicationClient(DefaultRequestAdapter(authProvider: const AnonymousAuthenticationProvider(), client: client), gateway).api;

  final bytes = Uint8List.fromList([0, 1, 127, 128, 255]); // not valid UTF-8, so any text round trip would change it
  final object = storageObjectForLogicalKey(api.v1.storage.objects, gateway.origin, 'a/b.bin');
  final written = await object.putAsync(bytes, (request) {
    request.headers.put('X-Yogabase-Content-Sha256', 'sha256:${sha256.convert(bytes)}'); // the contract's two required headers
    request.headers.put('Idempotency-Key', 'k1');
  });
  _expect(written?.sizeBytes == 5 && written?.key == 'a/b.bin', 'the put is answered by its JSON result');
  _expect(seen.last.url.toString() == 'https://apps.example.test/v1/storage/objects/a/b.bin', 'put url ${seen.last.url}');
  _expect(seen.last.headers['content-type'] == 'application/octet-stream' && _same(seen.last.bodyBytes, bytes), 'the put sends the bytes as they are');
  _expect(seen.last.headers['x-yogabase-content-sha256'] == 'sha256:${sha256.convert(bytes)}' && seen.last.headers['idempotency-key'] == 'k1', 'the required headers are sent');
  _expect(_same((await object.getAsync())!.toList(), bytes), 'the get returns the bytes as they are');

  await api.v1.projects.byProjectId(UuidValue.fromString('11111111-1111-4111-8111-111111111111')).functions.byFunctionId('f').versions.byFunctionVersion('1').invoke.postAsync(InvocationRequest()..idempotencyKey = 'k');
  _expect(seen.last.headers['content-type'] == 'application/json' && jsonDecode(seen.last.body) is Map, 'a JSON body still goes through the JSON writer');
  print('dart storage source checks: PASS');
}

bool _same(List<int> a, List<int> b) => a.length == b.length && Iterable<int>.generate(a.length).every((i) => a[i] == b[i]);

void _expect(bool condition, String message) {
  if (!condition) throw StateError('FAILED: $message');
}
