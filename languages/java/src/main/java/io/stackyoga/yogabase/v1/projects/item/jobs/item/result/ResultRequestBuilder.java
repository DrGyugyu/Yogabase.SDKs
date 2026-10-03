package io.stackyoga.yogabase.v1.projects.item.jobs.item.result;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.BaseRequestConfiguration;
import com.microsoft.kiota.HttpMethod;
import com.microsoft.kiota.RequestAdapter;
import com.microsoft.kiota.RequestInformation;
import com.microsoft.kiota.RequestOption;
import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParsableFactory;
import io.stackyoga.yogabase.models.Problem;
import java.io.InputStream;
import java.util.Collection;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /v1/projects/{projectId}/jobs/{jobId}/result
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class ResultRequestBuilder extends BaseRequestBuilder {
    /**
     * Instantiates a new {@link ResultRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public ResultRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/jobs/{jobId}/result", pathParameters);
    }
    /**
     * Instantiates a new {@link ResultRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public ResultRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/jobs/{jobId}/result", rawUrl);
    }
    /**
     * The result of the caller&apos;s own succeeded job: the bytes its function returned, exactly as it returned them, from one byte to 16 MiB. The gateway reads the stored object twice, once to check it against the digest the job recorded and once to send it, so no byte leaves before all of them have been checked; that digest is in X-Yogabase-Content-Sha256. 404 job_result_not_found is the one answer for another user&apos;s job, an id that does not exist, and a job that finished without a result (cancelled, dead-lettered, or succeeded with none), so it never says which. 409 job_result_not_ready is the caller&apos;s own job that has not settled. 500 job_result_reference_invalid and 502 job_result_integrity_failed mean the stored result is not the one the job recorded, and send no byte of it.
     * @return a {@link InputStream}
     * @throws Problem When receiving a 400 status code
     * @throws Problem When receiving a 401 status code
     * @throws Problem When receiving a 403 status code
     * @throws Problem When receiving a 404 status code
     * @throws Problem When receiving a 409 status code
     * @throws Problem When receiving a 500 status code
     * @throws Problem When receiving a 502 status code
     * @throws Problem When receiving a 503 status code
     */
    @jakarta.annotation.Nullable
    public InputStream get() {
        return get(null);
    }
    /**
     * The result of the caller&apos;s own succeeded job: the bytes its function returned, exactly as it returned them, from one byte to 16 MiB. The gateway reads the stored object twice, once to check it against the digest the job recorded and once to send it, so no byte leaves before all of them have been checked; that digest is in X-Yogabase-Content-Sha256. 404 job_result_not_found is the one answer for another user&apos;s job, an id that does not exist, and a job that finished without a result (cancelled, dead-lettered, or succeeded with none), so it never says which. 409 job_result_not_ready is the caller&apos;s own job that has not settled. 500 job_result_reference_invalid and 502 job_result_integrity_failed mean the stored result is not the one the job recorded, and send no byte of it.
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @return a {@link InputStream}
     * @throws Problem When receiving a 400 status code
     * @throws Problem When receiving a 401 status code
     * @throws Problem When receiving a 403 status code
     * @throws Problem When receiving a 404 status code
     * @throws Problem When receiving a 409 status code
     * @throws Problem When receiving a 500 status code
     * @throws Problem When receiving a 502 status code
     * @throws Problem When receiving a 503 status code
     */
    @jakarta.annotation.Nullable
    public InputStream get(@jakarta.annotation.Nullable final java.util.function.Consumer<GetRequestConfiguration> requestConfiguration) {
        final RequestInformation requestInfo = toGetRequestInformation(requestConfiguration);
        final HashMap<String, ParsableFactory<? extends Parsable>> errorMapping = new HashMap<String, ParsableFactory<? extends Parsable>>();
        errorMapping.put("400", Problem::createFromDiscriminatorValue);
        errorMapping.put("401", Problem::createFromDiscriminatorValue);
        errorMapping.put("403", Problem::createFromDiscriminatorValue);
        errorMapping.put("404", Problem::createFromDiscriminatorValue);
        errorMapping.put("409", Problem::createFromDiscriminatorValue);
        errorMapping.put("500", Problem::createFromDiscriminatorValue);
        errorMapping.put("502", Problem::createFromDiscriminatorValue);
        errorMapping.put("503", Problem::createFromDiscriminatorValue);
        return this.requestAdapter.sendPrimitive(requestInfo, errorMapping, InputStream.class);
    }
    /**
     * The result of the caller&apos;s own succeeded job: the bytes its function returned, exactly as it returned them, from one byte to 16 MiB. The gateway reads the stored object twice, once to check it against the digest the job recorded and once to send it, so no byte leaves before all of them have been checked; that digest is in X-Yogabase-Content-Sha256. 404 job_result_not_found is the one answer for another user&apos;s job, an id that does not exist, and a job that finished without a result (cancelled, dead-lettered, or succeeded with none), so it never says which. 409 job_result_not_ready is the caller&apos;s own job that has not settled. 500 job_result_reference_invalid and 502 job_result_integrity_failed mean the stored result is not the one the job recorded, and send no byte of it.
     * @return a {@link RequestInformation}
     */
    @jakarta.annotation.Nonnull
    public RequestInformation toGetRequestInformation() {
        return toGetRequestInformation(null);
    }
    /**
     * The result of the caller&apos;s own succeeded job: the bytes its function returned, exactly as it returned them, from one byte to 16 MiB. The gateway reads the stored object twice, once to check it against the digest the job recorded and once to send it, so no byte leaves before all of them have been checked; that digest is in X-Yogabase-Content-Sha256. 404 job_result_not_found is the one answer for another user&apos;s job, an id that does not exist, and a job that finished without a result (cancelled, dead-lettered, or succeeded with none), so it never says which. 409 job_result_not_ready is the caller&apos;s own job that has not settled. 500 job_result_reference_invalid and 502 job_result_integrity_failed mean the stored result is not the one the job recorded, and send no byte of it.
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @return a {@link RequestInformation}
     */
    @jakarta.annotation.Nonnull
    public RequestInformation toGetRequestInformation(@jakarta.annotation.Nullable final java.util.function.Consumer<GetRequestConfiguration> requestConfiguration) {
        final RequestInformation requestInfo = new RequestInformation(HttpMethod.GET, urlTemplate, pathParameters);
        requestInfo.configure(requestConfiguration, GetRequestConfiguration::new);
        requestInfo.headers.tryAdd("Accept", "application/octet-stream, application/problem+json");
        return requestInfo;
    }
    /**
     * Returns a request builder with the provided arbitrary URL. Using this method means any other path or query parameters are ignored.
     * @param rawUrl The raw URL to use for the request builder.
     * @return a {@link ResultRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public ResultRequestBuilder withUrl(@jakarta.annotation.Nonnull final String rawUrl) {
        Objects.requireNonNull(rawUrl);
        return new ResultRequestBuilder(rawUrl, requestAdapter);
    }
    /**
     * Configuration for the request such as headers, query parameters, and middleware options.
     */
    @jakarta.annotation.Generated("com.microsoft.kiota")
    public class GetRequestConfiguration extends BaseRequestConfiguration {
    }
}
