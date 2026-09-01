// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import '../../../../../../../models/problem_details.dart';
import './events_request_builder_get_query_parameters.dart';

/// auto generated
/// Builds and executes requests for operations under \api\v1\delivery\projects\{projectId}\operations\events
class EventsRequestBuilder extends BaseRequestBuilder<EventsRequestBuilder> {
    /// Clones the requestbuilder.
    @override
    EventsRequestBuilder clone() {
        return EventsRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [EventsRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    EventsRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/api/v1/delivery/projects/{projectId}/operations/events{?environmentId*,operationId*}", pathParameters) ;
    /// Instantiates a new [EventsRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    EventsRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/api/v1/delivery/projects/{projectId}/operations/events{?environmentId*,operationId*}", {RequestInformation.rawUrlKey : rawUrl}) ;
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<Iterable<int>?> getAsync([void Function(RequestConfiguration<EventsRequestBuilderGetQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toGetRequestInformation(requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '401' :  ProblemDetails.createFromDiscriminatorValue,
            '404' :  ProblemDetails.createFromDiscriminatorValue,
            '429' :  ProblemDetails.createFromDiscriminatorValue,
        };
        return await requestAdapter.sendPrimitiveCollection<int>(requestInfo, errorMapping);
    }
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toGetRequestInformation([void Function(RequestConfiguration<EventsRequestBuilderGetQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.get, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<EventsRequestBuilderGetQueryParameters>(requestConfiguration, () => EventsRequestBuilderGetQueryParameters());
        requestInfo.headers.put('Accept', 'text/event-stream, application/problem+json');
        return requestInfo;
    }
}
