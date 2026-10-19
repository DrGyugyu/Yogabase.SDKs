// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import '../../models/game_server.dart';
import '../../models/problem.dart';
import './item/with_server_item_request_builder.dart';
import './start/start_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\servers
class ServersRequestBuilder extends BaseRequestBuilder<ServersRequestBuilder> {
    ///  The start property
    StartRequestBuilder get start {
        return StartRequestBuilder(pathParameters, requestAdapter);
    }
    /// Gets an item from the yogabase_client.v1.servers.item collection
    ///  [serverId] Opaque game-server instance identifier, as returned by start or list.
    WithServerItemRequestBuilder byServerId(String serverId) {
        var urlTplParams = Map.of(pathParameters);
        urlTplParams.putIfAbsent('serverId', () => serverId);
        return WithServerItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    ServersRequestBuilder clone() {
        return ServersRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [ServersRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    ServersRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/servers", pathParameters) ;
    /// Instantiates a new [ServersRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    ServersRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/servers", {RequestInformation.rawUrlKey : rawUrl}) ;
    /// Every instance of the caller's environment as a bare array, ordered by server id. An instance whose address no member has observed is listed with an empty networkPorts rather than omitted or given a composed host.
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<Iterable<GameServer>?> getAsync([void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toGetRequestInformation(requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '401' :  Problem.createFromDiscriminatorValue,
            '503' :  Problem.createFromDiscriminatorValue,
        };
        return await requestAdapter.sendCollection<GameServer>(requestInfo, GameServer.createFromDiscriminatorValue, errorMapping);
    }
    /// Every instance of the caller's environment as a bare array, ordered by server id. An instance whose address no member has observed is listed with an empty networkPorts rather than omitted or given a composed host.
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toGetRequestInformation([void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.get, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<DefaultQueryParameters>(requestConfiguration, () => DefaultQueryParameters());
        requestInfo.headers.put('Accept', 'application/json');
        return requestInfo;
    }
}
