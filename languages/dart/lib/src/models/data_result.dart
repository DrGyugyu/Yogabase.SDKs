// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './data_result_rows.dart';

/// auto generated
class DataResult implements Parsable {
    ///  The affectedRows property
    int? affectedRows;
    ///  The rows property
    Iterable<DataResultRows>? rows;
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
        deserializerMap['rows'] = (node) => rows = node.getCollectionOfObjectValues<DataResultRows>(DataResultRows.createFromDiscriminatorValue);
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeIntValue('affectedRows', affectedRows);
        writer.writeCollectionOfObjectValues<DataResultRows>('rows', rows);
    }
}
