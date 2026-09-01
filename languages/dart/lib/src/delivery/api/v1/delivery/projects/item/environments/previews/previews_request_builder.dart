// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import '../../../../../../../models/delivery_environment_preview_request.dart';
import '../../../../../../../models/delivery_preview_response.dart';
import '../../../../../../../models/problem_details.dart';

/// auto generated
/// Builds and executes requests for operations under \api\v1\delivery\projects\{projectId}\environments\previews
class PreviewsRequestBuilder extends BaseRequestBuilder<PreviewsRequestBuilder> {
    /// Clones the requestbuilder.
    @override
    PreviewsRequestBuilder clone() {
        return PreviewsRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [PreviewsRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    PreviewsRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/api/v1/delivery/projects/{projectId}/environments/previews", pathParameters) ;
    /// Instantiates a new [PreviewsRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    PreviewsRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/api/v1/delivery/projects/{projectId}/environments/previews", {RequestInformation.rawUrlKey : rawUrl}) ;
    ///  [body] The request body
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<DeliveryPreviewResponse?> postAsync(DeliveryEnvironmentPreviewRequest body, [void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toPostRequestInformation(body, requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '400' :  ProblemDetails.createFromDiscriminatorValue,
            '401' :  ProblemDetails.createFromDiscriminatorValue,
            '409' :  ProblemDetails.createFromDiscriminatorValue,
            '412' :  ProblemDetails.createFromDiscriminatorValue,
            '422' :  ProblemDetails.createFromDiscriminatorValue,
            '429' :  ProblemDetails.createFromDiscriminatorValue,
        };
        return await requestAdapter.send<DeliveryPreviewResponse>(requestInfo, DeliveryPreviewResponse.createFromDiscriminatorValue, errorMapping);
    }
    ///  [body] The request body
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toPostRequestInformation(DeliveryEnvironmentPreviewRequest body, [void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.post, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<DefaultQueryParameters>(requestConfiguration, () => DefaultQueryParameters());
        requestInfo.headers.put('Accept', 'application/json');
        requestInfo.setContentFromParsable(requestAdapter, 'application/json', body);
        return requestInfo;
    }
}
