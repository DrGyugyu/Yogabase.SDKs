package io.stackyoga.yogabase.v1.data;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.RequestAdapter;
import io.stackyoga.yogabase.v1.data.item.WithResourceItemRequestBuilder;
import java.util.HashMap;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /v1/data
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class DataRequestBuilder extends BaseRequestBuilder {
    /**
     * Gets an item from the io.stackyoga.yogabase.v1.data.item collection
     * @param resource Unique identifier of the item
     * @return a {@link WithResourceItemRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public WithResourceItemRequestBuilder byResource(@jakarta.annotation.Nonnull final String resource) {
        Objects.requireNonNull(resource);
        final HashMap<String, Object> urlTplParams = new HashMap<String, Object>(this.pathParameters);
        urlTplParams.put("resource", resource);
        return new WithResourceItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /**
     * Instantiates a new {@link DataRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public DataRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/data", pathParameters);
    }
    /**
     * Instantiates a new {@link DataRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public DataRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/data", rawUrl);
    }
}
