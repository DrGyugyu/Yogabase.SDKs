import '../lib/src/yogabase_delivery_client.dart';
import '../lib/src/yogabase_delivery_bearer_token_provider.dart';
import '../lib/src/yogabase_delivery_sse.dart';

/// Standalone source-level checks; run with `dart run test/yogabase_delivery_source_test.dart`.
void main() async {
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
  _expect(
    YogabaseDeliveryClient.isTerminalOperationState('Blocked'),
    'Blocked must be terminal',
  );
  _expect(
    !YogabaseDeliveryClient.isTerminalOperationState('Running'),
    'Running must not be terminal',
  );

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
  print('dart delivery source checks: PASS');
}

void _expect(bool condition, String message) {
  if (!condition) {
    throw StateError(message);
  }
}
