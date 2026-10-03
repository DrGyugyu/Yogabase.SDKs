// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './data_filter.dart';
import './data_row.dart';

/// auto generated
class DataUpdateRequest implements Parsable {
    ///  The filters property
    Iterable<DataFilter>? filters;
    ///  Column names, or ["*"] for every column the caller may read.
    Iterable<String>? returning;
    ///  Column name to plain JSON value.
    DataRow? values;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DataUpdateRequest createFromDiscriminatorValue(ParseNode parseNode) {
        return DataUpdateRequest();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['filters'] = (node) => filters = node.getCollectionOfObjectValues<DataFilter>(DataFilter.createFromDiscriminatorValue);
        deserializerMap['returning'] = (node) => returning = node.getCollectionOfPrimitiveValues<String>();
        deserializerMap['values'] = (node) => values = node.getObjectValue<DataRow>(DataRow.createFromDiscriminatorValue);
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeCollectionOfObjectValues<DataFilter>('filters', filters);
        writer.writeCollectionOfPrimitiveValues<String?>('returning', returning);
        writer.writeObjectValue<DataRow>('values', values);
    }
}
