// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './clientConfig/client_config_request_builder.dart';
import './projects/projects_request_builder.dart';
import './servers/servers_request_builder.dart';
import './storage/storage_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \v1
class V1RequestBuilder extends BaseRequestBuilder<V1RequestBuilder> {
    ///  The clientConfig property
    ClientConfigRequestBuilder get clientConfig {
        return ClientConfigRequestBuilder(pathParameters, requestAdapter);
    }
    ///  The projects property
    ProjectsRequestBuilder get projects {
        return ProjectsRequestBuilder(pathParameters, requestAdapter);
    }
    ///  The servers property
    ServersRequestBuilder get servers {
        return ServersRequestBuilder(pathParameters, requestAdapter);
    }
    ///  The storage property
    StorageRequestBuilder get storage {
        return StorageRequestBuilder(pathParameters, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    V1RequestBuilder clone() {
        return V1RequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [V1RequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    V1RequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1", pathParameters) ;
    /// Instantiates a new [V1RequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    V1RequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1", {RequestInformation.rawUrlKey : rawUrl}) ;
}
