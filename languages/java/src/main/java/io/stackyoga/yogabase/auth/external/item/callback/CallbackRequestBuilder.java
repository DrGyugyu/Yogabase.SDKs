package io.stackyoga.yogabase.auth.external.item.callback;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.BaseRequestConfiguration;
import com.microsoft.kiota.HttpMethod;
import com.microsoft.kiota.RequestAdapter;
import com.microsoft.kiota.RequestInformation;
import com.microsoft.kiota.RequestOption;
import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParsableFactory;
import io.stackyoga.yogabase.models.ExternalIdentityCallbackRequest;
import io.stackyoga.yogabase.models.Problem;
import java.util.Collection;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /auth/external/{provider}/callback
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class CallbackRequestBuilder extends BaseRequestBuilder {
    /**
     * Instantiates a new {@link CallbackRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public CallbackRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/auth/external/{provider}/callback", pathParameters);
    }
    /**
     * Instantiates a new {@link CallbackRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public CallbackRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/auth/external/{provider}/callback", rawUrl);
    }
    /**
     * Completes a configured external identity flow and signs the user in. codeVerifier is the verifier whose challenge was sent to start: without it the code and state are refused as external_identity_invalid, so a redirect taken by another app signs nobody in. A provider-verified e-mail address no account uses starts an account (open-signup projects only); an address an account already uses is refused with 409 external_identity_email_in_use and nothing is linked.
     * @param body The request body
     * @return a {@link Problem}
     * @throws Problem When receiving a 400 status code
     * @throws Problem When receiving a 401 status code
     * @throws Problem When receiving a 403 status code
     * @throws Problem When receiving a 404 status code
     * @throws Problem When receiving a 409 status code
     * @throws Problem When receiving a 503 status code
     */
    @jakarta.annotation.Nullable
    public Problem post(@jakarta.annotation.Nonnull final ExternalIdentityCallbackRequest body) {
        return post(body, null);
    }
    /**
     * Completes a configured external identity flow and signs the user in. codeVerifier is the verifier whose challenge was sent to start: without it the code and state are refused as external_identity_invalid, so a redirect taken by another app signs nobody in. A provider-verified e-mail address no account uses starts an account (open-signup projects only); an address an account already uses is refused with 409 external_identity_email_in_use and nothing is linked.
     * @param body The request body
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @return a {@link Problem}
     * @throws Problem When receiving a 400 status code
     * @throws Problem When receiving a 401 status code
     * @throws Problem When receiving a 403 status code
     * @throws Problem When receiving a 404 status code
     * @throws Problem When receiving a 409 status code
     * @throws Problem When receiving a 503 status code
     */
    @jakarta.annotation.Nullable
    public Problem post(@jakarta.annotation.Nonnull final ExternalIdentityCallbackRequest body, @jakarta.annotation.Nullable final java.util.function.Consumer<PostRequestConfiguration> requestConfiguration) {
        Objects.requireNonNull(body);
        final RequestInformation requestInfo = toPostRequestInformation(body, requestConfiguration);
        final HashMap<String, ParsableFactory<? extends Parsable>> errorMapping = new HashMap<String, ParsableFactory<? extends Parsable>>();
        errorMapping.put("400", Problem::createFromDiscriminatorValue);
        errorMapping.put("401", Problem::createFromDiscriminatorValue);
        errorMapping.put("403", Problem::createFromDiscriminatorValue);
        errorMapping.put("404", Problem::createFromDiscriminatorValue);
        errorMapping.put("409", Problem::createFromDiscriminatorValue);
        errorMapping.put("503", Problem::createFromDiscriminatorValue);
        return this.requestAdapter.send(requestInfo, errorMapping, Problem::createFromDiscriminatorValue);
    }
    /**
     * Completes a configured external identity flow and signs the user in. codeVerifier is the verifier whose challenge was sent to start: without it the code and state are refused as external_identity_invalid, so a redirect taken by another app signs nobody in. A provider-verified e-mail address no account uses starts an account (open-signup projects only); an address an account already uses is refused with 409 external_identity_email_in_use and nothing is linked.
     * @param body The request body
     * @return a {@link RequestInformation}
     */
    @jakarta.annotation.Nonnull
    public RequestInformation toPostRequestInformation(@jakarta.annotation.Nonnull final ExternalIdentityCallbackRequest body) {
        return toPostRequestInformation(body, null);
    }
    /**
     * Completes a configured external identity flow and signs the user in. codeVerifier is the verifier whose challenge was sent to start: without it the code and state are refused as external_identity_invalid, so a redirect taken by another app signs nobody in. A provider-verified e-mail address no account uses starts an account (open-signup projects only); an address an account already uses is refused with 409 external_identity_email_in_use and nothing is linked.
     * @param body The request body
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @return a {@link RequestInformation}
     */
    @jakarta.annotation.Nonnull
    public RequestInformation toPostRequestInformation(@jakarta.annotation.Nonnull final ExternalIdentityCallbackRequest body, @jakarta.annotation.Nullable final java.util.function.Consumer<PostRequestConfiguration> requestConfiguration) {
        Objects.requireNonNull(body);
        final RequestInformation requestInfo = new RequestInformation(HttpMethod.POST, urlTemplate, pathParameters);
        requestInfo.configure(requestConfiguration, PostRequestConfiguration::new);
        requestInfo.headers.tryAdd("Accept", "application/problem+json");
        requestInfo.setContentFromParsable(requestAdapter, "application/json", body);
        return requestInfo;
    }
    /**
     * Returns a request builder with the provided arbitrary URL. Using this method means any other path or query parameters are ignored.
     * @param rawUrl The raw URL to use for the request builder.
     * @return a {@link CallbackRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public CallbackRequestBuilder withUrl(@jakarta.annotation.Nonnull final String rawUrl) {
        Objects.requireNonNull(rawUrl);
        return new CallbackRequestBuilder(rawUrl, requestAdapter);
    }
    /**
     * Configuration for the request such as headers, query parameters, and middleware options.
     */
    @jakarta.annotation.Generated("com.microsoft.kiota")
    public class PostRequestConfiguration extends BaseRequestConfiguration {
    }
}
