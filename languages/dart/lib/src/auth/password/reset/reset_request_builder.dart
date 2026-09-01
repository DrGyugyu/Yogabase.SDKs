// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './complete/complete_request_builder.dart';
import './request/request_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \auth\password\reset
class ResetRequestBuilder extends BaseRequestBuilder<ResetRequestBuilder> {
    ///  The complete property
    CompleteRequestBuilder get complete {
        return CompleteRequestBuilder(pathParameters, requestAdapter);
    }
    ///  The request property
    RequestRequestBuilder get request {
        return RequestRequestBuilder(pathParameters, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    ResetRequestBuilder clone() {
        return ResetRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [ResetRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    ResetRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/auth/password/reset", pathParameters) ;
    /// Instantiates a new [ResetRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    ResetRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/auth/password/reset", {RequestInformation.rawUrlKey : rawUrl}) ;
}
