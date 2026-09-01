package io.stackyoga.yogabase.auth.external.item;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.RequestAdapter;
import io.stackyoga.yogabase.auth.external.item.callback.CallbackRequestBuilder;
import java.util.HashMap;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /auth/external/{provider}
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class WithProviderItemRequestBuilder extends BaseRequestBuilder {
    /**
     * The callback property
     * @return a {@link CallbackRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public CallbackRequestBuilder callback() {
        return new CallbackRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * Instantiates a new {@link WithProviderItemRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public WithProviderItemRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/auth/external/{provider}", pathParameters);
    }
    /**
     * Instantiates a new {@link WithProviderItemRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public WithProviderItemRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/auth/external/{provider}", rawUrl);
    }
}
