package io.stackyoga.yogabase.auth.email;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.RequestAdapter;
import io.stackyoga.yogabase.auth.email.confirm.ConfirmRequestBuilder;
import java.util.HashMap;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /auth/email
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class EmailRequestBuilder extends BaseRequestBuilder {
    /**
     * The confirm property
     * @return a {@link ConfirmRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public ConfirmRequestBuilder confirm() {
        return new ConfirmRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * Instantiates a new {@link EmailRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public EmailRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/auth/email", pathParameters);
    }
    /**
     * Instantiates a new {@link EmailRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public EmailRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/auth/email", rawUrl);
    }
}
