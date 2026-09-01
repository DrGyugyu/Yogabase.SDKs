// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './instances/instances_request_builder.dart';
import './item/with_workflow_item_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\projects\{projectId}\workflows
class WorkflowsRequestBuilder extends BaseRequestBuilder<WorkflowsRequestBuilder> {
    ///  The instances property
    InstancesRequestBuilder get instances {
        return InstancesRequestBuilder(pathParameters, requestAdapter);
    }
    /// Gets an item from the stackyoga_yogabase.v1.projects.item.workflows.item collection
    ///  [workflowId] Unique identifier of the item
    WithWorkflowItemRequestBuilder byWorkflowId(String workflowId) {
        var urlTplParams = Map.of(pathParameters);
        urlTplParams.putIfAbsent('workflowId', () => workflowId);
        return WithWorkflowItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    WorkflowsRequestBuilder clone() {
        return WorkflowsRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [WorkflowsRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WorkflowsRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/workflows", pathParameters) ;
    /// Instantiates a new [WorkflowsRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WorkflowsRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/workflows", {RequestInformation.rawUrlKey : rawUrl}) ;
}
