// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './versions/versions_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\projects\{projectId}\actors\{functionId}
class WithFunctionItemRequestBuilder extends BaseRequestBuilder<WithFunctionItemRequestBuilder> {
    ///  The versions property
    VersionsRequestBuilder get versions {
        return VersionsRequestBuilder(pathParameters, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    WithFunctionItemRequestBuilder clone() {
        return WithFunctionItemRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [WithFunctionItemRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithFunctionItemRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/actors/{functionId}", pathParameters) ;
    /// Instantiates a new [WithFunctionItemRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithFunctionItemRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/actors/{functionId}", {RequestInformation.rawUrlKey : rawUrl}) ;
}
