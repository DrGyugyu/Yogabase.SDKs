import 'dart:convert';

import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:yogabase_client/yogabase_client.dart';
import 'package:yogabase_client/src/models/problem.dart';

/// Standalone source-level checks of the authored schedule client; run with
/// `dart run test/yogabase_schedules_source_test.dart`.
///
/// Every request goes to an in-memory [_RecordingRequestAdapter]; nothing here
/// reaches a network. The shapes pinned here are the ones the mirrored client
/// OpenAPI document declares for the schedule routes (`x-yogabase-schedules`
/// and the four operations) and the product's gateway writes, and each request
/// is the one the generated Kiota builder builds. The response parsing
/// is not covered here because it needs a real adapter, which the other
/// languages' tests do cover.
Future<void> main() async {
  await _checkCreateAndUpdateAreConditionalPutsOfTheExactBody();
  await _checkAnAbsentOrEmptyPayloadIsOmittedAndAOneShotIsSentInUtc();
  await _checkReadDeleteAndTriggerShapes();
  await _checkAWriteHoldsTheIdRuleAndAnOlderIdIsStillAddressed();
  await _checkMalformedInputIsRefusedBeforeTheAdapter();
  _checkRefusalsKeepTheirStatusCodeAndLimits();
  _checkTheApplicationClientHandsOutTheSchedulesClient();
  print('dart schedules source checks: PASS');
}

const _project = '11111111-1111-4111-8111-111111111111';
const _instance = '0123456789abcdef0123456789abcdef';
const _eTag = '"schedule-v3-$_instance"';
const _item =
    'https://apps.example.test/v1/projects/$_project/schedules/nightly';

YogabaseScheduleDefinition _definition({
  String scheduleId = 'nightly',
  Uint8List? payload,
  DateTime? oneShotAtUtc,
  YogabaseScheduleMisfirePolicy misfire =
      YogabaseScheduleMisfirePolicy.skipToNextOccurrence,
  YogabaseScheduleConcurrencyPolicy concurrency =
      YogabaseScheduleConcurrencyPolicy.allow,
  int maxCatchUp = 1,
  int maxAttempts = 3,
  int deadlineSeconds = 300,
  String functionId = 'orders-nightly',
  String functionVersion = '1',
  String cron = '*/5 * * * *',
  String timeZone = 'UTC',
}) =>
    YogabaseScheduleDefinition(
      scheduleId: scheduleId,
      functionId: functionId,
      functionVersion: functionVersion,
      cron: cron,
      timeZone: timeZone,
      misfirePolicy: misfire,
      concurrencyPolicy: concurrency,
      maxCatchUp: maxCatchUp,
      maxAttempts: maxAttempts,
      deadlineSeconds: deadlineSeconds,
      payload: payload,
      oneShotAtUtc: oneShotAtUtc,
    );

YogabaseSchedulesClient _client(_RecordingRequestAdapter adapter) =>
    YogabaseSchedulesClient(
      adapter,
      Uri.parse('https://apps.example.test/'),
      _project,
    );

/// Runs [call] and ignores the refusal the recording adapter's empty answer
/// produces: only the request it sent is under test.
Future<void> _send(Future<Object?> Function() call) async {
  try {
    await call();
  } on YogabaseSchedulesException {
    // The recording adapter answers nothing, which is not a schedule.
  }
}

Map<String, dynamic> _body(RequestInformation request) =>
    jsonDecode(utf8.decode(request.content!)) as Map<String, dynamic>;

Future<void> _checkCreateAndUpdateAreConditionalPutsOfTheExactBody() async {
  final adapter = _RecordingRequestAdapter();
  final client = _client(adapter);
  _expect(
    adapter.baseUrl == 'https://apps.example.test',
    'the origin becomes the adapter base URL: ${adapter.baseUrl}',
  );

  await _send(() => client.create(
        _definition(payload: Uint8List.fromList(utf8.encode('hello'))),
      ));
  final create = adapter.requests.single;
  _expect(
    create.httpMethod == HttpMethod.put && create.uri.toString() == _item,
    'create request: ${create.httpMethod} ${create.uri}',
  );
  // Exactly one of the two preconditions: a create is conditional on there
  // being nothing there.
  _expect(
    create.headers['If-None-Match']?.single == '*' &&
        create.headers['If-Match'] == null,
    'create preconditions: ${create.headers}',
  );
  _expect(
    create.headers['Content-Type']?.single == 'application/json',
    'create Content-Type: ${create.headers['Content-Type']}',
  );
  final body = _body(create);
  _expect(
    body.length == 11 &&
        body['scheduleId'] == 'nightly' &&
        body['functionId'] == 'orders-nightly' &&
        body['functionVersion'] == '1' &&
        body['cron'] == '*/5 * * * *' &&
        body['timeZone'] == 'UTC' &&
        body['misfirePolicy'] == 'SkipToNextOccurrence' &&
        body['concurrencyPolicy'] == 'Allow' &&
        body['maxCatchUp'] == 1 &&
        body['maxAttempts'] == 3 &&
        body['deadlineSeconds'] == 300 &&
        body['payloadBase64'] == 'aGVsbG8=',
    'create body: $body',
  );

  adapter.requests.clear();
  await _send(() => client.update(_definition(), _eTag));
  final update = adapter.requests.single;
  _expect(
    update.httpMethod == HttpMethod.put && update.uri.toString() == _item,
    'update request: ${update.httpMethod} ${update.uri}',
  );
  _expect(
    update.headers['If-Match']?.single == _eTag &&
        update.headers['If-None-Match'] == null,
    'update preconditions: ${update.headers}',
  );

  // Every policy is sent by its name.
  adapter.requests.clear();
  const misfires = {
    YogabaseScheduleMisfirePolicy.runImmediately: 'RunImmediately',
    YogabaseScheduleMisfirePolicy.skipToNextOccurrence: 'SkipToNextOccurrence',
    YogabaseScheduleMisfirePolicy.runAllMissed: 'RunAllMissed',
  };
  const concurrencies = {
    YogabaseScheduleConcurrencyPolicy.allow: 'Allow',
    YogabaseScheduleConcurrencyPolicy.forbid: 'Forbid',
    YogabaseScheduleConcurrencyPolicy.replace: 'Replace',
  };
  for (final misfire in misfires.entries) {
    for (final concurrency in concurrencies.entries) {
      adapter.requests.clear();
      await _send(() => client.create(
            _definition(misfire: misfire.key, concurrency: concurrency.key),
          ));
      final policies = _body(adapter.requests.single);
      _expect(
        policies['misfirePolicy'] == misfire.value &&
            policies['concurrencyPolicy'] == concurrency.value,
        'policies: $policies',
      );
    }
  }
}

Future<void> _checkAnAbsentOrEmptyPayloadIsOmittedAndAOneShotIsSentInUtc() async {
  final adapter = _RecordingRequestAdapter();
  final client = _client(adapter);

  for (final payload in <Uint8List?>[null, Uint8List(0)]) {
    adapter.requests.clear();
    await _send(() => client.create(_definition(payload: payload)));
    final body = _body(adapter.requests.single);
    _expect(
      !body.containsKey('payloadBase64') && !body.containsKey('oneShotAtUtc'),
      'an absent and an empty payload are both omitted: $body',
    );
  }

  // 12:00 at +09:00 is 03:00 UTC, and the gateway requires a UTC instant.
  adapter.requests.clear();
  await _send(() => client.create(
        _definition(oneShotAtUtc: DateTime.parse('2026-10-03T12:00:00+09:00')),
      ));
  final instant = _body(adapter.requests.single)['oneShotAtUtc'] as String;
  _expect(
    DateTime.parse(instant).isAtSameMomentAs(DateTime.utc(2026, 10, 3, 3)) &&
        (instant.endsWith('Z') || instant.endsWith('+00:00')),
    'the one-shot instant is sent in UTC: $instant',
  );

  // A payload at exactly the inline limit is sent and one byte more is refused.
  adapter.requests.clear();
  await _send(() => client.create(_definition(payload: Uint8List(1048576))));
  _expect(
    (_body(adapter.requests.single)['payloadBase64'] as String).length ==
        1398104,
    'a payload at the limit is 1,398,104 base64 characters',
  );
}

Future<void> _checkReadDeleteAndTriggerShapes() async {
  final adapter = _RecordingRequestAdapter();
  final client = _client(adapter);

  await _send(() => client.get('nightly'));
  final read = adapter.requests.single;
  _expect(
    read.httpMethod == HttpMethod.get && read.uri.toString() == _item,
    'read request: ${read.httpMethod} ${read.uri}',
  );
  _expect(read.content == null, 'a read sends no body');

  adapter.requests.clear();
  await client.delete('nightly', _eTag);
  final delete = adapter.requests.single;
  _expect(
    delete.httpMethod == HttpMethod.delete && delete.uri.toString() == _item,
    'delete request: ${delete.httpMethod} ${delete.uri}',
  );
  _expect(
    delete.headers['If-Match']?.single == _eTag && delete.content == null,
    'delete precondition: ${delete.headers}',
  );

  adapter.requests.clear();
  await _send(() => client.trigger(
        'nightly',
        _eTag,
        YogabaseScheduleTriggerRequest(
          idempotencyKey: 'run-1',
          deadline: DateTime.parse('2026-10-03T21:30:00+09:00'),
        ),
      ));
  final run = adapter.requests.single;
  _expect(
    run.httpMethod == HttpMethod.post && run.uri.toString() == '$_item/trigger',
    'trigger request: ${run.httpMethod} ${run.uri}',
  );
  _expect(
    run.headers['If-Match']?.single == _eTag && run.headers['If-None-Match'] == null,
    'trigger precondition: ${run.headers}',
  );
  final body = _body(run);
  _expect(
    body.length == 2 &&
        body['idempotencyKey'] == 'run-1' &&
        DateTime.parse(body['deadline'] as String)
            .isAtSameMomentAs(DateTime.utc(2026, 10, 3, 12, 30)),
    'trigger body (21:30 at +09:00 is 12:30 UTC): $body',
  );

  // A schedule id that needs escaping travels as one escaped path segment.
  adapter.requests.clear();
  await _send(() => client.get('weekly report/é?#'));
  _expect(
    adapter.requests.single.uri.toString() ==
        'https://apps.example.test/v1/projects/$_project/schedules/'
            'weekly%20report%2F%C3%A9%3F%23',
    'escaped id: ${adapter.requests.single.uri}',
  );
}

/// The id a request addresses: the path segment after `schedules`, decoded, so
/// that however the generated builder escapes a character the id is compared
/// as the gateway reads it.
String _scheduleIdOf(RequestInformation request) {
  final segments = request.uri.pathSegments;
  return segments[segments.indexOf('schedules') + 1];
}

Future<void> _checkAWriteHoldsTheIdRuleAndAnOlderIdIsStillAddressed() async {
  final adapter = _RecordingRequestAdapter();
  final client = _client(adapter);

  // An id that follows the rule is sent unchanged in the body and travels as
  // one segment of the path.
  final followers = <String>[
    'a',
    'Z',
    '0',
    'nightly',
    'a.b_c~d:e-f',
    'orders-nightly.v2:eu~west_1',
    'a' * 128,
  ];
  for (final id in followers) {
    adapter.requests.clear();
    await _send(() => client.create(_definition(scheduleId: id)));
    final request = adapter.requests.single;
    _expect(
      _scheduleIdOf(request) == id && _body(request)['scheduleId'] == id,
      'an id that follows the rule travels as one segment: ${request.uri}',
    );
  }

  // An older id, written before the rule, is still read, run and deleted by its
  // id, percent-encoded as any path segment is, but a write of it is refused.
  const older = 'weekly report/é?#';
  adapter.requests.clear();
  await _send(() => client.get(older));
  await _send(() => client.trigger(
        older,
        _eTag,
        YogabaseScheduleTriggerRequest(
          idempotencyKey: 'run-1',
          deadline: DateTime.now().toUtc().add(const Duration(minutes: 10)),
        ),
      ));
  await client.delete(older, _eTag);
  _expect(
    adapter.requests.length == 3 &&
        adapter.requests.every((request) => _scheduleIdOf(request) == older),
    'an older id is addressed by a read, a run and a delete: '
    '${adapter.requests.map((request) => request.uri).toList()}',
  );

  adapter.requests.clear();
  final writes = <Future<Object?> Function()>[
    () => client.create(_definition(scheduleId: older)),
    () => client.update(_definition(scheduleId: older), _eTag),
  ];
  for (final write in writes) {
    var refused = false;
    try {
      await write();
    } on ArgumentError {
      refused = true;
    }
    _expect(refused, 'a write of an older id must be refused');
  }
  _expect(adapter.requests.isEmpty, 'a refused write must send no request');
}

Future<void> _checkMalformedInputIsRefusedBeforeTheAdapter() async {
  final adapter = _RecordingRequestAdapter();
  final client = _client(adapter);
  final deadline = DateTime.now().toUtc().add(const Duration(minutes: 10));

  final calls = <String, Future<Object?> Function()>{
    'a blank schedule id': () => client.create(_definition(scheduleId: '  ')),
    'a padded schedule id': () =>
        client.create(_definition(scheduleId: ' nightly')),
    'a schedule id with a control character': () =>
        client.create(_definition(scheduleId: 'night\nly')),
    'a schedule id over 128 characters': () =>
        client.create(_definition(scheduleId: 'x' * 129)),
    'a schedule id of one dot': () =>
        client.create(_definition(scheduleId: '.')),
    'a schedule id of two dots': () =>
        client.create(_definition(scheduleId: '..')),
    'a schedule id with trailing white space': () =>
        client.create(_definition(scheduleId: 'nightly ')),
    'a schedule id with a slash': () =>
        client.create(_definition(scheduleId: 'weekly/report')),
    'a schedule id with a space inside': () =>
        client.create(_definition(scheduleId: 'weekly report')),
    'a schedule id that starts with a dash': () =>
        client.create(_definition(scheduleId: '-nightly')),
    'a schedule id that starts with a dot': () =>
        client.create(_definition(scheduleId: '.nightly')),
    'a schedule id that starts with an underscore': () =>
        client.create(_definition(scheduleId: '_nightly')),
    'a schedule id that starts with a tilde': () =>
        client.create(_definition(scheduleId: '~nightly')),
    'a schedule id that starts with a colon': () =>
        client.create(_definition(scheduleId: ':nightly')),
    'a schedule id with a percent sign': () =>
        client.create(_definition(scheduleId: 'weekly%2Freport')),
    'a schedule id with a question mark': () =>
        client.create(_definition(scheduleId: 'nightly?')),
    'a schedule id with a hash': () =>
        client.create(_definition(scheduleId: 'night#ly')),
    'a schedule id with a non-ASCII letter': () =>
        client.create(_definition(scheduleId: 'nightlé')),
    // ARABIC-INDIC DIGIT THREE: a digit to Unicode, but not to the contract's
    // [0-9].
    'a schedule id with a non-ASCII digit': () =>
        client.create(_definition(scheduleId: 'night\u0663')),
    'a schedule id with a trailing newline': () =>
        client.create(_definition(scheduleId: 'nightly\n')),
    'an update with a schedule id with a slash': () => client.update(
          _definition(scheduleId: 'weekly/report'),
          _eTag,
        ),
    'a blank function id': () => client.create(_definition(functionId: ' ')),
    'a function id over 256 characters': () =>
        client.create(_definition(functionId: 'x' * 257)),
    'a function version over 64 characters': () =>
        client.create(_definition(functionVersion: 'x' * 65)),
    'a blank cron': () => client.create(_definition(cron: '')),
    'a cron over 128 characters': () =>
        client.create(_definition(cron: '1' * 129)),
    'a blank time zone': () => client.create(_definition(timeZone: '')),
    'a time zone over 128 characters': () =>
        client.create(_definition(timeZone: 'z' * 129)),
    'a catch-up of zero': () => client.create(_definition(maxCatchUp: 0)),
    'a catch-up over 128': () => client.create(_definition(maxCatchUp: 129)),
    'no attempts': () => client.create(_definition(maxAttempts: 0)),
    'attempts over 16': () => client.create(_definition(maxAttempts: 17)),
    'a deadline of zero': () => client.create(_definition(deadlineSeconds: 0)),
    'a deadline over a day': () =>
        client.create(_definition(deadlineSeconds: 86401)),
    'a payload over 1 MiB': () =>
        client.create(_definition(payload: Uint8List(1048577))),
    'an update with a payload over 1 MiB': () => client.update(
          _definition(payload: Uint8List(1048577)),
          _eTag,
        ),
    'a read of a schedule id of two dots': () => client.get('..'),
    'a run with a blank key': () => client.trigger(
          'nightly',
          _eTag,
          YogabaseScheduleTriggerRequest(idempotencyKey: '   ', deadline: deadline),
        ),
    'a run with a padded key': () => client.trigger(
          'nightly',
          _eTag,
          YogabaseScheduleTriggerRequest(idempotencyKey: ' k', deadline: deadline),
        ),
    'a run with a key over 256 characters': () => client.trigger(
          'nightly',
          _eTag,
          YogabaseScheduleTriggerRequest(
            idempotencyKey: 'k' * 257,
            deadline: deadline,
          ),
        ),
  };
  // Every ETag that is not the gateway's token, on every conditional call.
  const malformed = <String>[
    '',
    '*',
    'schedule-v3-$_instance',
    'W/"schedule-v3-$_instance"',
    '"schedule-v3-0123456789ABCDEF0123456789abcdef"',
    '"schedule-v3-0123456789abcdef0123456789abcde"',
    '"schedule-v0-$_instance"',
    '"schedule-v03-$_instance"',
    '"schedule-v9223372036854775808-$_instance"',
    '"schedule-v3-00000000000000000000000000000000"',
    '"v3-$_instance"',
  ];
  for (final eTag in malformed) {
    calls['update with the ETag "$eTag"'] =
        () => client.update(_definition(), eTag);
    calls['delete with the ETag "$eTag"'] =
        () => client.delete('nightly', eTag);
    calls['trigger with the ETag "$eTag"'] = () => client.trigger(
          'nightly',
          eTag,
          YogabaseScheduleTriggerRequest(idempotencyKey: 'k', deadline: deadline),
        );
  }
  for (final entry in calls.entries) {
    var refused = false;
    try {
      await entry.value();
    } on ArgumentError {
      refused = true;
    }
    _expect(refused, '${entry.key} must be refused');
  }
  _expect(adapter.requests.isEmpty, 'a refused call must send no request');

  // The largest version that fits a 64-bit integer is an accepted ETag.
  await client.delete(
    'nightly',
    '"schedule-v9223372036854775807-$_instance"',
  );
  _expect(adapter.requests.length == 1, 'the largest version is accepted');

  var originRefused = false;
  try {
    YogabaseSchedulesClient(
      adapter,
      Uri.parse('https://apps.example.test/path'),
      _project,
    );
  } on ArgumentError {
    originRefused = true;
  }
  _expect(originRefused, 'the origin must be origin-only');
  var projectRefused = false;
  try {
    YogabaseSchedulesClient(
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
  final tooLarge = YogabaseSchedulesException.fromProblem(
    Problem(
      additionalData: <String, Object?>{
        'limitBytes': 1048576,
        'maxBodyBytes': 8394167,
      },
      statusCode: 413,
      code: YogabaseScheduleErrorCodes.payloadTooLarge,
      detail: 'The request is larger than this route accepts.',
    ),
  );
  _expect(tooLarge.status == 413, 'status: ${tooLarge.status}');
  _expect(
    tooLarge.code == 'schedule_payload_too_large',
    'code: ${tooLarge.code}',
  );
  _expect(tooLarge.limitBytes == 1048576, 'limitBytes: ${tooLarge.limitBytes}');
  _expect(
    tooLarge.maxBodyBytes == 8394167,
    'maxBodyBytes: ${tooLarge.maxBodyBytes}',
  );

  // The schedule routes name a code only on the 413: a problem with none reads
  // from its status.
  const expectations = <int, String>{
    400: 'request_failed',
    401: 'authentication_required',
    403: 'forbidden',
    404: 'not_found',
    409: 'conflict',
    412: 'precondition_failed',
    415: 'unsupported_media_type',
    428: 'precondition_required',
    429: 'rate_limited',
    503: 'request_failed',
  };
  for (final expectation in expectations.entries) {
    final fromProblem = YogabaseSchedulesException.fromProblem(
      Problem(
        additionalData: <String, Object?>{'limitBytes': -1, 'maxBodyBytes': '8394167'},
        statusCode: expectation.key,
        title: 'Fixed title.',
      ),
    );
    _expect(
      fromProblem.code == expectation.value &&
          fromProblem.status == expectation.key &&
          fromProblem.limitBytes == null &&
          fromProblem.maxBodyBytes == null &&
          fromProblem.message == 'Fixed title.',
      'a ${expectation.key} problem with no code reads as ${expectation.value}: ${fromProblem.code}',
    );
    _expect(
      YogabaseSchedulesException.fromStatus(expectation.key).code ==
          expectation.value,
      'a bare ${expectation.key} reads as ${expectation.value}',
    );
  }
  _expect(
    YogabaseSchedulesException.fromStatus(413).code == 'payload_too_large' &&
        YogabaseSchedulesException.fromStatus(413).code !=
            YogabaseScheduleErrorCodes.payloadTooLarge,
    'a bare 413 is not schedule_payload_too_large',
  );
}

void _checkTheApplicationClientHandsOutTheSchedulesClient() {
  final adapter = _RecordingRequestAdapter();
  final application = YogabaseApplicationClient(
    adapter,
    Uri.parse('https://apps.example.test'),
  );
  final schedules = application.schedules(_project);
  _expect(
    schedules.origin == application.origin,
    'the schedules client shares the origin',
  );
  _expect(
    identical(schedules.requestAdapter, application.requestAdapter),
    'the schedules client shares the adapter',
  );
  _expect(
    schedules.projectId == _project,
    'the schedules client names its project',
  );
}

/// In-memory [RequestAdapter] that records each request.
class _RecordingRequestAdapter implements RequestAdapter {
  final List<RequestInformation> requests = <RequestInformation>[];

  @override
  String? baseUrl;

  // The generated client registers its JSON writer in the default registry when
  // it is built, which is all a request body needs.
  @override
  SerializationWriterFactory get serializationWriterFactory =>
      SerializationWriterFactoryRegistry.defaultInstance;

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
