package io.stackyoga.yogabase.v1.projects.item;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.RequestAdapter;
import io.stackyoga.yogabase.v1.projects.item.actors.ActorsRequestBuilder;
import io.stackyoga.yogabase.v1.projects.item.functions.FunctionsRequestBuilder;
import io.stackyoga.yogabase.v1.projects.item.jobs.JobsRequestBuilder;
import io.stackyoga.yogabase.v1.projects.item.schedules.SchedulesRequestBuilder;
import io.stackyoga.yogabase.v1.projects.item.workflows.WorkflowsRequestBuilder;
import java.util.HashMap;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /v1/projects/{projectId}
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class WithProjectItemRequestBuilder extends BaseRequestBuilder {
    /**
     * The actors property
     * @return a {@link ActorsRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public ActorsRequestBuilder actors() {
        return new ActorsRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * The functions property
     * @return a {@link FunctionsRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public FunctionsRequestBuilder functions() {
        return new FunctionsRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * The jobs property
     * @return a {@link JobsRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public JobsRequestBuilder jobs() {
        return new JobsRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * The schedules property
     * @return a {@link SchedulesRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public SchedulesRequestBuilder schedules() {
        return new SchedulesRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * The workflows property
     * @return a {@link WorkflowsRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public WorkflowsRequestBuilder workflows() {
        return new WorkflowsRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * Instantiates a new {@link WithProjectItemRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public WithProjectItemRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}", pathParameters);
    }
    /**
     * Instantiates a new {@link WithProjectItemRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public WithProjectItemRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}", rawUrl);
    }
}
