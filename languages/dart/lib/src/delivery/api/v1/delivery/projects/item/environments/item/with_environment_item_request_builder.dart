// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import '../../../../../../../models/delivery_environment_response.dart';
import '../../../../../../../models/problem_details.dart';
import './artifacts/artifacts_request_builder.dart';
import './decommissions/decommissions_request_builder.dart';
import './promotions/promotions_request_builder.dart';
import './releases/releases_request_builder.dart';
import './rollbacks/rollbacks_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \api\v1\delivery\projects\{projectId}\environments\{environmentId}
class WithEnvironmentItemRequestBuilder extends BaseRequestBuilder<WithEnvironmentItemRequestBuilder> {
    ///  The artifacts property
    ArtifactsRequestBuilder get artifacts {
        return ArtifactsRequestBuilder(pathParameters, requestAdapter);
    }
    ///  The decommissions property
    DecommissionsRequestBuilder get decommissions {
        return DecommissionsRequestBuilder(pathParameters, requestAdapter);
    }
    ///  The promotions property
    PromotionsRequestBuilder get promotions {
        return PromotionsRequestBuilder(pathParameters, requestAdapter);
    }
    ///  The releases property
    ReleasesRequestBuilder get releases {
        return ReleasesRequestBuilder(pathParameters, requestAdapter);
    }
    ///  The rollbacks property
    RollbacksRequestBuilder get rollbacks {
        return RollbacksRequestBuilder(pathParameters, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    WithEnvironmentItemRequestBuilder clone() {
        return WithEnvironmentItemRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [WithEnvironmentItemRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithEnvironmentItemRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/api/v1/delivery/projects/{projectId}/environments/{environmentId}", pathParameters) ;
    /// Instantiates a new [WithEnvironmentItemRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithEnvironmentItemRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/api/v1/delivery/projects/{projectId}/environments/{environmentId}", {RequestInformation.rawUrlKey : rawUrl}) ;
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<DeliveryEnvironmentResponse?> getAsync([void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toGetRequestInformation(requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '401' :  ProblemDetails.createFromDiscriminatorValue,
            '404' :  ProblemDetails.createFromDiscriminatorValue,
            '429' :  ProblemDetails.createFromDiscriminatorValue,
        };
        return await requestAdapter.send<DeliveryEnvironmentResponse>(requestInfo, DeliveryEnvironmentResponse.createFromDiscriminatorValue, errorMapping);
    }
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toGetRequestInformation([void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.get, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<DefaultQueryParameters>(requestConfiguration, () => DefaultQueryParameters());
        requestInfo.headers.put('Accept', 'application/json');
        return requestInfo;
    }
}
