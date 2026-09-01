// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './cancel/cancel_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\projects\{projectId}\workflows\instances\{workflowInstanceId}
class WithWorkflowInstanceItemRequestBuilder extends BaseRequestBuilder<WithWorkflowInstanceItemRequestBuilder> {
    ///  The cancel property
    CancelRequestBuilder get cancel {
        return CancelRequestBuilder(pathParameters, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    WithWorkflowInstanceItemRequestBuilder clone() {
        return WithWorkflowInstanceItemRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [WithWorkflowInstanceItemRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithWorkflowInstanceItemRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/workflows/instances/{workflowInstanceId}", pathParameters) ;
    /// Instantiates a new [WithWorkflowInstanceItemRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithWorkflowInstanceItemRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/workflows/instances/{workflowInstanceId}", {RequestInformation.rawUrlKey : rawUrl}) ;
}
