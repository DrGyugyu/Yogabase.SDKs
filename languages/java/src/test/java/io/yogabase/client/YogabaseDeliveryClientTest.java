package io.yogabase.client;

import com.microsoft.kiota.BaseRequestConfiguration;
import com.microsoft.kiota.RequestAdapter;
import io.yogabase.client.delivery.models.DeliveryOperationEnvelope;
import java.io.ByteArrayInputStream;
import java.io.InputStream;
import java.lang.reflect.Proxy;
import java.net.URI;
import java.nio.charset.StandardCharsets;
import java.time.Duration;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import java.util.concurrent.atomic.AtomicReference;
import java.util.stream.Collectors;

/** Executable conformance checks for the authored Java delivery facade. */
public final class YogabaseDeliveryClientTest {
    private YogabaseDeliveryClientTest() {
    }

    public static void main(String[] args) throws Exception {
        assertTrue(YogabaseDeliveryClient.isTerminalOperationState("Succeeded"));
        assertTrue(YogabaseDeliveryClient.isTerminalOperationState("Blocked"));
        for (String state : List.of("AwaitingApproval", "Queued", "Dispatched", "Running")) {
            assertFalse(YogabaseDeliveryClient.isTerminalOperationState(state));
        }

        final BaseRequestConfiguration configuration = new BaseRequestConfiguration() { };
        YogabaseDeliveryRequestOptions.apply(
            configuration, "delivery-token", "W/\"7\"", "request-123", "opaque-resume-token");
        assertHeader(configuration, "Authorization", "Bearer delivery-token");
        assertHeader(configuration, "If-Match", "W/\"7\"");
        assertHeader(configuration, "Idempotency-Key", "request-123");
        assertHeader(configuration, "Last-Event-ID", "opaque-resume-token");

        final var tokenProvider = new YogabaseDeliveryBearerTokenProvider(
            URI.create("https://delivery.example.test"), () -> "delivery-token");
        assertEquals("delivery-token", tokenProvider.getAuthorizationToken(
            URI.create("https://delivery.example.test/api/v1/delivery"), Map.of()));
        assertEquals("", tokenProvider.getAuthorizationToken(
            URI.create("http://delivery.example.test/api/v1/delivery"), Map.of()));
        assertThrows(() -> new YogabaseDeliveryClient(
            noOpAdapter(), URI.create("http://delivery.example.test")));

        final String sse = ": heartbeat\n\n"
            + "id: opaque:resume-token\n"
            + "event: delivery-operation\n"
            + "data: {\"state\":\"Blocked\"}\n"
            + "data: {\"detail\":\"done\"}\n\n"
            + "event: ignored\n"
            + "data: {}\n\n";
        try (var events = YogabaseDeliverySseParser.parse(
            new ByteArrayInputStream(sse.getBytes(StandardCharsets.UTF_8)))) {
            final List<YogabaseDeliverySseEvent> parsed = events.collect(Collectors.toList());
            assertEquals(1, parsed.size());
            assertEquals("opaque:resume-token", parsed.get(0).id());
            assertEquals("delivery-operation", parsed.get(0).event());
            assertEquals("{\"state\":\"Blocked\"}\n{\"detail\":\"done\"}", parsed.get(0).dataJson());
        }

        final AtomicReference<String> baseUrl = new AtomicReference<>();
        final AtomicReference<com.microsoft.kiota.RequestInformation> request = new AtomicReference<>();
        final RequestAdapter adapter = (RequestAdapter) Proxy.newProxyInstance(
            RequestAdapter.class.getClassLoader(),
            new Class<?>[] { RequestAdapter.class },
            (proxy, method, arguments) -> switch (method.getName()) {
                case "setBaseUrl" -> { baseUrl.set((String) arguments[0]); yield null; }
                case "getBaseUrl" -> baseUrl.get();
                case "sendPrimitive" -> {
                    request.set((com.microsoft.kiota.RequestInformation) arguments[0]);
                    yield new ByteArrayInputStream(new byte[0]);
                }
                default -> null;
            });
        try (var client = new YogabaseDeliveryClient(adapter, URI.create("https://delivery.example.test"))) {
            client.openOperationEvents(
                UUID.randomUUID(), UUID.randomUUID(), UUID.randomUUID(), "opaque-resume-token").close();
        }
        assertEquals("https://delivery.example.test", baseUrl.get());
        assertHeader(request.get(), "Last-Event-ID", "opaque-resume-token");
        System.out.println("java delivery facade tests: PASS");
    }

    private static RequestAdapter noOpAdapter() {
        final AtomicReference<String> baseUrl = new AtomicReference<>();
        return (RequestAdapter) Proxy.newProxyInstance(
            RequestAdapter.class.getClassLoader(),
            new Class<?>[] { RequestAdapter.class },
            (proxy, method, arguments) -> switch (method.getName()) {
                case "setBaseUrl" -> { baseUrl.set((String) arguments[0]); yield null; }
                case "getBaseUrl" -> baseUrl.get();
                default -> null;
            });
    }

    private static void assertHeader(BaseRequestConfiguration configuration, String name, String expected) {
        final var values = configuration.headers.get(name);
        if (values == null || !values.contains(expected)) {
            throw new AssertionError("Expected " + name + " header to contain " + expected);
        }
    }

    private static void assertHeader(com.microsoft.kiota.RequestInformation information, String name, String expected) {
        final var values = information.headers.get(name);
        if (values == null || !values.contains(expected)) {
            throw new AssertionError("Expected request " + name + " header to contain " + expected);
        }
    }

    private static void assertTrue(boolean value) {
        if (!value) {
            throw new AssertionError("Expected true");
        }
    }

    private static void assertFalse(boolean value) {
        if (value) {
            throw new AssertionError("Expected false");
        }
    }

    private static void assertEquals(Object expected, Object actual) {
        if (!expected.equals(actual)) {
            throw new AssertionError("Expected " + expected + " but got " + actual);
        }
    }

    private static void assertThrows(Runnable action) {
        try {
            action.run();
        } catch (IllegalArgumentException expected) {
            return;
        }
        throw new AssertionError("Expected IllegalArgumentException");
    }
}
