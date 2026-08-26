// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './objects/objects_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\storage
class StorageRequestBuilder extends BaseRequestBuilder<StorageRequestBuilder> {
    ///  The objects property
    ObjectsRequestBuilder get objects {
        return ObjectsRequestBuilder(pathParameters, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    StorageRequestBuilder clone() {
        return StorageRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [StorageRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    StorageRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/storage", pathParameters) ;
    /// Instantiates a new [StorageRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    StorageRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/storage", {RequestInformation.rawUrlKey : rawUrl}) ;
}
