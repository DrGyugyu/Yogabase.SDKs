// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import '../../../models/game_server.dart';
import '../../../models/game_server_start_request.dart';
import '../../../models/problem.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\servers\start
class StartRequestBuilder extends BaseRequestBuilder<StartRequestBuilder> {
    /// Clones the requestbuilder.
    @override
    StartRequestBuilder clone() {
        return StartRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [StartRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    StartRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/servers/start", pathParameters) ;
    /// Instantiates a new [StartRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    StartRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/servers/start", {RequestInformation.rawUrlKey : rawUrl}) ;
    /// Allocates one Ready instance of a hosted service to the signed-in player and returns the address to connect to. The Control Plane decides which instance; the gateway relays. A project with no Ready instance carrying an observed address is refused with server_none_available rather than given an address nothing answers on.
    ///  [body] The request body
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<GameServer?> postAsync(GameServerStartRequest body, [void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toPostRequestInformation(body, requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '400' :  Problem.createFromDiscriminatorValue,
            '401' :  Problem.createFromDiscriminatorValue,
            '409' :  Problem.createFromDiscriminatorValue,
            '429' :  Problem.createFromDiscriminatorValue,
            '503' :  Problem.createFromDiscriminatorValue,
        };
        return await requestAdapter.send<GameServer>(requestInfo, GameServer.createFromDiscriminatorValue, errorMapping);
    }
    /// Allocates one Ready instance of a hosted service to the signed-in player and returns the address to connect to. The Control Plane decides which instance; the gateway relays. A project with no Ready instance carrying an observed address is refused with server_none_available rather than given an address nothing answers on.
    ///  [body] The request body
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toPostRequestInformation(GameServerStartRequest body, [void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.post, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<DefaultQueryParameters>(requestConfiguration, () => DefaultQueryParameters());
        requestInfo.headers.put('Accept', 'application/json');
        requestInfo.setContentFromParsable(requestAdapter, 'application/json', body);
        return requestInfo;
    }
}
