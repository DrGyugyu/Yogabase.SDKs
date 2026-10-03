// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import '../../../../../models/problem.dart';
import '../../../../../models/schedule.dart';
import '../../../../../models/schedule_definition_request.dart';
import './trigger/trigger_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\projects\{projectId}\schedules\{scheduleId}
class WithScheduleItemRequestBuilder extends BaseRequestBuilder<WithScheduleItemRequestBuilder> {
    ///  The trigger property
    TriggerRequestBuilder get trigger {
        return TriggerRequestBuilder(pathParameters, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    WithScheduleItemRequestBuilder clone() {
        return WithScheduleItemRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [WithScheduleItemRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithScheduleItemRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/schedules/{scheduleId}", pathParameters) ;
    /// Instantiates a new [WithScheduleItemRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithScheduleItemRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/schedules/{scheduleId}", {RequestInformation.rawUrlKey : rawUrl}) ;
    /// Deletes the caller's own schedule, 204. The ETag last read is required in If-Match: 428 without it, 412 when it is stale. A schedule that does not exist or is another user's is a bare 404, to a request whose ETag is well formed whatever version it names.
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<void> deleteAsync([void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toDeleteRequestInformation(requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '400' :  Problem.createFromDiscriminatorValue,
            '401' :  Problem.createFromDiscriminatorValue,
            '403' :  Problem.createFromDiscriminatorValue,
            '412' :  Problem.createFromDiscriminatorValue,
            '428' :  Problem.createFromDiscriminatorValue,
            '503' :  Problem.createFromDiscriminatorValue,
        };
        return await requestAdapter.sendNoContent(requestInfo, errorMapping);
    }
    /// Reads the caller's own schedule, with the ETag to send as If-Match to change, delete or run it. A schedule that does not exist and one that is another user's are the same bare 404.
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<Schedule?> getAsync([void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toGetRequestInformation(requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '400' :  Problem.createFromDiscriminatorValue,
            '401' :  Problem.createFromDiscriminatorValue,
            '403' :  Problem.createFromDiscriminatorValue,
            '503' :  Problem.createFromDiscriminatorValue,
        };
        return await requestAdapter.send<Schedule>(requestInfo, Schedule.createFromDiscriminatorValue, errorMapping);
    }
    /// Creates or updates the schedule scheduleId as the signed-in user, who then owns it: only the owner reads, changes, deletes or runs it. Create with If-None-Match: * (201); update with the ETag last read as If-Match (200); exactly one of the two, and 428 when neither is sent. The answer is the schedule with its new ETag. The id in the path must be the id in the body. A schedule is recurring (cron, in timeZone) or a one-shot (oneShotAtUtc: it runs once and is then deleted), and an update cannot turn one into the other (409). Its function version must be an admitted Stateless Lambda version of the project, else 409. A request body that cannot be read as one JSON object of exactly the documented members is a bare 400, and one that is not JSON a bare 415; a member that is missing, out of range or invalid (a cron expression that does not parse, an unknown time zone, a payload that is not base64) is a 400 with a problem body, except that a missing policy is not refused (see where each is described). A payload over the limit is 413 schedule_payload_too_large.
    ///  [body] The request body
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<Schedule?> putAsync(ScheduleDefinitionRequest body, [void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toPutRequestInformation(body, requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '400' :  Problem.createFromDiscriminatorValue,
            '401' :  Problem.createFromDiscriminatorValue,
            '403' :  Problem.createFromDiscriminatorValue,
            '412' :  Problem.createFromDiscriminatorValue,
            '413' :  Problem.createFromDiscriminatorValue,
            '428' :  Problem.createFromDiscriminatorValue,
            '503' :  Problem.createFromDiscriminatorValue,
        };
        return await requestAdapter.send<Schedule>(requestInfo, Schedule.createFromDiscriminatorValue, errorMapping);
    }
    /// Deletes the caller's own schedule, 204. The ETag last read is required in If-Match: 428 without it, 412 when it is stale. A schedule that does not exist or is another user's is a bare 404, to a request whose ETag is well formed whatever version it names.
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toDeleteRequestInformation([void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.delete, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<DefaultQueryParameters>(requestConfiguration, () => DefaultQueryParameters());
        requestInfo.headers.put('Accept', 'application/problem+json');
        return requestInfo;
    }
    /// Reads the caller's own schedule, with the ETag to send as If-Match to change, delete or run it. A schedule that does not exist and one that is another user's are the same bare 404.
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toGetRequestInformation([void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.get, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<DefaultQueryParameters>(requestConfiguration, () => DefaultQueryParameters());
        requestInfo.headers.put('Accept', 'application/json');
        return requestInfo;
    }
    /// Creates or updates the schedule scheduleId as the signed-in user, who then owns it: only the owner reads, changes, deletes or runs it. Create with If-None-Match: * (201); update with the ETag last read as If-Match (200); exactly one of the two, and 428 when neither is sent. The answer is the schedule with its new ETag. The id in the path must be the id in the body. A schedule is recurring (cron, in timeZone) or a one-shot (oneShotAtUtc: it runs once and is then deleted), and an update cannot turn one into the other (409). Its function version must be an admitted Stateless Lambda version of the project, else 409. A request body that cannot be read as one JSON object of exactly the documented members is a bare 400, and one that is not JSON a bare 415; a member that is missing, out of range or invalid (a cron expression that does not parse, an unknown time zone, a payload that is not base64) is a 400 with a problem body, except that a missing policy is not refused (see where each is described). A payload over the limit is 413 schedule_payload_too_large.
    ///  [body] The request body
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toPutRequestInformation(ScheduleDefinitionRequest body, [void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.put, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<DefaultQueryParameters>(requestConfiguration, () => DefaultQueryParameters());
        requestInfo.headers.put('Accept', 'application/json');
        requestInfo.setContentFromParsable(requestAdapter, 'application/json', body);
        return requestInfo;
    }
}
