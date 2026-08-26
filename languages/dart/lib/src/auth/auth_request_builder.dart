// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './signup/signup_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \auth
class AuthRequestBuilder extends BaseRequestBuilder<AuthRequestBuilder> {
    ///  The signup property
    SignupRequestBuilder get signup {
        return SignupRequestBuilder(pathParameters, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    AuthRequestBuilder clone() {
        return AuthRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [AuthRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    AuthRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/auth", pathParameters) ;
    /// Instantiates a new [AuthRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    AuthRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/auth", {RequestInformation.rawUrlKey : rawUrl}) ;
}
