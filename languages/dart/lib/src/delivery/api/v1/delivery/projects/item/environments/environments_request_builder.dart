// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';
import '../../../../../../models/delivery_environment_list.dart';
import '../../../../../../models/delivery_operation_envelope.dart';
import '../../../../../../models/delivery_submit_request.dart';
import '../../../../../../models/problem_details.dart';
import './environments_request_builder_get_query_parameters.dart';
import './item/with_environment_item_request_builder.dart';
import './previews/previews_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \api\v1\delivery\projects\{projectId}\environments
class EnvironmentsRequestBuilder extends BaseRequestBuilder<EnvironmentsRequestBuilder> {
    ///  The previews property
    PreviewsRequestBuilder get previews {
        return PreviewsRequestBuilder(pathParameters, requestAdapter);
    }
    /// Gets an item from the yogabase_client_delivery.api.v1.delivery.projects.item.environments.item collection
    ///  [environmentId] Unique identifier of the item
    WithEnvironmentItemRequestBuilder byEnvironmentId(UuidValue environmentId) {
        var urlTplParams = Map.of(pathParameters);
        urlTplParams.putIfAbsent('environmentId', () => environmentId);
        return WithEnvironmentItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    EnvironmentsRequestBuilder clone() {
        return EnvironmentsRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [EnvironmentsRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    EnvironmentsRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/api/v1/delivery/projects/{projectId}/environments{?cursor*,limit*}", pathParameters) ;
    /// Instantiates a new [EnvironmentsRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    EnvironmentsRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/api/v1/delivery/projects/{projectId}/environments{?cursor*,limit*}", {RequestInformation.rawUrlKey : rawUrl}) ;
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<DeliveryEnvironmentList?> getAsync([void Function(RequestConfiguration<EnvironmentsRequestBuilderGetQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toGetRequestInformation(requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '401' :  ProblemDetails.createFromDiscriminatorValue,
            '404' :  ProblemDetails.createFromDiscriminatorValue,
            '429' :  ProblemDetails.createFromDiscriminatorValue,
        };
        return await requestAdapter.send<DeliveryEnvironmentList>(requestInfo, DeliveryEnvironmentList.createFromDiscriminatorValue, errorMapping);
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
    RequestInformation toGetRequestInformation([void Function(RequestConfiguration<EnvironmentsRequestBuilderGetQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.get, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<EnvironmentsRequestBuilderGetQueryParameters>(requestConfiguration, () => EnvironmentsRequestBuilderGetQueryParameters());
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
