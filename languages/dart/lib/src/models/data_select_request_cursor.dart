// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './data_cursor.dart';
import './data_select_request_cursor_member1.dart';

/// auto generated
/// Composed type wrapper for classes [DataCursor], [DataSelectRequestCursorMember1]
class DataSelectRequestCursor implements Parsable {
    ///  Composed type representation for type [DataCursor]
    DataCursor? dataCursor;
    ///  Composed type representation for type [DataSelectRequestCursorMember1]
    DataSelectRequestCursorMember1? dataSelectRequestCursorMember1;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DataSelectRequestCursor createFromDiscriminatorValue(ParseNode parseNode) {
        var result = DataSelectRequestCursor();
        var mappingValue = parseNode.getChildNode('')?.getStringValue();
        if('DataCursor' == mappingValue) {
            result.dataCursor = DataCursor();
        }
        return result;
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        if(dataCursor != null) {
            return dataCursor!.getFieldDeserializers();
        }
        else if(dataSelectRequestCursorMember1 != null) {
            return dataSelectRequestCursorMember1!.getFieldDeserializers();
        }
        return <String, void Function(ParseNode)>{};
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        if(dataCursor != null) {
            writer.writeObjectValue<DataCursor>(null, dataCursor);
        }
        else if(dataSelectRequestCursorMember1 != null) {
            writer.writeObjectValue<DataSelectRequestCursorMember1>(null, dataSelectRequestCursorMember1);
        }
    }
}
