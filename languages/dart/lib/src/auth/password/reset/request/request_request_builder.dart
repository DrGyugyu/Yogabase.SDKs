// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import '../../../../models/password_reset_request.dart';

/// auto generated
/// Builds and executes requests for operations under \auth\password\reset\request
class RequestRequestBuilder extends BaseRequestBuilder<RequestRequestBuilder> {
    /// Clones the requestbuilder.
    @override
    RequestRequestBuilder clone() {
        return RequestRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [RequestRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    RequestRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/auth/password/reset/request", pathParameters) ;
    /// Instantiates a new [RequestRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    RequestRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/auth/password/reset/request", {RequestInformation.rawUrlKey : rawUrl}) ;
    /// Always returns the same accepted response so account existence and throttling are not disclosed.
    ///  [body] The request body
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<void> postAsync(PasswordResetRequest body, [void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toPostRequestInformation(body, requestConfiguration);
        return await requestAdapter.sendNoContent(requestInfo, {});
    }
    /// Always returns the same accepted response so account existence and throttling are not disclosed.
    ///  [body] The request body
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toPostRequestInformation(PasswordResetRequest body, [void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.post, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<DefaultQueryParameters>(requestConfiguration, () => DefaultQueryParameters());
        requestInfo.setContentFromParsable(requestAdapter, 'application/json', body);
        return requestInfo;
    }
}
