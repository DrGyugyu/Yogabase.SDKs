// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './data_row.dart';

/// auto generated
class DataInsertRequest implements Parsable {
    ///  Column names, or ["*"] for every column the caller may read.
    Iterable<String>? returning;
    ///  The rows property
    Iterable<DataRow>? rows;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DataInsertRequest createFromDiscriminatorValue(ParseNode parseNode) {
        return DataInsertRequest();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['returning'] = (node) => returning = node.getCollectionOfPrimitiveValues<String>();
        deserializerMap['rows'] = (node) => rows = node.getCollectionOfObjectValues<DataRow>(DataRow.createFromDiscriminatorValue);
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeCollectionOfPrimitiveValues<String?>('returning', returning);
        writer.writeCollectionOfObjectValues<DataRow>('rows', rows);
    }
}
