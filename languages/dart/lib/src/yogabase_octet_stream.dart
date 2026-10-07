import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:microsoft_kiota_serialization_text/microsoft_kiota_serialization_text.dart';

// The Storage routes carry raw bytes as application/octet-stream, which the Kiota Dart runtime has no
// factories for. These are the smallest pair: the text ones, which refuse structured data, with the bytes
// passed through untouched.
void registerOctetStreamFactories() {
  ApiClientBuilder.registerDefaultSerializer(_BytesWriterFactory.new);
  ApiClientBuilder.registerDefaultDeserializer(_BytesNodeFactory.new);
}

class _BytesWriterFactory implements SerializationWriterFactory {
  @override
  String get validContentType => 'application/octet-stream';

  @override
  SerializationWriter getSerializationWriter(String contentType) => _BytesWriter();
}

class _BytesNodeFactory implements ParseNodeFactory {
  @override
  String get validContentType => 'application/octet-stream';

  @override
  ParseNode getRootParseNode(String contentType, Uint8List content) => _BytesNode(content);
}

class _BytesWriter extends TextSerializationWriter {
  Uint8List _bytes = Uint8List(0);

  @override
  void writeByteArrayValue(String? key, Uint8List? value) => _bytes = value ?? _bytes;

  @override
  Uint8List getSerializedContent() => _bytes;
}

class _BytesNode extends TextParseNode {
  _BytesNode(this._bytes) : super(null);

  final Uint8List _bytes;

  @override
  Iterable<T> getCollectionOfPrimitiveValues<T>() => _bytes as Iterable<T>;
}
