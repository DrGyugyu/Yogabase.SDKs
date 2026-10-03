// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './data_row.dart';

/// auto generated
class DataUpsertRequest implements Parsable {
    ///  The ignoreDuplicates property
    bool? ignoreDuplicates;
    ///  The onConflict property
    Iterable<String>? onConflict;
    ///  Column names, or ["*"] for every column the caller may read.
    Iterable<String>? returning;
    ///  The rows property
    Iterable<DataRow>? rows;
    /// Instantiates a new [DataUpsertRequest] and sets the default values.
    DataUpsertRequest() :  
        ignoreDuplicates = false;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DataUpsertRequest createFromDiscriminatorValue(ParseNode parseNode) {
        return DataUpsertRequest();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['ignoreDuplicates'] = (node) => ignoreDuplicates = node.getBoolValue();
        deserializerMap['onConflict'] = (node) => onConflict = node.getCollectionOfPrimitiveValues<String>();
        deserializerMap['returning'] = (node) => returning = node.getCollectionOfPrimitiveValues<String>();
        deserializerMap['rows'] = (node) => rows = node.getCollectionOfObjectValues<DataRow>(DataRow.createFromDiscriminatorValue);
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeBoolValue('ignoreDuplicates', value:ignoreDuplicates);
        writer.writeCollectionOfPrimitiveValues<String?>('onConflict', onConflict);
        writer.writeCollectionOfPrimitiveValues<String?>('returning', returning);
        writer.writeCollectionOfObjectValues<DataRow>('rows', rows);
    }
}
