// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './start/start_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\projects\{projectId}\workflows\{workflowId}\versions\{definitionVersion}
class WithDefinitionVersionItemRequestBuilder extends BaseRequestBuilder<WithDefinitionVersionItemRequestBuilder> {
    ///  The start property
    StartRequestBuilder get start {
        return StartRequestBuilder(pathParameters, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    WithDefinitionVersionItemRequestBuilder clone() {
        return WithDefinitionVersionItemRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [WithDefinitionVersionItemRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithDefinitionVersionItemRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/workflows/{workflowId}/versions/{definitionVersion}", pathParameters) ;
    /// Instantiates a new [WithDefinitionVersionItemRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithDefinitionVersionItemRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/workflows/{workflowId}/versions/{definitionVersion}", {RequestInformation.rawUrlKey : rawUrl}) ;
}
