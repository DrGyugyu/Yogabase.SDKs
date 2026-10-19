package io.yogabase.client.v1.projects.item.workflows.item.versions.item;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.RequestAdapter;
import io.yogabase.client.v1.projects.item.workflows.item.versions.item.start.StartRequestBuilder;
import java.util.HashMap;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /v1/projects/{projectId}/workflows/{workflowId}/versions/{definitionVersion}
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class WithDefinitionVersionItemRequestBuilder extends BaseRequestBuilder {
    /**
     * The start property
     * @return a {@link StartRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public StartRequestBuilder start() {
        return new StartRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * Instantiates a new {@link WithDefinitionVersionItemRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public WithDefinitionVersionItemRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/workflows/{workflowId}/versions/{definitionVersion}", pathParameters);
    }
    /**
     * Instantiates a new {@link WithDefinitionVersionItemRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public WithDefinitionVersionItemRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/workflows/{workflowId}/versions/{definitionVersion}", rawUrl);
    }
}
