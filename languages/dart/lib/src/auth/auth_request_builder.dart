// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './email/email_request_builder.dart';
import './external_/external_request_builder.dart';
import './login/login_request_builder.dart';
import './logout/logout_request_builder.dart';
import './mfa/mfa_request_builder.dart';
import './password/password_request_builder.dart';
import './signup/signup_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \auth
class AuthRequestBuilder extends BaseRequestBuilder<AuthRequestBuilder> {
    ///  The email property
    EmailRequestBuilder get email {
        return EmailRequestBuilder(pathParameters, requestAdapter);
    }
    ///  The external property
    ExternalRequestBuilder get external_ {
        return ExternalRequestBuilder(pathParameters, requestAdapter);
    }
    ///  The login property
    LoginRequestBuilder get login {
        return LoginRequestBuilder(pathParameters, requestAdapter);
    }
    ///  The logout property
    LogoutRequestBuilder get logout {
        return LogoutRequestBuilder(pathParameters, requestAdapter);
    }
    ///  The mfa property
    MfaRequestBuilder get mfa {
        return MfaRequestBuilder(pathParameters, requestAdapter);
    }
    ///  The password property
    PasswordRequestBuilder get password {
        return PasswordRequestBuilder(pathParameters, requestAdapter);
    }
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
