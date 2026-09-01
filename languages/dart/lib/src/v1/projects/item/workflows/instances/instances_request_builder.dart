// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';
import './item/with_workflow_instance_item_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\projects\{projectId}\workflows\instances
class InstancesRequestBuilder extends BaseRequestBuilder<InstancesRequestBuilder> {
    /// Gets an item from the stackyoga_yogabase.v1.projects.item.workflows.instances.item collection
    ///  [workflowInstanceId] Unique identifier of the item
    WithWorkflowInstanceItemRequestBuilder byWorkflowInstanceId(UuidValue workflowInstanceId) {
        var urlTplParams = Map.of(pathParameters);
        urlTplParams.putIfAbsent('workflowInstanceId', () => workflowInstanceId);
        return WithWorkflowInstanceItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    InstancesRequestBuilder clone() {
        return InstancesRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [InstancesRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    InstancesRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/workflows/instances", pathParameters) ;
    /// Instantiates a new [InstancesRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    InstancesRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/workflows/instances", {RequestInformation.rawUrlKey : rawUrl}) ;
}
