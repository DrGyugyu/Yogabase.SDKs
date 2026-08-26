// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './data_value.dart';

/// auto generated
class DataCursor implements Parsable {
    ///  The column property
    String? column;
    ///  The descending property
    bool? descending;
    ///  The lastValue property
    DataValue? lastValue;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DataCursor createFromDiscriminatorValue(ParseNode parseNode) {
        return DataCursor();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['column'] = (node) => column = node.getStringValue();
        deserializerMap['descending'] = (node) => descending = node.getBoolValue();
        deserializerMap['lastValue'] = (node) => lastValue = node.getObjectValue<DataValue>(DataValue.createFromDiscriminatorValue);
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeStringValue('column', column);
        writer.writeBoolValue('descending', value:descending);
        writer.writeObjectValue<DataValue>('lastValue', lastValue);
    }
}
