// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './item/with_schedule_item_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\projects\{projectId}\schedules
class SchedulesRequestBuilder extends BaseRequestBuilder<SchedulesRequestBuilder> {
    /// Gets an item from the stackyoga_yogabase.v1.projects.item.schedules.item collection
    ///  [scheduleId] The schedule's id: the one it was written under, which a write holds to the pattern of the scheduleId member of the definition (one segment of this path, so there is nothing to escape). The id is the project's, so one that another user holds cannot be created (412). A schedule written before the pattern was enforced under another id is still found by that id, percent-encoded as any path segment is, by a read, a run and a delete; a write of it is refused, so to change it delete it and create it again under an id that follows the pattern.
    WithScheduleItemRequestBuilder byScheduleId(String scheduleId) {
        var urlTplParams = Map.of(pathParameters);
        urlTplParams.putIfAbsent('scheduleId', () => scheduleId);
        return WithScheduleItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    SchedulesRequestBuilder clone() {
        return SchedulesRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [SchedulesRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    SchedulesRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/schedules", pathParameters) ;
    /// Instantiates a new [SchedulesRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    SchedulesRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/schedules", {RequestInformation.rawUrlKey : rawUrl}) ;
}
