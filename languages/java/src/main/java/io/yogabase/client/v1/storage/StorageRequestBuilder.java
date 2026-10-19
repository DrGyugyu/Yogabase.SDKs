package io.yogabase.client.v1.storage;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.RequestAdapter;
import io.yogabase.client.v1.storage.objectsescaped.ObjectsRequestBuilder;
import java.util.HashMap;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /v1/storage
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class StorageRequestBuilder extends BaseRequestBuilder {
    /**
     * The objects property
     * @return a {@link ObjectsRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public ObjectsRequestBuilder objectsEscaped() {
        return new ObjectsRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * Instantiates a new {@link StorageRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public StorageRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/storage", pathParameters);
    }
    /**
     * Instantiates a new {@link StorageRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public StorageRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/storage", rawUrl);
    }
}
