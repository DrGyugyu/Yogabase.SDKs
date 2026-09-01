// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import '../../../../../../../../../models/delivery_release_response.dart';
import '../../../../../../../../../models/problem_details.dart';

/// auto generated
/// Builds and executes requests for operations under \api\v1\delivery\projects\{projectId}\environments\{environmentId}\releases\{releaseId}
class WithReleaseItemRequestBuilder extends BaseRequestBuilder<WithReleaseItemRequestBuilder> {
    /// Clones the requestbuilder.
    @override
    WithReleaseItemRequestBuilder clone() {
        return WithReleaseItemRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [WithReleaseItemRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithReleaseItemRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/api/v1/delivery/projects/{projectId}/environments/{environmentId}/releases/{releaseId}", pathParameters) ;
    /// Instantiates a new [WithReleaseItemRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithReleaseItemRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/api/v1/delivery/projects/{projectId}/environments/{environmentId}/releases/{releaseId}", {RequestInformation.rawUrlKey : rawUrl}) ;
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<DeliveryReleaseResponse?> getAsync([void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toGetRequestInformation(requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '401' :  ProblemDetails.createFromDiscriminatorValue,
            '404' :  ProblemDetails.createFromDiscriminatorValue,
            '429' :  ProblemDetails.createFromDiscriminatorValue,
        };
        return await requestAdapter.send<DeliveryReleaseResponse>(requestInfo, DeliveryReleaseResponse.createFromDiscriminatorValue, errorMapping);
    }
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toGetRequestInformation([void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.get, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<DefaultQueryParameters>(requestConfiguration, () => DefaultQueryParameters());
        requestInfo.headers.put('Accept', 'application/json');
        return requestInfo;
    }
}
