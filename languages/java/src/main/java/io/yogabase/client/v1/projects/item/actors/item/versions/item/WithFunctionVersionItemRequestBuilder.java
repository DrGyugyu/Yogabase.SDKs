package io.yogabase.client.v1.projects.item.actors.item.versions.item;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.RequestAdapter;
import io.yogabase.client.v1.projects.item.actors.item.versions.item.instances.InstancesRequestBuilder;
import java.util.HashMap;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /v1/projects/{projectId}/actors/{functionId}/versions/{functionVersion}
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class WithFunctionVersionItemRequestBuilder extends BaseRequestBuilder {
    /**
     * The instances property
     * @return a {@link InstancesRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public InstancesRequestBuilder instances() {
        return new InstancesRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * Instantiates a new {@link WithFunctionVersionItemRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public WithFunctionVersionItemRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/actors/{functionId}/versions/{functionVersion}", pathParameters);
    }
    /**
     * Instantiates a new {@link WithFunctionVersionItemRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public WithFunctionVersionItemRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/actors/{functionId}/versions/{functionVersion}", rawUrl);
    }
}
