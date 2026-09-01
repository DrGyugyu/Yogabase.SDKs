// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './regenerate/regenerate_request_builder.dart';
import './use/use_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \auth\mfa\recovery
class RecoveryRequestBuilder extends BaseRequestBuilder<RecoveryRequestBuilder> {
    ///  The regenerate property
    RegenerateRequestBuilder get regenerate {
        return RegenerateRequestBuilder(pathParameters, requestAdapter);
    }
    ///  The use property
    UseRequestBuilder get use {
        return UseRequestBuilder(pathParameters, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    RecoveryRequestBuilder clone() {
        return RecoveryRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [RecoveryRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    RecoveryRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/auth/mfa/recovery", pathParameters) ;
    /// Instantiates a new [RecoveryRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    RecoveryRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/auth/mfa/recovery", {RequestInformation.rawUrlKey : rawUrl}) ;
}
