// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import '../../../../models/problem.dart';
import '../../../../models/recovery_codes_response.dart';

/// auto generated
/// Builds and executes requests for operations under \auth\mfa\recovery\regenerate
class RegenerateRequestBuilder extends BaseRequestBuilder<RegenerateRequestBuilder> {
    /// Clones the requestbuilder.
    @override
    RegenerateRequestBuilder clone() {
        return RegenerateRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [RegenerateRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    RegenerateRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/auth/mfa/recovery/regenerate", pathParameters) ;
    /// Instantiates a new [RegenerateRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    RegenerateRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/auth/mfa/recovery/regenerate", {RequestInformation.rawUrlKey : rawUrl}) ;
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<RecoveryCodesResponse?> postAsync([void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toPostRequestInformation(requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '400' :  Problem.createFromDiscriminatorValue,
        };
        return await requestAdapter.send<RecoveryCodesResponse>(requestInfo, RecoveryCodesResponse.createFromDiscriminatorValue, errorMapping);
    }
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toPostRequestInformation([void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.post, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<DefaultQueryParameters>(requestConfiguration, () => DefaultQueryParameters());
        requestInfo.headers.put('Accept', 'application/json');
        return requestInfo;
    }
}
