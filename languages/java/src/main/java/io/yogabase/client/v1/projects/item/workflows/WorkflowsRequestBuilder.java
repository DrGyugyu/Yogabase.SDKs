package io.yogabase.client.v1.projects.item.workflows;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.RequestAdapter;
import io.yogabase.client.v1.projects.item.workflows.instances.InstancesRequestBuilder;
import io.yogabase.client.v1.projects.item.workflows.item.WithWorkflowItemRequestBuilder;
import java.util.HashMap;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /v1/projects/{projectId}/workflows
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class WorkflowsRequestBuilder extends BaseRequestBuilder {
    /**
     * The instances property
     * @return a {@link InstancesRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public InstancesRequestBuilder instances() {
        return new InstancesRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * Gets an item from the io.yogabase.client.v1.projects.item.workflows.item collection
     * @param workflowId Unique identifier of the item
     * @return a {@link WithWorkflowItemRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public WithWorkflowItemRequestBuilder byWorkflowId(@jakarta.annotation.Nonnull final String workflowId) {
        Objects.requireNonNull(workflowId);
        final HashMap<String, Object> urlTplParams = new HashMap<String, Object>(this.pathParameters);
        urlTplParams.put("workflowId", workflowId);
        return new WithWorkflowItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /**
     * Instantiates a new {@link WorkflowsRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public WorkflowsRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/workflows", pathParameters);
    }
    /**
     * Instantiates a new {@link WorkflowsRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public WorkflowsRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/workflows", rawUrl);
    }
}
