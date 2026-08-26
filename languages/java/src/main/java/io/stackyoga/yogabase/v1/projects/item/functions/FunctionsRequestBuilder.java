package io.stackyoga.yogabase.v1.projects.item.functions;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.RequestAdapter;
import io.stackyoga.yogabase.v1.projects.item.functions.item.WithFunctionItemRequestBuilder;
import java.util.HashMap;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /v1/projects/{projectId}/functions
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class FunctionsRequestBuilder extends BaseRequestBuilder {
    /**
     * Gets an item from the io.stackyoga.yogabase.v1.projects.item.functions.item collection
     * @param functionId Unique identifier of the item
     * @return a {@link WithFunctionItemRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public WithFunctionItemRequestBuilder byFunctionId(@jakarta.annotation.Nonnull final String functionId) {
        Objects.requireNonNull(functionId);
        final HashMap<String, Object> urlTplParams = new HashMap<String, Object>(this.pathParameters);
        urlTplParams.put("functionId", functionId);
        return new WithFunctionItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /**
     * Instantiates a new {@link FunctionsRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public FunctionsRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/functions", pathParameters);
    }
    /**
     * Instantiates a new {@link FunctionsRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public FunctionsRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/projects/{projectId}/functions", rawUrl);
    }
}
