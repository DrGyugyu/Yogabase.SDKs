// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import '../../../../../../models/problem.dart';
import '../../../../../../models/schedule_trigger_request.dart';
import '../../../../../../models/schedule_trigger_result.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\projects\{projectId}\schedules\{scheduleId}\trigger
class TriggerRequestBuilder extends BaseRequestBuilder<TriggerRequestBuilder> {
    /// Clones the requestbuilder.
    @override
    TriggerRequestBuilder clone() {
        return TriggerRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [TriggerRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    TriggerRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/schedules/{scheduleId}/trigger", pathParameters) ;
    /// Instantiates a new [TriggerRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    TriggerRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/schedules/{scheduleId}/trigger", {RequestInformation.rawUrlKey : rawUrl}) ;
    /// Runs the caller's own schedule once now, outside its cadence: it enqueues one invocation of the schedule's function with the schedule's payload, as the schedule's owner. The ETag last read is required in If-Match (428 without it, 412 when it is stale), and the idempotency key makes the request repeatable: the same key against the same version answers the same invocation, 202 again with outcome 1 where the first said 0. The deadline is when the invocation must finish: after now and at most an hour from now, else 400. A schedule that does not exist or is another user's is a bare 404. A schedule whose function version is no longer an admitted Stateless one of the project is a bare 409, as a write of it is. A run is not an occurrence of the schedule: its concurrencyPolicy neither holds it back nor is held back by it.
    ///  [body] The request body
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<ScheduleTriggerResult?> postAsync(ScheduleTriggerRequest body, [void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toPostRequestInformation(body, requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '400' :  Problem.createFromDiscriminatorValue,
            '401' :  Problem.createFromDiscriminatorValue,
            '403' :  Problem.createFromDiscriminatorValue,
            '412' :  Problem.createFromDiscriminatorValue,
            '428' :  Problem.createFromDiscriminatorValue,
            '503' :  Problem.createFromDiscriminatorValue,
        };
        return await requestAdapter.send<ScheduleTriggerResult>(requestInfo, ScheduleTriggerResult.createFromDiscriminatorValue, errorMapping);
    }
    /// Runs the caller's own schedule once now, outside its cadence: it enqueues one invocation of the schedule's function with the schedule's payload, as the schedule's owner. The ETag last read is required in If-Match (428 without it, 412 when it is stale), and the idempotency key makes the request repeatable: the same key against the same version answers the same invocation, 202 again with outcome 1 where the first said 0. The deadline is when the invocation must finish: after now and at most an hour from now, else 400. A schedule that does not exist or is another user's is a bare 404. A schedule whose function version is no longer an admitted Stateless one of the project is a bare 409, as a write of it is. A run is not an occurrence of the schedule: its concurrencyPolicy neither holds it back nor is held back by it.
    ///  [body] The request body
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toPostRequestInformation(ScheduleTriggerRequest body, [void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.post, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<DefaultQueryParameters>(requestConfiguration, () => DefaultQueryParameters());
        requestInfo.headers.put('Accept', 'application/json');
        requestInfo.setContentFromParsable(requestAdapter, 'application/json', body);
        return requestInfo;
    }
}
