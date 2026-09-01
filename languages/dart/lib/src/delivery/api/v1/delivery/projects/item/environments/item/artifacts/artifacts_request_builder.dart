// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';
import '../../../../../../../../models/delivery_artifact_list.dart';
import '../../../../../../../../models/delivery_operation_envelope.dart';
import '../../../../../../../../models/delivery_submit_request.dart';
import '../../../../../../../../models/problem_details.dart';
import './artifacts_request_builder_get_query_parameters.dart';
import './item/with_admission_item_request_builder.dart';
import './previews/previews_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \api\v1\delivery\projects\{projectId}\environments\{environmentId}\artifacts
class ArtifactsRequestBuilder extends BaseRequestBuilder<ArtifactsRequestBuilder> {
    ///  The previews property
    PreviewsRequestBuilder get previews {
        return PreviewsRequestBuilder(pathParameters, requestAdapter);
    }
    /// Gets an item from the stackyoga_yogabase_delivery.api.v1.delivery.projects.item.environments.item.artifacts.item collection
    ///  [admissionId] Unique identifier of the item
    WithAdmissionItemRequestBuilder byAdmissionId(UuidValue admissionId) {
        var urlTplParams = Map.of(pathParameters);
        urlTplParams.putIfAbsent('admissionId', () => admissionId);
        return WithAdmissionItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    ArtifactsRequestBuilder clone() {
        return ArtifactsRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [ArtifactsRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    ArtifactsRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/api/v1/delivery/projects/{projectId}/environments/{environmentId}/artifacts{?cursor*,limit*}", pathParameters) ;
    /// Instantiates a new [ArtifactsRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    ArtifactsRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/api/v1/delivery/projects/{projectId}/environments/{environmentId}/artifacts{?cursor*,limit*}", {RequestInformation.rawUrlKey : rawUrl}) ;
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<DeliveryArtifactList?> getAsync([void Function(RequestConfiguration<ArtifactsRequestBuilderGetQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toGetRequestInformation(requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '401' :  ProblemDetails.createFromDiscriminatorValue,
            '404' :  ProblemDetails.createFromDiscriminatorValue,
            '429' :  ProblemDetails.createFromDiscriminatorValue,
        };
        return await requestAdapter.send<DeliveryArtifactList>(requestInfo, DeliveryArtifactList.createFromDiscriminatorValue, errorMapping);
    }
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
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toGetRequestInformation([void Function(RequestConfiguration<ArtifactsRequestBuilderGetQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.get, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<ArtifactsRequestBuilderGetQueryParameters>(requestConfiguration, () => ArtifactsRequestBuilderGetQueryParameters());
        requestInfo.headers.put('Accept', 'application/json');
        return requestInfo;
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
