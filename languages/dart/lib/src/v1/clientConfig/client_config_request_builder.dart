// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './item/with_publishable_key_item_request_builder.dart';

/// auto generated
/// Builds and executes requests for operations under \v1\client-config
class ClientConfigRequestBuilder extends BaseRequestBuilder<ClientConfigRequestBuilder> {
    /// Gets an item from the stackyoga_yogabase.v1.clientConfig.item collection
    ///  [publishableKey] Unique identifier of the item
    WithPublishableKeyItemRequestBuilder byPublishableKey(String publishableKey) {
        var urlTplParams = Map.of(pathParameters);
        urlTplParams.putIfAbsent('publishableKey', () => publishableKey);
        return WithPublishableKeyItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    ClientConfigRequestBuilder clone() {
        return ClientConfigRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [ClientConfigRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    ClientConfigRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/client-config", pathParameters) ;
    /// Instantiates a new [ClientConfigRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    ClientConfigRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/v1/client-config", {RequestInformation.rawUrlKey : rawUrl}) ;
}
