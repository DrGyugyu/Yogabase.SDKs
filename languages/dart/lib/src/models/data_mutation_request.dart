// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './data_filter.dart';
import './data_mutation_request_values.dart';

/// auto generated
class DataMutationRequest implements Parsable {
    ///  The filters property
    Iterable<DataFilter>? filters;
    ///  The returningColumns property
    Iterable<String>? returningColumns;
    ///  The values property
    DataMutationRequestValues? values;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DataMutationRequest createFromDiscriminatorValue(ParseNode parseNode) {
        return DataMutationRequest();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['filters'] = (node) => filters = node.getCollectionOfObjectValues<DataFilter>(DataFilter.createFromDiscriminatorValue);
        deserializerMap['returningColumns'] = (node) => returningColumns = node.getCollectionOfPrimitiveValues<String>();
        deserializerMap['values'] = (node) => values = node.getObjectValue<DataMutationRequestValues>(DataMutationRequestValues.createFromDiscriminatorValue);
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeCollectionOfObjectValues<DataFilter>('filters', filters);
        writer.writeCollectionOfPrimitiveValues<String?>('returningColumns', returningColumns);
        writer.writeObjectValue<DataMutationRequestValues>('values', values);
    }
}
