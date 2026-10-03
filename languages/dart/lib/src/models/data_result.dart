// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';

/// auto generated
class DataResult implements Parsable {
    ///  The affectedRows property
    int? affectedRows;
    ///  For a select, true when another page exists past this one.
    bool? hasMore;
    ///  The rows property
    UntypedNode? rows;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DataResult createFromDiscriminatorValue(ParseNode parseNode) {
        return DataResult();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['affectedRows'] = (node) => affectedRows = node.getIntValue();
        deserializerMap['hasMore'] = (node) => hasMore = node.getBoolValue();
        deserializerMap['rows'] = (node) => rows = node.getObjectValue<UntypedNode>(UntypedNode.createFromDiscriminatorValue);
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeIntValue('affectedRows', affectedRows);
        writer.writeBoolValue('hasMore', value:hasMore);
        writer.writeObjectValue<UntypedNode>('rows', rows);
    }
}
