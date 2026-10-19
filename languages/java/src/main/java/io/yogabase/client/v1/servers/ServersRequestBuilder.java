package io.yogabase.client.v1.servers;

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
import io.yogabase.client.v1.servers.item.WithServerItemRequestBuilder;
import io.yogabase.client.v1.servers.start.StartRequestBuilder;
import java.util.Collection;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /v1/servers
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class ServersRequestBuilder extends BaseRequestBuilder {
    /**
     * The start property
     * @return a {@link StartRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public StartRequestBuilder start() {
        return new StartRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * Gets an item from the io.yogabase.client.v1.servers.item collection
     * @param serverId Opaque game-server instance identifier, as returned by start or list.
     * @return a {@link WithServerItemRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public WithServerItemRequestBuilder byServerId(@jakarta.annotation.Nonnull final String serverId) {
        Objects.requireNonNull(serverId);
        final HashMap<String, Object> urlTplParams = new HashMap<String, Object>(this.pathParameters);
        urlTplParams.put("serverId", serverId);
        return new WithServerItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /**
     * Instantiates a new {@link ServersRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public ServersRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/servers", pathParameters);
    }
    /**
     * Instantiates a new {@link ServersRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public ServersRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/servers", rawUrl);
    }
    /**
     * Every instance of the caller&apos;s environment as a bare array, ordered by server id. An instance whose address no member has observed is listed with an empty networkPorts rather than omitted or given a composed host.
     * @return a {@link java.util.List<GameServer>}
     * @throws Problem When receiving a 401 status code
     * @throws Problem When receiving a 503 status code
     */
    @jakarta.annotation.Nullable
    public java.util.List<GameServer> get() {
        return get(null);
    }
    /**
     * Every instance of the caller&apos;s environment as a bare array, ordered by server id. An instance whose address no member has observed is listed with an empty networkPorts rather than omitted or given a composed host.
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @return a {@link java.util.List<GameServer>}
     * @throws Problem When receiving a 401 status code
     * @throws Problem When receiving a 503 status code
     */
    @jakarta.annotation.Nullable
    public java.util.List<GameServer> get(@jakarta.annotation.Nullable final java.util.function.Consumer<GetRequestConfiguration> requestConfiguration) {
        final RequestInformation requestInfo = toGetRequestInformation(requestConfiguration);
        final HashMap<String, ParsableFactory<? extends Parsable>> errorMapping = new HashMap<String, ParsableFactory<? extends Parsable>>();
        errorMapping.put("401", Problem::createFromDiscriminatorValue);
        errorMapping.put("503", Problem::createFromDiscriminatorValue);
        return this.requestAdapter.sendCollection(requestInfo, errorMapping, GameServer::createFromDiscriminatorValue);
    }
    /**
     * Every instance of the caller&apos;s environment as a bare array, ordered by server id. An instance whose address no member has observed is listed with an empty networkPorts rather than omitted or given a composed host.
     * @return a {@link RequestInformation}
     */
    @jakarta.annotation.Nonnull
    public RequestInformation toGetRequestInformation() {
        return toGetRequestInformation(null);
    }
    /**
     * Every instance of the caller&apos;s environment as a bare array, ordered by server id. An instance whose address no member has observed is listed with an empty networkPorts rather than omitted or given a composed host.
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
     * @return a {@link ServersRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public ServersRequestBuilder withUrl(@jakarta.annotation.Nonnull final String rawUrl) {
        Objects.requireNonNull(rawUrl);
        return new ServersRequestBuilder(rawUrl, requestAdapter);
    }
    /**
     * Configuration for the request such as headers, query parameters, and middleware options.
     */
    @jakarta.annotation.Generated("com.microsoft.kiota")
    public class GetRequestConfiguration extends BaseRequestConfiguration {
    }
}
