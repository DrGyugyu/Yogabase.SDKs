// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './data_filter.dart';
import './data_ordering.dart';
import './data_select_request_cursor.dart';

/// auto generated
class DataSelectRequest implements Parsable {
    ///  The columns property
    Iterable<String>? columns;
    ///  The cursor property
    DataSelectRequestCursor? cursor;
    ///  The filters property
    Iterable<DataFilter>? filters;
    ///  The ordering property
    Iterable<DataOrdering>? ordering;
    ///  The pageSize property
    int? pageSize;
    ///  The queryDepth property
    int? queryDepth;
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
        deserializerMap['cursor'] = (node) => cursor = node.getObjectValue<DataSelectRequestCursor>(DataSelectRequestCursor.createFromDiscriminatorValue);
        deserializerMap['filters'] = (node) => filters = node.getCollectionOfObjectValues<DataFilter>(DataFilter.createFromDiscriminatorValue);
        deserializerMap['ordering'] = (node) => ordering = node.getCollectionOfObjectValues<DataOrdering>(DataOrdering.createFromDiscriminatorValue);
        deserializerMap['pageSize'] = (node) => pageSize = node.getIntValue();
        deserializerMap['queryDepth'] = (node) => queryDepth = node.getIntValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeCollectionOfPrimitiveValues<String?>('columns', columns);
        writer.writeObjectValue<DataSelectRequestCursor>('cursor', cursor);
        writer.writeCollectionOfObjectValues<DataFilter>('filters', filters);
        writer.writeCollectionOfObjectValues<DataOrdering>('ordering', ordering);
        writer.writeIntValue('pageSize', pageSize);
        writer.writeIntValue('queryDepth', queryDepth);
    }
}
