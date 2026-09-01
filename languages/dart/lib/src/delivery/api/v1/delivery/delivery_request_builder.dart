// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './projects/projects_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \api\v1\delivery
class DeliveryRequestBuilder extends BaseRequestBuilder<DeliveryRequestBuilder> {
    ///  The projects property
    ProjectsRequestBuilder get projects {
        return ProjectsRequestBuilder(pathParameters, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    DeliveryRequestBuilder clone() {
        return DeliveryRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [DeliveryRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    DeliveryRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/api/v1/delivery", pathParameters) ;
    /// Instantiates a new [DeliveryRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    DeliveryRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/api/v1/delivery", {RequestInformation.rawUrlKey : rawUrl}) ;
}
