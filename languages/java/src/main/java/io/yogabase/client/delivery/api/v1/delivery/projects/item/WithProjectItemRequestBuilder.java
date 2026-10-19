package io.yogabase.client.delivery.api.v1.delivery.projects.item;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.RequestAdapter;
import io.yogabase.client.delivery.api.v1.delivery.projects.item.environments.EnvironmentsRequestBuilder;
import io.yogabase.client.delivery.api.v1.delivery.projects.item.operations.OperationsRequestBuilder;
import io.yogabase.client.delivery.api.v1.delivery.projects.item.templates.TemplatesRequestBuilder;
import java.util.HashMap;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /api/v1/delivery/projects/{projectId}
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class WithProjectItemRequestBuilder extends BaseRequestBuilder {
    /**
     * The environments property
     * @return a {@link EnvironmentsRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public EnvironmentsRequestBuilder environments() {
        return new EnvironmentsRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * The operations property
     * @return a {@link OperationsRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public OperationsRequestBuilder operations() {
        return new OperationsRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * The templates property
     * @return a {@link TemplatesRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public TemplatesRequestBuilder templates() {
        return new TemplatesRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * Instantiates a new {@link WithProjectItemRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public WithProjectItemRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/api/v1/delivery/projects/{projectId}", pathParameters);
    }
    /**
     * Instantiates a new {@link WithProjectItemRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public WithProjectItemRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/api/v1/delivery/projects/{projectId}", rawUrl);
    }
}
