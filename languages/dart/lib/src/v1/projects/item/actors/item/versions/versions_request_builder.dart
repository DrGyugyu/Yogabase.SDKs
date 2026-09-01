// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './item/with_function_version_item_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\projects\{projectId}\actors\{functionId}\versions
class VersionsRequestBuilder extends BaseRequestBuilder<VersionsRequestBuilder> {
    /// Gets an item from the stackyoga_yogabase.v1.projects.item.actors.item.versions.item collection
    ///  [functionVersion] Unique identifier of the item
    WithFunctionVersionItemRequestBuilder byFunctionVersion(String functionVersion) {
        var urlTplParams = Map.of(pathParameters);
        urlTplParams.putIfAbsent('functionVersion', () => functionVersion);
        return WithFunctionVersionItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    VersionsRequestBuilder clone() {
        return VersionsRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [VersionsRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    VersionsRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/actors/{functionId}/versions", pathParameters) ;
    /// Instantiates a new [VersionsRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    VersionsRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/actors/{functionId}/versions", {RequestInformation.rawUrlKey : rawUrl}) ;
}
