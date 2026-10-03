// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';

/// auto generated
class DataOrder implements Parsable {
    ///  The column property
    String? column;
    ///  The descending property
    bool? descending;
    ///  The nullsFirst property
    bool? nullsFirst;
    /// Instantiates a new [DataOrder] and sets the default values.
    DataOrder() :  
        descending = false;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DataOrder createFromDiscriminatorValue(ParseNode parseNode) {
        return DataOrder();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['column'] = (node) => column = node.getStringValue();
        deserializerMap['descending'] = (node) => descending = node.getBoolValue();
        deserializerMap['nullsFirst'] = (node) => nullsFirst = node.getBoolValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeStringValue('column', column);
        writer.writeBoolValue('descending', value:descending);
        writer.writeBoolValue('nullsFirst', value:nullsFirst);
    }
}
