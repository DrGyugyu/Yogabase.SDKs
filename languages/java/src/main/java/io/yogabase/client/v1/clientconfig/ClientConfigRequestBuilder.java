package io.yogabase.client.v1.clientconfig;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.RequestAdapter;
import io.yogabase.client.v1.clientconfig.item.WithPublishableKeyItemRequestBuilder;
import java.util.HashMap;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /v1/client-config
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class ClientConfigRequestBuilder extends BaseRequestBuilder {
    /**
     * Gets an item from the io.yogabase.client.v1.clientConfig.item collection
     * @param publishableKey Unique identifier of the item
     * @return a {@link WithPublishableKeyItemRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public WithPublishableKeyItemRequestBuilder byPublishableKey(@jakarta.annotation.Nonnull final String publishableKey) {
        Objects.requireNonNull(publishableKey);
        final HashMap<String, Object> urlTplParams = new HashMap<String, Object>(this.pathParameters);
        urlTplParams.put("publishableKey", publishableKey);
        return new WithPublishableKeyItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /**
     * Instantiates a new {@link ClientConfigRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public ClientConfigRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/client-config", pathParameters);
    }
    /**
     * Instantiates a new {@link ClientConfigRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public ClientConfigRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/client-config", rawUrl);
    }
}
