// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './data_filter.dart';
import './data_order.dart';

/// auto generated
class DataSelectRequest implements Parsable {
    ///  Column names, or ["*"] for every column the caller may read.
    Iterable<String>? columns;
    ///  The filters property
    Iterable<DataFilter>? filters;
    ///  The limit property
    int? limit;
    ///  The offset property
    int? offset;
    ///  The order property
    Iterable<DataOrder>? order;
    /// Instantiates a new [DataSelectRequest] and sets the default values.
    DataSelectRequest() :  
        limit = 100,
        offset = 0;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DataSelectRequest createFromDiscriminatorValue(ParseNode parseNode) {
        return DataSelectRequest();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['columns'] = (node) => columns = node.getCollectionOfPrimitiveValues<String>();
        deserializerMap['filters'] = (node) => filters = node.getCollectionOfObjectValues<DataFilter>(DataFilter.createFromDiscriminatorValue);
        deserializerMap['limit'] = (node) => limit = node.getIntValue();
        deserializerMap['offset'] = (node) => offset = node.getIntValue();
        deserializerMap['order'] = (node) => order = node.getCollectionOfObjectValues<DataOrder>(DataOrder.createFromDiscriminatorValue);
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeCollectionOfPrimitiveValues<String?>('columns', columns);
        writer.writeCollectionOfObjectValues<DataFilter>('filters', filters);
        writer.writeIntValue('limit', limit);
        writer.writeIntValue('offset', offset);
        writer.writeCollectionOfObjectValues<DataOrder>('order', order);
    }
}
