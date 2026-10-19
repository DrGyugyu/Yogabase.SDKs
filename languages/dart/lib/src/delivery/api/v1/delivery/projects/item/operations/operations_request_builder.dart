// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';
import '../../../../../../models/delivery_operation_list.dart';
import '../../../../../../models/problem_details.dart';
import './events/events_request_builder.dart';
import './item/with_operation_item_request_builder.dart';
import './operations_request_builder_get_query_parameters.dart';

/// auto generated
/// Builds and executes requests for operations under \api\v1\delivery\projects\{projectId}\operations
class OperationsRequestBuilder extends BaseRequestBuilder<OperationsRequestBuilder> {
    ///  The events property
    EventsRequestBuilder get events {
        return EventsRequestBuilder(pathParameters, requestAdapter);
    }
    /// Gets an item from the yogabase_client_delivery.api.v1.delivery.projects.item.operations.item collection
    ///  [operationId] Unique identifier of the item
    WithOperationItemRequestBuilder byOperationId(UuidValue operationId) {
        var urlTplParams = Map.of(pathParameters);
        urlTplParams.putIfAbsent('operationId', () => operationId);
        return WithOperationItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    OperationsRequestBuilder clone() {
        return OperationsRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [OperationsRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    OperationsRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/api/v1/delivery/projects/{projectId}/operations{?cursor*,limit*}", pathParameters) ;
    /// Instantiates a new [OperationsRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    OperationsRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/api/v1/delivery/projects/{projectId}/operations{?cursor*,limit*}", {RequestInformation.rawUrlKey : rawUrl}) ;
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<DeliveryOperationList?> getAsync([void Function(RequestConfiguration<OperationsRequestBuilderGetQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toGetRequestInformation(requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '401' :  ProblemDetails.createFromDiscriminatorValue,
            '404' :  ProblemDetails.createFromDiscriminatorValue,
            '429' :  ProblemDetails.createFromDiscriminatorValue,
        };
        return await requestAdapter.send<DeliveryOperationList>(requestInfo, DeliveryOperationList.createFromDiscriminatorValue, errorMapping);
    }
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toGetRequestInformation([void Function(RequestConfiguration<OperationsRequestBuilderGetQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.get, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<OperationsRequestBuilderGetQueryParameters>(requestConfiguration, () => OperationsRequestBuilderGetQueryParameters());
        requestInfo.headers.put('Accept', 'application/json');
        return requestInfo;
    }
}
