// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './instances/instances_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\projects\{projectId}\actors\{functionId}\versions\{functionVersion}
class WithFunctionVersionItemRequestBuilder extends BaseRequestBuilder<WithFunctionVersionItemRequestBuilder> {
    ///  The instances property
    InstancesRequestBuilder get instances {
        return InstancesRequestBuilder(pathParameters, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    WithFunctionVersionItemRequestBuilder clone() {
        return WithFunctionVersionItemRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [WithFunctionVersionItemRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithFunctionVersionItemRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/actors/{functionId}/versions/{functionVersion}", pathParameters) ;
    /// Instantiates a new [WithFunctionVersionItemRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithFunctionVersionItemRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/actors/{functionId}/versions/{functionVersion}", {RequestInformation.rawUrlKey : rawUrl}) ;
}
