package io.stackyoga.yogabase.v1.projects.item.actors.item.versions.item.instances.item;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.RequestAdapter;
import io.stackyoga.yogabase.v1.projects.item.actors.item.versions.item.instances.item.invoke.InvokeRequestBuilder;
import java.util.HashMap;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /v1/projects/{projectId}/actors/{functionId}/versions/{functionVersion}/instances/{instanceKey}
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class WithInstanceKeyItemRequestBuilder extends BaseRequestBuilder {
    /**
     * The invoke property
     * @return a {@link InvokeRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public InvokeRequestBuilder invoke() {
        return new InvokeRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * Instantiates a new {@link WithInstanceKeyItemRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public WithInstanceKeyItemRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/actors/{functionId}/versions/{functionVersion}/instances/{instanceKey}", pathParameters);
    }
    /**
     * Instantiates a new {@link WithInstanceKeyItemRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public WithInstanceKeyItemRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/actors/{functionId}/versions/{functionVersion}/instances/{instanceKey}", rawUrl);
    }
}
