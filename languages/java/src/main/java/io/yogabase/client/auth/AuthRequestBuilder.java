package io.yogabase.client.auth;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.RequestAdapter;
import io.yogabase.client.auth.email.EmailRequestBuilder;
import io.yogabase.client.auth.external.ExternalRequestBuilder;
import io.yogabase.client.auth.login.LoginRequestBuilder;
import io.yogabase.client.auth.logout.LogoutRequestBuilder;
import io.yogabase.client.auth.mfa.MfaRequestBuilder;
import io.yogabase.client.auth.password.PasswordRequestBuilder;
import io.yogabase.client.auth.signup.SignupRequestBuilder;
import java.util.HashMap;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /auth
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class AuthRequestBuilder extends BaseRequestBuilder {
    /**
     * The email property
     * @return a {@link EmailRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public EmailRequestBuilder email() {
        return new EmailRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * The external property
     * @return a {@link ExternalRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public ExternalRequestBuilder external() {
        return new ExternalRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * The login property
     * @return a {@link LoginRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public LoginRequestBuilder login() {
        return new LoginRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * The logout property
     * @return a {@link LogoutRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public LogoutRequestBuilder logout() {
        return new LogoutRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * The mfa property
     * @return a {@link MfaRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public MfaRequestBuilder mfa() {
        return new MfaRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * The password property
     * @return a {@link PasswordRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public PasswordRequestBuilder password() {
        return new PasswordRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * The signup property
     * @return a {@link SignupRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public SignupRequestBuilder signup() {
        return new SignupRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * Instantiates a new {@link AuthRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public AuthRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/auth", pathParameters);
    }
    /**
     * Instantiates a new {@link AuthRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public AuthRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/auth", rawUrl);
    }
}
