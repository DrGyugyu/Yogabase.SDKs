package io.yogabase.client.v1.projects.item.jobs.item;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.BaseRequestConfiguration;
import com.microsoft.kiota.HttpMethod;
import com.microsoft.kiota.RequestAdapter;
import com.microsoft.kiota.RequestInformation;
import com.microsoft.kiota.RequestOption;
import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParsableFactory;
import io.yogabase.client.models.Job;
import io.yogabase.client.models.Problem;
import io.yogabase.client.v1.projects.item.jobs.item.cancel.CancelRequestBuilder;
import io.yogabase.client.v1.projects.item.jobs.item.result.ResultRequestBuilder;
import java.util.Collection;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /v1/projects/{projectId}/jobs/{jobId}
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class WithJobItemRequestBuilder extends BaseRequestBuilder {
    /**
     * The cancel property
     * @return a {@link CancelRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public CancelRequestBuilder cancel() {
        return new CancelRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * The result property
     * @return a {@link ResultRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public ResultRequestBuilder result() {
        return new ResultRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * Instantiates a new {@link WithJobItemRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public WithJobItemRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/jobs/{jobId}", pathParameters);
    }
    /**
     * Instantiates a new {@link WithJobItemRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public WithJobItemRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/jobs/{jobId}", rawUrl);
    }
    /**
     * Reads the caller&apos;s own job: its state, how far it has got and how it ended. Another user&apos;s job is answered exactly as one that does not exist, 404 job_not_found.
     * @return a {@link Job}
     * @throws Problem When receiving a 400 status code
     * @throws Problem When receiving a 401 status code
     * @throws Problem When receiving a 403 status code
     * @throws Problem When receiving a 404 status code
     * @throws Problem When receiving a 503 status code
     */
    @jakarta.annotation.Nullable
    public Job get() {
        return get(null);
    }
    /**
     * Reads the caller&apos;s own job: its state, how far it has got and how it ended. Another user&apos;s job is answered exactly as one that does not exist, 404 job_not_found.
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @return a {@link Job}
     * @throws Problem When receiving a 400 status code
     * @throws Problem When receiving a 401 status code
     * @throws Problem When receiving a 403 status code
     * @throws Problem When receiving a 404 status code
     * @throws Problem When receiving a 503 status code
     */
    @jakarta.annotation.Nullable
    public Job get(@jakarta.annotation.Nullable final java.util.function.Consumer<GetRequestConfiguration> requestConfiguration) {
        final RequestInformation requestInfo = toGetRequestInformation(requestConfiguration);
        final HashMap<String, ParsableFactory<? extends Parsable>> errorMapping = new HashMap<String, ParsableFactory<? extends Parsable>>();
        errorMapping.put("400", Problem::createFromDiscriminatorValue);
        errorMapping.put("401", Problem::createFromDiscriminatorValue);
        errorMapping.put("403", Problem::createFromDiscriminatorValue);
        errorMapping.put("404", Problem::createFromDiscriminatorValue);
        errorMapping.put("503", Problem::createFromDiscriminatorValue);
        return this.requestAdapter.send(requestInfo, errorMapping, Job::createFromDiscriminatorValue);
    }
    /**
     * Reads the caller&apos;s own job: its state, how far it has got and how it ended. Another user&apos;s job is answered exactly as one that does not exist, 404 job_not_found.
     * @return a {@link RequestInformation}
     */
    @jakarta.annotation.Nonnull
    public RequestInformation toGetRequestInformation() {
        return toGetRequestInformation(null);
    }
    /**
     * Reads the caller&apos;s own job: its state, how far it has got and how it ended. Another user&apos;s job is answered exactly as one that does not exist, 404 job_not_found.
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
     * @return a {@link WithJobItemRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public WithJobItemRequestBuilder withUrl(@jakarta.annotation.Nonnull final String rawUrl) {
        Objects.requireNonNull(rawUrl);
        return new WithJobItemRequestBuilder(rawUrl, requestAdapter);
    }
    /**
     * Configuration for the request such as headers, query parameters, and middleware options.
     */
    @jakarta.annotation.Generated("com.microsoft.kiota")
    public class GetRequestConfiguration extends BaseRequestConfiguration {
    }
}
