// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import '../../../../models/game_server.dart';
import '../../../../models/problem.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\servers\{serverId}\stop
class StopRequestBuilder extends BaseRequestBuilder<StopRequestBuilder> {
    /// Clones the requestbuilder.
    @override
    StopRequestBuilder clone() {
        return StopRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [StopRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    StopRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/servers/{serverId}/stop", pathParameters) ;
    /// Instantiates a new [StopRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    StopRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/servers/{serverId}/stop", {RequestInformation.rawUrlKey : rawUrl}) ;
    /// Releases the allocation this caller holds on one instance, returning it to the pool. It is never a deletion: retirement is decided by the environment's own capacity policy, and an instance allocated to a different caller is refused with server_stop_not_available.
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<GameServer?> postAsync([void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toPostRequestInformation(requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '401' :  Problem.createFromDiscriminatorValue,
            '404' :  Problem.createFromDiscriminatorValue,
            '409' :  Problem.createFromDiscriminatorValue,
            '503' :  Problem.createFromDiscriminatorValue,
        };
        return await requestAdapter.send<GameServer>(requestInfo, GameServer.createFromDiscriminatorValue, errorMapping);
    }
    /// Releases the allocation this caller holds on one instance, returning it to the pool. It is never a deletion: retirement is decided by the environment's own capacity policy, and an instance allocated to a different caller is refused with server_stop_not_available.
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toPostRequestInformation([void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.post, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<DefaultQueryParameters>(requestConfiguration, () => DefaultQueryParameters());
        requestInfo.headers.put('Accept', 'application/json');
        return requestInfo;
    }
}
