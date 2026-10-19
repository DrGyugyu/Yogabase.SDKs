import 'dart:async';
import 'dart:convert';

import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:yogabase_client/yogabase_client.dart';
import 'package:yogabase_client/src/delivery/models/delivery_operation_envelope.dart';
import 'package:uuid/uuid.dart';

/// Standalone source-level checks; run with `dart run test/yogabase_delivery_source_test.dart`.
///
/// Every request goes to an in-memory [_RecordingRequestAdapter]; nothing here reaches a network.
void main() async {
  await _checkSseParsing();
  await _checkCancellationTokenAndBearerOrigin();
  _checkStorageLogicalKeys();
  _checkGeneratedApplicationSurface();
  await _checkPollingReturnsTerminalState();
  await _checkCancelledPollReleasesItsDelayTimer();
  await _checkPollCancelledBeforeItStartsSendsNothing();
  await _checkEventStreamRequestCarriesResumeHeaders();
  print('dart delivery source checks: PASS');
}

final _projectId = UuidValue.fromString('11111111-1111-4111-8111-111111111111');
final _operationId = UuidValue.fromString('22222222-2222-4222-8222-222222222222');

Future<void> _checkSseParsing() async {
  final events = YogabaseDeliverySseParser.parseText(
    ': heartbeat\n\n'
    'id: opaque:resume-token\n'
    'event: delivery-operation\n'
    'data: {"state":"Blocked"}\n\n',
  ).toList();
  _expect(events.length == 1, 'heartbeat should not become an event');
  _expect(events.single.id == 'opaque:resume-token', 'SSE id must stay opaque');
  _expect(events.single.event == 'delivery-operation', 'event type mismatch');
  _expect(events.single.dataJson == '{"state":"Blocked"}', 'SSE data mismatch');

  // The SSE grammar removes exactly one space after the colon; anything else is value.
  final spaced = YogabaseDeliverySseParser.parseText(
    'id:  two-space-resume\n'
    'event: delivery-operation\n'
    'data:   {"state":"Running"}\n\n',
  ).single;
  _expect(spaced.id == ' two-space-resume', 'only one leading space may be removed from an id');
  _expect(spaced.dataJson == '  {"state":"Running"}', 'only one leading space may be removed from data');

  _expect(
    YogabaseDeliveryClient.isTerminalOperationState('Blocked'),
    'Blocked must be terminal',
  );
  _expect(
    !YogabaseDeliveryClient.isTerminalOperationState('Running'),
    'Running must not be terminal',
  );
}

Future<void> _checkCancellationTokenAndBearerOrigin() async {
  final token = YogabaseCancellationToken();
  _expect(!token.isCancelled, 'token should start active');
  token.cancel();
  _expect(token.isCancelled, 'cancel should complete the token');
  var cancellationObserved = false;
  try {
    await token.whenCancelled;
    cancellationObserved = token.isCancelled;
  } catch (_) {
    cancellationObserved = true;
  }
  _expect(cancellationObserved, 'cancellation completion should be observable');
  token.cancel();
  _expect(token.isCancelled, 'a second cancel must be a no-op');

  final provider = YogabaseDeliveryBearerTokenProvider(
    deliveryOrigin: Uri.parse('https://delivery.example.test'),
    tokenProvider: (_, _) async => 'delivery-token',
  );
  _expect(
    await provider.getAuthorizationToken(
          Uri.parse('https://delivery.example.test/api/v1/delivery'),
        ) ==
        'delivery-token',
    'the exact delivery origin should receive the token',
  );
  _expect(
    await provider.getAuthorizationToken(
          Uri.parse('http://delivery.example.test/api/v1/delivery'),
        ) ==
        '',
    'an HTTP downgrade must not receive the token',
  );
  var insecureOriginRejected = false;
  try {
    YogabaseDeliveryBearerTokenProvider(
      deliveryOrigin: Uri.parse('http://delivery.example.test'),
      tokenProvider: (_, _) async => 'delivery-token',
    );
  } on ArgumentError {
    insecureOriginRejected = true;
  }
  _expect(insecureOriginRejected, 'delivery origins must use HTTPS');
}

/// The same valid and invalid keys as languages/python/tests/test_storage_logical_key.py.
void _checkStorageLogicalKeys() {
  _expect(
    buildStorageObjectUrl('https://apps.example.test:8443/', 'avatars/My photo (1).png') ==
        'https://apps.example.test:8443/v1/storage/objects/avatars/My%20photo%20%281%29.png',
    'hierarchical keys keep slashes and encode each segment',
  );
  _expect(
    buildStorageObjectUrl('https://apps.example.test/', 'café/猫.json') ==
        'https://apps.example.test/v1/storage/objects/caf%C3%A9/%E7%8C%AB.json',
    'non-ASCII segments are UTF-8 percent-encoded',
  );
  final invalidKeys = <String>[
    '', ' ', '/a', 'projects/a', 'https://example.test/a', r'a\b', 'a%b',
    'a\u0000b', 'a//b', '.', '..', 'a/./b', 'a/../b', 'x' * 1025,
  ];
  for (final key in invalidKeys) {
    var rejected = false;
    try {
      buildStorageObjectUrl('https://apps.example.test/', key);
    } on ArgumentError {
      rejected = true;
    }
    _expect(rejected, 'invalid storage key must fail closed: ${jsonEncode(key.length > 16 ? '${key.substring(0, 16)}...' : key)}');
  }
}

void _checkGeneratedApplicationSurface() {
  final adapter = _RecordingRequestAdapter();
  final client = YogabaseApplicationClient(adapter, Uri.parse('https://apps.example.test'));
  _expect(adapter.baseUrl == 'https://apps.example.test', 'application origin becomes the adapter base URL');

  final item = storageObjectForLogicalKey(
    client.api.v1.storage.objects,
    adapter.baseUrl!,
    'avatars/user 1/photo.png',
  );
  _expect(
    item.toGetRequestInformation().uri.toString() ==
        'https://apps.example.test/v1/storage/objects/avatars/user%201/photo.png',
    'the logical-key adapter must address the generated item builder by raw URL',
  );

  final list = client.api.v1.servers.toGetRequestInformation();
  _expect(
    list.httpMethod == HttpMethod.get && list.uri.toString() == 'https://apps.example.test/v1/servers',
    'generated game-server list request: ${list.httpMethod} ${list.uri}',
  );
  final stop = client.api.v1.servers.byServerId('srv-1').stop.toPostRequestInformation();
  _expect(
    stop.httpMethod == HttpMethod.post && stop.uri.toString() == 'https://apps.example.test/v1/servers/srv-1/stop',
    'generated game-server stop request: ${stop.httpMethod} ${stop.uri}',
  );
}

Future<void> _checkPollingReturnsTerminalState() async {
  final adapter = _RecordingRequestAdapter()
    ..operations.add(DeliveryOperationEnvelope()..state = 'Running')
    ..operations.add(DeliveryOperationEnvelope()..state = 'Succeeded');
  final client = YogabaseDeliveryClient(adapter, Uri.parse('https://delivery.example.test'));
  final operation = await client.pollOperation(
    projectId: _projectId,
    operationId: _operationId,
    interval: const Duration(milliseconds: 1),
    maxAttempts: 3,
  );
  _expect(operation.state == 'Succeeded', 'polling must return the terminal operation');
  _expect(adapter.requests.length == 2, 'polling must stop at the terminal state');
  _expect(
    adapter.requests.first.uri.toString() ==
        'https://delivery.example.test/api/v1/delivery/projects/$_projectId/operations/$_operationId',
    'poll request URI: ${adapter.requests.first.uri}',
  );
}

/// Cancelling during the polling delay must end the poll and release the delay's timer; a live
/// timer would keep the isolate running for the whole interval after the caller gave up.
Future<void> _checkCancelledPollReleasesItsDelayTimer() async {
  final timers = <Timer>[];
  final delayStarted = Completer<void>();
  final adapter = _RecordingRequestAdapter()
    ..operations.add(DeliveryOperationEnvelope()..state = 'Running');
  final client = YogabaseDeliveryClient(adapter, Uri.parse('https://delivery.example.test'));
  final token = YogabaseCancellationToken();

  final polling = runZoned(
    () => client.pollOperation(
      projectId: _projectId,
      operationId: _operationId,
      interval: const Duration(hours: 1),
      maxAttempts: 2,
      cancellationToken: token,
    ),
    zoneSpecification: ZoneSpecification(
      createTimer: (self, parent, zone, duration, callback) {
        final timer = parent.createTimer(zone, duration, callback);
        timers.add(timer);
        if (!delayStarted.isCompleted) {
          delayStarted.complete();
        }
        return timer;
      },
    ),
  );

  await delayStarted.future;
  token.cancel();
  var cancelled = false;
  try {
    await polling;
  } on YogabaseOperationPollingCancelledException {
    cancelled = true;
  }
  final active = timers.where((timer) => timer.isActive).toList();
  for (final timer in active) {
    timer.cancel();
  }
  _expect(cancelled, 'cancelling during the delay must end the poll');
  _expect(adapter.requests.length == 1, 'no request may follow cancellation');
  _expect(
    active.isEmpty,
    'a cancelled poll left ${active.length} of ${timers.length} delay timer(s) running',
  );
}

Future<void> _checkPollCancelledBeforeItStartsSendsNothing() async {
  final adapter = _RecordingRequestAdapter();
  final client = YogabaseDeliveryClient(adapter, Uri.parse('https://delivery.example.test'));
  final token = YogabaseCancellationToken()..cancel();
  var cancelled = false;
  try {
    await client.pollOperation(
      projectId: _projectId,
      operationId: _operationId,
      cancellationToken: token,
    );
  } on YogabaseOperationPollingCancelledException {
    cancelled = true;
  }
  _expect(cancelled, 'an already-cancelled token must stop polling');
  _expect(adapter.requests.isEmpty, 'an already-cancelled poll must send no request');
}

Future<void> _checkEventStreamRequestCarriesResumeHeaders() async {
  final adapter = _RecordingRequestAdapter()
    ..eventStreamBytes = utf8.encode(
      ': heartbeat\n\nid: opaque:1\nevent: delivery-operation\ndata: {"state":"Succeeded"}\n\n',
    );
  final client = YogabaseDeliveryClient(adapter, Uri.parse('https://delivery.example.test'));
  final events = await client
      .streamOperationEvents(projectId: _projectId, operationId: _operationId, lastEventId: 'opaque:0')
      .toList();
  _expect(events.length == 1 && events.single.id == 'opaque:1', 'the buffered stream yields one resumable event');
  final request = adapter.requests.single;
  _expect(request.headers['Last-Event-ID']?.single == 'opaque:0', 'Last-Event-ID must be sent verbatim');
  _expect(
    request.headers['Accept']?.single == 'text/event-stream, application/problem+json',
    'the stream must accept text/event-stream: ${request.headers['Accept']}',
  );
  _expect(
    request.uri.toString() ==
        'https://delivery.example.test/api/v1/delivery/projects/$_projectId/operations/events?operationId=$_operationId',
    'event stream request URI: ${request.uri}',
  );
}

/// In-memory [RequestAdapter] that records each request and answers from queued results.
class _RecordingRequestAdapter implements RequestAdapter {
  final List<RequestInformation> requests = <RequestInformation>[];
  final List<DeliveryOperationEnvelope> operations = <DeliveryOperationEnvelope>[];
  Iterable<int>? eventStreamBytes;

  @override
  String? baseUrl;

  @override
  SerializationWriterFactory get serializationWriterFactory => throw UnimplementedError();

  @override
  Future<ModelType?> send<ModelType extends Parsable>(
    RequestInformation requestInfo,
    ParsableFactory<ModelType> factory, [
    Map<String, ParsableFactory<Parsable>>? errorMapping,
  ]) async {
    requests.add(requestInfo);
    return operations.removeAt(0) as ModelType;
  }

  @override
  Future<Iterable<ModelType>?> sendCollection<ModelType extends Parsable>(
    RequestInformation requestInfo,
    ParsableFactory<ModelType> factory, [
    Map<String, ParsableFactory<Parsable>>? errorMapping,
  ]) async {
    requests.add(requestInfo);
    return <ModelType>[];
  }

  @override
  Future<ModelType?> sendPrimitive<ModelType>(
    RequestInformation requestInfo, [
    Map<String, ParsableFactory<Parsable>>? errorMapping,
  ]) async {
    requests.add(requestInfo);
    return null;
  }

  @override
  Future<Iterable<ModelType>?> sendPrimitiveCollection<ModelType>(
    RequestInformation requestInfo, [
    Map<String, ParsableFactory<Parsable>>? errorMapping,
  ]) async {
    requests.add(requestInfo);
    return eventStreamBytes as Iterable<ModelType>?;
  }

  @override
  Future<void> sendNoContent(
    RequestInformation requestInfo, [
    Map<String, ParsableFactory<Parsable>>? errorMapping,
  ]) async {
    requests.add(requestInfo);
  }

  @override
  Future<T?> convertToNativeRequest<T>(RequestInformation requestInfo) async => null;

  @override
  void enableBackingStore(BackingStoreFactory backingStoreFactory) {}
}

void _expect(bool condition, String message) {
  if (!condition) {
    throw StateError(message);
  }
}
