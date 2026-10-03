// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import '../../../../models/data_insert_request.dart';
import '../../../../models/data_result.dart';
import '../../../../models/problem.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\data\{table}\insert
class InsertRequestBuilder extends BaseRequestBuilder<InsertRequestBuilder> {
    /// Clones the requestbuilder.
    @override
    InsertRequestBuilder clone() {
        return InsertRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [InsertRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    InsertRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/data/{table}/insert", pathParameters) ;
    /// Instantiates a new [InsertRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    InsertRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/data/{table}/insert", {RequestInformation.rawUrlKey : rawUrl}) ;
    /// Inserts one to 500 rows. The column list is the union of the rows' keys; a row that leaves a column out takes its default. A policy's WITH CHECK refusal is 403 data_row_not_permitted.
    ///  [body] The request body
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<DataResult?> postAsync(DataInsertRequest body, [void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toPostRequestInformation(body, requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '400' :  Problem.createFromDiscriminatorValue,
            '401' :  Problem.createFromDiscriminatorValue,
            '403' :  Problem.createFromDiscriminatorValue,
            '409' :  Problem.createFromDiscriminatorValue,
            '413' :  Problem.createFromDiscriminatorValue,
            '503' :  Problem.createFromDiscriminatorValue,
        };
        return await requestAdapter.send<DataResult>(requestInfo, DataResult.createFromDiscriminatorValue, errorMapping);
    }
    /// Inserts one to 500 rows. The column list is the union of the rows' keys; a row that leaves a column out takes its default. A policy's WITH CHECK refusal is 403 data_row_not_permitted.
    ///  [body] The request body
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toPostRequestInformation(DataInsertRequest body, [void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.post, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<DefaultQueryParameters>(requestConfiguration, () => DefaultQueryParameters());
        requestInfo.headers.put('Accept', 'application/json');
        requestInfo.setContentFromParsable(requestAdapter, 'application/json', body);
        return requestInfo;
    }
}
