package io.stackyoga.yogabase.v1.data.item;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.RequestAdapter;
import io.stackyoga.yogabase.v1.data.item.delete.DeleteRequestBuilder;
import io.stackyoga.yogabase.v1.data.item.insert.InsertRequestBuilder;
import io.stackyoga.yogabase.v1.data.item.select.SelectRequestBuilder;
import io.stackyoga.yogabase.v1.data.item.update.UpdateRequestBuilder;
import java.util.HashMap;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /v1/data/{resource}
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class WithResourceItemRequestBuilder extends BaseRequestBuilder {
    /**
     * The deletePath property
     * @return a {@link DeleteRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public DeleteRequestBuilder deletePath() {
        return new DeleteRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * The insert property
     * @return a {@link InsertRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public InsertRequestBuilder insert() {
        return new InsertRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * The select property
     * @return a {@link SelectRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public SelectRequestBuilder select() {
        return new SelectRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * The update property
     * @return a {@link UpdateRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public UpdateRequestBuilder update() {
        return new UpdateRequestBuilder(pathParameters, requestAdapter);
    }
    /**
     * Instantiates a new {@link WithResourceItemRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public WithResourceItemRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/data/{resource}", pathParameters);
    }
    /**
     * Instantiates a new {@link WithResourceItemRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public WithResourceItemRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/data/{resource}", rawUrl);
    }
}
