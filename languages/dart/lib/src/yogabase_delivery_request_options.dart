import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';

/// Applies delivery-plane headers to any generated Kiota request configuration.
class YogabaseDeliveryRequestOptions {
  const YogabaseDeliveryRequestOptions._();

  static final RegExp _etag = RegExp(r'^W/"[0-9]+"$');

  static void apply<T extends AbstractQueryParameters>(
    RequestConfiguration<T> configuration, {
    String? bearerToken,
    String? ifMatch,
    String? idempotencyKey,
    String? lastEventId,
  }) {
    if (bearerToken != null) {
      _requireNonBlankBounded(bearerToken, 4096, 'bearerToken');
      configuration.headers.put('Authorization', 'Bearer $bearerToken');
    }
    if (ifMatch != null) {
      if (!_etag.hasMatch(ifMatch)) {
        throw ArgumentError(
          'ifMatch must be a weak numeric ETag such as W/"12"',
        );
      }
      configuration.headers.put('If-Match', ifMatch);
    }
    if (idempotencyKey != null) {
      _requireNonBlankBounded(idempotencyKey, 128, 'idempotencyKey');
      configuration.headers.put('Idempotency-Key', idempotencyKey);
    }
    if (lastEventId != null) {
      _requireNonBlankBounded(lastEventId, 4096, 'lastEventId');
      configuration.headers.put('Last-Event-ID', lastEventId);
    }
  }

  static void _requireNonBlankBounded(String value, int maxLength, String name) {
    if (value.trim().isEmpty || value.length > maxLength) {
      throw ArgumentError('$name must be non-blank and at most $maxLength characters');
    }
  }
}
