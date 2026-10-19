// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './item/with_provider_item_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \auth\external
class ExternalRequestBuilder extends BaseRequestBuilder<ExternalRequestBuilder> {
    /// Gets an item from the yogabase_client.auth.external.item collection
    ///  [provider] Unique identifier of the item
    WithProviderItemRequestBuilder byProvider(String provider) {
        var urlTplParams = Map.of(pathParameters);
        urlTplParams.putIfAbsent('provider', () => provider);
        return WithProviderItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    ExternalRequestBuilder clone() {
        return ExternalRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [ExternalRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    ExternalRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/auth/external", pathParameters) ;
    /// Instantiates a new [ExternalRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    ExternalRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/auth/external", {RequestInformation.rawUrlKey : rawUrl}) ;
}
