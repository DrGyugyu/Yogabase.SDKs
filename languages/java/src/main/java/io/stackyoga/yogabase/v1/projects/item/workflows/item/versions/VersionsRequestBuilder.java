package io.stackyoga.yogabase.v1.projects.item.workflows.item.versions;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.RequestAdapter;
import io.stackyoga.yogabase.v1.projects.item.workflows.item.versions.item.WithDefinitionVersionItemRequestBuilder;
import java.util.HashMap;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /v1/projects/{projectId}/workflows/{workflowId}/versions
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class VersionsRequestBuilder extends BaseRequestBuilder {
    /**
     * Gets an item from the io.stackyoga.yogabase.v1.projects.item.workflows.item.versions.item collection
     * @param definitionVersion Unique identifier of the item
     * @return a {@link WithDefinitionVersionItemRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public WithDefinitionVersionItemRequestBuilder byDefinitionVersion(@jakarta.annotation.Nonnull final String definitionVersion) {
        Objects.requireNonNull(definitionVersion);
        final HashMap<String, Object> urlTplParams = new HashMap<String, Object>(this.pathParameters);
        urlTplParams.put("definitionVersion", definitionVersion);
        return new WithDefinitionVersionItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /**
     * Instantiates a new {@link VersionsRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public VersionsRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/workflows/{workflowId}/versions", pathParameters);
    }
    /**
     * Instantiates a new {@link VersionsRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public VersionsRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/workflows/{workflowId}/versions", rawUrl);
    }
}
