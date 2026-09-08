// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import '../../../models/game_server.dart';
import '../../../models/problem.dart';
import './stop/stop_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\servers\{serverId}
class WithServerItemRequestBuilder extends BaseRequestBuilder<WithServerItemRequestBuilder> {
    ///  The stop property
    StopRequestBuilder get stop {
        return StopRequestBuilder(pathParameters, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    WithServerItemRequestBuilder clone() {
        return WithServerItemRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [WithServerItemRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithServerItemRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/servers/{serverId}", pathParameters) ;
    /// Instantiates a new [WithServerItemRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithServerItemRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/servers/{serverId}", {RequestInformation.rawUrlKey : rawUrl}) ;
    /// One instance of the caller's environment. A server id from another project is answered exactly as one that does not exist.
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<GameServer?> getAsync([void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toGetRequestInformation(requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '401' :  Problem.createFromDiscriminatorValue,
            '404' :  Problem.createFromDiscriminatorValue,
            '503' :  Problem.createFromDiscriminatorValue,
        };
        return await requestAdapter.send<GameServer>(requestInfo, GameServer.createFromDiscriminatorValue, errorMapping);
    }
    /// One instance of the caller's environment. A server id from another project is answered exactly as one that does not exist.
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toGetRequestInformation([void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.get, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<DefaultQueryParameters>(requestConfiguration, () => DefaultQueryParameters());
        requestInfo.headers.put('Accept', 'application/json');
        return requestInfo;
    }
}
