// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './functions/functions_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\projects\{projectId}
class WithProjectItemRequestBuilder extends BaseRequestBuilder<WithProjectItemRequestBuilder> {
    ///  The functions property
    FunctionsRequestBuilder get functions {
        return FunctionsRequestBuilder(pathParameters, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    WithProjectItemRequestBuilder clone() {
        return WithProjectItemRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [WithProjectItemRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithProjectItemRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}", pathParameters) ;
    /// Instantiates a new [WithProjectItemRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithProjectItemRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}", {RequestInformation.rawUrlKey : rawUrl}) ;
}
