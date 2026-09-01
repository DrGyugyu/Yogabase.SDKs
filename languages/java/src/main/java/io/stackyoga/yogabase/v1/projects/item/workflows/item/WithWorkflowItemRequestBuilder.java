package io.stackyoga.yogabase.v1.projects.item.workflows.item;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.RequestAdapter;
import io.stackyoga.yogabase.v1.projects.item.workflows.item.versions.VersionsRequestBuilder;
import java.util.HashMap;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /v1/projects/{projectId}/workflows/{workflowId}
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class WithWorkflowItemRequestBuilder extends BaseRequestBuilder {
    /**
     * The versions property
     * @return a {@link VersionsRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public VersionsRequestBuilder versions() {
        return new VersionsRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * Instantiates a new {@link WithWorkflowItemRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public WithWorkflowItemRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/workflows/{workflowId}", pathParameters);
    }
    /**
     * Instantiates a new {@link WithWorkflowItemRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public WithWorkflowItemRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/workflows/{workflowId}", rawUrl);
    }
}
