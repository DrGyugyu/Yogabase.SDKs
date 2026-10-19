package io.yogabase.client.v1.servers.item;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.BaseRequestConfiguration;
import com.microsoft.kiota.HttpMethod;
import com.microsoft.kiota.RequestAdapter;
import com.microsoft.kiota.RequestInformation;
import com.microsoft.kiota.RequestOption;
import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParsableFactory;
import io.yogabase.client.models.GameServer;
import io.yogabase.client.models.Problem;
import io.yogabase.client.v1.servers.item.stop.StopRequestBuilder;
import java.util.Collection;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /v1/servers/{serverId}
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class WithServerItemRequestBuilder extends BaseRequestBuilder {
    /**
     * The stop property
     * @return a {@link StopRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public StopRequestBuilder stop() {
        return new StopRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * Instantiates a new {@link WithServerItemRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public WithServerItemRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/servers/{serverId}", pathParameters);
    }
    /**
     * Instantiates a new {@link WithServerItemRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public WithServerItemRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/servers/{serverId}", rawUrl);
    }
    /**
     * One instance of the caller&apos;s environment. A server id from another project is answered exactly as one that does not exist.
     * @return a {@link GameServer}
     * @throws Problem When receiving a 401 status code
     * @throws Problem When receiving a 404 status code
     * @throws Problem When receiving a 503 status code
     */
    @jakarta.annotation.Nullable
    public GameServer get() {
        return get(null);
    }
    /**
     * One instance of the caller&apos;s environment. A server id from another project is answered exactly as one that does not exist.
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @return a {@link GameServer}
     * @throws Problem When receiving a 401 status code
     * @throws Problem When receiving a 404 status code
     * @throws Problem When receiving a 503 status code
     */
    @jakarta.annotation.Nullable
    public GameServer get(@jakarta.annotation.Nullable final java.util.function.Consumer<GetRequestConfiguration> requestConfiguration) {
        final RequestInformation requestInfo = toGetRequestInformation(requestConfiguration);
        final HashMap<String, ParsableFactory<? extends Parsable>> errorMapping = new HashMap<String, ParsableFactory<? extends Parsable>>();
        errorMapping.put("401", Problem::createFromDiscriminatorValue);
        errorMapping.put("404", Problem::createFromDiscriminatorValue);
        errorMapping.put("503", Problem::createFromDiscriminatorValue);
        return this.requestAdapter.send(requestInfo, errorMapping, GameServer::createFromDiscriminatorValue);
    }
    /**
     * One instance of the caller&apos;s environment. A server id from another project is answered exactly as one that does not exist.
     * @return a {@link RequestInformation}
     */
    @jakarta.annotation.Nonnull
    public RequestInformation toGetRequestInformation() {
        return toGetRequestInformation(null);
    }
    /**
     * One instance of the caller&apos;s environment. A server id from another project is answered exactly as one that does not exist.
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
     * @return a {@link WithServerItemRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public WithServerItemRequestBuilder withUrl(@jakarta.annotation.Nonnull final String rawUrl) {
        Objects.requireNonNull(rawUrl);
        return new WithServerItemRequestBuilder(rawUrl, requestAdapter);
    }
    /**
     * Configuration for the request such as headers, query parameters, and middleware options.
     */
    @jakarta.annotation.Generated("com.microsoft.kiota")
    public class GetRequestConfiguration extends BaseRequestConfiguration {
    }
}
