package io.stackyoga.yogabase.auth.external.item.returnescaped;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.BaseRequestConfiguration;
import com.microsoft.kiota.HttpMethod;
import com.microsoft.kiota.QueryParameters;
import com.microsoft.kiota.RequestAdapter;
import com.microsoft.kiota.RequestInformation;
import com.microsoft.kiota.RequestOption;
import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParsableFactory;
import io.stackyoga.yogabase.models.Problem;
import java.util.Collection;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /auth/external/{provider}/return
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class ReturnRequestBuilder extends BaseRequestBuilder {
    /**
     * Instantiates a new {@link ReturnRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public ReturnRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/auth/external/{provider}/return{?code*,state*}", pathParameters);
    }
    /**
     * Instantiates a new {@link ReturnRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public ReturnRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/auth/external/{provider}/return{?code*,state*}", rawUrl);
    }
    /**
     * The one address registered with a provider (https://&lt;gateway&gt;/auth/external/&lt;id&gt;/return); no application calls it. A provider that answers by query lands here. It redirects to the redirect URI given to start, carrying the code and state, or error=access_denied and the state when the provider sent no code. Nothing else of the answer is read.
     * @throws Problem When receiving a 401 status code
     * @throws Problem When receiving a 404 status code
     * @throws Problem When receiving a 503 status code
     */
    public void get() {
        get(null);
    }
    /**
     * The one address registered with a provider (https://&lt;gateway&gt;/auth/external/&lt;id&gt;/return); no application calls it. A provider that answers by query lands here. It redirects to the redirect URI given to start, carrying the code and state, or error=access_denied and the state when the provider sent no code. Nothing else of the answer is read.
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @throws Problem When receiving a 401 status code
     * @throws Problem When receiving a 404 status code
     * @throws Problem When receiving a 503 status code
     */
    public void get(@jakarta.annotation.Nullable final java.util.function.Consumer<GetRequestConfiguration> requestConfiguration) {
        final RequestInformation requestInfo = toGetRequestInformation(requestConfiguration);
        final HashMap<String, ParsableFactory<? extends Parsable>> errorMapping = new HashMap<String, ParsableFactory<? extends Parsable>>();
        errorMapping.put("401", Problem::createFromDiscriminatorValue);
        errorMapping.put("404", Problem::createFromDiscriminatorValue);
        errorMapping.put("503", Problem::createFromDiscriminatorValue);
        this.requestAdapter.sendPrimitive(requestInfo, errorMapping, Void.class);
    }
    /**
     * The same address for a provider that answers by form_post (Apple). Same redirect, same fields.
     * @param body The request body
     * @throws Problem When receiving a 401 status code
     * @throws Problem When receiving a 404 status code
     * @throws Problem When receiving a 503 status code
     */
    public void post(@jakarta.annotation.Nonnull final ReturnPostRequestBody body) {
        post(body, null);
    }
    /**
     * The same address for a provider that answers by form_post (Apple). Same redirect, same fields.
     * @param body The request body
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @throws Problem When receiving a 401 status code
     * @throws Problem When receiving a 404 status code
     * @throws Problem When receiving a 503 status code
     */
    public void post(@jakarta.annotation.Nonnull final ReturnPostRequestBody body, @jakarta.annotation.Nullable final java.util.function.Consumer<PostRequestConfiguration> requestConfiguration) {
        Objects.requireNonNull(body);
        final RequestInformation requestInfo = toPostRequestInformation(body, requestConfiguration);
        final HashMap<String, ParsableFactory<? extends Parsable>> errorMapping = new HashMap<String, ParsableFactory<? extends Parsable>>();
        errorMapping.put("401", Problem::createFromDiscriminatorValue);
        errorMapping.put("404", Problem::createFromDiscriminatorValue);
        errorMapping.put("503", Problem::createFromDiscriminatorValue);
        this.requestAdapter.sendPrimitive(requestInfo, errorMapping, Void.class);
    }
    /**
     * The one address registered with a provider (https://&lt;gateway&gt;/auth/external/&lt;id&gt;/return); no application calls it. A provider that answers by query lands here. It redirects to the redirect URI given to start, carrying the code and state, or error=access_denied and the state when the provider sent no code. Nothing else of the answer is read.
     * @return a {@link RequestInformation}
     */
    @jakarta.annotation.Nonnull
    public RequestInformation toGetRequestInformation() {
        return toGetRequestInformation(null);
    }
    /**
     * The one address registered with a provider (https://&lt;gateway&gt;/auth/external/&lt;id&gt;/return); no application calls it. A provider that answers by query lands here. It redirects to the redirect URI given to start, carrying the code and state, or error=access_denied and the state when the provider sent no code. Nothing else of the answer is read.
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @return a {@link RequestInformation}
     */
    @jakarta.annotation.Nonnull
    public RequestInformation toGetRequestInformation(@jakarta.annotation.Nullable final java.util.function.Consumer<GetRequestConfiguration> requestConfiguration) {
        final RequestInformation requestInfo = new RequestInformation(HttpMethod.GET, urlTemplate, pathParameters);
        requestInfo.configure(requestConfiguration, GetRequestConfiguration::new, x -> x.queryParameters);
        requestInfo.headers.tryAdd("Accept", "application/problem+json");
        return requestInfo;
    }
    /**
     * The same address for a provider that answers by form_post (Apple). Same redirect, same fields.
     * @param body The request body
     * @return a {@link RequestInformation}
     */
    @jakarta.annotation.Nonnull
    public RequestInformation toPostRequestInformation(@jakarta.annotation.Nonnull final ReturnPostRequestBody body) {
        return toPostRequestInformation(body, null);
    }
    /**
     * The same address for a provider that answers by form_post (Apple). Same redirect, same fields.
     * @param body The request body
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @return a {@link RequestInformation}
     */
    @jakarta.annotation.Nonnull
    public RequestInformation toPostRequestInformation(@jakarta.annotation.Nonnull final ReturnPostRequestBody body, @jakarta.annotation.Nullable final java.util.function.Consumer<PostRequestConfiguration> requestConfiguration) {
        Objects.requireNonNull(body);
        final RequestInformation requestInfo = new RequestInformation(HttpMethod.POST, urlTemplate, pathParameters);
        requestInfo.configure(requestConfiguration, PostRequestConfiguration::new);
        requestInfo.headers.tryAdd("Accept", "application/problem+json");
        requestInfo.setContentFromParsable(requestAdapter, "application/x-www-form-urlencoded", body);
        return requestInfo;
    }
    /**
     * Returns a request builder with the provided arbitrary URL. Using this method means any other path or query parameters are ignored.
     * @param rawUrl The raw URL to use for the request builder.
     * @return a {@link ReturnRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public ReturnRequestBuilder withUrl(@jakarta.annotation.Nonnull final String rawUrl) {
        Objects.requireNonNull(rawUrl);
        return new ReturnRequestBuilder(rawUrl, requestAdapter);
    }
    /**
     * The one address registered with a provider (https://&lt;gateway&gt;/auth/external/&lt;id&gt;/return); no application calls it. A provider that answers by query lands here. It redirects to the redirect URI given to start, carrying the code and state, or error=access_denied and the state when the provider sent no code. Nothing else of the answer is read.
     */
    @jakarta.annotation.Generated("com.microsoft.kiota")
    public class GetQueryParameters implements QueryParameters {
        @jakarta.annotation.Nullable
        public String code;
        @jakarta.annotation.Nullable
        public String state;
        /**
         * Extracts the query parameters into a map for the URI template parsing.
         * @return a {@link Map<String, Object>}
         */
        @jakarta.annotation.Nonnull
        public Map<String, Object> toQueryParameters() {
            final Map<String, Object> allQueryParams = new HashMap();
            allQueryParams.put("code", code);
            allQueryParams.put("state", state);
            return allQueryParams;
        }
    }
    /**
     * Configuration for the request such as headers, query parameters, and middleware options.
     */
    @jakarta.annotation.Generated("com.microsoft.kiota")
    public class GetRequestConfiguration extends BaseRequestConfiguration {
        /**
         * Request query parameters
         */
        @jakarta.annotation.Nullable
        public GetQueryParameters queryParameters = new GetQueryParameters();
    }
    /**
     * Configuration for the request such as headers, query parameters, and middleware options.
     */
    @jakarta.annotation.Generated("com.microsoft.kiota")
    public class PostRequestConfiguration extends BaseRequestConfiguration {
    }
}
