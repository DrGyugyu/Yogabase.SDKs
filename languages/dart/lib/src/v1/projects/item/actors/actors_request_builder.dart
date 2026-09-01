// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './item/with_function_item_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\projects\{projectId}\actors
class ActorsRequestBuilder extends BaseRequestBuilder<ActorsRequestBuilder> {
    /// Gets an item from the stackyoga_yogabase.v1.projects.item.actors.item collection
    ///  [functionId] Unique identifier of the item
    WithFunctionItemRequestBuilder byFunctionId(String functionId) {
        var urlTplParams = Map.of(pathParameters);
        urlTplParams.putIfAbsent('functionId', () => functionId);
        return WithFunctionItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    ActorsRequestBuilder clone() {
        return ActorsRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [ActorsRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    ActorsRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/actors", pathParameters) ;
    /// Instantiates a new [ActorsRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    ActorsRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/actors", {RequestInformation.rawUrlKey : rawUrl}) ;
}
