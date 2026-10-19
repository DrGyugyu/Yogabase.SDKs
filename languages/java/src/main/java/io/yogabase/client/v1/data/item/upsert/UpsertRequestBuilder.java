package io.yogabase.client.v1.data.item.upsert;

import com.microsoft.kiota.BaseRequestBuilder;
import com.microsoft.kiota.BaseRequestConfiguration;
import com.microsoft.kiota.HttpMethod;
import com.microsoft.kiota.RequestAdapter;
import com.microsoft.kiota.RequestInformation;
import com.microsoft.kiota.RequestOption;
import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParsableFactory;
import io.yogabase.client.models.DataResult;
import io.yogabase.client.models.DataUpsertRequest;
import io.yogabase.client.models.Problem;
import java.util.Collection;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
/**
 * Builds and executes requests for operations under /v1/data/{table}/upsert
 */
@jakarta.annotation.Generated("com.microsoft.kiota")
public class UpsertRequestBuilder extends BaseRequestBuilder {
    /**
     * Instantiates a new {@link UpsertRequestBuilder} and sets the default values.
     * @param pathParameters Path parameters for the request
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public UpsertRequestBuilder(@jakarta.annotation.Nonnull final HashMap<String, Object> pathParameters, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/data/{table}/upsert", pathParameters);
    }
    /**
     * Instantiates a new {@link UpsertRequestBuilder} and sets the default values.
     * @param rawUrl The raw URL to use for the request builder.
     * @param requestAdapter The request adapter to use to execute the requests.
     */
    public UpsertRequestBuilder(@jakarta.annotation.Nonnull final String rawUrl, @jakarta.annotation.Nonnull final RequestAdapter requestAdapter) {
        super(requestAdapter, "{+baseurl}/v1/data/{table}/upsert", rawUrl);
    }
    /**
     * Inserts rows; a conflict on onConflict (the columns of a primary key or unique index) updates every other supplied column, or does nothing with ignoreDuplicates.
     * @param body The request body
     * @return a {@link DataResult}
     * @throws Problem When receiving a 400 status code
     * @throws Problem When receiving a 401 status code
     * @throws Problem When receiving a 403 status code
     * @throws Problem When receiving a 409 status code
     * @throws Problem When receiving a 413 status code
     * @throws Problem When receiving a 503 status code
     */
    @jakarta.annotation.Nullable
    public DataResult post(@jakarta.annotation.Nonnull final DataUpsertRequest body) {
        return post(body, null);
    }
    /**
     * Inserts rows; a conflict on onConflict (the columns of a primary key or unique index) updates every other supplied column, or does nothing with ignoreDuplicates.
     * @param body The request body
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @return a {@link DataResult}
     * @throws Problem When receiving a 400 status code
     * @throws Problem When receiving a 401 status code
     * @throws Problem When receiving a 403 status code
     * @throws Problem When receiving a 409 status code
     * @throws Problem When receiving a 413 status code
     * @throws Problem When receiving a 503 status code
     */
    @jakarta.annotation.Nullable
    public DataResult post(@jakarta.annotation.Nonnull final DataUpsertRequest body, @jakarta.annotation.Nullable final java.util.function.Consumer<PostRequestConfiguration> requestConfiguration) {
        Objects.requireNonNull(body);
        final RequestInformation requestInfo = toPostRequestInformation(body, requestConfiguration);
        final HashMap<String, ParsableFactory<? extends Parsable>> errorMapping = new HashMap<String, ParsableFactory<? extends Parsable>>();
        errorMapping.put("400", Problem::createFromDiscriminatorValue);
        errorMapping.put("401", Problem::createFromDiscriminatorValue);
        errorMapping.put("403", Problem::createFromDiscriminatorValue);
        errorMapping.put("409", Problem::createFromDiscriminatorValue);
        errorMapping.put("413", Problem::createFromDiscriminatorValue);
        errorMapping.put("503", Problem::createFromDiscriminatorValue);
        return this.requestAdapter.send(requestInfo, errorMapping, DataResult::createFromDiscriminatorValue);
    }
    /**
     * Inserts rows; a conflict on onConflict (the columns of a primary key or unique index) updates every other supplied column, or does nothing with ignoreDuplicates.
     * @param body The request body
     * @return a {@link RequestInformation}
     */
    @jakarta.annotation.Nonnull
    public RequestInformation toPostRequestInformation(@jakarta.annotation.Nonnull final DataUpsertRequest body) {
        return toPostRequestInformation(body, null);
    }
    /**
     * Inserts rows; a conflict on onConflict (the columns of a primary key or unique index) updates every other supplied column, or does nothing with ignoreDuplicates.
     * @param body The request body
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @return a {@link RequestInformation}
     */
    @jakarta.annotation.Nonnull
    public RequestInformation toPostRequestInformation(@jakarta.annotation.Nonnull final DataUpsertRequest body, @jakarta.annotation.Nullable final java.util.function.Consumer<PostRequestConfiguration> requestConfiguration) {
        Objects.requireNonNull(body);
        final RequestInformation requestInfo = new RequestInformation(HttpMethod.POST, urlTemplate, pathParameters);
        requestInfo.configure(requestConfiguration, PostRequestConfiguration::new);
        requestInfo.headers.tryAdd("Accept", "application/json");
        requestInfo.setContentFromParsable(requestAdapter, "application/json", body);
        return requestInfo;
    }
    /**
     * Returns a request builder with the provided arbitrary URL. Using this method means any other path or query parameters are ignored.
     * @param rawUrl The raw URL to use for the request builder.
     * @return a {@link UpsertRequestBuilder}
     */
    @jakarta.annotation.Nonnull
    public UpsertRequestBuilder withUrl(@jakarta.annotation.Nonnull final String rawUrl) {
        Objects.requireNonNull(rawUrl);
        return new UpsertRequestBuilder(rawUrl, requestAdapter);
    }
    /**
     * Configuration for the request such as headers, query parameters, and middleware options.
     */
    @jakarta.annotation.Generated("com.microsoft.kiota")
    public class PostRequestConfiguration extends BaseRequestConfiguration {
    }
}
