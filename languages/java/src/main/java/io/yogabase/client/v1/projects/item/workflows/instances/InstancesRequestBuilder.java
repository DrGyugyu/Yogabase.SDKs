package io.yogabase.client.v1.projects.item.workflows.instances;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.RequestAdapter;
import io.yogabase.client.v1.projects.item.workflows.instances.item.WithWorkflowInstanceItemRequestBuilder;
import java.util.HashMap;
import java.util.Objects;
import java.util.UUID;
/**
 * Builds and executes requests for operations under /v1/projects/{projectId}/workflows/instances
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class InstancesRequestBuilder extends BaseRequestBuilder {
    /**
     * Gets an item from the io.yogabase.client.v1.projects.item.workflows.instances.item collection
     * @param workflowInstanceId Unique identifier of the item
     * @return a {@link WithWorkflowInstanceItemRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public WithWorkflowInstanceItemRequestBuilder byWorkflowInstanceId(@jakarta.annotation.Nonnull final UUID workflowInstanceId) {
        Objects.requireNonNull(workflowInstanceId);
        final HashMap<String, Object> urlTplParams = new HashMap<String, Object>(this.pathParameters);
        urlTplParams.put("workflowInstanceId", workflowInstanceId);
        return new WithWorkflowInstanceItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /**
     * Instantiates a new {@link InstancesRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public InstancesRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/workflows/instances", pathParameters);
    }
    /**
     * Instantiates a new {@link InstancesRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public InstancesRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/workflows/instances", rawUrl);
    }
}
