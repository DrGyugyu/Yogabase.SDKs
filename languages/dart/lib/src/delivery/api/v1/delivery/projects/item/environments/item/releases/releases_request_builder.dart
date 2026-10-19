// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';
import '../../../../../../../../models/delivery_operation_envelope.dart';
import '../../../../../../../../models/delivery_release_list.dart';
import '../../../../../../../../models/delivery_submit_request.dart';
import '../../../../../../../../models/problem_details.dart';
import './item/with_release_item_request_builder.dart';
import './previews/previews_request_builder.dart';
import './releases_request_builder_get_query_parameters.dart';

/// auto generated
/// Builds and executes requests for operations under \api\v1\delivery\projects\{projectId}\environments\{environmentId}\releases
class ReleasesRequestBuilder extends BaseRequestBuilder<ReleasesRequestBuilder> {
    ///  The previews property
    PreviewsRequestBuilder get previews {
        return PreviewsRequestBuilder(pathParameters, requestAdapter);
    }
    /// Gets an item from the yogabase_client_delivery.api.v1.delivery.projects.item.environments.item.releases.item collection
    ///  [releaseId] Unique identifier of the item
    WithReleaseItemRequestBuilder byReleaseId(UuidValue releaseId) {
        var urlTplParams = Map.of(pathParameters);
        urlTplParams.putIfAbsent('releaseId', () => releaseId);
        return WithReleaseItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    ReleasesRequestBuilder clone() {
        return ReleasesRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [ReleasesRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    ReleasesRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/api/v1/delivery/projects/{projectId}/environments/{environmentId}/releases{?cursor*,limit*}", pathParameters) ;
    /// Instantiates a new [ReleasesRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    ReleasesRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/api/v1/delivery/projects/{projectId}/environments/{environmentId}/releases{?cursor*,limit*}", {RequestInformation.rawUrlKey : rawUrl}) ;
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<DeliveryReleaseList?> getAsync([void Function(RequestConfiguration<ReleasesRequestBuilderGetQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toGetRequestInformation(requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '401' :  ProblemDetails.createFromDiscriminatorValue,
            '404' :  ProblemDetails.createFromDiscriminatorValue,
            '429' :  ProblemDetails.createFromDiscriminatorValue,
        };
        return await requestAdapter.send<DeliveryReleaseList>(requestInfo, DeliveryReleaseList.createFromDiscriminatorValue, errorMapping);
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
    RequestInformation toGetRequestInformation([void Function(RequestConfiguration<ReleasesRequestBuilderGetQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.get, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<ReleasesRequestBuilderGetQueryParameters>(requestConfiguration, () => ReleasesRequestBuilderGetQueryParameters());
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
