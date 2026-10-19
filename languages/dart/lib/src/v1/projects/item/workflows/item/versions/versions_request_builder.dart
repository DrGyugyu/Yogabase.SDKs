// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './item/with_definition_version_item_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\projects\{projectId}\workflows\{workflowId}\versions
class VersionsRequestBuilder extends BaseRequestBuilder<VersionsRequestBuilder> {
    /// Gets an item from the yogabase_client.v1.projects.item.workflows.item.versions.item collection
    ///  [definitionVersion] Unique identifier of the item
    WithDefinitionVersionItemRequestBuilder byDefinitionVersion(String definitionVersion) {
        var urlTplParams = Map.of(pathParameters);
        urlTplParams.putIfAbsent('definitionVersion', () => definitionVersion);
        return WithDefinitionVersionItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    VersionsRequestBuilder clone() {
        return VersionsRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [VersionsRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    VersionsRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/workflows/{workflowId}/versions", pathParameters) ;
    /// Instantiates a new [VersionsRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    VersionsRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/workflows/{workflowId}/versions", {RequestInformation.rawUrlKey : rawUrl}) ;
}
