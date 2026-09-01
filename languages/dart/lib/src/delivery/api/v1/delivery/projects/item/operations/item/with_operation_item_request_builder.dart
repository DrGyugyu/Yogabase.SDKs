// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import '../../../../../../../models/delivery_operation_envelope.dart';
import '../../../../../../../models/problem_details.dart';
import './abandon/abandon_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \api\v1\delivery\projects\{projectId}\operations\{operationId}
class WithOperationItemRequestBuilder extends BaseRequestBuilder<WithOperationItemRequestBuilder> {
    ///  The abandon property
    AbandonRequestBuilder get abandon {
        return AbandonRequestBuilder(pathParameters, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    WithOperationItemRequestBuilder clone() {
        return WithOperationItemRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [WithOperationItemRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithOperationItemRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/api/v1/delivery/projects/{projectId}/operations/{operationId}", pathParameters) ;
    /// Instantiates a new [WithOperationItemRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithOperationItemRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/api/v1/delivery/projects/{projectId}/operations/{operationId}", {RequestInformation.rawUrlKey : rawUrl}) ;
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<DeliveryOperationEnvelope?> getAsync([void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toGetRequestInformation(requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '401' :  ProblemDetails.createFromDiscriminatorValue,
            '404' :  ProblemDetails.createFromDiscriminatorValue,
            '429' :  ProblemDetails.createFromDiscriminatorValue,
        };
        return await requestAdapter.send<DeliveryOperationEnvelope>(requestInfo, DeliveryOperationEnvelope.createFromDiscriminatorValue, errorMapping);
    }
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toGetRequestInformation([void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.get, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<DefaultQueryParameters>(requestConfiguration, () => DefaultQueryParameters());
        requestInfo.headers.put('Accept', 'application/json');
        return requestInfo;
    }
}
