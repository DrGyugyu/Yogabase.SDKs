// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import '../../../../../../../../models/problem.dart';
import '../../../../../../../../models/workflow_start_request.dart';
import '../../../../../../../../models/workflow_start_result.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\projects\{projectId}\workflows\{workflowId}\versions\{definitionVersion}\start
class StartRequestBuilder extends BaseRequestBuilder<StartRequestBuilder> {
    /// Clones the requestbuilder.
    @override
    StartRequestBuilder clone() {
        return StartRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [StartRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    StartRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/workflows/{workflowId}/versions/{definitionVersion}/start", pathParameters) ;
    /// Instantiates a new [StartRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    StartRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/workflows/{workflowId}/versions/{definitionVersion}/start", {RequestInformation.rawUrlKey : rawUrl}) ;
    /// Starts or idempotently replays one exact immutable Workflow definition.
    ///  [body] The request body
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<WorkflowStartResult?> postAsync(WorkflowStartRequest body, [void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toPostRequestInformation(body, requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '400' :  Problem.createFromDiscriminatorValue,
            '401' :  Problem.createFromDiscriminatorValue,
            '403' :  Problem.createFromDiscriminatorValue,
            '404' :  Problem.createFromDiscriminatorValue,
            '409' :  Problem.createFromDiscriminatorValue,
            '422' :  Problem.createFromDiscriminatorValue,
            '503' :  Problem.createFromDiscriminatorValue,
        };
        return await requestAdapter.send<WorkflowStartResult>(requestInfo, WorkflowStartResult.createFromDiscriminatorValue, errorMapping);
    }
    /// Starts or idempotently replays one exact immutable Workflow definition.
    ///  [body] The request body
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toPostRequestInformation(WorkflowStartRequest body, [void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.post, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<DefaultQueryParameters>(requestConfiguration, () => DefaultQueryParameters());
        requestInfo.headers.put('Accept', 'application/json');
        requestInfo.setContentFromParsable(requestAdapter, 'application/json', body);
        return requestInfo;
    }
}
