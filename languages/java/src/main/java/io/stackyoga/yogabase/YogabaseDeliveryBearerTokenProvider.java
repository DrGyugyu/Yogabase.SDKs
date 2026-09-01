package io.stackyoga.yogabase;

import com.microsoft.kiota.authentication.AccessTokenProvider;
import com.microsoft.kiota.authentication.AllowedHostsValidator;
import java.net.URI;
import java.util.Map;
import java.util.Objects;
import java.util.function.Supplier;

/**
 * Lazy bearer-token provider for a delivery-only Kiota adapter. The token is never
 * stored by this provider; callers can rotate it in their supplier.
 */
public final class YogabaseDeliveryBearerTokenProvider implements AccessTokenProvider {
    private final URI deliveryOrigin;
    private final Supplier<String> tokenSupplier;
    private final AllowedHostsValidator allowedHostsValidator;

    public YogabaseDeliveryBearerTokenProvider(URI deliveryOrigin, Supplier<String> tokenSupplier) {
        final URI origin = YogabaseClientOrigin.requireDeliveryOrigin(deliveryOrigin, "deliveryOrigin");
        this.deliveryOrigin = origin;
        this.tokenSupplier = Objects.requireNonNull(tokenSupplier, "tokenSupplier");
        this.allowedHostsValidator = new AllowedHostsValidator(origin.getHost());
    }

    @Override
    public String getAuthorizationToken(URI uri, Map<String, Object> additionalAuthenticationContext) {
        if (!allowedHostsValidator.isUrlHostValid(uri)
            || !YogabaseClientOrigin.sameOrigin(deliveryOrigin, uri)) {
            return "";
        }
        final String token = tokenSupplier.get();
        if (token == null || token.isBlank()) {
            throw new IllegalStateException("delivery bearer token supplier returned no token");
        }
        return token;
    }

    @Override
    public AllowedHostsValidator getAllowedHostsValidator() {
        return allowedHostsValidator;
    }
}
