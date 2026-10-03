// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import '../../../../../models/job.dart';
import '../../../../../models/problem.dart';
import './cancel/cancel_request_builder.dart';
import './result/result_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\projects\{projectId}\jobs\{jobId}
class WithJobItemRequestBuilder extends BaseRequestBuilder<WithJobItemRequestBuilder> {
    ///  The cancel property
    CancelRequestBuilder get cancel {
        return CancelRequestBuilder(pathParameters, requestAdapter);
    }
    ///  The result property
    ResultRequestBuilder get result {
        return ResultRequestBuilder(pathParameters, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    WithJobItemRequestBuilder clone() {
        return WithJobItemRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [WithJobItemRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithJobItemRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/jobs/{jobId}", pathParameters) ;
    /// Instantiates a new [WithJobItemRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithJobItemRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/jobs/{jobId}", {RequestInformation.rawUrlKey : rawUrl}) ;
    /// Reads the caller's own job: its state, how far it has got and how it ended. Another user's job is answered exactly as one that does not exist, 404 job_not_found.
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<Job?> getAsync([void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toGetRequestInformation(requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '400' :  Problem.createFromDiscriminatorValue,
            '401' :  Problem.createFromDiscriminatorValue,
            '403' :  Problem.createFromDiscriminatorValue,
            '404' :  Problem.createFromDiscriminatorValue,
            '503' :  Problem.createFromDiscriminatorValue,
        };
        return await requestAdapter.send<Job>(requestInfo, Job.createFromDiscriminatorValue, errorMapping);
    }
    /// Reads the caller's own job: its state, how far it has got and how it ended. Another user's job is answered exactly as one that does not exist, 404 job_not_found.
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toGetRequestInformation([void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.get, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<DefaultQueryParameters>(requestConfiguration, () => DefaultQueryParameters());
        requestInfo.headers.put('Accept', 'application/json');
        return requestInfo;
    }
}
