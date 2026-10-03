package io.stackyoga.yogabase.v1.data;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.RequestAdapter;
import io.stackyoga.yogabase.v1.data.item.WithTableItemRequestBuilder;
import java.util.HashMap;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /v1/data
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class DataRequestBuilder extends BaseRequestBuilder {
    /**
     * Gets an item from the io.stackyoga.yogabase.v1.data.item collection
     * @param table A table in the project&apos;s public schema, exactly as the database names it. A name that is not exposed to the caller, and a name that does not exist, are the same 403 data_table_not_exposed.
     * @return a {@link WithTableItemRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public WithTableItemRequestBuilder byTable(@jakarta.annotation.Nonnull final String table) {
        Objects.requireNonNull(table);
        final HashMap<String, Object> urlTplParams = new HashMap<String, Object>(this.pathParameters);
        urlTplParams.put("table", table);
        return new WithTableItemRequestBuilder(urlTplParams, requestAdapter);
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
