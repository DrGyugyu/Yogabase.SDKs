// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './data_value_type.dart';

/// auto generated
class DataValue implements Parsable {
    ///  The type property
    DataValueType? type_;
    ///  The value property
    UntypedNode? value;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DataValue createFromDiscriminatorValue(ParseNode parseNode) {
        return DataValue();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['type'] = (node) => type_ = node.getEnumValue<DataValueType>((stringValue) => DataValueType.values.where((enumVal) => enumVal.value == stringValue).firstOrNull);
        deserializerMap['value'] = (node) => value = node.getObjectValue<UntypedNode>(UntypedNode.createFromDiscriminatorValue);
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeEnumValue<DataValueType>('type', type_, (e) => e?.value);
        writer.writeObjectValue<UntypedNode>('value', value);
    }
}
