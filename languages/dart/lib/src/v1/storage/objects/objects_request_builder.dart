// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './item/with_key_item_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\storage\objects
class ObjectsRequestBuilder extends BaseRequestBuilder<ObjectsRequestBuilder> {
    /// Gets an item from the yogabase_client.v1.storage.objects.item collection
    ///  [key] Project-local logical key. The live route is a catch-all path tail; SDKs must validate the key and percent-encode each segment separately while preserving literal slash separators.
    WithKeyItemRequestBuilder byKey(String key) {
        var urlTplParams = Map.of(pathParameters);
        urlTplParams.putIfAbsent('key', () => key);
        return WithKeyItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    ObjectsRequestBuilder clone() {
        return ObjectsRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [ObjectsRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    ObjectsRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/storage/objects", pathParameters) ;
    /// Instantiates a new [ObjectsRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    ObjectsRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/storage/objects", {RequestInformation.rawUrlKey : rawUrl}) ;
}
