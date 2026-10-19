package io.yogabase.client.v1.projects.item.schedules.item.trigger;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.BaseRequestConfiguration;
import com.microsoft.kiota.HttpMethod;
import com.microsoft.kiota.RequestAdapter;
import com.microsoft.kiota.RequestInformation;
import com.microsoft.kiota.RequestOption;
import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParsableFactory;
import io.yogabase.client.models.Problem;
import io.yogabase.client.models.ScheduleTriggerRequest;
import io.yogabase.client.models.ScheduleTriggerResult;
import java.util.Collection;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /v1/projects/{projectId}/schedules/{scheduleId}/trigger
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class TriggerRequestBuilder extends BaseRequestBuilder {
    /**
     * Instantiates a new {@link TriggerRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public TriggerRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/schedules/{scheduleId}/trigger", pathParameters);
    }
    /**
     * Instantiates a new {@link TriggerRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public TriggerRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/schedules/{scheduleId}/trigger", rawUrl);
    }
    /**
     * Runs the caller&apos;s own schedule once now, outside its cadence: it enqueues one invocation of the schedule&apos;s function with the schedule&apos;s payload, as the schedule&apos;s owner. The ETag last read is required in If-Match (428 without it, 412 when it is stale), and the idempotency key makes the request repeatable: the same key against the same version answers the same invocation, 202 again with outcome 1 where the first said 0. The deadline is when the invocation must finish: after now and at most an hour from now, else 400. A schedule that does not exist or is another user&apos;s is a bare 404. A schedule whose function version is no longer an admitted Stateless one of the project is a bare 409, as a write of it is. A run is not an occurrence of the schedule: its concurrencyPolicy neither holds it back nor is held back by it.
     * @param body The request body
     * @return a {@link ScheduleTriggerResult}
     * @throws Problem When receiving a 400 status code
     * @throws Problem When receiving a 401 status code
     * @throws Problem When receiving a 403 status code
     * @throws Problem When receiving a 412 status code
     * @throws Problem When receiving a 428 status code
     * @throws Problem When receiving a 503 status code
     */
    @jakarta.annotation.Nullable
    public ScheduleTriggerResult post(@jakarta.annotation.Nonnull final ScheduleTriggerRequest body) {
        return post(body, null);
    }
    /**
     * Runs the caller&apos;s own schedule once now, outside its cadence: it enqueues one invocation of the schedule&apos;s function with the schedule&apos;s payload, as the schedule&apos;s owner. The ETag last read is required in If-Match (428 without it, 412 when it is stale), and the idempotency key makes the request repeatable: the same key against the same version answers the same invocation, 202 again with outcome 1 where the first said 0. The deadline is when the invocation must finish: after now and at most an hour from now, else 400. A schedule that does not exist or is another user&apos;s is a bare 404. A schedule whose function version is no longer an admitted Stateless one of the project is a bare 409, as a write of it is. A run is not an occurrence of the schedule: its concurrencyPolicy neither holds it back nor is held back by it.
     * @param body The request body
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @return a {@link ScheduleTriggerResult}
     * @throws Problem When receiving a 400 status code
     * @throws Problem When receiving a 401 status code
     * @throws Problem When receiving a 403 status code
     * @throws Problem When receiving a 412 status code
     * @throws Problem When receiving a 428 status code
     * @throws Problem When receiving a 503 status code
     */
    @jakarta.annotation.Nullable
    public ScheduleTriggerResult post(@jakarta.annotation.Nonnull final ScheduleTriggerRequest body, @jakarta.annotation.Nullable final java.util.function.Consumer<PostRequestConfiguration> requestConfiguration) {
        Objects.requireNonNull(body);
        final RequestInformation requestInfo = toPostRequestInformation(body, requestConfiguration);
        final HashMap<String, ParsableFactory<? extends Parsable>> errorMapping = new HashMap<String, ParsableFactory<? extends Parsable>>();
        errorMapping.put("400", Problem::createFromDiscriminatorValue);
        errorMapping.put("401", Problem::createFromDiscriminatorValue);
        errorMapping.put("403", Problem::createFromDiscriminatorValue);
        errorMapping.put("412", Problem::createFromDiscriminatorValue);
        errorMapping.put("428", Problem::createFromDiscriminatorValue);
        errorMapping.put("503", Problem::createFromDiscriminatorValue);
        return this.requestAdapter.send(requestInfo, errorMapping, ScheduleTriggerResult::createFromDiscriminatorValue);
    }
    /**
     * Runs the caller&apos;s own schedule once now, outside its cadence: it enqueues one invocation of the schedule&apos;s function with the schedule&apos;s payload, as the schedule&apos;s owner. The ETag last read is required in If-Match (428 without it, 412 when it is stale), and the idempotency key makes the request repeatable: the same key against the same version answers the same invocation, 202 again with outcome 1 where the first said 0. The deadline is when the invocation must finish: after now and at most an hour from now, else 400. A schedule that does not exist or is another user&apos;s is a bare 404. A schedule whose function version is no longer an admitted Stateless one of the project is a bare 409, as a write of it is. A run is not an occurrence of the schedule: its concurrencyPolicy neither holds it back nor is held back by it.
     * @param body The request body
     * @return a {@link RequestInformation}
     */
    @jakarta.annotation.Nonnull
    public RequestInformation toPostRequestInformation(@jakarta.annotation.Nonnull final ScheduleTriggerRequest body) {
        return toPostRequestInformation(body, null);
    }
    /**
     * Runs the caller&apos;s own schedule once now, outside its cadence: it enqueues one invocation of the schedule&apos;s function with the schedule&apos;s payload, as the schedule&apos;s owner. The ETag last read is required in If-Match (428 without it, 412 when it is stale), and the idempotency key makes the request repeatable: the same key against the same version answers the same invocation, 202 again with outcome 1 where the first said 0. The deadline is when the invocation must finish: after now and at most an hour from now, else 400. A schedule that does not exist or is another user&apos;s is a bare 404. A schedule whose function version is no longer an admitted Stateless one of the project is a bare 409, as a write of it is. A run is not an occurrence of the schedule: its concurrencyPolicy neither holds it back nor is held back by it.
     * @param body The request body
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @return a {@link RequestInformation}
     */
    @jakarta.annotation.Nonnull
    public RequestInformation toPostRequestInformation(@jakarta.annotation.Nonnull final ScheduleTriggerRequest body, @jakarta.annotation.Nullable final java.util.function.Consumer<PostRequestConfiguration> requestConfiguration) {
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
     * @return a {@link TriggerRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public TriggerRequestBuilder withUrl(@jakarta.annotation.Nonnull final String rawUrl) {
        Objects.requireNonNull(rawUrl);
        return new TriggerRequestBuilder(rawUrl, requestAdapter);
    }
    /**
     * Configuration for the request such as headers, query parameters, and middleware options.
     */
    @jakarta.annotation.Generated("com.microsoft.kiota")
    public class PostRequestConfiguration extends BaseRequestConfiguration {
    }
}
