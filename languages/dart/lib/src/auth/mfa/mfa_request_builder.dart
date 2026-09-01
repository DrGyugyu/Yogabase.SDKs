// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './confirm/confirm_request_builder.dart';
import './recovery/recovery_request_builder.dart';
import './setup/setup_request_builder.dart';
import './verify/verify_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \auth\mfa
class MfaRequestBuilder extends BaseRequestBuilder<MfaRequestBuilder> {
    ///  The confirm property
    ConfirmRequestBuilder get confirm {
        return ConfirmRequestBuilder(pathParameters, requestAdapter);
    }
    ///  The recovery property
    RecoveryRequestBuilder get recovery {
        return RecoveryRequestBuilder(pathParameters, requestAdapter);
    }
    ///  The setup property
    SetupRequestBuilder get setup {
        return SetupRequestBuilder(pathParameters, requestAdapter);
    }
    ///  The verify property
    VerifyRequestBuilder get verify {
        return VerifyRequestBuilder(pathParameters, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    MfaRequestBuilder clone() {
        return MfaRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [MfaRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    MfaRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/auth/mfa", pathParameters) ;
    /// Instantiates a new [MfaRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    MfaRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/auth/mfa", {RequestInformation.rawUrlKey : rawUrl}) ;
}
