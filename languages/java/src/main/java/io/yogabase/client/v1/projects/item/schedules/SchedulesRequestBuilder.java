package io.yogabase.client.v1.projects.item.schedules;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.RequestAdapter;
import io.yogabase.client.v1.projects.item.schedules.item.WithScheduleItemRequestBuilder;
import java.util.HashMap;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /v1/projects/{projectId}/schedules
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class SchedulesRequestBuilder extends BaseRequestBuilder {
    /**
     * Gets an item from the io.yogabase.client.v1.projects.item.schedules.item collection
     * @param scheduleId The schedule&apos;s id: the one it was written under, which a write holds to the pattern of the scheduleId member of the definition (one segment of this path, so there is nothing to escape). The id is the project&apos;s, so one that another user holds cannot be created (412). A schedule written before the pattern was enforced under another id is still found by that id, percent-encoded as any path segment is, by a read, a run and a delete; a write of it is refused, so to change it delete it and create it again under an id that follows the pattern.
     * @return a {@link WithScheduleItemRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public WithScheduleItemRequestBuilder byScheduleId(@jakarta.annotation.Nonnull final String scheduleId) {
        Objects.requireNonNull(scheduleId);
        final HashMap<String, Object> urlTplParams = new HashMap<String, Object>(this.pathParameters);
        urlTplParams.put("scheduleId", scheduleId);
        return new WithScheduleItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /**
     * Instantiates a new {@link SchedulesRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public SchedulesRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/schedules", pathParameters);
    }
    /**
     * Instantiates a new {@link SchedulesRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public SchedulesRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/schedules", rawUrl);
    }
}
