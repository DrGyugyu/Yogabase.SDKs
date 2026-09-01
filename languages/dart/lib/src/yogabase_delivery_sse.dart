import 'dart:convert';

/// One delivery SSE event. [id] is opaque and should be echoed as
/// Last-Event-ID after reconnect; this parser never interprets or persists it.
class YogabaseDeliverySseEvent {
  const YogabaseDeliverySseEvent({
    required this.id,
    required this.event,
    required this.dataJson,
  });

  final String? id;
  final String event;
  final String dataJson;
}

/// Parses the buffered SSE bytes returned by the current Dart Kiota adapter.
/// Comment/heartbeat frames are ignored.
class YogabaseDeliverySseParser {
  const YogabaseDeliverySseParser._();

  static Iterable<YogabaseDeliverySseEvent> parse(Iterable<int> bytes) {
    return parseText(utf8.decode(bytes.toList()));
  }

  static Iterable<YogabaseDeliverySseEvent> parseText(String text) sync* {
    String? id;
    String? event;
    final data = <String>[];

    for (final line in const LineSplitter().convert(text)) {
      if (line.isEmpty) {
        final parsed = _dispatch(id, event, data);
        if (parsed != null) {
          yield parsed;
        }
        id = null;
        event = null;
        data.clear();
        continue;
      }
      if (line.startsWith(':')) {
        continue;
      }
      final separator = line.indexOf(':');
      final field = separator < 0 ? line : line.substring(0, separator);
      var value = separator < 0 ? '' : line.substring(separator + 1);
      if (value.startsWith(' ')) {
        value = value.substring(1);
      }
      switch (field) {
        case 'id':
          id = value;
          break;
        case 'event':
          event = value;
          break;
        case 'data':
          data.add(value);
          break;
        default:
          break;
      }
    }

    final parsed = _dispatch(id, event, data);
    if (parsed != null) {
      yield parsed;
    }
  }

  static YogabaseDeliverySseEvent? _dispatch(
    String? id,
    String? event,
    List<String> data,
  ) {
    if (event != 'delivery-operation' || data.isEmpty) {
      return null;
    }
    return YogabaseDeliverySseEvent(
      id: id,
      event: event!,
      dataJson: data.join('\n'),
    );
  }
}
