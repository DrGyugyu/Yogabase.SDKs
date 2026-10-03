// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import '../../../../../../../../models/invocation_request.dart';
import '../../../../../../../../models/invocation_result.dart';
import '../../../../../../../../models/problem.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\projects\{projectId}\functions\{functionId}\versions\{functionVersion}\invoke
class InvokeRequestBuilder extends BaseRequestBuilder<InvokeRequestBuilder> {
    /// Clones the requestbuilder.
    @override
    InvokeRequestBuilder clone() {
        return InvokeRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [InvokeRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    InvokeRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/functions/{functionId}/versions/{functionVersion}/invoke", pathParameters) ;
    /// Instantiates a new [InvokeRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    InvokeRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/functions/{functionId}/versions/{functionVersion}/invoke", {RequestInformation.rawUrlKey : rawUrl}) ;
    ///  [body] The request body
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<InvocationResult?> postAsync(InvocationRequest body, [void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toPostRequestInformation(body, requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '400' :  Problem.createFromDiscriminatorValue,
            '401' :  Problem.createFromDiscriminatorValue,
            '403' :  Problem.createFromDiscriminatorValue,
            '404' :  InvocationResult.createFromDiscriminatorValue,
            '408' :  InvocationResult.createFromDiscriminatorValue,
            '409' :  InvocationResult.createFromDiscriminatorValue,
            '413' :  Problem.createFromDiscriminatorValue,
            '422' :  InvocationResult.createFromDiscriminatorValue,
            '503' :  InvocationResult.createFromDiscriminatorValue,
        };
        return await requestAdapter.send<InvocationResult>(requestInfo, InvocationResult.createFromDiscriminatorValue, errorMapping);
    }
    ///  [body] The request body
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toPostRequestInformation(InvocationRequest body, [void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.post, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<DefaultQueryParameters>(requestConfiguration, () => DefaultQueryParameters());
        requestInfo.headers.put('Accept', 'application/json');
        requestInfo.setContentFromParsable(requestAdapter, 'application/json', body);
        return requestInfo;
    }
}
