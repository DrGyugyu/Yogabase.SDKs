// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './environments/environments_request_builder.dart';
import './operations/operations_request_builder.dart';
import './templates/templates_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \api\v1\delivery\projects\{projectId}
class WithProjectItemRequestBuilder extends BaseRequestBuilder<WithProjectItemRequestBuilder> {
    ///  The environments property
    EnvironmentsRequestBuilder get environments {
        return EnvironmentsRequestBuilder(pathParameters, requestAdapter);
    }
    ///  The operations property
    OperationsRequestBuilder get operations {
        return OperationsRequestBuilder(pathParameters, requestAdapter);
    }
    ///  The templates property
    TemplatesRequestBuilder get templates {
        return TemplatesRequestBuilder(pathParameters, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    WithProjectItemRequestBuilder clone() {
        return WithProjectItemRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [WithProjectItemRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithProjectItemRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/api/v1/delivery/projects/{projectId}", pathParameters) ;
    /// Instantiates a new [WithProjectItemRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithProjectItemRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/api/v1/delivery/projects/{projectId}", {RequestInformation.rawUrlKey : rawUrl}) ;
}
