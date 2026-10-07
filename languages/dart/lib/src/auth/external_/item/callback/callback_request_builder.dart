// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import '../../../../models/external_identity_callback_request.dart';
import '../../../../models/problem.dart';

/// auto generated
/// Builds and executes requests for operations under \auth\external\{provider}\callback
class CallbackRequestBuilder extends BaseRequestBuilder<CallbackRequestBuilder> {
    /// Clones the requestbuilder.
    @override
    CallbackRequestBuilder clone() {
        return CallbackRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [CallbackRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    CallbackRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/auth/external/{provider}/callback", pathParameters) ;
    /// Instantiates a new [CallbackRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    CallbackRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/auth/external/{provider}/callback", {RequestInformation.rawUrlKey : rawUrl}) ;
    /// Completes a configured external identity flow and signs the user in. codeVerifier is the verifier whose challenge was sent to start: without it the code and state are refused as external_identity_invalid, so a redirect taken by another app signs nobody in. A provider-verified e-mail address no account uses starts an account (open-signup projects only); an address an account already uses is refused with 409 external_identity_email_in_use and nothing is linked.
    ///  [body] The request body
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<Problem?> postAsync(ExternalIdentityCallbackRequest body, [void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toPostRequestInformation(body, requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '400' :  Problem.createFromDiscriminatorValue,
            '401' :  Problem.createFromDiscriminatorValue,
            '403' :  Problem.createFromDiscriminatorValue,
            '404' :  Problem.createFromDiscriminatorValue,
            '409' :  Problem.createFromDiscriminatorValue,
            '503' :  Problem.createFromDiscriminatorValue,
        };
        return await requestAdapter.send<Problem>(requestInfo, Problem.createFromDiscriminatorValue, errorMapping);
    }
    /// Completes a configured external identity flow and signs the user in. codeVerifier is the verifier whose challenge was sent to start: without it the code and state are refused as external_identity_invalid, so a redirect taken by another app signs nobody in. A provider-verified e-mail address no account uses starts an account (open-signup projects only); an address an account already uses is refused with 409 external_identity_email_in_use and nothing is linked.
    ///  [body] The request body
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toPostRequestInformation(ExternalIdentityCallbackRequest body, [void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.post, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<DefaultQueryParameters>(requestConfiguration, () => DefaultQueryParameters());
        requestInfo.headers.put('Accept', 'application/problem+json');
        requestInfo.setContentFromParsable(requestAdapter, 'application/json', body);
        return requestInfo;
    }
}
