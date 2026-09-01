// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './invoke/invoke_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\projects\{projectId}\actors\{functionId}\versions\{functionVersion}\instances\{instanceKey}
class WithInstanceKeyItemRequestBuilder extends BaseRequestBuilder<WithInstanceKeyItemRequestBuilder> {
    ///  The invoke property
    InvokeRequestBuilder get invoke {
        return InvokeRequestBuilder(pathParameters, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    WithInstanceKeyItemRequestBuilder clone() {
        return WithInstanceKeyItemRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [WithInstanceKeyItemRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithInstanceKeyItemRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/actors/{functionId}/versions/{functionVersion}/instances/{instanceKey}", pathParameters) ;
    /// Instantiates a new [WithInstanceKeyItemRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithInstanceKeyItemRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/actors/{functionId}/versions/{functionVersion}/instances/{instanceKey}", {RequestInformation.rawUrlKey : rawUrl}) ;
}
