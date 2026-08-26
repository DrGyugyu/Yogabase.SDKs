// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './item/with_resource_item_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\data
class DataRequestBuilder extends BaseRequestBuilder<DataRequestBuilder> {
    /// Gets an item from the stackyoga_yogabase.v1.data.item collection
    ///  [resource] Unique identifier of the item
    WithResourceItemRequestBuilder byResource(String resource) {
        var urlTplParams = Map.of(pathParameters);
        urlTplParams.putIfAbsent('resource', () => resource);
        return WithResourceItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    DataRequestBuilder clone() {
        return DataRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [DataRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    DataRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/data", pathParameters) ;
    /// Instantiates a new [DataRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    DataRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/data", {RequestInformation.rawUrlKey : rawUrl}) ;
}
