package io.stackyoga.yogabase.auth.mfa.recovery;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.RequestAdapter;
import io.stackyoga.yogabase.auth.mfa.recovery.regenerate.RegenerateRequestBuilder;
import io.stackyoga.yogabase.auth.mfa.recovery.use.UseRequestBuilder;
import java.util.HashMap;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /auth/mfa/recovery
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class RecoveryRequestBuilder extends BaseRequestBuilder {
    /**
     * The regenerate property
     * @return a {@link RegenerateRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public RegenerateRequestBuilder regenerate() {
        return new RegenerateRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * The use property
     * @return a {@link UseRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public UseRequestBuilder use() {
        return new UseRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * Instantiates a new {@link RecoveryRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public RecoveryRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/auth/mfa/recovery", pathParameters);
    }
    /**
     * Instantiates a new {@link RecoveryRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public RecoveryRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/auth/mfa/recovery", rawUrl);
    }
}
