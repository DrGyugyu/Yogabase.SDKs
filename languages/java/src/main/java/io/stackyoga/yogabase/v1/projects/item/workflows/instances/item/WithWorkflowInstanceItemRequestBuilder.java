package io.stackyoga.yogabase.v1.projects.item.workflows.instances.item;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.RequestAdapter;
import io.stackyoga.yogabase.v1.projects.item.workflows.instances.item.cancel.CancelRequestBuilder;
import java.util.HashMap;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /v1/projects/{projectId}/workflows/instances/{workflowInstanceId}
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class WithWorkflowInstanceItemRequestBuilder extends BaseRequestBuilder {
    /**
     * The cancel property
     * @return a {@link CancelRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public CancelRequestBuilder cancel() {
        return new CancelRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * Instantiates a new {@link WithWorkflowInstanceItemRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public WithWorkflowInstanceItemRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/workflows/instances/{workflowInstanceId}", pathParameters);
    }
    /**
     * Instantiates a new {@link WithWorkflowInstanceItemRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public WithWorkflowInstanceItemRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/workflows/instances/{workflowInstanceId}", rawUrl);
    }
}
