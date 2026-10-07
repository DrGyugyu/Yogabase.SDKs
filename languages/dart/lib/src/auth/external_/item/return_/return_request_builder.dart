// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import '../../../../models/problem.dart';
import './return_post_request_body.dart';
import './return_request_builder_get_query_parameters.dart';

/// auto generated
/// Builds and executes requests for operations under \auth\external\{provider}\return
class ReturnRequestBuilder extends BaseRequestBuilder<ReturnRequestBuilder> {
    /// Clones the requestbuilder.
    @override
    ReturnRequestBuilder clone() {
        return ReturnRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [ReturnRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    ReturnRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/auth/external/{provider}/return{?code*,state*}", pathParameters) ;
    /// Instantiates a new [ReturnRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    ReturnRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/auth/external/{provider}/return{?code*,state*}", {RequestInformation.rawUrlKey : rawUrl}) ;
    /// The one address registered with a provider (https://<gateway>/auth/external/<id>/return); no application calls it. A provider that answers by query lands here. It redirects to the redirect URI given to start, carrying the code and state, or error=access_denied and the state when the provider sent no code. Nothing else of the answer is read.
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<void> getAsync([void Function(RequestConfiguration<ReturnRequestBuilderGetQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toGetRequestInformation(requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '401' :  Problem.createFromDiscriminatorValue,
            '404' :  Problem.createFromDiscriminatorValue,
            '503' :  Problem.createFromDiscriminatorValue,
        };
        return await requestAdapter.sendNoContent(requestInfo, errorMapping);
    }
    /// The same address for a provider that answers by form_post (Apple). Same redirect, same fields.
    ///  [body] The request body
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<void> postAsync(ReturnPostRequestBody body, [void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toPostRequestInformation(body, requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '401' :  Problem.createFromDiscriminatorValue,
            '404' :  Problem.createFromDiscriminatorValue,
            '503' :  Problem.createFromDiscriminatorValue,
        };
        return await requestAdapter.sendNoContent(requestInfo, errorMapping);
    }
    /// The one address registered with a provider (https://<gateway>/auth/external/<id>/return); no application calls it. A provider that answers by query lands here. It redirects to the redirect URI given to start, carrying the code and state, or error=access_denied and the state when the provider sent no code. Nothing else of the answer is read.
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toGetRequestInformation([void Function(RequestConfiguration<ReturnRequestBuilderGetQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.get, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<ReturnRequestBuilderGetQueryParameters>(requestConfiguration, () => ReturnRequestBuilderGetQueryParameters());
        requestInfo.headers.put('Accept', 'application/problem+json');
        return requestInfo;
    }
    /// The same address for a provider that answers by form_post (Apple). Same redirect, same fields.
    ///  [body] The request body
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toPostRequestInformation(ReturnPostRequestBody body, [void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.post, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<DefaultQueryParameters>(requestConfiguration, () => DefaultQueryParameters());
        requestInfo.headers.put('Accept', 'application/problem+json');
        requestInfo.setContentFromParsable(requestAdapter, 'application/x-www-form-urlencoded', body);
        return requestInfo;
    }
}
