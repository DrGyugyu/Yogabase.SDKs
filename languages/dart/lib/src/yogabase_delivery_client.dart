import 'dart:async';

import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';

import 'delivery/yogabase_delivery_api_client.dart';
import 'delivery/models/delivery_operation_envelope.dart';
import 'yogabase_delivery_bearer_token_provider.dart';
import 'yogabase_delivery_request_options.dart';
import 'yogabase_delivery_sse.dart';

/// A cancellation signal for bounded delivery polling.
class YogabaseCancellationToken {
  final Completer<void> _completer = Completer<void>();

  bool get isCancelled => _completer.isCompleted;
  Future<void> get whenCancelled => _completer.future;

  void cancel() {
    if (!_completer.isCompleted) {
      _completer.complete();
    }
  }
}

/// Thrown when delivery polling is cancelled between requests.
class YogabaseOperationPollingCancelledException implements Exception {
  const YogabaseOperationPollingCancelledException();

  @override
  String toString() => 'YogabaseOperationPollingCancelledException';
}

/// Control Plane delivery facade with an origin and adapter separate from the
/// application-plane client.
class YogabaseDeliveryClient {
  YogabaseDeliveryClient(RequestAdapter requestAdapter, Uri deliveryOrigin)
      : origin = _requireOrigin(deliveryOrigin, 'deliveryOrigin'),
        requestAdapter = _configureAdapter(
          requestAdapter,
          _requireOrigin(deliveryOrigin, 'deliveryOrigin'),
        ),
        api = YogabaseDeliveryApiClient(requestAdapter);

  static const terminalOperationStates = <String>{
    'Succeeded',
    'Failed',
    'Abandoned',
    'Cancelled',
    'Blocked',
  };

  final Uri origin;
  final RequestAdapter requestAdapter;
  final YogabaseDeliveryApiClient api;

  static bool isTerminalOperationState(String? state) {
    return state != null && terminalOperationStates.contains(state);
  }

  Future<DeliveryOperationEnvelope> pollOperation({
    required UuidValue projectId,
    required UuidValue operationId,
    Duration interval = const Duration(seconds: 2),
    int maxAttempts = 30,
    YogabaseCancellationToken? cancellationToken,
  }) async {
    if (interval <= Duration.zero || maxAttempts < 1) {
      throw ArgumentError('interval must be positive and maxAttempts must be at least one');
    }
    final token = cancellationToken ?? YogabaseCancellationToken();
    for (var attempt = 0; attempt < maxAttempts; attempt++) {
      _throwIfCancelled(token);
      final operation = await api.api.v1.delivery.projects
          .byProjectId(projectId)
          .operations
          .byOperationId(operationId)
          .getAsync();
      if (operation == null || operation.state == null) {
        throw StateError('delivery operation response omitted state');
      }
      if (isTerminalOperationState(operation.state)) {
        return operation;
      }
      if (attempt + 1 < maxAttempts) {
        await _delayOrCancel(interval, token);
      }
    }
    throw StateError(
      'delivery operation did not reach a terminal state within the polling bound',
    );
  }

  /// Reads operation events and preserves each opaque SSE resume ID.
  ///
  /// The generated Dart adapter currently returns the SSE response as buffered
  /// bytes, so this stream begins after that response is available. A custom
  /// streaming adapter can reuse [YogabaseDeliverySseParser] incrementally.
  Stream<YogabaseDeliverySseEvent> streamOperationEvents({
    required UuidValue projectId,
    UuidValue? environmentId,
    UuidValue? operationId,
    String? lastEventId,
  }) async* {
    final bytes = await api.api.v1.delivery.projects
        .byProjectId(projectId)
        .operations
        .events
        .getAsync((configuration) {
      configuration.queryParameters.environmentId = environmentId;
      configuration.queryParameters.operationId = operationId;
      YogabaseDeliveryRequestOptions.apply(
        configuration,
        lastEventId: lastEventId,
      );
    });
    if (bytes != null) {
      for (final event in YogabaseDeliverySseParser.parse(bytes)) {
        yield event;
      }
    }
  }

  static RequestAdapter _configureAdapter(RequestAdapter adapter, Uri origin) {
    adapter.baseUrl = origin.toString();
    return adapter;
  }

  static Uri _requireOrigin(Uri value, String name) {
    final scheme = value.scheme.toLowerCase();
    if (scheme != 'https' ||
        value.host.isEmpty ||
        value.userInfo.isNotEmpty ||
        value.query.isNotEmpty ||
        value.fragment.isNotEmpty ||
        (value.path.isNotEmpty && value.path != '/')) {
      throw ArgumentError('$name must be an origin-only HTTPS URL');
    }
    return Uri(
      scheme: scheme,
      host: value.host,
      port: value.hasPort ? value.port : null,
    );
  }

  static void _throwIfCancelled(YogabaseCancellationToken token) {
    if (token.isCancelled) {
      throw const YogabaseOperationPollingCancelledException();
    }
  }

  static Future<void> _delayOrCancel(
    Duration interval,
    YogabaseCancellationToken token,
  ) async {
    await Future.any<void>(<Future<void>>[
      Future<void>.delayed(interval),
      token.whenCancelled.then<void>((_) {
        throw const YogabaseOperationPollingCancelledException();
      }),
    ]);
  }
}

/// Re-exported here for callers that configure a delivery adapter.
typedef YogabaseDeliveryTokenProvider = YogabaseDeliveryBearerTokenProvider;
