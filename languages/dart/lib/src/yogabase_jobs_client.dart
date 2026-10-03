import 'dart:convert';
import 'dart:typed_data';

import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';

import 'models/job_submit_request.dart';
import 'models/problem.dart';
import 'v1/projects/item/jobs/item/with_job_item_request_builder.dart';
import 'v1/projects/item/jobs/jobs_request_builder.dart';
import 'yogabase_api_client.dart';
import 'yogabase_gateway_support.dart';

/// Where a durable job is in its lifecycle: [queued], then [running], then it
/// settles as [succeeded], [cancelled] or [deadLettered]. [failed] is a
/// checkpoint a job passes through on the way to a retry ([queued] again) or to
/// [deadLettered]; a job never rests in it, so it is not terminal.
enum YogabaseJobStatus {
  queued('Queued'),
  running('Running'),
  succeeded('Succeeded'),
  failed('Failed'),
  cancelled('Cancelled'),
  deadLettered('DeadLettered');

  const YogabaseJobStatus(this.wireName);

  /// The name the gateway writes, such as `DeadLettered`.
  final String wireName;

  /// True once the job has settled and will not change again.
  bool get isTerminal =>
      this == succeeded || this == cancelled || this == deadLettered;

  /// The status the gateway named, or null for a name this client was never
  /// told of.
  static YogabaseJobStatus? fromWireName(String? value) {
    for (final status in values) {
      if (status.wireName == value) {
        return status;
      }
    }
    return null;
  }
}

/// One durable job as the gateway reads it back: its state, how far it has got
/// and how it ended. The payload never travels back, and a result is read
/// separately ([YogabaseJobsClient.result]).
class YogabaseJob {
  const YogabaseJob({
    required this.jobId,
    required this.jobType,
    required this.status,
    required this.attempt,
    required this.maxAttempts,
    required this.progressPercent,
    required this.cancellationRequested,
    required this.resultReference,
    required this.failureCode,
    required this.createdAt,
    required this.updatedAt,
    required this.completedAt,
  });

  /// The job's id; every other call names the job by it.
  final String jobId;

  /// The id of the function the job runs.
  final String jobType;

  final YogabaseJobStatus status;

  /// How many attempts have started; 0 while the job has never run.
  final int attempt;

  /// How many attempts the job may have in all, from 1 to 10.
  final int maxAttempts;

  /// How far the function says it has got, from 0 to 100.
  final int progressPercent;

  /// True once a cancel has been asked for; a running job is stopped by its
  /// worker, so this can be true while the status is still running.
  final bool cancellationRequested;

  /// A name for the stored result, not a capability: read the bytes with
  /// [YogabaseJobsClient.result]. Null while the job has none.
  final String? resultReference;

  /// Why the job last failed, as the function or the worker named it, or null.
  final String? failureCode;

  final DateTime createdAt;
  final DateTime updatedAt;

  /// When the job settled, or null while it has not.
  final DateTime? completedAt;

  /// True once the job has settled: succeeded, cancelled or dead-lettered.
  bool get isTerminal => status.isTerminal;
}

/// The stored result of a job that succeeded with one.
///
/// Unlike the other languages' Jobs clients, this one cannot check the bytes
/// against the `X-Yogabase-Content-Sha256` header the gateway sends: a typed
/// Kiota call does not hand a Dart caller the response headers (only a native
/// response handler does, and this client sends the generated builder's own
/// request), and this package has no hashing dependency. The gateway has
/// already verified the stored object against the digest the job recorded
/// before it sent a byte.
class YogabaseJobResult {
  const YogabaseJobResult(this.content);

  /// The result bytes, as the gateway sent them.
  final Uint8List content;
}

/// The codes the Jobs routes refuse with, as [YogabaseJobsException.code]
/// carries them. Every code up to [resultReferenceInvalid] is the gateway's;
/// [responseInvalid] is this client's own, for an answer that is not what the
/// route declares.
class YogabaseJobErrorCodes {
  YogabaseJobErrorCodes._();

  /// 422: the job type is not the id of a deployed function.
  static const String typeNotRegistered = 'job_type_not_registered';

  /// 409: the idempotency key already belongs to a different request by this
  /// user.
  static const String idempotencyConflict = 'job_idempotency_conflict';

  /// 413: the payload is over 1 MiB before base64 encoding, or the request body
  /// is over the largest the route reads. [YogabaseJobsException.limitBytes]
  /// and [YogabaseJobsException.maxBodyBytes] name both limits.
  static const String payloadTooLarge = 'job_payload_too_large';

  /// 400: a field is malformed, or the payload is not base64.
  static const String requestInvalid = 'job_request_invalid';

  /// 404: no such job, which is also another user's job (the two are never
  /// told apart).
  static const String notFound = 'job_not_found';

  /// 403: the token may not call the Jobs routes, or names another project.
  static const String accessDenied = 'job_access_denied';

  /// 503: the job store could not be reached.
  static const String unavailable = 'job_unavailable';

  /// 404: the job has no result to give (another user's, unknown, or finished
  /// without one).
  static const String resultNotFound = 'job_result_not_found';

  /// 409: the job is the caller's own and has not settled.
  static const String resultNotReady = 'job_result_not_ready';

  /// 502: the stored result is not what the job recorded; no byte of it was
  /// sent.
  static const String resultIntegrityFailed = 'job_result_integrity_failed';

  /// 500: the job's stored result reference is not that job's own; no byte of
  /// it was sent.
  static const String resultReferenceInvalid = 'job_result_reference_invalid';

  /// This client: the gateway's answer to a Jobs call is not what the route
  /// declares.
  static const String responseInvalid = 'job_response_invalid';
}

/// A non-successful Jobs response, or an answer the Jobs client refuses, with
/// the gateway's problem details.
class YogabaseJobsException implements Exception {
  const YogabaseJobsException(
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

  /// The gateway's stable code (see [YogabaseJobErrorCodes]), or one derived
  /// from the status when the body names none: `authentication_required`,
  /// `forbidden`, `not_found`, `conflict`, `payload_too_large`, `rate_limited`
  /// or `request_failed`.
  final String code;

  /// The most bytes the refused payload may have, before base64, when a 413
  /// named it; otherwise null.
  final int? limitBytes;

  /// The most bytes the whole request body may have, when a 413 named it;
  /// otherwise null.
  final int? maxBodyBytes;

  /// The refusal a Kiota [Problem] carries, with the limits it names.
  static YogabaseJobsException fromProblem(Problem problem) {
    final status = statusOf(problem);
    return YogabaseJobsException(
      problem.detail ?? problem.title ?? 'Yogabase returned HTTP $status.',
      status,
      problem.code ?? _codeFromStatus(status),
      limitBytes: _positive(problem.additionalData['limitBytes']),
      maxBodyBytes: _positive(problem.additionalData['maxBodyBytes']),
    );
  }

  /// The HTTP status a Kiota [ApiException] carries: its response status code,
  /// or for a [Problem] the status member of the document, or 0 when it has
  /// neither.
  static int statusOf(ApiException exception) => yogabaseStatusOf(exception);

  /// The refusal for a status the contract maps to no problem document.
  static YogabaseJobsException fromStatus(int status) => YogabaseJobsException(
        'Yogabase returned HTTP $status.',
        status,
        _codeFromStatus(status),
      );

  static int? _positive(Object? value) => yogabasePositiveInt(value);

  static String _codeFromStatus(int status) =>
      YogabaseStatusErrorCodes.forStatus(status);

  @override
  String toString() => 'YogabaseJobsException($status $code): $message';
}

/// Durable background jobs for the signed-in user: submit a run of a deployed
/// stateless function, read it, cancel it and fetch what it returned, against
/// `/v1/projects/{projectId}/jobs` on the application gateway and behind the
/// same project token (the `lambda.invoke` scope) as invoking a function. A job
/// type is the id of a deployed stateless function. A job belongs to the user
/// who submitted it, and another user's job is answered exactly as one that
/// does not exist.
///
/// The product's client OpenAPI document describes the Jobs routes, so Kiota
/// generates their request builders (`api.v1.projects.byProjectId(id).jobs`).
/// This class wraps those builders: each request is the one the generated
/// builder builds (its route, its media types and its serialized body), sent
/// through the Kiota request adapter of the generated application client
/// ([YogabaseApplicationClient.jobs]), and so with its authentication and
/// origin. What it adds is what a generated builder does not give: it
/// validates each call before it is sent, refuses an answer that is not what
/// the route declares, and names every refusal by its code. Neither this class
/// nor its standalone source check has been run or analyzed: no Dart SDK was
/// available when it was written.
///
/// No capability gates it: the bootstrap document carries no capability
/// identifier for Jobs, so the client does not pretend to know whether an
/// installation serves them. An installation without them answers a plain 404
/// whose [YogabaseJobsException.code] is `not_found`; a missing job is
/// [YogabaseJobErrorCodes.notFound], a different code.
class YogabaseJobsClient {
  YogabaseJobsClient(
    RequestAdapter requestAdapter,
    Uri applicationOrigin,
    String projectId,
  )   : origin = _requireOrigin(applicationOrigin, 'applicationOrigin'),
        projectId = _requireUuid(projectId, 'projectId'),
        requestAdapter = requestAdapter {
    requestAdapter.baseUrl = origin.toString();
  }

  /// The most bytes a job's payload may have, before base64 encoding: the 1 MiB
  /// inline limit.
  static const int maximumPayloadBytes = 1048576;

  /// The most attempts a job may be allowed.
  static const int maximumAttempts = 10;

  /// The most bytes a job's result may have: 16 MiB.
  static const int maximumResultBytes = 16 * 1048576;

  final Uri origin;
  final String projectId;
  final RequestAdapter requestAdapter;

  static final RegExp _jobTypePattern = RegExp(r'^[A-Za-z0-9._:-]{1,128}$');
  static final RegExp _failureCodePattern = RegExp(r'^[a-z0-9_]{1,128}$');
  static final RegExp _uuidPattern = RegExp(
    r'^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$',
  );
  static final RegExp _controlCharacter = RegExp(r'[\u0000-\u001f\u007f-\u009f]');
  static final RegExp _referenceForbidden =
      RegExp(r'[\s\u0000-\u001f\u007f-\u009f]');

  /// Starts a job and returns it as the gateway accepted it (202), normally
  /// [YogabaseJobStatus.queued]. Idempotent per user and [idempotencyKey]: the
  /// same request again returns the job it already made, whatever that job has
  /// become.
  ///
  /// [jobType] is the id of a deployed stateless function (1 to 128 ASCII
  /// letters, digits, `.`, `-`, `_` or `:`). [idempotencyKey] is 1 to 256
  /// characters, trimmed, with no control character; keep one across the
  /// retries of one submit. [payload] is at most [maximumPayloadBytes] bytes.
  /// [maxAttempts] is 1 to 10; the gateway's default is 5.
  ///
  /// Throws a [YogabaseJobsException] for a refusal: `job_type_not_registered`
  /// (422), `job_idempotency_conflict` (409), `job_payload_too_large` (413),
  /// `job_request_invalid` (400), `job_access_denied` (403) or `job_unavailable`
  /// (503).
  Future<YogabaseJob> submit({
    required String jobType,
    required String idempotencyKey,
    Uint8List? payload,
    int? maxAttempts,
  }) async {
    _validateSubmit(jobType, idempotencyKey, payload, maxAttempts);
    // A job with no payload and one with an empty payload are the same job to
    // the gateway.
    final body = JobSubmitRequest()
      ..jobType = jobType
      ..idempotencyKey = idempotencyKey
      ..payloadBase64 =
          payload != null && payload.isNotEmpty ? base64Encode(payload) : null
      ..maxAttempts = maxAttempts;
    return _sendJob(_jobs().toPostRequestInformation(body));
  }

  /// Reads the caller's own job. Poll this until it [YogabaseJob.isTerminal].
  /// Throws `job_not_found` (404) for no such job and for another user's.
  Future<YogabaseJob> get(String jobId) async =>
      _sendJob(toGetRequestInformation(jobId));

  /// Asks for the caller's own job to stop, idempotently, and returns it: a
  /// queued job is cancelled at once, a running one is flagged
  /// ([YogabaseJob.cancellationRequested]) and stopped by its worker, and a
  /// settled one is returned as it is.
  Future<YogabaseJob> cancel(String jobId) async =>
      _sendJob(toCancelRequestInformation(jobId));

  /// Reads the result of the caller's own job. See [YogabaseJobResult] for why
  /// the bytes are not checked against the gateway's digest header here.
  ///
  /// Throws `job_result_not_ready` (409) while the job has not settled,
  /// `job_result_not_found` (404) for no result to give,
  /// `job_result_integrity_failed` (502) or `job_result_reference_invalid`
  /// (500) from the gateway, and `job_response_invalid` (502) from this client
  /// for a result larger than [maximumResultBytes].
  Future<YogabaseJobResult> result(String jobId) async {
    final requestInfo = toResultRequestInformation(jobId);
    try {
      final bytes = await requestAdapter.sendPrimitiveCollection<int>(
        requestInfo,
        _errorMapping(),
      );
      if (bytes == null) {
        throw const YogabaseJobsException(
          'Yogabase returned an invalid job result.',
          502,
          YogabaseJobErrorCodes.responseInvalid,
        );
      }
      final content = Uint8List.fromList(bytes.toList());
      if (content.length > maximumResultBytes) {
        throw const YogabaseJobsException(
          'Yogabase returned an oversized job result.',
          502,
          YogabaseJobErrorCodes.responseInvalid,
        );
      }
      return YogabaseJobResult(content);
    } on Problem catch (problem) {
      throw YogabaseJobsException.fromProblem(problem);
    } on ApiException catch (exception) {
      throw YogabaseJobsException.fromStatus(YogabaseJobsException.statusOf(exception));
    }
  }

  /// The request that reads one job: the one the generated builder builds.
  RequestInformation toGetRequestInformation(String jobId) =>
      _job(jobId).toGetRequestInformation();

  /// The request that cancels one job: the one the generated builder builds.
  RequestInformation toCancelRequestInformation(String jobId) =>
      _job(jobId).cancel.toPostRequestInformation();

  /// The request that reads one job's result: the one the generated builder
  /// builds.
  RequestInformation toResultRequestInformation(String jobId) =>
      _job(jobId).result.toGetRequestInformation();

  /// The generated Jobs request builder of this client's project. It is built
  /// for each call from the generated client's own navigation, so the route,
  /// the path parameters and the base URL the adapter holds now are the
  /// generated code's and never restated here.
  JobsRequestBuilder _jobs() => YogabaseApiClient(requestAdapter)
      .v1
      .projects
      .byProjectId(UuidValue.fromString(projectId))
      .jobs;

  WithJobItemRequestBuilder _job(String jobId) =>
      _jobs().byJobId(UuidValue.fromString(_requireUuid(jobId, 'jobId')));

  Map<String, ParsableFactory<Parsable>> _errorMapping() =>
      <String, ParsableFactory<Parsable>>{
        '400': Problem.createFromDiscriminatorValue,
        '401': Problem.createFromDiscriminatorValue,
        '403': Problem.createFromDiscriminatorValue,
        '404': Problem.createFromDiscriminatorValue,
        '409': Problem.createFromDiscriminatorValue,
        '413': Problem.createFromDiscriminatorValue,
        '422': Problem.createFromDiscriminatorValue,
        '500': Problem.createFromDiscriminatorValue,
        '502': Problem.createFromDiscriminatorValue,
        '503': Problem.createFromDiscriminatorValue,
      };

  Future<YogabaseJob> _sendJob(RequestInformation requestInfo) async {
    try {
      final document = await requestAdapter.send<_JobDocument>(
        requestInfo,
        _JobDocument.createFromDiscriminatorValue,
        _errorMapping(),
      );
      return _readJob(document);
    } on Problem catch (problem) {
      throw YogabaseJobsException.fromProblem(problem);
    } on ApiException catch (exception) {
      throw YogabaseJobsException.fromStatus(YogabaseJobsException.statusOf(exception));
    }
  }

  // A job the gateway describes wrongly is worse than one it refuses: a caller
  // that polls for a state it was never told about waits for ever. Reject the
  // answer instead of returning a guess. A member this client does not know is
  // ignored, as the generated clients keep unknown members aside.
  static YogabaseJob _readJob(_JobDocument? document) {
    final status = YogabaseJobStatus.fromWireName(document?.status);
    if (document == null ||
        document.jobId == null ||
        !_uuidPattern.hasMatch(document.jobId!) ||
        document.jobId!.replaceAll('-', '').replaceAll('0', '').isEmpty ||
        document.jobType == null ||
        !_jobTypePattern.hasMatch(document.jobType!) ||
        status == null ||
        document.attempt == null ||
        document.attempt! < 0 ||
        document.maxAttempts == null ||
        document.maxAttempts! < 1 ||
        document.maxAttempts! > maximumAttempts ||
        document.progressPercent == null ||
        document.progressPercent! < 0 ||
        document.progressPercent! > 100 ||
        document.cancellationRequested == null ||
        (document.resultReference != null &&
            !_isResultReference(document.resultReference!)) ||
        (document.failureCode != null &&
            !_failureCodePattern.hasMatch(document.failureCode!)) ||
        document.createdAt == null ||
        document.updatedAt == null) {
      throw const YogabaseJobsException(
        'Yogabase returned an invalid job.',
        502,
        YogabaseJobErrorCodes.responseInvalid,
      );
    }
    return YogabaseJob(
      jobId: document.jobId!,
      jobType: document.jobType!,
      status: status,
      attempt: document.attempt!,
      maxAttempts: document.maxAttempts!,
      progressPercent: document.progressPercent!,
      cancellationRequested: document.cancellationRequested!,
      resultReference: document.resultReference,
      failureCode: document.failureCode,
      createdAt: document.createdAt!,
      updatedAt: document.updatedAt!,
      completedAt: document.completedAt,
    );
  }

  // The reference is opaque to a client: bounded, an object:// name, and
  // nothing that could be read as more than text. Its grammar belongs to the
  // gateway and is not restated here.
  static bool _isResultReference(String value) =>
      value.length > 9 &&
      value.length <= 2048 &&
      value.startsWith('object://') &&
      !_referenceForbidden.hasMatch(value);

  static void _validateSubmit(
    String jobType,
    String idempotencyKey,
    Uint8List? payload,
    int? maxAttempts,
  ) {
    if (!_jobTypePattern.hasMatch(jobType)) {
      throw ArgumentError.value(
        jobType,
        'jobType',
        "a job type is 1 to 128 ASCII letters, digits, '.', '-', '_' or ':', "
            'with no surrounding whitespace',
      );
    }
    if (idempotencyKey.trim().isEmpty ||
        idempotencyKey.length > 256 ||
        idempotencyKey != idempotencyKey.trim() ||
        _controlCharacter.hasMatch(idempotencyKey)) {
      throw ArgumentError.value(
        idempotencyKey,
        'idempotencyKey',
        'an idempotency key is 1 to 256 characters, trimmed, with no control '
            'character',
      );
    }
    if (payload != null && payload.length > maximumPayloadBytes) {
      throw ArgumentError.value(
        payload.length,
        'payload',
        'a job payload is limited to one MiB before base64 encoding',
      );
    }
    if (maxAttempts != null && (maxAttempts < 1 || maxAttempts > maximumAttempts)) {
      throw ArgumentError.value(
        maxAttempts,
        'maxAttempts',
        'a job allows between 1 and 10 attempts',
      );
    }
  }

  static String _requireUuid(String value, String name) {
    if (!_uuidPattern.hasMatch(value) ||
        value.replaceAll('-', '').replaceAll('0', '').isEmpty) {
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

/// What the gateway writes for one job, before it is checked.
class _JobDocument implements Parsable {
  String? jobId;
  String? jobType;
  String? status;
  int? attempt;
  int? maxAttempts;
  int? progressPercent;
  bool? cancellationRequested;
  String? resultReference;
  String? failureCode;
  DateTime? createdAt;
  DateTime? updatedAt;
  DateTime? completedAt;

  static _JobDocument createFromDiscriminatorValue(ParseNode parseNode) {
    return _JobDocument();
  }

  @override
  Map<String, void Function(ParseNode)> getFieldDeserializers() {
    var deserializerMap = <String, void Function(ParseNode)>{};
    deserializerMap['jobId'] = (node) => jobId = node.getStringValue();
    deserializerMap['jobType'] = (node) => jobType = node.getStringValue();
    deserializerMap['status'] = (node) => status = node.getStringValue();
    deserializerMap['attempt'] = (node) => attempt = node.getIntValue();
    deserializerMap['maxAttempts'] = (node) => maxAttempts = node.getIntValue();
    deserializerMap['progressPercent'] =
        (node) => progressPercent = node.getIntValue();
    deserializerMap['cancellationRequested'] =
        (node) => cancellationRequested = node.getBoolValue();
    deserializerMap['resultReference'] =
        (node) => resultReference = node.getStringValue();
    deserializerMap['failureCode'] =
        (node) => failureCode = node.getStringValue();
    deserializerMap['createdAt'] = (node) => createdAt = node.getDateTimeValue();
    deserializerMap['updatedAt'] = (node) => updatedAt = node.getDateTimeValue();
    deserializerMap['completedAt'] =
        (node) => completedAt = node.getDateTimeValue();
    return deserializerMap;
  }

  @override
  void serialize(SerializationWriter writer) {
    writer.writeStringValue('jobId', jobId);
    writer.writeStringValue('jobType', jobType);
    writer.writeStringValue('status', status);
    writer.writeIntValue('attempt', attempt);
    writer.writeIntValue('maxAttempts', maxAttempts);
    writer.writeIntValue('progressPercent', progressPercent);
    writer.writeBoolValue('cancellationRequested', value: cancellationRequested);
    writer.writeStringValue('resultReference', resultReference);
    writer.writeStringValue('failureCode', failureCode);
    writer.writeDateTimeValue('createdAt', createdAt);
    writer.writeDateTimeValue('updatedAt', updatedAt);
    writer.writeDateTimeValue('completedAt', completedAt);
  }
}
