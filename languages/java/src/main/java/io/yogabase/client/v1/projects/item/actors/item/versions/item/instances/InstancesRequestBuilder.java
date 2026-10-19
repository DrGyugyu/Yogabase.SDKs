package io.yogabase.client.v1.projects.item.actors.item.versions.item.instances;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.RequestAdapter;
import io.yogabase.client.v1.projects.item.actors.item.versions.item.instances.item.WithInstanceKeyItemRequestBuilder;
import java.util.HashMap;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /v1/projects/{projectId}/actors/{functionId}/versions/{functionVersion}/instances
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class InstancesRequestBuilder extends BaseRequestBuilder {
    /**
     * Gets an item from the io.yogabase.client.v1.projects.item.actors.item.versions.item.instances.item collection
     * @param instanceKey Unique identifier of the item
     * @return a {@link WithInstanceKeyItemRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public WithInstanceKeyItemRequestBuilder byInstanceKey(@jakarta.annotation.Nonnull final String instanceKey) {
        Objects.requireNonNull(instanceKey);
        final HashMap<String, Object> urlTplParams = new HashMap<String, Object>(this.pathParameters);
        urlTplParams.put("instanceKey", instanceKey);
        return new WithInstanceKeyItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /**
     * Instantiates a new {@link InstancesRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public InstancesRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/actors/{functionId}/versions/{functionVersion}/instances", pathParameters);
    }
    /**
     * Instantiates a new {@link InstancesRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public InstancesRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/actors/{functionId}/versions/{functionVersion}/instances", rawUrl);
    }
}
