package io.stackyoga.yogabase.auth.mfa;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.RequestAdapter;
import io.stackyoga.yogabase.auth.mfa.confirm.ConfirmRequestBuilder;
import io.stackyoga.yogabase.auth.mfa.recovery.RecoveryRequestBuilder;
import io.stackyoga.yogabase.auth.mfa.setup.SetupRequestBuilder;
import io.stackyoga.yogabase.auth.mfa.verify.VerifyRequestBuilder;
import java.util.HashMap;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /auth/mfa
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class MfaRequestBuilder extends BaseRequestBuilder {
    /**
     * The confirm property
     * @return a {@link ConfirmRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public ConfirmRequestBuilder confirm() {
        return new ConfirmRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * The recovery property
     * @return a {@link RecoveryRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public RecoveryRequestBuilder recovery() {
        return new RecoveryRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * The setup property
     * @return a {@link SetupRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public SetupRequestBuilder setup() {
        return new SetupRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * The verify property
     * @return a {@link VerifyRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public VerifyRequestBuilder verify() {
        return new VerifyRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * Instantiates a new {@link MfaRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public MfaRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/auth/mfa", pathParameters);
    }
    /**
     * Instantiates a new {@link MfaRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public MfaRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/auth/mfa", rawUrl);
    }
}
