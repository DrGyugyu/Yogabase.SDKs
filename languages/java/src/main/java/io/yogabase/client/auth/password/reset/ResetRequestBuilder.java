package io.yogabase.client.auth.password.reset;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.RequestAdapter;
import io.yogabase.client.auth.password.reset.complete.CompleteRequestBuilder;
import io.yogabase.client.auth.password.reset.request.RequestRequestBuilder;
import java.util.HashMap;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /auth/password/reset
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class ResetRequestBuilder extends BaseRequestBuilder {
    /**
     * The complete property
     * @return a {@link CompleteRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public CompleteRequestBuilder complete() {
        return new CompleteRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * The request property
     * @return a {@link RequestRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public RequestRequestBuilder request() {
        return new RequestRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * Instantiates a new {@link ResetRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public ResetRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/auth/password/reset", pathParameters);
    }
    /**
     * Instantiates a new {@link ResetRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public ResetRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/auth/password/reset", rawUrl);
    }
}
