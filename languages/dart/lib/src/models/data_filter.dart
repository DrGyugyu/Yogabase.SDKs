// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './data_filter_op.dart';

/// auto generated
class DataFilter implements Parsable {
    ///  The column property
    String? column;
    ///  The op property
    DataFilterOp? op;
    ///  Absent for isNull and notNull; an array of 1-100 non-null values for in; a string pattern for like and ilike; otherwise one non-null JSON value.
    UntypedNode? value;
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
        deserializerMap['op'] = (node) => op = node.getEnumValue<DataFilterOp>((stringValue) => DataFilterOp.values.where((enumVal) => enumVal.value == stringValue).firstOrNull);
        deserializerMap['value'] = (node) => value = node.getObjectValue<UntypedNode>(UntypedNode.createFromDiscriminatorValue);
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeStringValue('column', column);
        writer.writeEnumValue<DataFilterOp>('op', op, (e) => e?.value);
        writer.writeObjectValue<UntypedNode>('value', value);
    }
}
