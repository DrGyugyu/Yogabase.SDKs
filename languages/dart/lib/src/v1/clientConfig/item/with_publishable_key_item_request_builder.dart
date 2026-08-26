// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import '../../../models/client_configuration.dart';
import '../../../models/problem.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\client-config\{publishableKey}
class WithPublishableKeyItemRequestBuilder extends BaseRequestBuilder<WithPublishableKeyItemRequestBuilder> {
    /// Clones the requestbuilder.
    @override
    WithPublishableKeyItemRequestBuilder clone() {
        return WithPublishableKeyItemRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [WithPublishableKeyItemRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithPublishableKeyItemRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/client-config/{publishableKey}", pathParameters) ;
    /// Instantiates a new [WithPublishableKeyItemRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    WithPublishableKeyItemRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/client-config/{publishableKey}", {RequestInformation.rawUrlKey : rawUrl}) ;
    /// Resolves secret-free configuration. A publishable key identifies configuration and never authenticates a request.
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<ClientConfiguration?> getAsync([void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toGetRequestInformation(requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '404' :  Problem.createFromDiscriminatorValue,
            '429' :  Problem.createFromDiscriminatorValue,
        };
        return await requestAdapter.send<ClientConfiguration>(requestInfo, ClientConfiguration.createFromDiscriminatorValue, errorMapping);
    }
    /// Resolves secret-free configuration. A publishable key identifies configuration and never authenticates a request.
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toGetRequestInformation([void Function(RequestConfiguration<DefaultQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.get, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<DefaultQueryParameters>(requestConfiguration, () => DefaultQueryParameters());
        requestInfo.headers.put('Accept', 'application/json');
        return requestInfo;
    }
}
