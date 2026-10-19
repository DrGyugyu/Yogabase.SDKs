package io.yogabase.client;

import com.microsoft.kiota.RequestAdapter;
import io.yogabase.client.delivery.YogabaseDeliveryApiClient;
import io.yogabase.client.delivery.api.v1.delivery.projects.item.operations.events.EventsRequestBuilder;
import io.yogabase.client.delivery.models.DeliveryOperationEnvelope;
import java.io.InputStream;
import java.net.URI;
import java.time.Duration;
import java.util.Set;
import java.util.UUID;
import java.util.Objects;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.Future;

/** Control Plane delivery facade with an origin and credentials separate from application traffic. */
public final class YogabaseDeliveryClient implements AutoCloseable {
    public static final Set<String> TERMINAL_OPERATION_STATES = Set.of(
        "Succeeded", "Failed", "Abandoned", "Cancelled", "Blocked");

    private final URI origin;
    private final RequestAdapter requestAdapter;
    private final YogabaseDeliveryApiClient api;
    private final ExecutorService pollingExecutor;
    private final boolean ownsPollingExecutor;

    public YogabaseDeliveryClient(RequestAdapter requestAdapter, URI deliveryOrigin) {
        this(requestAdapter, deliveryOrigin, Executors.newCachedThreadPool(r -> {
            final Thread thread = new Thread(r, "yogabase-delivery-poller");
            thread.setDaemon(true);
            return thread;
        }), true);
    }

    public YogabaseDeliveryClient(RequestAdapter requestAdapter, URI deliveryOrigin, ExecutorService pollingExecutor) {
        this(requestAdapter, deliveryOrigin, pollingExecutor, false);
    }

    private YogabaseDeliveryClient(
        RequestAdapter requestAdapter,
        URI deliveryOrigin,
        ExecutorService pollingExecutor,
        boolean ownsPollingExecutor) {
        this.requestAdapter = Objects.requireNonNull(requestAdapter, "requestAdapter");
        this.origin = YogabaseClientOrigin.requireDeliveryOrigin(deliveryOrigin, "deliveryOrigin");
        this.pollingExecutor = Objects.requireNonNull(pollingExecutor, "pollingExecutor");
        this.ownsPollingExecutor = ownsPollingExecutor;
        requestAdapter.setBaseUrl(YogabaseClientOrigin.baseUrl(this.origin));
        this.api = new YogabaseDeliveryApiClient(requestAdapter);
    }

    public URI origin() {
        return origin;
    }

    public RequestAdapter requestAdapter() {
        return requestAdapter;
    }

    public YogabaseDeliveryApiClient api() {
        return api;
    }

    public Future<DeliveryOperationEnvelope> pollOperation(
        UUID projectId,
        UUID operationId,
        Duration interval,
        int maxAttempts) {
        Objects.requireNonNull(projectId, "projectId");
        Objects.requireNonNull(operationId, "operationId");
        Objects.requireNonNull(interval, "interval");
        if (interval.isNegative() || interval.isZero() || maxAttempts < 1) {
            throw new IllegalArgumentException("interval must be positive and maxAttempts must be at least one");
        }
        return pollingExecutor.submit(() -> {
            for (int attempt = 0; attempt < maxAttempts; attempt++) {
                if (Thread.currentThread().isInterrupted()) {
                    throw new InterruptedException("delivery operation polling cancelled");
                }
                final DeliveryOperationEnvelope operation = api.api().v1().delivery().projects()
                    .byProjectId(projectId).operations().byOperationId(operationId).get();
                if (operation == null || operation.getState() == null) {
                    throw new IllegalStateException("delivery operation response omitted state");
                }
                if (isTerminalOperationState(operation.getState())) {
                    return operation;
                }
                if (attempt + 1 < maxAttempts) {
                    Thread.sleep(interval.toMillis(), interval.getNano() % 1_000_000);
                }
            }
            throw new IllegalStateException("delivery operation did not reach a terminal state within the polling bound");
        });
    }

    public static boolean isTerminalOperationState(String state) {
        return state != null && TERMINAL_OPERATION_STATES.contains(state);
    }

    /** Opens the generated SSE response; callers should close the returned stream. */
    public InputStream openOperationEvents(
        UUID projectId,
        UUID environmentId,
        UUID operationId,
        String lastEventId) {
        Objects.requireNonNull(projectId, "projectId");
        final EventsRequestBuilder events = api.api().v1().delivery().projects()
            .byProjectId(projectId).operations().events();
        return events.get(configuration -> {
            configuration.queryParameters.environmentId = environmentId;
            configuration.queryParameters.operationId = operationId;
            YogabaseDeliveryRequestOptions.apply(configuration, null, null, null, lastEventId);
        });
    }

    public void close() {
        if (ownsPollingExecutor) {
            pollingExecutor.shutdownNow();
        }
    }
}
