// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';
import '../../../../models/job.dart';
import '../../../../models/job_submit_request.dart';
import '../../../../models/problem.dart';
import './item/with_job_item_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\projects\{projectId}\jobs
class JobsRequestBuilder extends BaseRequestBuilder<JobsRequestBuilder> {
    /// Gets an item from the stackyoga_yogabase.v1.projects.item.jobs.item collection
    ///  [jobId] A job's id, as returned by submit. An id that is not a uuid in its hyphenated form, or is the all-zero uuid, is 400 job_request_invalid.
    WithJobItemRequestBuilder byJobId(UuidValue jobId) {
        var urlTplParams = Map.of(pathParameters);
        urlTplParams.putIfAbsent('jobId', () => jobId);
        return WithJobItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    JobsRequestBuilder clone() {
        return JobsRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [JobsRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    JobsRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/jobs", pathParameters) ;
    /// Instantiates a new [JobsRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    JobsRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/jobs", {RequestInformation.rawUrlKey : rawUrl}) ;
    /// Starts one durable run of a deployed stateless function as the signed-in user. The job type is the id of a deployed stateless function, run by its highest released version; an id no released function carries is 422 job_type_not_registered and records nothing. The idempotency key is a member of the body, belongs to the signed-in user, and binds the whole request (job type, payload and maxAttempts): the same request under it answers the job it already made, 202 again and in whatever state that job has reached, and a different request under it is 409 job_idempotency_conflict. The 202 carries the job's own route in Location. A request body that cannot be read as one JSON object of exactly the documented members is a bare 400, and one that is not JSON a bare 415; a member that is missing or out of range (an empty or over-long job type or key, maxAttempts outside 1 to 10, a payload that is not base64) is 400 job_request_invalid.
    ///  [body] The request body
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<Job?> postAsync(JobSubmitRequest body, [void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toPostRequestInformation(body, requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '400' :  Problem.createFromDiscriminatorValue,
            '401' :  Problem.createFromDiscriminatorValue,
            '403' :  Problem.createFromDiscriminatorValue,
            '409' :  Problem.createFromDiscriminatorValue,
            '413' :  Problem.createFromDiscriminatorValue,
            '422' :  Problem.createFromDiscriminatorValue,
            '503' :  Problem.createFromDiscriminatorValue,
        };
        return await requestAdapter.send<Job>(requestInfo, Job.createFromDiscriminatorValue, errorMapping);
    }
    /// Starts one durable run of a deployed stateless function as the signed-in user. The job type is the id of a deployed stateless function, run by its highest released version; an id no released function carries is 422 job_type_not_registered and records nothing. The idempotency key is a member of the body, belongs to the signed-in user, and binds the whole request (job type, payload and maxAttempts): the same request under it answers the job it already made, 202 again and in whatever state that job has reached, and a different request under it is 409 job_idempotency_conflict. The 202 carries the job's own route in Location. A request body that cannot be read as one JSON object of exactly the documented members is a bare 400, and one that is not JSON a bare 415; a member that is missing or out of range (an empty or over-long job type or key, maxAttempts outside 1 to 10, a payload that is not base64) is 400 job_request_invalid.
    ///  [body] The request body
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toPostRequestInformation(JobSubmitRequest body, [void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.post, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<DefaultQueryParameters>(requestConfiguration, () => DefaultQueryParameters());
        requestInfo.headers.put('Accept', 'application/json');
        requestInfo.setContentFromParsable(requestAdapter, 'application/json', body);
        return requestInfo;
    }
}
