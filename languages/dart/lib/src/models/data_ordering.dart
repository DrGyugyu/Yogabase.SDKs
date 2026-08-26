// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';

/// auto generated
class DataOrdering implements Parsable {
    ///  The column property
    String? column;
    ///  The descending property
    bool? descending;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DataOrdering createFromDiscriminatorValue(ParseNode parseNode) {
        return DataOrdering();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['column'] = (node) => column = node.getStringValue();
        deserializerMap['descending'] = (node) => descending = node.getBoolValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeStringValue('column', column);
        writer.writeBoolValue('descending', value:descending);
    }
}
