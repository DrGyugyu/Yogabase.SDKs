package io.yogabase.client;

import com.google.gson.GsonBuilder;
import com.microsoft.kiota.RequestAdapter;
import com.microsoft.kiota.authentication.AccessTokenProvider;
import com.microsoft.kiota.authentication.AllowedHostsValidator;
import com.microsoft.kiota.authentication.BaseBearerTokenAuthenticationProvider;
import com.microsoft.kiota.http.OkHttpRequestAdapter;
import io.yogabase.client.models.ActorInvocationRequest;
import io.yogabase.client.models.DataInsertRequest;
import io.yogabase.client.models.Problem;
import io.yogabase.client.models.WorkflowCancelRequest;
import io.yogabase.client.models.WorkflowStartRequest;
import java.net.URI;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import java.util.function.Consumer;
import okhttp3.MediaType;
import okhttp3.OkHttpClient;
import okhttp3.Protocol;
import okhttp3.Request;
import okhttp3.Response;
import okhttp3.ResponseBody;

/**
 * Executable checks of the Kiota-generated application client against the mirrored contract's newer refusals: the
 * actor invoke, workflow start and workflow cancel routes answer an over-limit request with a 413
 * {@code payload_too_large} problem (480 KiB of payload, and a body limit for each route), and the Data API with a
 * 413 {@code data_request_too_large}, each naming {@code limitBytes} and {@code maxBodyBytes}. The client under test
 * is generated, not authored; what is authored here is only the scripted transport.
 */
public final class YogabaseGeneratedSurfaceTest {
    private static final String GATEWAY = "https://apps.example.test";
    private static final UUID PROJECT = UUID.fromString("11111111-1111-4111-8111-111111111111");
    private static final UUID INSTANCE = UUID.fromString("55555555-5555-4555-8555-555555555555");
    private static int cases;

    private YogabaseGeneratedSurfaceTest() {
    }

    public static void main(String[] args) {
        theActorAndWorkflowRoutesRefuseAnOverLimitRequestWithAPayloadTooLargeProblem();
        theDataApiRefusesAnOverLimitRequestWithADataRequestTooLargeProblem();
        System.out.println("java generated surface tests: PASS (" + cases + " checks)");
    }

    private static void theActorAndWorkflowRoutesRefuseAnOverLimitRequestWithAPayloadTooLargeProblem() {
        final Object[][] routes = {
            {3_935_575L, "actors/counter/versions/1/instances/customer/invoke",
                (Consumer<YogabaseApiClient>) api -> api.v1().projects().byProjectId(PROJECT).actors().byFunctionId("counter")
                    .versions().byFunctionVersion("1").instances().byInstanceKey("customer").invoke().post(new ActorInvocationRequest())},
            {3_934_405L, "workflows/onboard/versions/1/start",
                (Consumer<YogabaseApiClient>) api -> api.v1().projects().byProjectId(PROJECT).workflows().byWorkflowId("onboard")
                    .versions().byDefinitionVersion("1").start().post(new WorkflowStartRequest())},
            {3_934_417L, "workflows/instances/" + INSTANCE + "/cancel",
                (Consumer<YogabaseApiClient>) api -> api.v1().projects().byProjectId(PROJECT).workflows().instances()
                    .byWorkflowInstanceId(INSTANCE).cancel().post(new WorkflowCancelRequest())},
        };
        for (final Object[] route : routes) {
            final long maxBodyBytes = (Long) route[0];
            final String path = (String) route[1];
            @SuppressWarnings("unchecked")
            final Consumer<YogabaseApiClient> send = (Consumer<YogabaseApiClient>) route[2];
            final List<Request> requests = new ArrayList<>();
            final Problem problem = assertThrows(Problem.class, () -> send.accept(client(requests, 413, "payload_too_large", 491_520L, maxBodyBytes)));
            assertEquals(GATEWAY + "/v1/projects/" + PROJECT + "/" + path, requests.get(0).url().toString());
            assertEquals("POST", requests.get(0).method());
            assertEquals("payload_too_large", problem.getCode());
            assertEquals(413, problem.getResponseStatusCode());
            assertEquals(491_520L, ((Number) problem.getAdditionalData().get("limitBytes")).longValue());
            assertEquals(maxBodyBytes, ((Number) problem.getAdditionalData().get("maxBodyBytes")).longValue());
        }
    }

    private static void theDataApiRefusesAnOverLimitRequestWithADataRequestTooLargeProblem() {
        final List<Request> requests = new ArrayList<>();
        final Problem problem = assertThrows(
            Problem.class,
            () -> client(requests, 413, "data_request_too_large", 1_048_576L, 1_114_112L).v1().data().byTable("notes").insert().post(new DataInsertRequest()));
        assertEquals(GATEWAY + "/v1/data/notes/insert", requests.get(0).url().toString());
        assertEquals("data_request_too_large", problem.getCode());
        assertEquals(1_048_576L, ((Number) problem.getAdditionalData().get("limitBytes")).longValue());
        assertEquals(1_114_112L, ((Number) problem.getAdditionalData().get("maxBodyBytes")).longValue());
    }

    /** The generated client over a real Kiota OkHttp adapter whose client answers every call with the same 413 problem. */
    private static YogabaseApiClient client(List<Request> requests, int status, String code, long limitBytes, long maxBodyBytes) {
        final Map<String, Object> document = new LinkedHashMap<>();
        document.put("type", "https://tools.ietf.org/html/rfc9110#section-15.5.14");
        document.put("title", "Payload too large.");
        document.put("status", status);
        document.put("detail", "The request is larger than this route accepts.");
        document.put("code", code);
        document.put("limitBytes", limitBytes);
        document.put("maxBodyBytes", maxBodyBytes);
        final byte[] body = new GsonBuilder().create().toJson(document).getBytes(StandardCharsets.UTF_8);
        final OkHttpClient http = new OkHttpClient.Builder().addInterceptor(chain -> {
            final Request request = chain.request();
            requests.add(request);
            return new Response.Builder().request(request).protocol(Protocol.HTTP_1_1).code(status).message("x")
                .body(ResponseBody.create(body, MediaType.get("application/problem+json"))).build();
        }).build();
        final AccessTokenProvider tokens = new AccessTokenProvider() {
            @Override
            public String getAuthorizationToken(URI uri, Map<String, Object> additionalAuthenticationContext) {
                return "user-token";
            }

            @Override
            public AllowedHostsValidator getAllowedHostsValidator() {
                return new AllowedHostsValidator("apps.example.test");
            }
        };
        final RequestAdapter adapter = new OkHttpRequestAdapter(new BaseBearerTokenAuthenticationProvider(tokens), null, null, http);
        adapter.setBaseUrl(GATEWAY);
        return new YogabaseApiClient(adapter);
    }

    private static void assertEquals(Object expected, Object actual) {
        cases++;
        if (expected == null ? actual != null : !expected.equals(actual)) {
            throw new AssertionError("Expected " + expected + " but got " + actual);
        }
    }

    private static <T extends Throwable> T assertThrows(Class<T> type, Runnable action) {
        cases++;
        try {
            action.run();
        } catch (Throwable thrown) {
            if (type.isInstance(thrown)) {
                return type.cast(thrown);
            }
            throw new AssertionError("Expected " + type.getSimpleName() + " but got " + thrown, thrown);
        }
        throw new AssertionError("Expected " + type.getSimpleName());
    }
}
