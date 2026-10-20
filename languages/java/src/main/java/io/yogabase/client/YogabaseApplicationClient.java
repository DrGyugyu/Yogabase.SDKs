package io.yogabase.client;

import com.microsoft.kiota.RequestAdapter;
import com.microsoft.kiota.bundle.DefaultRequestAdapter;
import java.net.URI;
import java.util.Objects;
import java.util.UUID;

/**
 * Application-plane facade. Its adapter, origin, and credentials are intentionally
 * independent from {@link YogabaseDeliveryClient}.
 */
public final class YogabaseApplicationClient {
    private final URI origin;
    private final RequestAdapter requestAdapter;
    private final YogabaseApiClient api;

    public YogabaseApplicationClient(RequestAdapter requestAdapter, URI applicationOrigin) {
        this.requestAdapter = Objects.requireNonNull(requestAdapter, "requestAdapter");
        this.origin = YogabaseClientOrigin.requireOrigin(applicationOrigin, "applicationOrigin");
        requestAdapter.setBaseUrl(YogabaseClientOrigin.baseUrl(this.origin));
        this.api = new YogabaseApiClient(requestAdapter);
    }

    /**
     * The application client of a signed-in user: a Kiota bundle adapter whose bearer is the session's, bound to the
     * gateway origin the user signed in at ({@link YogabaseSignIn}).
     */
    public static YogabaseApplicationClient forSession(YogabaseSession session) {
        Objects.requireNonNull(session, "session");
        return new YogabaseApplicationClient(new DefaultRequestAdapter(session.authenticationProvider()), session.gatewayOrigin());
    }

    public URI origin() {
        return origin;
    }

    public RequestAdapter requestAdapter() {
        return requestAdapter;
    }

    public YogabaseApiClient api() {
        return api;
    }

    /**
     * Durable background jobs of one project for the signed-in user, over the generated Jobs routes. It
     * shares this client's adapter, and so its authentication and origin.
     */
    public YogabaseJobsClient jobs(UUID projectId) {
        return new YogabaseJobsClient(requestAdapter, origin, projectId);
    }

    /**
     * The signed-in user's own schedules of one project, beside the generated schedule routes: each write is
     * conditional on the schedule's ETag. It shares this client's adapter, and so its authentication and origin.
     */
    public YogabaseSchedulesClient schedules(UUID projectId) {
        return new YogabaseSchedulesClient(requestAdapter, origin, projectId);
    }
}
