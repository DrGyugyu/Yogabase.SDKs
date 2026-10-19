// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';
import './item/with_project_item_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \api\v1\delivery\projects
class ProjectsRequestBuilder extends BaseRequestBuilder<ProjectsRequestBuilder> {
    /// Gets an item from the yogabase_client_delivery.api.v1.delivery.projects.item collection
    ///  [projectId] Unique identifier of the item
    WithProjectItemRequestBuilder byProjectId(UuidValue projectId) {
        var urlTplParams = Map.of(pathParameters);
        urlTplParams.putIfAbsent('projectId', () => projectId);
        return WithProjectItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    ProjectsRequestBuilder clone() {
        return ProjectsRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [ProjectsRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    ProjectsRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/api/v1/delivery/projects", pathParameters) ;
    /// Instantiates a new [ProjectsRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    ProjectsRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/api/v1/delivery/projects", {RequestInformation.rawUrlKey : rawUrl}) ;
}
