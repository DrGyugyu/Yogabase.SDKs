// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import '../../../models/mfa_setup_response.dart';
import '../../../models/problem.dart';

/// auto generated
/// Builds and executes requests for operations under \auth\mfa\setup
class SetupRequestBuilder extends BaseRequestBuilder<SetupRequestBuilder> {
    /// Clones the requestbuilder.
    @override
    SetupRequestBuilder clone() {
        return SetupRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [SetupRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    SetupRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/auth/mfa/setup", pathParameters) ;
    /// Instantiates a new [SetupRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    SetupRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/auth/mfa/setup", {RequestInformation.rawUrlKey : rawUrl}) ;
    /// Returns an authenticator enrollment key only to the protected interactive project session.
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<MfaSetupResponse?> postAsync([void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toPostRequestInformation(requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '401' :  Problem.createFromDiscriminatorValue,
        };
        return await requestAdapter.send<MfaSetupResponse>(requestInfo, MfaSetupResponse.createFromDiscriminatorValue, errorMapping);
    }
    /// Returns an authenticator enrollment key only to the protected interactive project session.
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toPostRequestInformation([void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.post, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<DefaultQueryParameters>(requestConfiguration, () => DefaultQueryParameters());
        requestInfo.headers.put('Accept', 'application/json');
        return requestInfo;
    }
}
