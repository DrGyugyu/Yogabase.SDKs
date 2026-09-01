// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './confirm/confirm_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \auth\email
class EmailRequestBuilder extends BaseRequestBuilder<EmailRequestBuilder> {
    ///  The confirm property
    ConfirmRequestBuilder get confirm {
        return ConfirmRequestBuilder(pathParameters, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    EmailRequestBuilder clone() {
        return EmailRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [EmailRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    EmailRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/auth/email", pathParameters) ;
    /// Instantiates a new [EmailRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    EmailRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/auth/email", {RequestInformation.rawUrlKey : rawUrl}) ;
}
