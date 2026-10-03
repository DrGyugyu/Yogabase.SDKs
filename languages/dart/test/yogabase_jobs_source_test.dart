import 'dart:typed_data';

import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:stackyoga_yogabase_client_preview/stackyoga_yogabase_client_preview.dart';
import 'package:stackyoga_yogabase_client_preview/src/models/problem.dart';

/// Standalone source-level checks of the authored Jobs client; run with
/// `dart run test/yogabase_jobs_source_test.dart`.
///
/// Every request goes to an in-memory [_RecordingRequestAdapter]; nothing here
/// reaches a network. The shapes pinned here are the ones the product's gateway
/// writes (DurableJobsGateway) and its developer documentation states. This
/// program was written without a Dart SDK on hand and has not been run or
/// analyzed; the submit request body and the response parsing are not covered
/// here because they need a real serialization writer factory and a real
/// adapter, which the other languages' tests do cover.
Future<void> main() async {
  _checkStatusNamesAndTerminalStates();
  _checkRequestShapes();
  await _checkMalformedInputIsRefusedBeforeTheAdapter();
  _checkRefusalsKeepTheirStatusCodeAndLimits();
  _checkTheApplicationClientHandsOutTheJobsClient();
  print('dart jobs source checks: PASS');
}

const _project = '11111111-1111-4111-8111-111111111111';
const _job = '5d0a8f0e-3c63-4d5e-9a0b-7c1f3a9e2b41';
const _jobs = 'https://apps.example.test/v1/projects/$_project/jobs';

void _checkStatusNamesAndTerminalStates() {
  const settled = {'Succeeded', 'Cancelled', 'DeadLettered'};
  for (final status in YogabaseJobStatus.values) {
    _expect(
      YogabaseJobStatus.fromWireName(status.wireName) == status,
      '${status.wireName} must read back as itself',
    );
    _expect(
      status.isTerminal == settled.contains(status.wireName),
      '${status.wireName} terminal flag',
    );
  }
  _expect(
    YogabaseJobStatus.fromWireName('Paused') == null,
    'a state this client was never told of is not guessed at',
  );
  _expect(
    YogabaseJobStatus.fromWireName('queued') == null,
    'status names are exact',
  );
}

void _checkRequestShapes() {
  final adapter = _RecordingRequestAdapter();
  final client = YogabaseJobsClient(
    adapter,
    Uri.parse('https://apps.example.test/'),
    _project,
  );
  _expect(
    adapter.baseUrl == 'https://apps.example.test',
    'the origin becomes the adapter base URL: ${adapter.baseUrl}',
  );

  final read = client.toGetRequestInformation(_job);
  _expect(
    read.httpMethod == HttpMethod.get && read.uri.toString() == '$_jobs/$_job',
    'read request: ${read.httpMethod} ${read.uri}',
  );
  _expect(
    read.headers['Accept']?.single == 'application/json, application/problem+json',
    'read Accept: ${read.headers['Accept']}',
  );

  final cancel = client.toCancelRequestInformation(_job);
  _expect(
    cancel.httpMethod == HttpMethod.post &&
        cancel.uri.toString() == '$_jobs/$_job/cancel',
    'cancel request: ${cancel.httpMethod} ${cancel.uri}',
  );

  final result = client.toResultRequestInformation(_job);
  _expect(
    result.httpMethod == HttpMethod.get &&
        result.uri.toString() == '$_jobs/$_job/result',
    'result request: ${result.httpMethod} ${result.uri}',
  );
  _expect(
    result.headers['Accept']?.single ==
        'application/octet-stream, application/problem+json',
    'result Accept: ${result.headers['Accept']}',
  );
  _expect(adapter.requests.isEmpty, 'building a request sends nothing');
}

Future<void> _checkMalformedInputIsRefusedBeforeTheAdapter() async {
  final adapter = _RecordingRequestAdapter();
  final client = YogabaseJobsClient(
    adapter,
    Uri.parse('https://apps.example.test'),
    _project,
  );

  final submissions = <String, Future<Object?> Function()>{
    'a job type with a space': () =>
        client.submit(jobType: 'orders create', idempotencyKey: 'k'),
    'an empty job type': () => client.submit(jobType: '', idempotencyKey: 'k'),
    'a job type over 128 characters': () =>
        client.submit(jobType: 'a' * 129, idempotencyKey: 'k'),
    'a padded idempotency key': () =>
        client.submit(jobType: 'orders-create', idempotencyKey: ' k'),
    'an empty idempotency key': () =>
        client.submit(jobType: 'orders-create', idempotencyKey: ''),
    'an idempotency key with a control character': () =>
        client.submit(jobType: 'orders-create', idempotencyKey: 'a\nb'),
    'an idempotency key over 256 characters': () =>
        client.submit(jobType: 'orders-create', idempotencyKey: 'k' * 257),
    'a payload over 1 MiB': () => client.submit(
          jobType: 'orders-create',
          idempotencyKey: 'k',
          payload: Uint8List(1048577),
        ),
    'an attempt budget of 0': () => client.submit(
          jobType: 'orders-create',
          idempotencyKey: 'k',
          maxAttempts: 0,
        ),
    'an attempt budget of 11': () => client.submit(
          jobType: 'orders-create',
          idempotencyKey: 'k',
          maxAttempts: 11,
        ),
    'a read of a job id that is not a UUID': () => client.get('not-a-guid'),
    'a cancel of the empty job id': () =>
        client.cancel('00000000-0000-0000-0000-000000000000'),
    'a result of a job id that is not a UUID': () => client.result('x'),
  };
  for (final entry in submissions.entries) {
    var refused = false;
    try {
      await entry.value();
    } on ArgumentError {
      refused = true;
    }
    _expect(refused, '${entry.key} must be refused');
  }
  _expect(adapter.requests.isEmpty, 'a refused call must send no request');

  var originRefused = false;
  try {
    YogabaseJobsClient(adapter, Uri.parse('https://apps.example.test/path'), _project);
  } on ArgumentError {
    originRefused = true;
  }
  _expect(originRefused, 'the origin must be origin-only');
  var projectRefused = false;
  try {
    YogabaseJobsClient(
      adapter,
      Uri.parse('https://apps.example.test'),
      '00000000-0000-0000-0000-000000000000',
    );
  } on ArgumentError {
    projectRefused = true;
  }
  _expect(projectRefused, 'the project must be a non-empty UUID');
}

void _checkRefusalsKeepTheirStatusCodeAndLimits() {
  final tooLarge = YogabaseJobsException.fromProblem(
    Problem(
      additionalData: <String, Object?>{
        'limitBytes': 1048576,
        'maxBodyBytes': 8391225,
      },
      statusCode: 413,
      code: YogabaseJobErrorCodes.payloadTooLarge,
      detail: 'The request is larger than this route accepts.',
    ),
  );
  _expect(tooLarge.status == 413, 'status: ${tooLarge.status}');
  _expect(
    tooLarge.code == 'job_payload_too_large',
    'code: ${tooLarge.code}',
  );
  _expect(tooLarge.limitBytes == 1048576, 'limitBytes: ${tooLarge.limitBytes}');
  _expect(
    tooLarge.maxBodyBytes == 8391225,
    'maxBodyBytes: ${tooLarge.maxBodyBytes}',
  );

  final unnamed = YogabaseJobsException.fromProblem(
    Problem(
      additionalData: <String, Object?>{'limitBytes': -1, 'maxBodyBytes': '8391225'},
      statusCode: 404,
    ),
  );
  _expect(unnamed.code == 'not_found', 'a problem with no code reads from its status: ${unnamed.code}');
  _expect(
    unnamed.limitBytes == null && unnamed.maxBodyBytes == null,
    'limits that are not positive whole numbers are not reported',
  );

  _expect(
    YogabaseJobsException.fromStatus(413).code == 'payload_too_large',
    'a bare 413 reads as payload_too_large',
  );
  _expect(
    YogabaseJobsException.fromStatus(404).code == 'not_found' &&
        YogabaseJobsException.fromStatus(404).code != YogabaseJobErrorCodes.notFound,
    'a plain 404 is not job_not_found',
  );
}

void _checkTheApplicationClientHandsOutTheJobsClient() {
  final adapter = _RecordingRequestAdapter();
  final application = YogabaseApplicationClient(
    adapter,
    Uri.parse('https://apps.example.test'),
  );
  final jobs = application.jobs(_project);
  _expect(jobs.origin == application.origin, 'the jobs client shares the origin');
  _expect(
    identical(jobs.requestAdapter, application.requestAdapter),
    'the jobs client shares the adapter',
  );
  _expect(jobs.projectId == _project, 'the jobs client names its project');
}

/// In-memory [RequestAdapter] that records each request.
class _RecordingRequestAdapter implements RequestAdapter {
  final List<RequestInformation> requests = <RequestInformation>[];

  @override
  String? baseUrl;

  @override
  SerializationWriterFactory get serializationWriterFactory =>
      throw UnimplementedError();

  @override
  Future<ModelType?> send<ModelType extends Parsable>(
    RequestInformation requestInfo,
    ParsableFactory<ModelType> factory, [
    Map<String, ParsableFactory<Parsable>>? errorMapping,
  ]) async {
    requests.add(requestInfo);
    return null;
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
    return null;
  }

  @override
  Future<void> sendNoContent(
    RequestInformation requestInfo, [
    Map<String, ParsableFactory<Parsable>>? errorMapping,
  ]) async {
    requests.add(requestInfo);
  }

  @override
  Future<T?> convertToNativeRequest<T>(RequestInformation requestInfo) async =>
      null;

  @override
  void enableBackingStore(BackingStoreFactory backingStoreFactory) {}
}

void _expect(bool condition, String message) {
  if (!condition) {
    throw StateError(message);
  }
}
