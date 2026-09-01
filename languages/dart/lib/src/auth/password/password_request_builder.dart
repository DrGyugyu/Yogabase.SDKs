// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './reset/reset_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \auth\password
class PasswordRequestBuilder extends BaseRequestBuilder<PasswordRequestBuilder> {
    ///  The reset property
    ResetRequestBuilder get reset {
        return ResetRequestBuilder(pathParameters, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    PasswordRequestBuilder clone() {
        return PasswordRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [PasswordRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    PasswordRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/auth/password", pathParameters) ;
    /// Instantiates a new [PasswordRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    PasswordRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/auth/password", {RequestInformation.rawUrlKey : rawUrl}) ;
}
