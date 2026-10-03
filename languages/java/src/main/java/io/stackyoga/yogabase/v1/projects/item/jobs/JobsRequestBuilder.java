package io.stackyoga.yogabase.v1.projects.item.jobs;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.BaseRequestConfiguration;
import com.microsoft.kiota.HttpMethod;
import com.microsoft.kiota.RequestAdapter;
import com.microsoft.kiota.RequestInformation;
import com.microsoft.kiota.RequestOption;
import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParsableFactory;
import io.stackyoga.yogabase.models.Job;
import io.stackyoga.yogabase.models.JobSubmitRequest;
import io.stackyoga.yogabase.models.Problem;
import io.stackyoga.yogabase.v1.projects.item.jobs.item.WithJobItemRequestBuilder;
import java.util.Collection;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
import java.util.UUID;
/**
 * Builds and executes requests for operations under /v1/projects/{projectId}/jobs
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class JobsRequestBuilder extends BaseRequestBuilder {
    /**
     * Gets an item from the io.stackyoga.yogabase.v1.projects.item.jobs.item collection
     * @param jobId A job&apos;s id, as returned by submit. An id that is not a uuid in its hyphenated form, or is the all-zero uuid, is 400 job_request_invalid.
     * @return a {@link WithJobItemRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public WithJobItemRequestBuilder byJobId(@jakarta.annotation.Nonnull final UUID jobId) {
        Objects.requireNonNull(jobId);
        final HashMap<String, Object> urlTplParams = new HashMap<String, Object>(this.pathParameters);
        urlTplParams.put("jobId", jobId);
        return new WithJobItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /**
     * Instantiates a new {@link JobsRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public JobsRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/jobs", pathParameters);
    }
    /**
     * Instantiates a new {@link JobsRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public JobsRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/jobs", rawUrl);
    }
    /**
     * Starts one durable run of a deployed stateless function as the signed-in user. The job type is the id of a deployed stateless function, run by its highest released version; an id no released function carries is 422 job_type_not_registered and records nothing. The idempotency key is a member of the body, belongs to the signed-in user, and binds the whole request (job type, payload and maxAttempts): the same request under it answers the job it already made, 202 again and in whatever state that job has reached, and a different request under it is 409 job_idempotency_conflict. The 202 carries the job&apos;s own route in Location. A request body that cannot be read as one JSON object of exactly the documented members is a bare 400, and one that is not JSON a bare 415; a member that is missing or out of range (an empty or over-long job type or key, maxAttempts outside 1 to 10, a payload that is not base64) is 400 job_request_invalid.
     * @param body The request body
     * @return a {@link Job}
     * @throws Problem When receiving a 400 status code
     * @throws Problem When receiving a 401 status code
     * @throws Problem When receiving a 403 status code
     * @throws Problem When receiving a 409 status code
     * @throws Problem When receiving a 413 status code
     * @throws Problem When receiving a 422 status code
     * @throws Problem When receiving a 503 status code
     */
    @jakarta.annotation.Nullable
    public Job post(@jakarta.annotation.Nonnull final JobSubmitRequest body) {
        return post(body, null);
    }
    /**
     * Starts one durable run of a deployed stateless function as the signed-in user. The job type is the id of a deployed stateless function, run by its highest released version; an id no released function carries is 422 job_type_not_registered and records nothing. The idempotency key is a member of the body, belongs to the signed-in user, and binds the whole request (job type, payload and maxAttempts): the same request under it answers the job it already made, 202 again and in whatever state that job has reached, and a different request under it is 409 job_idempotency_conflict. The 202 carries the job&apos;s own route in Location. A request body that cannot be read as one JSON object of exactly the documented members is a bare 400, and one that is not JSON a bare 415; a member that is missing or out of range (an empty or over-long job type or key, maxAttempts outside 1 to 10, a payload that is not base64) is 400 job_request_invalid.
     * @param body The request body
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @return a {@link Job}
     * @throws Problem When receiving a 400 status code
     * @throws Problem When receiving a 401 status code
     * @throws Problem When receiving a 403 status code
     * @throws Problem When receiving a 409 status code
     * @throws Problem When receiving a 413 status code
     * @throws Problem When receiving a 422 status code
     * @throws Problem When receiving a 503 status code
     */
    @jakarta.annotation.Nullable
    public Job post(@jakarta.annotation.Nonnull final JobSubmitRequest body, @jakarta.annotation.Nullable final java.util.function.Consumer<PostRequestConfiguration> requestConfiguration) {
        Objects.requireNonNull(body);
        final RequestInformation requestInfo = toPostRequestInformation(body, requestConfiguration);
        final HashMap<String, ParsableFactory<? extends Parsable>> errorMapping = new HashMap<String, ParsableFactory<? extends Parsable>>();
        errorMapping.put("400", Problem::createFromDiscriminatorValue);
        errorMapping.put("401", Problem::createFromDiscriminatorValue);
        errorMapping.put("403", Problem::createFromDiscriminatorValue);
        errorMapping.put("409", Problem::createFromDiscriminatorValue);
        errorMapping.put("413", Problem::createFromDiscriminatorValue);
        errorMapping.put("422", Problem::createFromDiscriminatorValue);
        errorMapping.put("503", Problem::createFromDiscriminatorValue);
        return this.requestAdapter.send(requestInfo, errorMapping, Job::createFromDiscriminatorValue);
    }
    /**
     * Starts one durable run of a deployed stateless function as the signed-in user. The job type is the id of a deployed stateless function, run by its highest released version; an id no released function carries is 422 job_type_not_registered and records nothing. The idempotency key is a member of the body, belongs to the signed-in user, and binds the whole request (job type, payload and maxAttempts): the same request under it answers the job it already made, 202 again and in whatever state that job has reached, and a different request under it is 409 job_idempotency_conflict. The 202 carries the job&apos;s own route in Location. A request body that cannot be read as one JSON object of exactly the documented members is a bare 400, and one that is not JSON a bare 415; a member that is missing or out of range (an empty or over-long job type or key, maxAttempts outside 1 to 10, a payload that is not base64) is 400 job_request_invalid.
     * @param body The request body
     * @return a {@link RequestInformation}
     */
    @jakarta.annotation.Nonnull
    public RequestInformation toPostRequestInformation(@jakarta.annotation.Nonnull final JobSubmitRequest body) {
        return toPostRequestInformation(body, null);
    }
    /**
     * Starts one durable run of a deployed stateless function as the signed-in user. The job type is the id of a deployed stateless function, run by its highest released version; an id no released function carries is 422 job_type_not_registered and records nothing. The idempotency key is a member of the body, belongs to the signed-in user, and binds the whole request (job type, payload and maxAttempts): the same request under it answers the job it already made, 202 again and in whatever state that job has reached, and a different request under it is 409 job_idempotency_conflict. The 202 carries the job&apos;s own route in Location. A request body that cannot be read as one JSON object of exactly the documented members is a bare 400, and one that is not JSON a bare 415; a member that is missing or out of range (an empty or over-long job type or key, maxAttempts outside 1 to 10, a payload that is not base64) is 400 job_request_invalid.
     * @param body The request body
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @return a {@link RequestInformation}
     */
    @jakarta.annotation.Nonnull
    public RequestInformation toPostRequestInformation(@jakarta.annotation.Nonnull final JobSubmitRequest body, @jakarta.annotation.Nullable final java.util.function.Consumer<PostRequestConfiguration> requestConfiguration) {
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
     * @return a {@link JobsRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public JobsRequestBuilder withUrl(@jakarta.annotation.Nonnull final String rawUrl) {
        Objects.requireNonNull(rawUrl);
        return new JobsRequestBuilder(rawUrl, requestAdapter);
    }
    /**
     * Configuration for the request such as headers, query parameters, and middleware options.
     */
    @jakarta.annotation.Generated("com.microsoft.kiota")
    public class PostRequestConfiguration extends BaseRequestConfiguration {
    }
}
