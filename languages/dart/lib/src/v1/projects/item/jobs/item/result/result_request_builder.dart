// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import '../../../../../../models/problem.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\projects\{projectId}\jobs\{jobId}\result
class ResultRequestBuilder extends BaseRequestBuilder<ResultRequestBuilder> {
    /// Clones the requestbuilder.
    @override
    ResultRequestBuilder clone() {
        return ResultRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [ResultRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    ResultRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/jobs/{jobId}/result", pathParameters) ;
    /// Instantiates a new [ResultRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    ResultRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/jobs/{jobId}/result", {RequestInformation.rawUrlKey : rawUrl}) ;
    /// The result of the caller's own succeeded job: the bytes its function returned, exactly as it returned them, from one byte to 16 MiB. The gateway reads the stored object twice, once to check it against the digest the job recorded and once to send it, so no byte leaves before all of them have been checked; that digest is in X-Yogabase-Content-Sha256. 404 job_result_not_found is the one answer for another user's job, an id that does not exist, and a job that finished without a result (cancelled, dead-lettered, or succeeded with none), so it never says which. 409 job_result_not_ready is the caller's own job that has not settled. 500 job_result_reference_invalid and 502 job_result_integrity_failed mean the stored result is not the one the job recorded, and send no byte of it.
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<Iterable<int>?> getAsync([void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toGetRequestInformation(requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '400' :  Problem.createFromDiscriminatorValue,
            '401' :  Problem.createFromDiscriminatorValue,
            '403' :  Problem.createFromDiscriminatorValue,
            '404' :  Problem.createFromDiscriminatorValue,
            '409' :  Problem.createFromDiscriminatorValue,
            '500' :  Problem.createFromDiscriminatorValue,
            '502' :  Problem.createFromDiscriminatorValue,
            '503' :  Problem.createFromDiscriminatorValue,
        };
        return await requestAdapter.sendPrimitiveCollection<int>(requestInfo, errorMapping);
    }
    /// The result of the caller's own succeeded job: the bytes its function returned, exactly as it returned them, from one byte to 16 MiB. The gateway reads the stored object twice, once to check it against the digest the job recorded and once to send it, so no byte leaves before all of them have been checked; that digest is in X-Yogabase-Content-Sha256. 404 job_result_not_found is the one answer for another user's job, an id that does not exist, and a job that finished without a result (cancelled, dead-lettered, or succeeded with none), so it never says which. 409 job_result_not_ready is the caller's own job that has not settled. 500 job_result_reference_invalid and 502 job_result_integrity_failed mean the stored result is not the one the job recorded, and send no byte of it.
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toGetRequestInformation([void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.get, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<DefaultQueryParameters>(requestConfiguration, () => DefaultQueryParameters());
        requestInfo.headers.put('Accept', 'application/octet-stream, application/problem+json');
        return requestInfo;
    }
}
