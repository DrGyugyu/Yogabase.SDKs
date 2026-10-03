// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './delete/delete_request_builder.dart';
import './insert/insert_request_builder.dart';
import './select/select_request_builder.dart';
import './update/update_request_builder.dart';
import './upsert/upsert_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\data\{table}
class WithTableItemRequestBuilder extends BaseRequestBuilder<WithTableItemRequestBuilder> {
    ///  The deletePath property
    DeleteRequestBuilder get deletePath {
        return DeleteRequestBuilder(pathParameters, requestAdapter);
    }
    ///  The insert property
    InsertRequestBuilder get insert {
        return InsertRequestBuilder(pathParameters, requestAdapter);
    }
    ///  The select property
    SelectRequestBuilder get select {
        return SelectRequestBuilder(pathParameters, requestAdapter);
    }
    ///  The update property
    UpdateRequestBuilder get update {
        return UpdateRequestBuilder(pathParameters, requestAdapter);
    }
    ///  The upsert property
    UpsertRequestBuilder get upsert {
        return UpsertRequestBuilder(pathParameters, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    WithTableItemRequestBuilder clone() {
        return WithTableItemRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [WithTableItemRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithTableItemRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/data/{table}", pathParameters) ;
    /// Instantiates a new [WithTableItemRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithTableItemRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/data/{table}", {RequestInformation.rawUrlKey : rawUrl}) ;
}
