// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import '../../../../../../../../models/delivery_operation_envelope.dart';
import '../../../../../../../../models/delivery_submit_request.dart';
import '../../../../../../../../models/problem_details.dart';
import './previews/previews_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \api\v1\delivery\projects\{projectId}\environments\{environmentId}\promotions
class PromotionsRequestBuilder extends BaseRequestBuilder<PromotionsRequestBuilder> {
    ///  The previews property
    PreviewsRequestBuilder get previews {
        return PreviewsRequestBuilder(pathParameters, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    PromotionsRequestBuilder clone() {
        return PromotionsRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [PromotionsRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    PromotionsRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/api/v1/delivery/projects/{projectId}/environments/{environmentId}/promotions", pathParameters) ;
    /// Instantiates a new [PromotionsRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    PromotionsRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/api/v1/delivery/projects/{projectId}/environments/{environmentId}/promotions", {RequestInformation.rawUrlKey : rawUrl}) ;
    ///  [body] The request body
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<DeliveryOperationEnvelope?> postAsync(DeliverySubmitRequest body, [void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toPostRequestInformation(body, requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '400' :  ProblemDetails.createFromDiscriminatorValue,
            '401' :  ProblemDetails.createFromDiscriminatorValue,
            '409' :  ProblemDetails.createFromDiscriminatorValue,
            '422' :  ProblemDetails.createFromDiscriminatorValue,
            '429' :  ProblemDetails.createFromDiscriminatorValue,
        };
        return await requestAdapter.send<DeliveryOperationEnvelope>(requestInfo, DeliveryOperationEnvelope.createFromDiscriminatorValue, errorMapping);
    }
    ///  [body] The request body
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toPostRequestInformation(DeliverySubmitRequest body, [void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.post, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<DefaultQueryParameters>(requestConfiguration, () => DefaultQueryParameters());
        requestInfo.headers.put('Accept', 'application/json');
        requestInfo.setContentFromParsable(requestAdapter, 'application/json', body);
        return requestInfo;
    }
}
