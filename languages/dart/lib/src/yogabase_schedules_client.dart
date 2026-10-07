import 'dart:convert';

import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';

import 'models/concurrency_policy.dart';
import 'models/misfire_policy.dart';
import 'models/problem.dart';
import 'models/schedule_definition_request.dart';
import 'models/schedule_trigger_request.dart';
import 'v1/projects/item/schedules/item/with_schedule_item_request_builder.dart';
import 'yogabase_api_client.dart';
import 'yogabase_gateway_support.dart';

/// What a schedule does about occurrences it missed while the scheduler was
/// unavailable. A definition always names one: a request that leaves it out is
/// a 400 whose problem names the member.
enum YogabaseScheduleMisfirePolicy {
  /// Runs once, now, however many occurrences were missed, and resumes the
  /// cadence.
  runImmediately('RunImmediately'),

  /// Abandons what it missed and resumes at the next occurrence.
  skipToNextOccurrence('SkipToNextOccurrence'),

  /// Runs each missed occurrence, oldest first, up to
  /// [YogabaseScheduleDefinition.maxCatchUp].
  runAllMissed('RunAllMissed');

  const YogabaseScheduleMisfirePolicy(this.wireName);

  /// The name the gateway reads, such as `SkipToNextOccurrence`.
  final String wireName;
}

/// What a schedule does when an occurrence is due while an earlier occurrence
/// of the same schedule has not finished: its invocation is queued (waiting to
/// start, or to be retried) or running, and its deadline has not passed. A
/// skipped occurrence is consumed, not deferred: the schedule moves on to its
/// next occurrence. A manual run is not an occurrence, so the policy neither
/// holds it back nor is held back by it, and a one-shot has no earlier
/// occurrence, so the policy does not apply to it. A definition always names
/// one: a request that leaves it out is a 400 whose problem names the member.
enum YogabaseScheduleConcurrencyPolicy {
  /// Starts the new occurrence alongside the earlier one.
  allow('Allow'),

  /// Skips the new occurrence. When several are due at once
  /// ([YogabaseScheduleMisfirePolicy.runAllMissed] after an outage), only the
  /// oldest starts.
  forbid('Forbid'),

  /// Ends an earlier occurrence that is still queued, with the failure code
  /// `schedule_occurrence_replaced`, and starts the new one. An occurrence that
  /// is already running cannot be stopped, because nothing can interrupt a
  /// Lambda that is executing, so while one is running this skips the new
  /// occurrence, as [forbid] does. When several are due at once, only the
  /// newest starts.
  replace('Replace');

  const YogabaseScheduleConcurrencyPolicy(this.wireName);

  /// The name the gateway reads, such as `Forbid`.
  final String wireName;
}

/// Whether a manual run enqueued a new invocation or found the one its key had
/// already made.
enum YogabaseScheduleTriggerOutcome {
  /// This request enqueued the invocation (the gateway's outcome 0).
  enqueued,

  /// The same key against the same version had already made the invocation,
  /// and this is it (outcome 1).
  replayed,
}

/// What a client asks the schedule write route to hold: a recurring schedule
/// ([cron] evaluated in [timeZone]) or a one-shot ([oneShotAtUtc]) of one
/// deployed stateless function version. The project and the owner are never
/// fields: the project is the client's own and the owner is the signed-in user.
/// Every member but [payload] and [oneShotAtUtc] is required, the two policies
/// included: a request that leaves one out is a 400 whose problem names the
/// member, and a definition here cannot leave one out.
class YogabaseScheduleDefinition {
  const YogabaseScheduleDefinition({
    required this.scheduleId,
    required this.functionId,
    required this.functionVersion,
    required this.cron,
    required this.timeZone,
    required this.misfirePolicy,
    required this.concurrencyPolicy,
    required this.maxCatchUp,
    required this.maxAttempts,
    required this.deadlineSeconds,
    this.payload,
    this.oneShotAtUtc,
  });

  /// The schedule's id, chosen by the caller: 1 to 128 characters, a letter or
  /// a digit first and then letters, digits and `.` `_` `~` `:` `-` only
  /// (`^[A-Za-z0-9][A-Za-z0-9._~:-]{0,127}$`). The id is one segment of the
  /// schedule's URL path, so there is nothing to escape, and the console
  /// addresses the schedule by the same id. The id belongs to the project, so
  /// one that another user holds cannot be created. A schedule written before
  /// the rule under another id can still be read, run and deleted by that id,
  /// but a write of it is refused: delete it and create it again under an id
  /// that follows the rule.
  final String scheduleId;

  /// The id of a deployed Stateless Lambda, at most 256 characters.
  final String functionId;

  /// The exact released version of that function which every occurrence runs,
  /// at most 64 characters. It must be admitted for the project, and an update
  /// checks it again.
  final String functionVersion;

  /// Five fields, minute hour day-of-month month day-of-week, each a number,
  /// a star, a list (`1,15`), a range (`1-5`), a step or, for the month and the
  /// day of the week, a name (`JAN`, `MON`); 7 is Sunday. No seconds field and
  /// no `@` macros; at most 128 characters. A one-shot still sends a valid
  /// expression, which does not decide when it runs.
  final String cron;

  /// The time zone the cron expression is evaluated in, by the id the
  /// gateway's system knows it by (`UTC`, `Asia/Seoul`), at most 128
  /// characters.
  final String timeZone;

  /// What to do about occurrences missed while the scheduler was unavailable.
  final YogabaseScheduleMisfirePolicy misfirePolicy;

  /// What to do when an occurrence is due while an earlier one has not
  /// finished; see [YogabaseScheduleConcurrencyPolicy].
  final YogabaseScheduleConcurrencyPolicy concurrencyPolicy;

  /// The most missed occurrences [YogabaseScheduleMisfirePolicy.runAllMissed]
  /// runs after an outage, 1 to 128; the other policies ignore it but the
  /// gateway still requires it.
  final int maxCatchUp;

  /// How many times an occurrence's invocation may be attempted, 1 to 16.
  final int maxAttempts;

  /// How long an occurrence's invocation has to finish, in seconds, 1 to 86400.
  final int deadlineSeconds;

  /// The request every occurrence's invocation carries, at most
  /// [YogabaseSchedulesClient.maximumPayloadBytes] bytes, or null (or empty)
  /// for none.
  final Uint8List? payload;

  /// Makes the schedule a one-shot: it runs once at this instant, which is sent
  /// as UTC, and is then deleted. Null is a recurring schedule. An update keeps
  /// a schedule's kind: it cannot turn a one-shot into a recurring schedule or
  /// the reverse (a 409).
  final DateTime? oneShotAtUtc;
}

/// One schedule as the gateway holds it, with the ETag of this version of it.
/// Send [eTag] back to change, delete or run exactly this version.
class YogabaseSchedule {
  const YogabaseSchedule({
    required this.scheduleId,
    required this.projectId,
    required this.subject,
    required this.functionId,
    required this.functionVersion,
    required this.version,
    required this.instanceId,
    required this.lastOccurrence,
    required this.oneShotAtUtc,
    required this.eTag,
  });

  final String scheduleId;

  /// The project the schedule belongs to: the client's own.
  final String projectId;

  /// The owner: the sub of the user who created the schedule.
  final String subject;

  /// The id of the function every occurrence runs.
  final String functionId;

  /// The exact function version every occurrence runs.
  final String functionVersion;

  /// The schedule's version as decimal text: a 64-bit number a JSON number
  /// cannot carry exactly. It changes whenever the schedule does, including
  /// when the scheduler records an occurrence; the [eTag] carries it with the
  /// instance.
  final String version;

  /// Lasts until the schedule is deleted: a schedule deleted and created again
  /// has a new one.
  final String instanceId;

  /// How far the scheduler has got, or null for a one-shot.
  final DateTime? lastOccurrence;

  /// The instant a one-shot runs, or null for a recurring schedule.
  final DateTime? oneShotAtUtc;

  /// The ETag to send as `If-Match` to change, delete or run exactly this
  /// version: the quotes included, `"schedule-v<version>-<instance>"` with the
  /// instance as 32 lowercase hexadecimal digits. It is built from [version]
  /// and [instanceId], which the gateway's ETag header carries too: unlike the
  /// other languages' schedule clients, this one does not check the header
  /// against the body, because a typed Kiota call does not hand a Dart caller
  /// the response headers. The scheduler advances a recurring schedule's
  /// version as it runs, so an ETag held for long is stale: read the schedule
  /// again.
  final String eTag;

  /// True for a one-shot, which runs once and is then deleted.
  bool get isOneShot => oneShotAtUtc != null;
}

/// What a client asks the schedule trigger route to do: run the schedule once
/// now.
class YogabaseScheduleTriggerRequest {
  const YogabaseScheduleTriggerRequest({
    required this.idempotencyKey,
    required this.deadline,
  });

  /// The caller's key for this run: 1 to 256 characters, with no leading or
  /// trailing white space. The same key against the same version of the
  /// schedule is the same run, so retrying a trigger is safe.
  final String idempotencyKey;

  /// When the invocation must finish: later than now and at most 3600 seconds
  /// from now, else the gateway refuses it. Sent as a UTC instant.
  final DateTime deadline;
}

/// The invocation a manual run made, or the one the same key already made.
class YogabaseScheduleTriggerResult {
  const YogabaseScheduleTriggerResult({
    required this.outcome,
    required this.invocationId,
  });

  /// Whether this request enqueued the invocation or found the one its key had
  /// already made.
  final YogabaseScheduleTriggerOutcome outcome;

  /// The invocation's id; it derives from the schedule's id, instance and
  /// version and the idempotency key, so a repeat has the same one.
  final String invocationId;
}

/// The codes the schedule routes refuse with, as
/// [YogabaseSchedulesException.code] carries them. Only one refusal of the
/// gateway's names a code; every other is a status with a fixed title, or no
/// body at all, and its code is derived from the status (see
/// [YogabaseStatusErrorCodes]). The last is this client's own, for an answer
/// that is not what the route declares.
class YogabaseScheduleErrorCodes {
  YogabaseScheduleErrorCodes._();

  /// 413: the payload is over 1 MiB before base64 encoding, or the request body
  /// is over the largest the route reads.
  /// [YogabaseSchedulesException.limitBytes] and
  /// [YogabaseSchedulesException.maxBodyBytes] name both limits.
  static const String payloadTooLarge = 'schedule_payload_too_large';

  /// This client: the gateway's answer to a schedule call is not what the route
  /// declares.
  static const String responseInvalid = 'schedule_response_invalid';
}

/// A non-successful schedule response, or an answer the schedule client
/// refuses, with the gateway's problem details.
class YogabaseSchedulesException implements Exception {
  const YogabaseSchedulesException(
    this.message,
    this.status,
    this.code, {
    this.limitBytes,
    this.maxBodyBytes,
  });

  final String message;

  /// The HTTP status of the gateway's answer, or 502 for an answer this client
  /// refused.
  final int status;

  /// The gateway's stable code (see [YogabaseScheduleErrorCodes]), or one
  /// derived from the status when the body names none (see
  /// [YogabaseStatusErrorCodes]).
  final String code;

  /// The most bytes the refused payload may have, before base64, when a 413
  /// named it; otherwise null.
  final int? limitBytes;

  /// The most bytes the whole request body may have, when a 413 named it;
  /// otherwise null.
  final int? maxBodyBytes;

  /// The refusal a Kiota [Problem] carries, with the limits it names.
  static YogabaseSchedulesException fromProblem(Problem problem) {
    final status = yogabaseStatusOf(problem);
    return YogabaseSchedulesException(
      problem.detail ?? problem.title ?? 'Yogabase returned HTTP $status.',
      status,
      problem.code ?? YogabaseStatusErrorCodes.forStatus(status),
      limitBytes: yogabasePositiveInt(problem.additionalData['limitBytes']),
      maxBodyBytes: yogabasePositiveInt(problem.additionalData['maxBodyBytes']),
    );
  }

  /// The refusal for a status the contract maps to no problem document.
  static YogabaseSchedulesException fromStatus(int status) =>
      YogabaseSchedulesException(
        'Yogabase returned HTTP $status.',
        status,
        YogabaseStatusErrorCodes.forStatus(status),
      );

  @override
  String toString() => 'YogabaseSchedulesException($status $code): $message';
}

/// The signed-in user's own schedules: create, change, read, delete and run once
/// a recurring or one-shot schedule of a deployed stateless function, against
/// `/v1/projects/{projectId}/schedules/{scheduleId}` on the application gateway
/// and behind the same project token (the `lambda.invoke` scope) as invoking a
/// function. A schedule belongs to the user who created it, and only that user
/// reads, changes, deletes or runs it: another user's schedule is answered as
/// one that does not exist (except that updating it is a 403 and creating a
/// schedule under its id is a 412).
///
/// The product's client OpenAPI document describes the schedule routes, so Kiota
/// generates their request builders
/// (`api.v1.projects.byProjectId(id).schedules.byScheduleId(id)`). This class
/// wraps those builders: each request is the one the generated builder builds
/// (its route, its media types and its serialized body), sent through the Kiota
/// request adapter of the generated application client
/// ([YogabaseApplicationClient.schedules]), and so with its authentication and
/// origin. What it adds is what a generated builder does not give: every write
/// is conditional on the schedule's ETag, which it sends as the header the route
/// wants and builds from the schedule it reads back; it validates each call
/// before it is sent, refuses an answer that is not what the route declares,
/// and names every refusal. Neither this class nor its standalone source check
/// has been run or analyzed: no Dart SDK was available when it was written.
///
/// The write is conditional. [create] sends `If-None-Match: *`; [update],
/// [delete] and [trigger] send the ETag last read as `If-Match`. A stale ETag,
/// creating an id that is already held and updating one that does not exist are
/// each a 412 that changes nothing, and the scheduler advances a recurring
/// schedule's version as it runs, so an ETag held for long is stale: [get] reads
/// it again.
///
/// No capability gates it: the bootstrap document carries no capability
/// identifier for schedules, so the client does not pretend to know whether an
/// installation serves them. An installation without the Scheduler answers every
/// route with a 404 that has no body, which reads as `not_found`, as does a
/// schedule that does not exist.
class YogabaseSchedulesClient {
  YogabaseSchedulesClient(
    RequestAdapter requestAdapter,
    Uri applicationOrigin,
    String projectId,
  )   : origin = _requireOrigin(applicationOrigin, 'applicationOrigin'),
        projectId = _requireUuid(projectId, 'projectId'),
        requestAdapter = requestAdapter {
    requestAdapter.baseUrl = origin.toString();
  }

  /// The most characters a schedule id may have.
  static const int maximumScheduleIdLength = 128;

  /// The most characters a function id may have.
  static const int maximumFunctionIdLength = 256;

  /// The most characters a function version may have.
  static const int maximumFunctionVersionLength = 64;

  /// The most characters a cron expression may have.
  static const int maximumCronLength = 128;

  /// The most characters a time zone id may have.
  static const int maximumTimeZoneIdLength = 128;

  /// The most missed occurrences a schedule may catch up.
  static const int maximumCatchUp = 128;

  /// The most attempts an occurrence's invocation may be allowed.
  static const int maximumAttempts = 16;

  /// The fewest seconds an occurrence's invocation may be given.
  static const int minimumDeadlineSeconds = 1;

  /// The most seconds an occurrence's invocation may be given: 24 hours.
  static const int maximumDeadlineSeconds = 86400;

  /// The most characters a manual run's idempotency key may have.
  static const int maximumIdempotencyKeyLength = 256;

  /// The furthest a manual run's deadline may be from now: one hour.
  static const int maximumManualDeadlineSeconds = 3600;

  /// The most bytes a schedule's payload may have, before base64 encoding: the
  /// 1 MiB inline limit.
  static const int maximumPayloadBytes = 1048576;

  final Uri origin;
  final String projectId;
  final RequestAdapter requestAdapter;

  static final RegExp _uuidPattern = RegExp(
    r'^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$',
  );
  static final RegExp _controlCharacter = RegExp(r'[\u0000-\u001f\u007f-\u009f]');
  static final RegExp _eTagPattern =
      RegExp(r'^"schedule-v([1-9][0-9]*)-([0-9a-f]{32})"$');
  static final RegExp _versionPattern = RegExp(r'^[1-9][0-9]*$');

  // The pattern of the contract's definition member scheduleId, which holds a
  // write to what one segment of a URL path carries without being escaped. A
  // read, a run and a delete accept more (_requireScheduleId), because a
  // schedule written before the rule is still found by its id.
  static final RegExp _writableScheduleIdPattern =
      RegExp(r'^[A-Za-z0-9][A-Za-z0-9._~:-]{0,127}$');

  /// The largest 64-bit signed integer, as text.
  static const String _maximumInt64 = '9223372036854775807';

  /// Creates the schedule as the signed-in user, who then owns it (201), and
  /// answers it with its ETag.
  ///
  /// Throws a [YogabaseSchedulesException] for a refusal: `precondition_failed`
  /// (412) when the id is already held, `conflict` (409) when the function
  /// version is not an admitted Stateless one of the project,
  /// `schedule_payload_too_large` (413), a 400 for a member the gateway refuses
  /// (a cron expression that does not parse, an unknown time zone, a payload
  /// that is not base64), a 403 or a 503.
  Future<YogabaseSchedule> create(YogabaseScheduleDefinition definition) async =>
      _write(definition, null);

  /// Changes the schedule the [eTag] names (200) and answers it with its new
  /// ETag. The definition names the same schedule id and keeps the schedule's
  /// kind: recurring or one-shot. A schedule written before the id rule under
  /// an id that does not follow it cannot be changed: delete it and create it
  /// again under an id that does.
  ///
  /// Throws `precondition_failed` (412) when the ETag is stale or the schedule
  /// does not exist, `forbidden` (403) for another user's schedule, `conflict`
  /// (409) for a function version that is not admitted or a schedule that would
  /// change kind, and the refusals of [create].
  Future<YogabaseSchedule> update(
    YogabaseScheduleDefinition definition,
    String eTag,
  ) async =>
      _write(definition, _requireETag(eTag));

  /// Reads the caller's own schedule, with the ETag to change, delete or run it.
  /// Throws `not_found` (404) for no such schedule and for another user's.
  Future<YogabaseSchedule> get(String scheduleId) async {
    final id = _requireScheduleId(scheduleId);
    final requestInfo = _item(id).toGetRequestInformation();
    try {
      final document = await requestAdapter.send<_ScheduleDocument>(
        requestInfo,
        _ScheduleDocument.createFromDiscriminatorValue,
        _errorMapping(),
      );
      return _readSchedule(document, id);
    } on YogabaseSchedulesException {
      rethrow;
    } on Problem catch (problem) {
      throw YogabaseSchedulesException.fromProblem(problem);
    } on ApiException catch (exception) {
      throw YogabaseSchedulesException.fromStatus(yogabaseStatusOf(exception));
    } on FormatException {
      throw _invalid('Yogabase returned an invalid schedule.');
    } on TypeError {
      throw _invalid('Yogabase returned an invalid schedule.');
    }
  }

  /// Deletes the schedule the [eTag] names (204). Throws `precondition_failed`
  /// (412) when the ETag is stale and `not_found` (404) for no such schedule and
  /// for another user's.
  Future<void> delete(String scheduleId, String eTag) async {
    final id = _requireScheduleId(scheduleId);
    final token = _requireETag(eTag);
    final requestInfo = _item(id).toDeleteRequestInformation(
      (configuration) => configuration.headers.put('If-Match', token),
    );
    try {
      await requestAdapter.sendNoContent(requestInfo, _errorMapping());
    } on Problem catch (problem) {
      throw YogabaseSchedulesException.fromProblem(problem);
    } on ApiException catch (exception) {
      throw YogabaseSchedulesException.fromStatus(yogabaseStatusOf(exception));
    }
  }

  /// Runs the schedule the [eTag] names once now, outside its cadence: it
  /// enqueues one invocation of the schedule's function with the schedule's
  /// payload, as the schedule's owner (202). The same idempotency key against
  /// the same version answers the same invocation, with
  /// [YogabaseScheduleTriggerOutcome.replayed] where the first said
  /// [YogabaseScheduleTriggerOutcome.enqueued]. A manual run is not an
  /// occurrence of the schedule: its [YogabaseScheduleConcurrencyPolicy]
  /// neither holds it back nor is held back by it.
  ///
  /// Throws `precondition_failed` (412) when the ETag is stale, `not_found`
  /// (404), `conflict` (409) when the queue holds the key for a different
  /// request or the schedule's function version is no longer an admitted
  /// Stateless one of the project (as for a write of it), and a 400 for a
  /// deadline that is not after now or is more than an hour away.
  Future<YogabaseScheduleTriggerResult> trigger(
    String scheduleId,
    String eTag,
    YogabaseScheduleTriggerRequest request,
  ) async {
    final id = _requireScheduleId(scheduleId);
    final token = _requireETag(eTag);
    final key = request.idempotencyKey;
    if (key.trim().isEmpty ||
        key.length > maximumIdempotencyKeyLength ||
        key != key.trim()) {
      throw ArgumentError.value(
        key,
        'idempotencyKey',
        'an idempotency key is 1 to 256 characters with no leading or '
            'trailing white space',
      );
    }
    final body = ScheduleTriggerRequest()
      ..idempotencyKey = key
      ..deadline = request.deadline.toUtc();
    final requestInfo = _item(id).trigger.toPostRequestInformation(
      body,
      (configuration) => configuration.headers.put('If-Match', token),
    );
    try {
      final document = await requestAdapter.send<_TriggerDocument>(
        requestInfo,
        _TriggerDocument.createFromDiscriminatorValue,
        _errorMapping(),
      );
      return _readTrigger(document);
    } on YogabaseSchedulesException {
      rethrow;
    } on Problem catch (problem) {
      throw YogabaseSchedulesException.fromProblem(problem);
    } on ApiException catch (exception) {
      throw YogabaseSchedulesException.fromStatus(yogabaseStatusOf(exception));
    } on FormatException {
      throw _invalid('Yogabase returned an invalid schedule run.');
    } on TypeError {
      throw _invalid('Yogabase returned an invalid schedule run.');
    }
  }

  Future<YogabaseSchedule> _write(
    YogabaseScheduleDefinition definition,
    String? eTag,
  ) async {
    final body = _toRequest(definition);
    // Exactly one of the two preconditions, never both and never neither: a
    // create must not overwrite what another request made first, and an update
    // must not overwrite what it never read.
    final requestInfo = _item(definition.scheduleId).toPutRequestInformation(
      body,
      (configuration) {
        if (eTag == null) {
          configuration.headers.put('If-None-Match', '*');
        } else {
          configuration.headers.put('If-Match', eTag);
        }
      },
    );
    try {
      final document = await requestAdapter.send<_ScheduleDocument>(
        requestInfo,
        _ScheduleDocument.createFromDiscriminatorValue,
        _errorMapping(),
      );
      return _readSchedule(document, definition.scheduleId);
    } on YogabaseSchedulesException {
      rethrow;
    } on Problem catch (problem) {
      throw YogabaseSchedulesException.fromProblem(problem);
    } on ApiException catch (exception) {
      throw YogabaseSchedulesException.fromStatus(yogabaseStatusOf(exception));
    } on FormatException {
      throw _invalid('Yogabase returned an invalid schedule.');
    } on TypeError {
      throw _invalid('Yogabase returned an invalid schedule.');
    }
  }

  /// The generated request builder of one schedule of this client's project. It
  /// is built for each call from the generated client's own navigation, so the
  /// route, the path parameters and the base URL the adapter holds now are the
  /// generated code's and never restated here.
  WithScheduleItemRequestBuilder _item(String scheduleId) =>
      YogabaseApiClient(requestAdapter)
          .v1
          .projects
          .byProjectId(UuidValue.fromString(projectId))
          .schedules
          .byScheduleId(scheduleId);

  Map<String, ParsableFactory<Parsable>> _errorMapping() =>
      <String, ParsableFactory<Parsable>>{
        '400': Problem.createFromDiscriminatorValue,
        '401': Problem.createFromDiscriminatorValue,
        '403': Problem.createFromDiscriminatorValue,
        '412': Problem.createFromDiscriminatorValue,
        '413': Problem.createFromDiscriminatorValue,
        '428': Problem.createFromDiscriminatorValue,
        '503': Problem.createFromDiscriminatorValue,
      };

  static YogabaseSchedulesException _invalid(String message) =>
      YogabaseSchedulesException(
        message,
        502,
        YogabaseScheduleErrorCodes.responseInvalid,
      );

  static ScheduleDefinitionRequest _toRequest(
    YogabaseScheduleDefinition definition,
  ) {
    _requireWritableScheduleId(definition.scheduleId);
    _requireBounded(definition.functionId, maximumFunctionIdLength, 'functionId');
    _requireBounded(
      definition.functionVersion,
      maximumFunctionVersionLength,
      'functionVersion',
    );
    _requireBounded(definition.cron, maximumCronLength, 'cron');
    _requireBounded(definition.timeZone, maximumTimeZoneIdLength, 'timeZone');
    if (definition.maxCatchUp < 1 || definition.maxCatchUp > maximumCatchUp) {
      throw ArgumentError.value(
        definition.maxCatchUp,
        'maxCatchUp',
        'a schedule catches up between 1 and 128 missed occurrences',
      );
    }
    if (definition.maxAttempts < 1 || definition.maxAttempts > maximumAttempts) {
      throw ArgumentError.value(
        definition.maxAttempts,
        'maxAttempts',
        'a schedule allows between 1 and 16 attempts',
      );
    }
    if (definition.deadlineSeconds < minimumDeadlineSeconds ||
        definition.deadlineSeconds > maximumDeadlineSeconds) {
      throw ArgumentError.value(
        definition.deadlineSeconds,
        'deadlineSeconds',
        "a schedule's deadline is between 1 second and 24 hours",
      );
    }
    final payload = definition.payload;
    if (payload != null && payload.length > maximumPayloadBytes) {
      throw ArgumentError.value(
        payload.length,
        'payload',
        'a schedule payload is limited to one MiB before base64 encoding',
      );
    }
    final oneShot = definition.oneShotAtUtc;
    return ScheduleDefinitionRequest()
      ..scheduleId = definition.scheduleId
      ..functionId = definition.functionId
      ..functionVersion = definition.functionVersion
      ..cron = definition.cron
      ..timeZone = definition.timeZone
      ..misfirePolicy = _misfire(definition.misfirePolicy)
      ..concurrencyPolicy = _concurrency(definition.concurrencyPolicy)
      ..maxCatchUp = definition.maxCatchUp
      ..maxAttempts = definition.maxAttempts
      ..deadlineSeconds = definition.deadlineSeconds
      // A schedule with no payload and one with an empty payload are the same
      // schedule to the gateway.
      ..payloadBase64 =
          payload != null && payload.isNotEmpty ? base64Encode(payload) : null
      ..oneShotAtUtc = oneShot?.toUtc();
  }

  static MisfirePolicy _misfire(YogabaseScheduleMisfirePolicy policy) =>
      MisfirePolicy.values.firstWhere((value) => value.value == policy.wireName);

  static ConcurrencyPolicy _concurrency(
    YogabaseScheduleConcurrencyPolicy policy,
  ) =>
      ConcurrencyPolicy.values
          .firstWhere((value) => value.value == policy.wireName);

  // The schedule the answer describes must be the one asked for, in this
  // client's own project: a gateway that describes another is not trusted, and a
  // caller that holds the wrong ETag for the wrong schedule would delete it. A
  // member this client does not know is ignored, as the generated clients keep
  // unknown members aside.
  YogabaseSchedule _readSchedule(
    _ScheduleDocument? document,
    String requestedScheduleId,
  ) {
    if (document == null ||
        document.scheduleId == null ||
        document.scheduleId != requestedScheduleId ||
        document.projectId == null ||
        !_isUuid(document.projectId!) ||
        document.projectId!.toLowerCase() != projectId.toLowerCase() ||
        document.subject == null ||
        !_isUuid(document.subject!) ||
        _isEmptyUuid(document.subject!) ||
        document.functionId == null ||
        document.functionId!.trim().isEmpty ||
        document.functionId!.length > maximumFunctionIdLength ||
        document.functionVersion == null ||
        document.functionVersion!.trim().isEmpty ||
        document.functionVersion!.length > maximumFunctionVersionLength ||
        document.version == null ||
        !_isVersion(document.version!) ||
        document.instanceId == null ||
        !_isUuid(document.instanceId!) ||
        _isEmptyUuid(document.instanceId!)) {
      throw _invalid('Yogabase returned an invalid schedule.');
    }
    final instance = document.instanceId!.replaceAll('-', '').toLowerCase();
    return YogabaseSchedule(
      scheduleId: document.scheduleId!,
      projectId: document.projectId!,
      subject: document.subject!,
      functionId: document.functionId!,
      functionVersion: document.functionVersion!,
      version: document.version!,
      instanceId: document.instanceId!,
      lastOccurrence: document.lastOccurrence,
      oneShotAtUtc: document.oneShotAtUtc,
      eTag: '"schedule-v${document.version}-$instance"',
    );
  }

  static YogabaseScheduleTriggerResult _readTrigger(_TriggerDocument? document) {
    final outcome = document?.outcome;
    final invocation = document?.invocationId;
    if (document == null ||
        (outcome != 0 && outcome != 1) ||
        invocation == null ||
        !_isUuid(invocation) ||
        _isEmptyUuid(invocation)) {
      throw _invalid('Yogabase returned an invalid schedule run.');
    }
    return YogabaseScheduleTriggerResult(
      outcome: outcome == 0
          ? YogabaseScheduleTriggerOutcome.enqueued
          : YogabaseScheduleTriggerOutcome.replayed,
      invocationId: invocation,
    );
  }

  static bool _isUuid(String value) => _uuidPattern.hasMatch(value);

  static bool _isEmptyUuid(String value) =>
      value.replaceAll('-', '').replaceAll('0', '').isEmpty;

  // A version is decimal text for a number a JSON number cannot carry exactly:
  // it must fit 64 bits, as the gateway's own parser requires of the token.
  static bool _isVersion(String value) {
    if (!_versionPattern.hasMatch(value) || value.length > _maximumInt64.length) {
      return false;
    }
    return value.length < _maximumInt64.length ||
        value.compareTo(_maximumInt64) <= 0;
  }

  static String _requireWritableScheduleId(String value) {
    if (!_writableScheduleIdPattern.hasMatch(value)) {
      throw ArgumentError.value(
        value,
        'scheduleId',
        'a schedule id to write is 1 to 128 characters, a letter or a digit '
            'first and then letters, digits and . _ ~ : - only',
      );
    }
    return value;
  }

  static String _requireScheduleId(String value) {
    if (value.trim().isEmpty ||
        value.length > maximumScheduleIdLength ||
        value != value.trim() ||
        _controlCharacter.hasMatch(value)) {
      throw ArgumentError.value(
        value,
        'scheduleId',
        'a schedule id is 1 to 128 characters, trimmed, with no control '
            'character',
      );
    }
    // The id is a path segment, and a URL parser collapses a segment of dots
    // (percent-encoded or not) into the one before it, so these two would
    // address another route altogether.
    if (value == '.' || value == '..') {
      throw ArgumentError.value(
        value,
        'scheduleId',
        'a schedule id of only dots cannot be addressed in a URL path; choose '
            'another id',
      );
    }
    return value;
  }

  static String _requireETag(String value) {
    final match = _eTagPattern.firstMatch(value);
    if (match == null ||
        !_isVersion(match.group(1)!) ||
        match.group(2)!.replaceAll('0', '').isEmpty) {
      throw ArgumentError.value(
        value,
        'eTag',
        'an ETag is the quoted token a schedule read or write answered, '
            '"schedule-v<version>-<instance>", exactly as sent',
      );
    }
    return value;
  }

  static void _requireBounded(String value, int maximumLength, String name) {
    if (value.trim().isEmpty || value.length > maximumLength) {
      throw ArgumentError.value(
        value,
        name,
        'is required and at most $maximumLength characters',
      );
    }
  }

  static String _requireUuid(String value, String name) {
    if (!_uuidPattern.hasMatch(value) || _isEmptyUuid(value)) {
      throw ArgumentError.value(value, name, 'must be a non-empty canonical UUID');
    }
    return value;
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

/// What the gateway writes for one schedule, before it is checked.
class _ScheduleDocument implements Parsable {
  String? scheduleId;
  String? projectId;
  String? subject;
  String? functionId;
  String? functionVersion;
  String? version;
  String? instanceId;
  DateTime? lastOccurrence;
  DateTime? oneShotAtUtc;

  static _ScheduleDocument createFromDiscriminatorValue(ParseNode parseNode) {
    return _ScheduleDocument();
  }

  @override
  Map<String, void Function(ParseNode)> getFieldDeserializers() {
    var deserializerMap = <String, void Function(ParseNode)>{};
    deserializerMap['scheduleId'] = (node) => scheduleId = node.getStringValue();
    deserializerMap['projectId'] = (node) => projectId = node.getStringValue();
    deserializerMap['subject'] = (node) => subject = node.getStringValue();
    deserializerMap['functionId'] = (node) => functionId = node.getStringValue();
    deserializerMap['functionVersion'] =
        (node) => functionVersion = node.getStringValue();
    deserializerMap['version'] = (node) => version = node.getStringValue();
    deserializerMap['instanceId'] = (node) => instanceId = node.getStringValue();
    deserializerMap['lastOccurrence'] =
        (node) => lastOccurrence = node.getDateTimeValue();
    deserializerMap['oneShotAtUtc'] =
        (node) => oneShotAtUtc = node.getDateTimeValue();
    return deserializerMap;
  }

  @override
  void serialize(SerializationWriter writer) {
    writer.writeStringValue('scheduleId', scheduleId);
    writer.writeStringValue('projectId', projectId);
    writer.writeStringValue('subject', subject);
    writer.writeStringValue('functionId', functionId);
    writer.writeStringValue('functionVersion', functionVersion);
    writer.writeStringValue('version', version);
    writer.writeStringValue('instanceId', instanceId);
    writer.writeDateTimeValue('lastOccurrence', lastOccurrence);
    writer.writeDateTimeValue('oneShotAtUtc', oneShotAtUtc);
  }
}

/// What the gateway writes for one manual run, before it is checked.
class _TriggerDocument implements Parsable {
  int? outcome;
  String? invocationId;

  static _TriggerDocument createFromDiscriminatorValue(ParseNode parseNode) {
    return _TriggerDocument();
  }

  @override
  Map<String, void Function(ParseNode)> getFieldDeserializers() {
    var deserializerMap = <String, void Function(ParseNode)>{};
    deserializerMap['outcome'] = (node) => outcome = node.getIntValue();
    deserializerMap['invocationId'] =
        (node) => invocationId = node.getStringValue();
    return deserializerMap;
  }

  @override
  void serialize(SerializationWriter writer) {
    writer.writeIntValue('outcome', outcome);
    writer.writeStringValue('invocationId', invocationId);
  }
}
