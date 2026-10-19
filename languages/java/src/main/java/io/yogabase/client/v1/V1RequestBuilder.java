package io.yogabase.client.v1;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.RequestAdapter;
import io.yogabase.client.v1.clientconfig.ClientConfigRequestBuilder;
import io.yogabase.client.v1.data.DataRequestBuilder;
import io.yogabase.client.v1.projects.ProjectsRequestBuilder;
import io.yogabase.client.v1.servers.ServersRequestBuilder;
import io.yogabase.client.v1.storage.StorageRequestBuilder;
import java.util.HashMap;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /v1
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class V1RequestBuilder extends BaseRequestBuilder {
    /**
     * The clientConfig property
     * @return a {@link ClientConfigRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public ClientConfigRequestBuilder clientConfig() {
        return new ClientConfigRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * The data property
     * @return a {@link DataRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public DataRequestBuilder data() {
        return new DataRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * The projects property
     * @return a {@link ProjectsRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public ProjectsRequestBuilder projects() {
        return new ProjectsRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * The servers property
     * @return a {@link ServersRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public ServersRequestBuilder servers() {
        return new ServersRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * The storage property
     * @return a {@link StorageRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public StorageRequestBuilder storage() {
        return new StorageRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * Instantiates a new {@link V1RequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public V1RequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1", pathParameters);
    }
    /**
     * Instantiates a new {@link V1RequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public V1RequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1", rawUrl);
    }
}
