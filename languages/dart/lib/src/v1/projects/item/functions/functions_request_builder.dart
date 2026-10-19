// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './item/with_function_item_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\projects\{projectId}\functions
class FunctionsRequestBuilder extends BaseRequestBuilder<FunctionsRequestBuilder> {
    /// Gets an item from the yogabase_client.v1.projects.item.functions.item collection
    ///  [functionId] Unique identifier of the item
    WithFunctionItemRequestBuilder byFunctionId(String functionId) {
        var urlTplParams = Map.of(pathParameters);
        urlTplParams.putIfAbsent('functionId', () => functionId);
        return WithFunctionItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    FunctionsRequestBuilder clone() {
        return FunctionsRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [FunctionsRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    FunctionsRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/functions", pathParameters) ;
    /// Instantiates a new [FunctionsRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    FunctionsRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/functions", {RequestInformation.rawUrlKey : rawUrl}) ;
}
