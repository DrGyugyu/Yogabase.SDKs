// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import '../../../../models/external_identity_start_request.dart';
import '../../../../models/external_identity_start_response.dart';
import '../../../../models/problem.dart';

/// auto generated
/// Builds and executes requests for operations under \auth\external\{provider}\start
class StartRequestBuilder extends BaseRequestBuilder<StartRequestBuilder> {
    /// Clones the requestbuilder.
    @override
    StartRequestBuilder clone() {
        return StartRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [StartRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    StartRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/auth/external/{provider}/start", pathParameters) ;
    /// Instantiates a new [StartRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    StartRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/auth/external/{provider}/start", {RequestInformation.rawUrlKey : rawUrl}) ;
    /// Begins sign-in with a provider the project configured, under the id the project gave it. The redirectUri must be one of the project's registered redirect URIs. The client generates a random verifier (43 to 128 characters of the unreserved set), keeps it, and sends its S256 challenge as codeChallenge. It opens authorizationUrl, compares the state the redirect comes back with to the one returned here, and sends the code, state and verifier to the callback.
    ///  [body] The request body
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<ExternalIdentityStartResponse?> postAsync(ExternalIdentityStartRequest body, [void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toPostRequestInformation(body, requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '400' :  Problem.createFromDiscriminatorValue,
            '404' :  Problem.createFromDiscriminatorValue,
            '503' :  Problem.createFromDiscriminatorValue,
        };
        return await requestAdapter.send<ExternalIdentityStartResponse>(requestInfo, ExternalIdentityStartResponse.createFromDiscriminatorValue, errorMapping);
    }
    /// Begins sign-in with a provider the project configured, under the id the project gave it. The redirectUri must be one of the project's registered redirect URIs. The client generates a random verifier (43 to 128 characters of the unreserved set), keeps it, and sends its S256 challenge as codeChallenge. It opens authorizationUrl, compares the state the redirect comes back with to the one returned here, and sends the code, state and verifier to the callback.
    ///  [body] The request body
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toPostRequestInformation(ExternalIdentityStartRequest body, [void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.post, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<DefaultQueryParameters>(requestConfiguration, () => DefaultQueryParameters());
        requestInfo.headers.put('Accept', 'application/json');
        requestInfo.setContentFromParsable(requestAdapter, 'application/json', body);
        return requestInfo;
    }
}
