// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import '../../../../../../models/job.dart';
import '../../../../../../models/problem.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\projects\{projectId}\jobs\{jobId}\cancel
class CancelRequestBuilder extends BaseRequestBuilder<CancelRequestBuilder> {
    /// Clones the requestbuilder.
    @override
    CancelRequestBuilder clone() {
        return CancelRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [CancelRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    CancelRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/jobs/{jobId}/cancel", pathParameters) ;
    /// Instantiates a new [CancelRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    CancelRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/jobs/{jobId}/cancel", {RequestInformation.rawUrlKey : rawUrl}) ;
    /// Asks for the caller's own job to stop, idempotently, and returns it: a queued job is cancelled at once, a running one is flagged (cancellationRequested) and stopped by its worker, and a job that has settled is returned as it is. The request takes no body. Another user's job is 404 job_not_found.
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<Job?> postAsync([void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toPostRequestInformation(requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '400' :  Problem.createFromDiscriminatorValue,
            '401' :  Problem.createFromDiscriminatorValue,
            '403' :  Problem.createFromDiscriminatorValue,
            '404' :  Problem.createFromDiscriminatorValue,
            '503' :  Problem.createFromDiscriminatorValue,
        };
        return await requestAdapter.send<Job>(requestInfo, Job.createFromDiscriminatorValue, errorMapping);
    }
    /// Asks for the caller's own job to stop, idempotently, and returns it: a queued job is cancelled at once, a running one is flagged (cancellationRequested) and stopped by its worker, and a job that has settled is returned as it is. The request takes no body. Another user's job is 404 job_not_found.
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toPostRequestInformation([void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.post, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<DefaultQueryParameters>(requestConfiguration, () => DefaultQueryParameters());
        requestInfo.headers.put('Accept', 'application/json');
        return requestInfo;
    }
}
