package io.stackyoga.yogabase.auth.external;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.RequestAdapter;
import io.stackyoga.yogabase.auth.external.item.WithProviderItemRequestBuilder;
import java.util.HashMap;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /auth/external
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class ExternalRequestBuilder extends BaseRequestBuilder {
    /**
     * Gets an item from the io.stackyoga.yogabase.auth.external.item collection
     * @param provider Unique identifier of the item
     * @return a {@link WithProviderItemRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public WithProviderItemRequestBuilder byProvider(@jakarta.annotation.Nonnull final String provider) {
        Objects.requireNonNull(provider);
        final HashMap<String, Object> urlTplParams = new HashMap<String, Object>(this.pathParameters);
        urlTplParams.put("provider", provider);
        return new WithProviderItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /**
     * Instantiates a new {@link ExternalRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public ExternalRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/auth/external", pathParameters);
    }
    /**
     * Instantiates a new {@link ExternalRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public ExternalRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/auth/external", rawUrl);
    }
}
