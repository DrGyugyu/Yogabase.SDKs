package io.yogabase.client;

import com.microsoft.kiota.BaseRequestConfiguration;
import com.microsoft.kiota.RequestHeaders;
import java.util.Objects;
import java.util.regex.Pattern;

/** Applies delivery-plane headers to any generated Kiota request configuration. */
public final class YogabaseDeliveryRequestOptions {
    private static final Pattern ETAG = Pattern.compile("^W/\\\"[0-9]+\\\"$");

    private YogabaseDeliveryRequestOptions() {
    }

    public static void apply(
        BaseRequestConfiguration configuration,
        String bearerToken,
        String ifMatch,
        String idempotencyKey,
        String lastEventId) {
        Objects.requireNonNull(configuration, "configuration");
        if (bearerToken != null) {
            requireNonBlankBounded(bearerToken, 4096, "bearerToken");
            headers(configuration).tryAdd("Authorization", "Bearer " + bearerToken);
        }
        if (ifMatch != null) {
            if (!ETAG.matcher(ifMatch).matches()) {
                throw new IllegalArgumentException("ifMatch must be a weak numeric ETag such as W/\"12\"");
            }
            headers(configuration).tryAdd("If-Match", ifMatch);
        }
        if (idempotencyKey != null) {
            requireNonBlankBounded(idempotencyKey, 128, "idempotencyKey");
            headers(configuration).tryAdd("Idempotency-Key", idempotencyKey);
        }
        if (lastEventId != null) {
            requireNonBlankBounded(lastEventId, 4096, "lastEventId");
            headers(configuration).tryAdd("Last-Event-ID", lastEventId);
        }
    }

    private static RequestHeaders headers(BaseRequestConfiguration configuration) {
        if (configuration.headers == null) {
            configuration.headers = new RequestHeaders();
        }
        return configuration.headers;
    }

    private static void requireNonBlankBounded(String value, int maxLength, String name) {
        if (value.isBlank() || value.length() > maxLength) {
            throw new IllegalArgumentException(name + " must be non-blank and at most " + maxLength + " characters");
        }
    }
}
