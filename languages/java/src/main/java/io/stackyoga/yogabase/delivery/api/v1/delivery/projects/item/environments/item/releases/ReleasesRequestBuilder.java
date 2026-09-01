package io.stackyoga.yogabase.delivery.api.v1.delivery.projects.item.environments.item.releases;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.BaseRequestConfiguration;
import com.microsoft.kiota.HttpMethod;
import com.microsoft.kiota.QueryParameters;
import com.microsoft.kiota.RequestAdapter;
import com.microsoft.kiota.RequestInformation;
import com.microsoft.kiota.RequestOption;
import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParsableFactory;
import io.stackyoga.yogabase.delivery.api.v1.delivery.projects.item.environments.item.releases.item.WithReleaseItemRequestBuilder;
import io.stackyoga.yogabase.delivery.api.v1.delivery.projects.item.environments.item.releases.previews.PreviewsRequestBuilder;
import io.stackyoga.yogabase.delivery.models.DeliveryOperationEnvelope;
import io.stackyoga.yogabase.delivery.models.DeliveryReleaseList;
import io.stackyoga.yogabase.delivery.models.DeliverySubmitRequest;
import io.stackyoga.yogabase.delivery.models.ProblemDetails;
import java.util.Collection;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
import java.util.UUID;
/**
 * Builds and executes requests for operations under /api/v1/delivery/projects/{projectId}/environments/{environmentId}/releases
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class ReleasesRequestBuilder extends BaseRequestBuilder {
    /**
     * The previews property
     * @return a {@link PreviewsRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public PreviewsRequestBuilder previews() {
        return new PreviewsRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * Gets an item from the io.stackyoga.yogabase.delivery.api.v1.delivery.projects.item.environments.item.releases.item collection
     * @param releaseId Unique identifier of the item
     * @return a {@link WithReleaseItemRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public WithReleaseItemRequestBuilder byReleaseId(@jakarta.annotation.Nonnull final UUID releaseId) {
        Objects.requireNonNull(releaseId);
        final HashMap<String, Object> urlTplParams = new HashMap<String, Object>(this.pathParameters);
        urlTplParams.put("releaseId", releaseId);
        return new WithReleaseItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /**
     * Instantiates a new {@link ReleasesRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public ReleasesRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/api/v1/delivery/projects/{projectId}/environments/{environmentId}/releases{?cursor*,limit*}", pathParameters);
    }
    /**
     * Instantiates a new {@link ReleasesRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public ReleasesRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/api/v1/delivery/projects/{projectId}/environments/{environmentId}/releases{?cursor*,limit*}", rawUrl);
    }
    /**
     * @return a {@link DeliveryReleaseList}
     * @throws ProblemDetails When receiving a 401 status code
     * @throws ProblemDetails When receiving a 404 status code
     * @throws ProblemDetails When receiving a 429 status code
     */
    @jakarta.annotation.Nullable
    public DeliveryReleaseList get() {
        return get(null);
    }
    /**
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @return a {@link DeliveryReleaseList}
     * @throws ProblemDetails When receiving a 401 status code
     * @throws ProblemDetails When receiving a 404 status code
     * @throws ProblemDetails When receiving a 429 status code
     */
    @jakarta.annotation.Nullable
    public DeliveryReleaseList get(@jakarta.annotation.Nullable final java.util.function.Consumer<GetRequestConfiguration> requestConfiguration) {
        final RequestInformation requestInfo = toGetRequestInformation(requestConfiguration);
        final HashMap<String, ParsableFactory<? extends Parsable>> errorMapping = new HashMap<String, ParsableFactory<? extends Parsable>>();
        errorMapping.put("401", ProblemDetails::createFromDiscriminatorValue);
        errorMapping.put("404", ProblemDetails::createFromDiscriminatorValue);
        errorMapping.put("429", ProblemDetails::createFromDiscriminatorValue);
        return this.requestAdapter.send(requestInfo, errorMapping, DeliveryReleaseList::createFromDiscriminatorValue);
    }
    /**
     * @param body The request body
     * @return a {@link DeliveryOperationEnvelope}
     * @throws ProblemDetails When receiving a 400 status code
     * @throws ProblemDetails When receiving a 401 status code
     * @throws ProblemDetails When receiving a 409 status code
     * @throws ProblemDetails When receiving a 422 status code
     * @throws ProblemDetails When receiving a 429 status code
     */
    @jakarta.annotation.Nullable
    public DeliveryOperationEnvelope post(@jakarta.annotation.Nonnull final DeliverySubmitRequest body) {
        return post(body, null);
    }
    /**
     * @param body The request body
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @return a {@link DeliveryOperationEnvelope}
     * @throws ProblemDetails When receiving a 400 status code
     * @throws ProblemDetails When receiving a 401 status code
     * @throws ProblemDetails When receiving a 409 status code
     * @throws ProblemDetails When receiving a 422 status code
     * @throws ProblemDetails When receiving a 429 status code
     */
    @jakarta.annotation.Nullable
    public DeliveryOperationEnvelope post(@jakarta.annotation.Nonnull final DeliverySubmitRequest body, @jakarta.annotation.Nullable final java.util.function.Consumer<PostRequestConfiguration> requestConfiguration) {
        Objects.requireNonNull(body);
        final RequestInformation requestInfo = toPostRequestInformation(body, requestConfiguration);
        final HashMap<String, ParsableFactory<? extends Parsable>> errorMapping = new HashMap<String, ParsableFactory<? extends Parsable>>();
        errorMapping.put("400", ProblemDetails::createFromDiscriminatorValue);
        errorMapping.put("401", ProblemDetails::createFromDiscriminatorValue);
        errorMapping.put("409", ProblemDetails::createFromDiscriminatorValue);
        errorMapping.put("422", ProblemDetails::createFromDiscriminatorValue);
        errorMapping.put("429", ProblemDetails::createFromDiscriminatorValue);
        return this.requestAdapter.send(requestInfo, errorMapping, DeliveryOperationEnvelope::createFromDiscriminatorValue);
    }
    /**
     * @return a {@link RequestInformation}
     */
    @jakarta.annotation.Nonnull
    public RequestInformation toGetRequestInformation() {
        return toGetRequestInformation(null);
    }
    /**
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @return a {@link RequestInformation}
     */
    @jakarta.annotation.Nonnull
    public RequestInformation toGetRequestInformation(@jakarta.annotation.Nullable final java.util.function.Consumer<GetRequestConfiguration> requestConfiguration) {
        final RequestInformation requestInfo = new RequestInformation(HttpMethod.GET, urlTemplate, pathParameters);
        requestInfo.configure(requestConfiguration, GetRequestConfiguration::new, x -> x.queryParameters);
        requestInfo.headers.tryAdd("Accept", "application/json");
        return requestInfo;
    }
    /**
     * @param body The request body
     * @return a {@link RequestInformation}
     */
    @jakarta.annotation.Nonnull
    public RequestInformation toPostRequestInformation(@jakarta.annotation.Nonnull final DeliverySubmitRequest body) {
        return toPostRequestInformation(body, null);
    }
    /**
     * @param body The request body
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @return a {@link RequestInformation}
     */
    @jakarta.annotation.Nonnull
    public RequestInformation toPostRequestInformation(@jakarta.annotation.Nonnull final DeliverySubmitRequest body, @jakarta.annotation.Nullable final java.util.function.Consumer<PostRequestConfiguration> requestConfiguration) {
        Objects.requireNonNull(body);
        final RequestInformation requestInfo = new RequestInformation(HttpMethod.POST, urlTemplate, pathParameters);
        requestInfo.configure(requestConfiguration, PostRequestConfiguration::new);
        requestInfo.headers.tryAdd("Accept", "application/json");
        requestInfo.setContentFromParsable(requestAdapter, "application/json", body);
        return requestInfo;
    }
    /**
     * Returns a request builder with the provided arbitrary URL. Using this method means any other path or query parameters are ignored.
     * @param rawUrl The raw URL to use for the request builder.
     * @return a {@link ReleasesRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public ReleasesRequestBuilder withUrl(@jakarta.annotation.Nonnull final String rawUrl) {
        Objects.requireNonNull(rawUrl);
        return new ReleasesRequestBuilder(rawUrl, requestAdapter);
    }
    @jakarta.annotation.Generated("com.microsoft.kiota")
    public class GetQueryParameters implements QueryParameters {
        @jakarta.annotation.Nullable
        public String cursor;
        @jakarta.annotation.Nullable
        public Integer limit;
        /**
         * Extracts the query parameters into a map for the URI template parsing.
         * @return a {@link Map<String, Object>}
         */
        @jakarta.annotation.Nonnull
        public Map<String, Object> toQueryParameters() {
            final Map<String, Object> allQueryParams = new HashMap();
            allQueryParams.put("cursor", cursor);
            allQueryParams.put("limit", limit);
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
