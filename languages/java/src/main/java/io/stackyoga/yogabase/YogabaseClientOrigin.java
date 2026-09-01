package io.stackyoga.yogabase;

import java.net.URI;
import java.util.Locale;
import java.util.Objects;

/** Validates and normalizes an origin used by one Yogabase client boundary. */
final class YogabaseClientOrigin {
    private YogabaseClientOrigin() {
    }

    static URI requireOrigin(URI value, String parameterName) {
        Objects.requireNonNull(value, parameterName);
        final String scheme = value.getScheme();
        final String rawPath = value.getRawPath();
        if (!value.isAbsolute()
            || value.isOpaque()
            || scheme == null
            || !(scheme.equalsIgnoreCase("https") || scheme.equalsIgnoreCase("http"))
            || value.getHost() == null
            || value.getRawUserInfo() != null
            || value.getRawQuery() != null
            || value.getRawFragment() != null
            || !(rawPath == null || rawPath.isEmpty() || rawPath.equals("/"))) {
            throw new IllegalArgumentException(parameterName + " must be an origin-only HTTP(S) URL");
        }
        return URI.create(value.toString().replaceFirst("/$", ""));
    }

    static URI requireDeliveryOrigin(URI value, String parameterName) {
        final URI origin = requireOrigin(value, parameterName);
        if (!origin.getScheme().equalsIgnoreCase("https")) {
            throw new IllegalArgumentException(parameterName + " must use HTTPS");
        }
        return origin;
    }

    static boolean sameOrigin(URI left, URI right) {
        return left != null
            && right != null
            && left.getScheme() != null
            && right.getScheme() != null
            && left.getHost() != null
            && right.getHost() != null
            && left.getScheme().equalsIgnoreCase(right.getScheme())
            && left.getHost().equalsIgnoreCase(right.getHost())
            && effectivePort(left) == effectivePort(right);
    }

    static String baseUrl(URI origin) {
        return origin.getScheme().toLowerCase(Locale.ROOT) + "://" + origin.getRawAuthority();
    }

    private static int effectivePort(URI value) {
        if (value.getPort() >= 0) {
            return value.getPort();
        }
        return value.getScheme().equalsIgnoreCase("https") ? 443 : 80;
    }
}
