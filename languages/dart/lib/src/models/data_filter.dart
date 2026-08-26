// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './data_value.dart';

/// auto generated
class DataFilter implements Parsable {
    ///  The column property
    String? column;
    ///  The operator property
    String? operator_;
    ///  The values property
    Iterable<DataValue>? values;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DataFilter createFromDiscriminatorValue(ParseNode parseNode) {
        return DataFilter();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['column'] = (node) => column = node.getStringValue();
        deserializerMap['operator'] = (node) => operator_ = node.getStringValue();
        deserializerMap['values'] = (node) => values = node.getCollectionOfObjectValues<DataValue>(DataValue.createFromDiscriminatorValue);
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeStringValue('column', column);
        writer.writeStringValue('operator', operator_);
        writer.writeCollectionOfObjectValues<DataValue>('values', values);
    }
}
