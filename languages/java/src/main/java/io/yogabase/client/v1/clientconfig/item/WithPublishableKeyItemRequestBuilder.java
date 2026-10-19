package io.yogabase.client.v1.clientconfig.item;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.BaseRequestConfiguration;
import com.microsoft.kiota.HttpMethod;
import com.microsoft.kiota.RequestAdapter;
import com.microsoft.kiota.RequestInformation;
import com.microsoft.kiota.RequestOption;
import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParsableFactory;
import io.yogabase.client.models.ClientConfiguration;
import io.yogabase.client.models.Problem;
import java.util.Collection;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /v1/client-config/{publishableKey}
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class WithPublishableKeyItemRequestBuilder extends BaseRequestBuilder {
    /**
     * Instantiates a new {@link WithPublishableKeyItemRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public WithPublishableKeyItemRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/client-config/{publishableKey}", pathParameters);
    }
    /**
     * Instantiates a new {@link WithPublishableKeyItemRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public WithPublishableKeyItemRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/client-config/{publishableKey}", rawUrl);
    }
    /**
     * Resolves secret-free configuration. A publishable key identifies configuration and never authenticates a request. The capabilities array always carries the four core rows; a requested-only row is added only for a client that names it in X-Yogabase-Accept-Capabilities.
     * @return a {@link ClientConfiguration}
     * @throws Problem When receiving a 404 status code
     * @throws Problem When receiving a 429 status code
     */
    @jakarta.annotation.Nullable
    public ClientConfiguration get() {
        return get(null);
    }
    /**
     * Resolves secret-free configuration. A publishable key identifies configuration and never authenticates a request. The capabilities array always carries the four core rows; a requested-only row is added only for a client that names it in X-Yogabase-Accept-Capabilities.
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @return a {@link ClientConfiguration}
     * @throws Problem When receiving a 404 status code
     * @throws Problem When receiving a 429 status code
     */
    @jakarta.annotation.Nullable
    public ClientConfiguration get(@jakarta.annotation.Nullable final java.util.function.Consumer<GetRequestConfiguration> requestConfiguration) {
        final RequestInformation requestInfo = toGetRequestInformation(requestConfiguration);
        final HashMap<String, ParsableFactory<? extends Parsable>> errorMapping = new HashMap<String, ParsableFactory<? extends Parsable>>();
        errorMapping.put("404", Problem::createFromDiscriminatorValue);
        errorMapping.put("429", Problem::createFromDiscriminatorValue);
        return this.requestAdapter.send(requestInfo, errorMapping, ClientConfiguration::createFromDiscriminatorValue);
    }
    /**
     * Resolves secret-free configuration. A publishable key identifies configuration and never authenticates a request. The capabilities array always carries the four core rows; a requested-only row is added only for a client that names it in X-Yogabase-Accept-Capabilities.
     * @return a {@link RequestInformation}
     */
    @jakarta.annotation.Nonnull
    public RequestInformation toGetRequestInformation() {
        return toGetRequestInformation(null);
    }
    /**
     * Resolves secret-free configuration. A publishable key identifies configuration and never authenticates a request. The capabilities array always carries the four core rows; a requested-only row is added only for a client that names it in X-Yogabase-Accept-Capabilities.
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @return a {@link RequestInformation}
     */
    @jakarta.annotation.Nonnull
    public RequestInformation toGetRequestInformation(@jakarta.annotation.Nullable final java.util.function.Consumer<GetRequestConfiguration> requestConfiguration) {
        final RequestInformation requestInfo = new RequestInformation(HttpMethod.GET, urlTemplate, pathParameters);
        requestInfo.configure(requestConfiguration, GetRequestConfiguration::new);
        requestInfo.headers.tryAdd("Accept", "application/json");
        return requestInfo;
    }
    /**
     * Returns a request builder with the provided arbitrary URL. Using this method means any other path or query parameters are ignored.
     * @param rawUrl The raw URL to use for the request builder.
     * @return a {@link WithPublishableKeyItemRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public WithPublishableKeyItemRequestBuilder withUrl(@jakarta.annotation.Nonnull final String rawUrl) {
        Objects.requireNonNull(rawUrl);
        return new WithPublishableKeyItemRequestBuilder(rawUrl, requestAdapter);
    }
    /**
     * Configuration for the request such as headers, query parameters, and middleware options.
     */
    @jakarta.annotation.Generated("com.microsoft.kiota")
    public class GetRequestConfiguration extends BaseRequestConfiguration {
    }
}
