package io.yogabase.client.v1.projects.item.schedules.item;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.BaseRequestConfiguration;
import com.microsoft.kiota.HttpMethod;
import com.microsoft.kiota.RequestAdapter;
import com.microsoft.kiota.RequestInformation;
import com.microsoft.kiota.RequestOption;
import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParsableFactory;
import io.yogabase.client.models.Problem;
import io.yogabase.client.models.Schedule;
import io.yogabase.client.models.ScheduleDefinitionRequest;
import io.yogabase.client.v1.projects.item.schedules.item.trigger.TriggerRequestBuilder;
import java.util.Collection;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /v1/projects/{projectId}/schedules/{scheduleId}
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class WithScheduleItemRequestBuilder extends BaseRequestBuilder {
    /**
     * The trigger property
     * @return a {@link TriggerRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public TriggerRequestBuilder trigger() {
        return new TriggerRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * Instantiates a new {@link WithScheduleItemRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public WithScheduleItemRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/schedules/{scheduleId}", pathParameters);
    }
    /**
     * Instantiates a new {@link WithScheduleItemRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public WithScheduleItemRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/schedules/{scheduleId}", rawUrl);
    }
    /**
     * Deletes the caller&apos;s own schedule, 204. The ETag last read is required in If-Match: 428 without it, 412 when it is stale. A schedule that does not exist or is another user&apos;s is a bare 404, to a request whose ETag is well formed whatever version it names.
     * @throws Problem When receiving a 400 status code
     * @throws Problem When receiving a 401 status code
     * @throws Problem When receiving a 403 status code
     * @throws Problem When receiving a 412 status code
     * @throws Problem When receiving a 428 status code
     * @throws Problem When receiving a 503 status code
     */
    public void delete() {
        delete(null);
    }
    /**
     * Deletes the caller&apos;s own schedule, 204. The ETag last read is required in If-Match: 428 without it, 412 when it is stale. A schedule that does not exist or is another user&apos;s is a bare 404, to a request whose ETag is well formed whatever version it names.
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @throws Problem When receiving a 400 status code
     * @throws Problem When receiving a 401 status code
     * @throws Problem When receiving a 403 status code
     * @throws Problem When receiving a 412 status code
     * @throws Problem When receiving a 428 status code
     * @throws Problem When receiving a 503 status code
     */
    public void delete(@jakarta.annotation.Nullable final java.util.function.Consumer<DeleteRequestConfiguration> requestConfiguration) {
        final RequestInformation requestInfo = toDeleteRequestInformation(requestConfiguration);
        final HashMap<String, ParsableFactory<? extends Parsable>> errorMapping = new HashMap<String, ParsableFactory<? extends Parsable>>();
        errorMapping.put("400", Problem::createFromDiscriminatorValue);
        errorMapping.put("401", Problem::createFromDiscriminatorValue);
        errorMapping.put("403", Problem::createFromDiscriminatorValue);
        errorMapping.put("412", Problem::createFromDiscriminatorValue);
        errorMapping.put("428", Problem::createFromDiscriminatorValue);
        errorMapping.put("503", Problem::createFromDiscriminatorValue);
        this.requestAdapter.sendPrimitive(requestInfo, errorMapping, Void.class);
    }
    /**
     * Reads the caller&apos;s own schedule, with the ETag to send as If-Match to change, delete or run it. A schedule that does not exist and one that is another user&apos;s are the same bare 404.
     * @return a {@link Schedule}
     * @throws Problem When receiving a 400 status code
     * @throws Problem When receiving a 401 status code
     * @throws Problem When receiving a 403 status code
     * @throws Problem When receiving a 503 status code
     */
    @jakarta.annotation.Nullable
    public Schedule get() {
        return get(null);
    }
    /**
     * Reads the caller&apos;s own schedule, with the ETag to send as If-Match to change, delete or run it. A schedule that does not exist and one that is another user&apos;s are the same bare 404.
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @return a {@link Schedule}
     * @throws Problem When receiving a 400 status code
     * @throws Problem When receiving a 401 status code
     * @throws Problem When receiving a 403 status code
     * @throws Problem When receiving a 503 status code
     */
    @jakarta.annotation.Nullable
    public Schedule get(@jakarta.annotation.Nullable final java.util.function.Consumer<GetRequestConfiguration> requestConfiguration) {
        final RequestInformation requestInfo = toGetRequestInformation(requestConfiguration);
        final HashMap<String, ParsableFactory<? extends Parsable>> errorMapping = new HashMap<String, ParsableFactory<? extends Parsable>>();
        errorMapping.put("400", Problem::createFromDiscriminatorValue);
        errorMapping.put("401", Problem::createFromDiscriminatorValue);
        errorMapping.put("403", Problem::createFromDiscriminatorValue);
        errorMapping.put("503", Problem::createFromDiscriminatorValue);
        return this.requestAdapter.send(requestInfo, errorMapping, Schedule::createFromDiscriminatorValue);
    }
    /**
     * Creates or updates the schedule scheduleId as the signed-in user, who then owns it: only the owner reads, changes, deletes or runs it. Create with If-None-Match: * (201); update with the ETag last read as If-Match (200); exactly one of the two, and 428 when neither is sent. The answer is the schedule with its new ETag. The id in the path must be the id in the body (else a 400 whose problem says so), and must follow the pattern of the definition&apos;s scheduleId member (else a 400 whose problem names the member and the rule). A schedule is recurring (cron, in timeZone) or a one-shot (oneShotAtUtc: it runs once and is then deleted), and an update cannot turn one into the other (409). Its function version must be an admitted Stateless Lambda version of the project, else 409. A request body that cannot be read as one JSON object of exactly the documented members is a bare 400, and one that is not JSON a bare 415; a member that is missing, out of range or invalid (a cron expression that does not parse, an unknown time zone, a payload that is not base64) is a 400 with a problem body; a missing misfirePolicy or concurrencyPolicy is refused like any other, and its problem names the member and the names it may take in its detail. A payload over the limit is 413 schedule_payload_too_large.
     * @param body The request body
     * @return a {@link Schedule}
     * @throws Problem When receiving a 400 status code
     * @throws Problem When receiving a 401 status code
     * @throws Problem When receiving a 403 status code
     * @throws Problem When receiving a 412 status code
     * @throws Problem When receiving a 413 status code
     * @throws Problem When receiving a 428 status code
     * @throws Problem When receiving a 503 status code
     */
    @jakarta.annotation.Nullable
    public Schedule put(@jakarta.annotation.Nonnull final ScheduleDefinitionRequest body) {
        return put(body, null);
    }
    /**
     * Creates or updates the schedule scheduleId as the signed-in user, who then owns it: only the owner reads, changes, deletes or runs it. Create with If-None-Match: * (201); update with the ETag last read as If-Match (200); exactly one of the two, and 428 when neither is sent. The answer is the schedule with its new ETag. The id in the path must be the id in the body (else a 400 whose problem says so), and must follow the pattern of the definition&apos;s scheduleId member (else a 400 whose problem names the member and the rule). A schedule is recurring (cron, in timeZone) or a one-shot (oneShotAtUtc: it runs once and is then deleted), and an update cannot turn one into the other (409). Its function version must be an admitted Stateless Lambda version of the project, else 409. A request body that cannot be read as one JSON object of exactly the documented members is a bare 400, and one that is not JSON a bare 415; a member that is missing, out of range or invalid (a cron expression that does not parse, an unknown time zone, a payload that is not base64) is a 400 with a problem body; a missing misfirePolicy or concurrencyPolicy is refused like any other, and its problem names the member and the names it may take in its detail. A payload over the limit is 413 schedule_payload_too_large.
     * @param body The request body
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @return a {@link Schedule}
     * @throws Problem When receiving a 400 status code
     * @throws Problem When receiving a 401 status code
     * @throws Problem When receiving a 403 status code
     * @throws Problem When receiving a 412 status code
     * @throws Problem When receiving a 413 status code
     * @throws Problem When receiving a 428 status code
     * @throws Problem When receiving a 503 status code
     */
    @jakarta.annotation.Nullable
    public Schedule put(@jakarta.annotation.Nonnull final ScheduleDefinitionRequest body, @jakarta.annotation.Nullable final java.util.function.Consumer<PutRequestConfiguration> requestConfiguration) {
        Objects.requireNonNull(body);
        final RequestInformation requestInfo = toPutRequestInformation(body, requestConfiguration);
        final HashMap<String, ParsableFactory<? extends Parsable>> errorMapping = new HashMap<String, ParsableFactory<? extends Parsable>>();
        errorMapping.put("400", Problem::createFromDiscriminatorValue);
        errorMapping.put("401", Problem::createFromDiscriminatorValue);
        errorMapping.put("403", Problem::createFromDiscriminatorValue);
        errorMapping.put("412", Problem::createFromDiscriminatorValue);
        errorMapping.put("413", Problem::createFromDiscriminatorValue);
        errorMapping.put("428", Problem::createFromDiscriminatorValue);
        errorMapping.put("503", Problem::createFromDiscriminatorValue);
        return this.requestAdapter.send(requestInfo, errorMapping, Schedule::createFromDiscriminatorValue);
    }
    /**
     * Deletes the caller&apos;s own schedule, 204. The ETag last read is required in If-Match: 428 without it, 412 when it is stale. A schedule that does not exist or is another user&apos;s is a bare 404, to a request whose ETag is well formed whatever version it names.
     * @return a {@link RequestInformation}
     */
    @jakarta.annotation.Nonnull
    public RequestInformation toDeleteRequestInformation() {
        return toDeleteRequestInformation(null);
    }
    /**
     * Deletes the caller&apos;s own schedule, 204. The ETag last read is required in If-Match: 428 without it, 412 when it is stale. A schedule that does not exist or is another user&apos;s is a bare 404, to a request whose ETag is well formed whatever version it names.
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @return a {@link RequestInformation}
     */
    @jakarta.annotation.Nonnull
    public RequestInformation toDeleteRequestInformation(@jakarta.annotation.Nullable final java.util.function.Consumer<DeleteRequestConfiguration> requestConfiguration) {
        final RequestInformation requestInfo = new RequestInformation(HttpMethod.DELETE, urlTemplate, pathParameters);
        requestInfo.configure(requestConfiguration, DeleteRequestConfiguration::new);
        requestInfo.headers.tryAdd("Accept", "application/problem+json");
        return requestInfo;
    }
    /**
     * Reads the caller&apos;s own schedule, with the ETag to send as If-Match to change, delete or run it. A schedule that does not exist and one that is another user&apos;s are the same bare 404.
     * @return a {@link RequestInformation}
     */
    @jakarta.annotation.Nonnull
    public RequestInformation toGetRequestInformation() {
        return toGetRequestInformation(null);
    }
    /**
     * Reads the caller&apos;s own schedule, with the ETag to send as If-Match to change, delete or run it. A schedule that does not exist and one that is another user&apos;s are the same bare 404.
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
     * Creates or updates the schedule scheduleId as the signed-in user, who then owns it: only the owner reads, changes, deletes or runs it. Create with If-None-Match: * (201); update with the ETag last read as If-Match (200); exactly one of the two, and 428 when neither is sent. The answer is the schedule with its new ETag. The id in the path must be the id in the body (else a 400 whose problem says so), and must follow the pattern of the definition&apos;s scheduleId member (else a 400 whose problem names the member and the rule). A schedule is recurring (cron, in timeZone) or a one-shot (oneShotAtUtc: it runs once and is then deleted), and an update cannot turn one into the other (409). Its function version must be an admitted Stateless Lambda version of the project, else 409. A request body that cannot be read as one JSON object of exactly the documented members is a bare 400, and one that is not JSON a bare 415; a member that is missing, out of range or invalid (a cron expression that does not parse, an unknown time zone, a payload that is not base64) is a 400 with a problem body; a missing misfirePolicy or concurrencyPolicy is refused like any other, and its problem names the member and the names it may take in its detail. A payload over the limit is 413 schedule_payload_too_large.
     * @param body The request body
     * @return a {@link RequestInformation}
     */
    @jakarta.annotation.Nonnull
    public RequestInformation toPutRequestInformation(@jakarta.annotation.Nonnull final ScheduleDefinitionRequest body) {
        return toPutRequestInformation(body, null);
    }
    /**
     * Creates or updates the schedule scheduleId as the signed-in user, who then owns it: only the owner reads, changes, deletes or runs it. Create with If-None-Match: * (201); update with the ETag last read as If-Match (200); exactly one of the two, and 428 when neither is sent. The answer is the schedule with its new ETag. The id in the path must be the id in the body (else a 400 whose problem says so), and must follow the pattern of the definition&apos;s scheduleId member (else a 400 whose problem names the member and the rule). A schedule is recurring (cron, in timeZone) or a one-shot (oneShotAtUtc: it runs once and is then deleted), and an update cannot turn one into the other (409). Its function version must be an admitted Stateless Lambda version of the project, else 409. A request body that cannot be read as one JSON object of exactly the documented members is a bare 400, and one that is not JSON a bare 415; a member that is missing, out of range or invalid (a cron expression that does not parse, an unknown time zone, a payload that is not base64) is a 400 with a problem body; a missing misfirePolicy or concurrencyPolicy is refused like any other, and its problem names the member and the names it may take in its detail. A payload over the limit is 413 schedule_payload_too_large.
     * @param body The request body
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @return a {@link RequestInformation}
     */
    @jakarta.annotation.Nonnull
    public RequestInformation toPutRequestInformation(@jakarta.annotation.Nonnull final ScheduleDefinitionRequest body, @jakarta.annotation.Nullable final java.util.function.Consumer<PutRequestConfiguration> requestConfiguration) {
        Objects.requireNonNull(body);
        final RequestInformation requestInfo = new RequestInformation(HttpMethod.PUT, urlTemplate, pathParameters);
        requestInfo.configure(requestConfiguration, PutRequestConfiguration::new);
        requestInfo.headers.tryAdd("Accept", "application/json");
        requestInfo.setContentFromParsable(requestAdapter, "application/json", body);
        return requestInfo;
    }
    /**
     * Returns a request builder with the provided arbitrary URL. Using this method means any other path or query parameters are ignored.
     * @param rawUrl The raw URL to use for the request builder.
     * @return a {@link WithScheduleItemRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public WithScheduleItemRequestBuilder withUrl(@jakarta.annotation.Nonnull final String rawUrl) {
        Objects.requireNonNull(rawUrl);
        return new WithScheduleItemRequestBuilder(rawUrl, requestAdapter);
    }
    /**
     * Configuration for the request such as headers, query parameters, and middleware options.
     */
    @jakarta.annotation.Generated("com.microsoft.kiota")
    public class DeleteRequestConfiguration extends BaseRequestConfiguration {
    }
    /**
     * Configuration for the request such as headers, query parameters, and middleware options.
     */
    @jakarta.annotation.Generated("com.microsoft.kiota")
    public class GetRequestConfiguration extends BaseRequestConfiguration {
    }
    /**
     * Configuration for the request such as headers, query parameters, and middleware options.
     */
    @jakarta.annotation.Generated("com.microsoft.kiota")
    public class PutRequestConfiguration extends BaseRequestConfiguration {
    }
}
