package io.stackyoga.yogabase.v1.projects.item.actors.item.versions;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.RequestAdapter;
import io.stackyoga.yogabase.v1.projects.item.actors.item.versions.item.WithFunctionVersionItemRequestBuilder;
import java.util.HashMap;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /v1/projects/{projectId}/actors/{functionId}/versions
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class VersionsRequestBuilder extends BaseRequestBuilder {
    /**
     * Gets an item from the io.stackyoga.yogabase.v1.projects.item.actors.item.versions.item collection
     * @param functionVersion Unique identifier of the item
     * @return a {@link WithFunctionVersionItemRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public WithFunctionVersionItemRequestBuilder byFunctionVersion(@jakarta.annotation.Nonnull final String functionVersion) {
        Objects.requireNonNull(functionVersion);
        final HashMap<String, Object> urlTplParams = new HashMap<String, Object>(this.pathParameters);
        urlTplParams.put("functionVersion", functionVersion);
        return new WithFunctionVersionItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /**
     * Instantiates a new {@link VersionsRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public VersionsRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/actors/{functionId}/versions", pathParameters);
    }
    /**
     * Instantiates a new {@link VersionsRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public VersionsRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/actors/{functionId}/versions", rawUrl);
    }
}
