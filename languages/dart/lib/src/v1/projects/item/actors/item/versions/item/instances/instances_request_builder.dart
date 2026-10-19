// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './item/with_instance_key_item_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\projects\{projectId}\actors\{functionId}\versions\{functionVersion}\instances
class InstancesRequestBuilder extends BaseRequestBuilder<InstancesRequestBuilder> {
    /// Gets an item from the yogabase_client.v1.projects.item.actors.item.versions.item.instances.item collection
    ///  [instanceKey] Unique identifier of the item
    WithInstanceKeyItemRequestBuilder byInstanceKey(String instanceKey) {
        var urlTplParams = Map.of(pathParameters);
        urlTplParams.putIfAbsent('instanceKey', () => instanceKey);
        return WithInstanceKeyItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    InstancesRequestBuilder clone() {
        return InstancesRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [InstancesRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    InstancesRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/actors/{functionId}/versions/{functionVersion}/instances", pathParameters) ;
    /// Instantiates a new [InstancesRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    InstancesRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/actors/{functionId}/versions/{functionVersion}/instances", {RequestInformation.rawUrlKey : rawUrl}) ;
}
