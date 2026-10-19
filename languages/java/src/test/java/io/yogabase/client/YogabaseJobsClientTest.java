package io.yogabase.client;

import com.google.gson.GsonBuilder;
import com.microsoft.kiota.RequestAdapter;
import com.microsoft.kiota.authentication.AccessTokenProvider;
import com.microsoft.kiota.authentication.AllowedHostsValidator;
import com.microsoft.kiota.authentication.BaseBearerTokenAuthenticationProvider;
import com.microsoft.kiota.http.OkHttpRequestAdapter;
import java.io.IOException;
import java.lang.reflect.Proxy;
import java.net.URI;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.time.OffsetDateTime;
import java.util.ArrayDeque;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Deque;
import java.util.HexFormat;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import java.util.concurrent.atomic.AtomicReference;
import java.util.function.Function;
import java.util.function.Supplier;
import okhttp3.MediaType;
import okhttp3.OkHttpClient;
import okhttp3.Protocol;
import okhttp3.Request;
import okhttp3.Response;
import okhttp3.ResponseBody;
import okio.Buffer;
import okio.BufferedSource;

/**
 * Executable conformance checks for the authored Java Jobs client. The shapes pinned here are the ones the
 * mirrored client OpenAPI document declares for the Jobs routes ({@code x-yogabase-jobs} and the four
 * operations) and the product's gateway writes. The client builds each request with the generated Kiota request
 * builders and runs over a real Kiota {@code OkHttpRequestAdapter} whose OkHttp client answers every call from a
 * script, so the request that reaches the wire is the one asserted, and it is held to the request the generated
 * builder builds on its own.
 */
public final class YogabaseJobsClientTest {
    private static final String GATEWAY = "https://apps.example.test";
    private static final UUID PROJECT = UUID.fromString("11111111-1111-4111-8111-111111111111");
    private static final UUID JOB = UUID.fromString("5d0a8f0e-3c63-4d5e-9a0b-7c1f3a9e2b41");
    private static final String JOBS = GATEWAY + "/v1/projects/" + PROJECT + "/jobs";
    private static final MediaType JSON = MediaType.get("application/json");
    private static final MediaType PROBLEM = MediaType.get("application/problem+json");
    private static final MediaType OCTETS = MediaType.get("application/octet-stream");
    private static int cases;

    private YogabaseJobsClientTest() {
    }

    public static void main(String[] args) throws Exception {
        submitSendsTheExactRouteBodyAndUserTokenAndReadsTheAcceptedJob();
        submitOmitsAnAbsentOrEmptyPayloadAndAnUnsetAttemptBudget();
        aRetriedSubmitIsTheSameRequestOnTheWireSoTheGatewayCanReplayIt();
        aSubmitAtExactlyTheInlineLimitIsSentAndOneByteMoreIsRefusedBeforeTheNetwork();
        aMalformedSubmitIsRefusedBeforeTheNetwork();
        theLongestJobTypeAndIdempotencyKeyAreSent();
        aNonAsciiKeyTravelsAsValidJson();
        aSubmitTheGatewayRefusesSurfacesItsStatusAndCode();
        aPayloadTooLargeRefusalNamesTheLimitsItCarries();
        aBare413ReadsAsPayloadTooLarge();
        anInstallationWithoutJobsAnswersAPlainNotFound();
        limitsThatAreNotPositiveWholeNumbersAreNotReported();
        getReadsOneJobByItsRouteAndMapsEveryField();
        everyStatusIsReadAndOnlyTheSettledOnesAreTerminal();
        anotherUsersJobAndAnUnknownJobAreTheSameNotFound();
        cancelPostsToTheCancelRouteWithNoBodyAndReadsTheJob();
        aJobIdThatIsNotNonEmptyIsRefusedBeforeTheNetworkOnEveryRoute();
        aJobTheRouteDoesNotDeclareIsRefusedNotGuessedAt();
        aMemberThisClientDoesNotKnowIsIgnored();
        theResultIsReadFromItsRouteAndCheckedAgainstTheDigestTheGatewaySent();
        aResultThatDoesNotHashToItsDigestIsNotReturned();
        aResultWithoutAWellFormedDigestIsNotTrusted();
        aResultWithTwoDigestHeadersIsNotTrusted();
        aResultLargerThanTheBoundIsRefusedDeclaredOrStreamed();
        aSuccessThatIsNotA200IsNotAResult();
        aResultTheGatewayRefusesSurfacesItsStatusAndCodeAndNoBytes();
        theOriginAndTheProjectAreValidatedAndTheApplicationClientHandsOutTheClient();
        anAdapterThatDoesNotHandBackItsNativeResponseIsNamed();
        theRequestsAreTheOnesTheGeneratedBuildersBuild();
        System.out.println("java jobs client tests: PASS (" + cases + " checks)");
    }

    // ---------------------------------------------------------------------------------------
    // Submit
    // ---------------------------------------------------------------------------------------

    private static void submitSendsTheExactRouteBodyAndUserTokenAndReadsTheAcceptedJob() {
        final Script script = new Script(request -> json(request, 202, jobJson(Map.of())));
        final YogabaseJob job = script.client().submit(
            new YogabaseJobSubmitRequest("orders-create", "order-42", "hello".getBytes(StandardCharsets.UTF_8), 3));

        final Request request = script.requests.get(0);
        assertEquals("POST", request.method());
        assertEquals(JOBS, request.url().toString());
        assertEquals("Bearer user-token", request.header("Authorization"));
        assertEquals("application/json", request.header("Content-Type") == null ? null : request.header("Content-Type").split(";")[0]);
        assertTrue(request.header("Accept").contains("application/json"));
        // The names are the gateway's submission, which refuses any other member; the member order is the
        // generated serializer's, so the body is compared as the JSON document it is.
        assertJson(
            "{\"jobType\":\"orders-create\",\"idempotencyKey\":\"order-42\",\"payloadBase64\":\"aGVsbG8=\",\"maxAttempts\":3}",
            script.bodyText(0));
        // The key rides in the body: the gateway reads no Idempotency-Key header on this route.
        assertEquals(null, request.header("Idempotency-Key"));
        assertEquals(JOB, job.jobId());
        assertEquals("orders-create", job.jobType());
        assertEquals(YogabaseJobStatus.QUEUED, job.status());
        assertEquals(0, job.attempt());
        assertEquals(5, job.maxAttempts());
        assertEquals(0, job.progressPercent());
        assertEquals(false, job.cancellationRequested());
        assertEquals(null, job.resultReference());
        assertEquals(null, job.failureCode());
        assertEquals(OffsetDateTime.parse("2026-10-03T05:00:00+00:00"), job.createdAt());
        assertEquals(null, job.completedAt());
        assertEquals(false, job.isTerminal());
    }

    private static void submitOmitsAnAbsentOrEmptyPayloadAndAnUnsetAttemptBudget() {
        final Script script = new Script(request -> json(request, 202, jobJson(Map.of("jobType", "emails-send"))));
        final YogabaseJobsClient jobs = script.client();
        jobs.submit(new YogabaseJobSubmitRequest("emails-send", "k"));
        jobs.submit(new YogabaseJobSubmitRequest("emails-send", "k", new byte[0], null));
        // A job with no payload and one with an empty payload are the same job to the gateway.
        assertJson("{\"jobType\":\"emails-send\",\"idempotencyKey\":\"k\"}", script.bodyText(0));
        assertJson("{\"jobType\":\"emails-send\",\"idempotencyKey\":\"k\"}", script.bodyText(1));
    }

    private static void aRetriedSubmitIsTheSameRequestOnTheWireSoTheGatewayCanReplayIt() {
        final Script script = new Script(
            request -> json(request, 202, jobJson(Map.of())),
            request -> json(request, 202, jobJson(Map.of("status", "Running"))));
        final YogabaseJobsClient jobs = script.client();
        final YogabaseJobSubmitRequest request = new YogabaseJobSubmitRequest("orders-create", "order-42", new byte[] {1, 2, 3}, null);
        final YogabaseJob first = jobs.submit(request);
        final YogabaseJob replay = jobs.submit(request);
        assertEquals(script.bodyText(0), script.bodyText(1));
        assertEquals(first.jobId(), replay.jobId());
        // 202 again, whatever the job has become.
        assertEquals(YogabaseJobStatus.RUNNING, replay.status());
    }

    private static void aSubmitAtExactlyTheInlineLimitIsSentAndOneByteMoreIsRefusedBeforeTheNetwork() {
        final Script script = new Script(request -> json(request, 202, jobJson(Map.of())));
        final YogabaseJobsClient jobs = script.client();
        jobs.submit(new YogabaseJobSubmitRequest("orders-create", "k-max", new byte[1_048_576], null));
        final String body = script.bodyText(0);
        final int start = body.indexOf("\"payloadBase64\":\"") + "\"payloadBase64\":\"".length();
        assertEquals(1_398_104, body.indexOf('"', start) - start);
        assertThrows(
            IllegalArgumentException.class,
            () -> jobs.submit(new YogabaseJobSubmitRequest("orders-create", "k-over", new byte[1_048_577], null)));
        assertEquals(1, script.requests.size());
    }

    private static void aMalformedSubmitIsRefusedBeforeTheNetwork() {
        final Script script = new Script(request -> json(request, 202, jobJson(Map.of())));
        final YogabaseJobsClient jobs = script.client();
        final List<YogabaseJobSubmitRequest> cases = new ArrayList<>();
        for (final String jobType : new String[] {
            "", "   ", " orders-create", "orders-create ", "orders create", "orders/create", "orders\ncreate", "ordérs", "a".repeat(129) }) {
            cases.add(new YogabaseJobSubmitRequest(jobType, "k"));
        }
        for (final String key : new String[] { "", "   ", " padded", "padded ", "has\ncontrol", "k".repeat(257) }) {
            cases.add(new YogabaseJobSubmitRequest("orders-create", key));
        }
        for (final int attempts : new int[] { 0, -1, 11 }) {
            cases.add(new YogabaseJobSubmitRequest("orders-create", "k", null, attempts));
        }
        cases.add(new YogabaseJobSubmitRequest(null, "k"));
        cases.add(new YogabaseJobSubmitRequest("orders-create", null));
        for (final YogabaseJobSubmitRequest malformed : cases) {
            assertThrows(IllegalArgumentException.class, () -> jobs.submit(malformed));
        }
        assertThrows(NullPointerException.class, () -> jobs.submit(null));
        assertEquals(0, script.requests.size());
    }

    private static void theLongestJobTypeAndIdempotencyKeyAreSent() {
        final Script script = new Script(request -> json(request, 202, jobJson(Map.of("jobType", "a".repeat(128)))));
        script.client().submit(new YogabaseJobSubmitRequest("a".repeat(128), "k".repeat(256)));
        assertEquals(1, script.requests.size());
    }

    private static void aNonAsciiKeyTravelsAsValidJson() {
        final Script script = new Script(request -> json(request, 202, jobJson(Map.of())));
        script.client().submit(new YogabaseJobSubmitRequest("orders-create", "주문-42\"\\"));
        final String body = script.bodyText(0);
        // The generated serializer writes the key as UTF-8 text, which is smaller than the six-byte escape the
        // gateway's body limit allows for every character, and it escapes the quote and the backslash.
        assertTrue(body.contains("주문-42"));
        assertEquals("주문-42\"\\", com.google.gson.JsonParser.parseString(body).getAsJsonObject().get("idempotencyKey").getAsString());
    }

    // ---------------------------------------------------------------------------------------
    // Refusals
    // ---------------------------------------------------------------------------------------

    private static void aSubmitTheGatewayRefusesSurfacesItsStatusAndCode() {
        final Object[][] refusals = {
            { 409, YogabaseJobErrorCodes.IDEMPOTENCY_CONFLICT, "The idempotency key belongs to a different job request." },
            { 422, YogabaseJobErrorCodes.TYPE_NOT_REGISTERED, "The job type is not registered." },
            { 400, YogabaseJobErrorCodes.REQUEST_INVALID, "Invalid job request." },
            { 403, YogabaseJobErrorCodes.ACCESS_DENIED, "Job access denied." },
            { 503, YogabaseJobErrorCodes.UNAVAILABLE, "Job persistence unavailable." },
        };
        for (final Object[] refusal : refusals) {
            final Script script = new Script(request -> problem(request, (Integer) refusal[0], (String) refusal[2], (String) refusal[1], Map.of()));
            final YogabaseJobsException exception = assertThrows(
                YogabaseJobsException.class, () -> script.client().submit(new YogabaseJobSubmitRequest("orders-create", "k")));
            assertEquals(refusal[0], exception.status());
            assertEquals(refusal[1], exception.code());
            assertEquals(refusal[2], exception.getMessage());
            assertEquals(null, exception.limitBytes());
            assertEquals(null, exception.maxBodyBytes());
        }
    }

    private static void aPayloadTooLargeRefusalNamesTheLimitsItCarries() {
        final Map<String, Object> extras = new LinkedHashMap<>();
        extras.put("detail", "A job payload may be at most 1048576 bytes before base64 encoding, and the whole request body at most 8391225 bytes.");
        extras.put("limitBytes", 1_048_576);
        extras.put("maxBodyBytes", 8_391_225);
        final Script script = new Script(request -> problem(request, 413, "The job payload is too large.", YogabaseJobErrorCodes.PAYLOAD_TOO_LARGE, extras));
        final YogabaseJobsException exception = assertThrows(
            YogabaseJobsException.class, () -> script.client().submit(new YogabaseJobSubmitRequest("orders-create", "k", new byte[] {1}, null)));
        assertEquals(413, exception.status());
        assertEquals("job_payload_too_large", exception.code());
        assertEquals(1_048_576L, exception.limitBytes());
        assertEquals(8_391_225L, exception.maxBodyBytes());
        assertTrue(exception.getMessage().contains("8391225"));
    }

    private static void aBare413ReadsAsPayloadTooLarge() {
        final Script script = new Script(request -> empty(request, 413));
        final YogabaseJobsException exception = assertThrows(
            YogabaseJobsException.class, () -> script.client().submit(new YogabaseJobSubmitRequest("orders-create", "k")));
        assertEquals(413, exception.status());
        assertEquals("payload_too_large", exception.code());
        assertEquals(null, exception.limitBytes());
    }

    private static void anInstallationWithoutJobsAnswersAPlainNotFound() {
        final Script script = new Script(request -> empty(request, 404));
        final YogabaseJobsException exception = assertThrows(YogabaseJobsException.class, () -> script.client().get(JOB));
        assertEquals(404, exception.status());
        assertEquals("not_found", exception.code());
        assertTrue(!YogabaseJobErrorCodes.NOT_FOUND.equals(exception.code()));
    }

    private static void limitsThatAreNotPositiveWholeNumbersAreNotReported() {
        final Map<String, Object> extras = new LinkedHashMap<>();
        extras.put("limitBytes", -1);
        extras.put("maxBodyBytes", "8391225");
        final Script script = new Script(request -> problem(request, 413, "x", YogabaseJobErrorCodes.PAYLOAD_TOO_LARGE, extras));
        final YogabaseJobsException exception = assertThrows(
            YogabaseJobsException.class, () -> script.client().submit(new YogabaseJobSubmitRequest("orders-create", "k")));
        assertEquals(null, exception.limitBytes());
        assertEquals(null, exception.maxBodyBytes());
    }

    // ---------------------------------------------------------------------------------------
    // Get and cancel
    // ---------------------------------------------------------------------------------------

    private static void getReadsOneJobByItsRouteAndMapsEveryField() {
        final Map<String, Object> overrides = new LinkedHashMap<>();
        overrides.put("status", "Succeeded");
        overrides.put("attempt", 2);
        overrides.put("maxAttempts", 3);
        overrides.put("progressPercent", 100);
        overrides.put("resultReference", "object://invocation-payload/abc/def");
        overrides.put("failureCode", "handler_retry");
        overrides.put("completedAt", "2026-10-03T05:00:09.1234567+00:00");
        final Script script = new Script(request -> json(request, 200, jobJson(overrides)));
        final YogabaseJob job = script.client().get(JOB);
        final Request request = script.requests.get(0);
        assertEquals("GET", request.method());
        assertEquals(JOBS + "/" + JOB, request.url().toString());
        assertEquals("Bearer user-token", request.header("Authorization"));
        assertEquals(null, request.body());
        assertEquals(YogabaseJobStatus.SUCCEEDED, job.status());
        assertEquals(2, job.attempt());
        assertEquals(3, job.maxAttempts());
        assertEquals(100, job.progressPercent());
        assertEquals("object://invocation-payload/abc/def", job.resultReference());
        assertEquals("handler_retry", job.failureCode());
        assertEquals(OffsetDateTime.parse("2026-10-03T05:00:09.1234567+00:00"), job.completedAt());
        assertEquals(true, job.isTerminal());
    }

    private static void everyStatusIsReadAndOnlyTheSettledOnesAreTerminal() {
        final Object[][] statuses = {
            { "Queued", YogabaseJobStatus.QUEUED, false }, { "Running", YogabaseJobStatus.RUNNING, false },
            { "Failed", YogabaseJobStatus.FAILED, false }, { "Succeeded", YogabaseJobStatus.SUCCEEDED, true },
            { "Cancelled", YogabaseJobStatus.CANCELLED, true }, { "DeadLettered", YogabaseJobStatus.DEAD_LETTERED, true },
        };
        for (final Object[] status : statuses) {
            final Script script = new Script(request -> json(request, 200, jobJson(Map.of("status", status[0]))));
            final YogabaseJob job = script.client().get(JOB);
            assertEquals(status[1], job.status());
            assertEquals(status[2], job.isTerminal());
            assertEquals(status[0], job.status().wireName());
        }
    }

    private static void anotherUsersJobAndAnUnknownJobAreTheSameNotFound() {
        final Script script = new Script(request -> problem(request, 404, "Job not found.", YogabaseJobErrorCodes.NOT_FOUND, Map.of()));
        final YogabaseJobsClient jobs = script.client();
        for (final Supplier<YogabaseJob> call : List.<Supplier<YogabaseJob>>of(() -> jobs.get(JOB), () -> jobs.cancel(JOB))) {
            final YogabaseJobsException exception = assertThrows(YogabaseJobsException.class, call::get);
            assertEquals(404, exception.status());
            assertEquals("job_not_found", exception.code());
        }
    }

    private static void cancelPostsToTheCancelRouteWithNoBodyAndReadsTheJob() throws IOException {
        final Script script = new Script(request -> json(request, 200, jobJson(Map.of("status", "Running", "cancellationRequested", true))));
        final YogabaseJob job = script.client().cancel(JOB);
        final Request request = script.requests.get(0);
        assertEquals("POST", request.method());
        assertEquals(JOBS + "/" + JOB + "/cancel", request.url().toString());
        assertEquals("Bearer user-token", request.header("Authorization"));
        assertEquals(0, script.bodies.get(0).length);
        // A running job is flagged and stopped by its worker, so the flag can be set while it still runs.
        assertEquals(true, job.cancellationRequested());
        assertEquals(YogabaseJobStatus.RUNNING, job.status());
    }

    private static void aJobIdThatIsNotNonEmptyIsRefusedBeforeTheNetworkOnEveryRoute() {
        final Script script = new Script(request -> json(request, 200, jobJson(Map.of())));
        final YogabaseJobsClient jobs = script.client();
        final UUID empty = new UUID(0L, 0L);
        assertThrows(IllegalArgumentException.class, () -> jobs.get(empty));
        assertThrows(IllegalArgumentException.class, () -> jobs.cancel(empty));
        assertThrows(IllegalArgumentException.class, () -> jobs.result(empty));
        assertThrows(NullPointerException.class, () -> jobs.get(null));
        assertEquals(0, script.requests.size());
    }

    // ---------------------------------------------------------------------------------------
    // Response validation
    // ---------------------------------------------------------------------------------------

    private static void aJobTheRouteDoesNotDeclareIsRefusedNotGuessedAt() {
        final List<String> documents = new ArrayList<>();
        for (final Map<String, Object> overrides : List.<Map<String, Object>>of(
            Map.of("status", "Paused"), Map.of("status", 1), Map.of("status", "queued"),
            Map.of("maxAttempts", 0), Map.of("maxAttempts", 11), Map.of("attempt", -1), Map.of("attempt", 1.5),
            Map.of("progressPercent", 101), Map.of("progressPercent", -1),
            Map.of("jobId", "not-a-guid"), Map.of("jobId", "00000000-0000-0000-0000-000000000000"),
            Map.of("jobType", "has space"), Map.of("jobType", ""), Map.of("failureCode", "Upper_Case"),
            Map.of("resultReference", "https://elsewhere.example/result"), Map.of("resultReference", "object://"),
            Map.of("resultReference", "object://with space"), Map.of("cancellationRequested", "false"),
            Map.of("createdAt", "yesterday"), Map.of("completedAt", "later"))) {
            documents.add(jobJson(overrides));
        }
        documents.addAll(List.of("[]", "null", "\"a string\"", "not json", "{}"));
        for (final String document : documents) {
            final Script script = new Script(request -> json(request, 200, document));
            final YogabaseJobsException exception = assertThrows(YogabaseJobsException.class, () -> script.client().get(JOB));
            assertEquals(502, exception.status());
            assertEquals("job_response_invalid", exception.code());
        }
    }

    private static void aMemberThisClientDoesNotKnowIsIgnored() {
        final Script script = new Script(request -> json(request, 200, jobJson(Map.of("futureMember", Map.of("nested", true)))));
        assertEquals(JOB, script.client().get(JOB).jobId());
    }

    // ---------------------------------------------------------------------------------------
    // Result
    // ---------------------------------------------------------------------------------------

    private static void theResultIsReadFromItsRouteAndCheckedAgainstTheDigestTheGatewaySent() {
        for (final int length : new int[] { 1, 81_920, 300_001 }) {
            final byte[] content = new byte[length];
            for (int index = 0; index < length; index++) {
                content[index] = (byte) ((index * 31 + length) % 251);
            }
            final Script script = new Script(request -> octets(request, 200, content, sha256(content)));
            final YogabaseJobResult result = script.client().result(JOB);
            final Request request = script.requests.get(0);
            assertEquals("GET", request.method());
            assertEquals(JOBS + "/" + JOB + "/result", request.url().toString());
            assertEquals("Bearer user-token", request.header("Authorization"));
            assertEquals("application/octet-stream, application/problem+json", request.header("Accept"));
            assertTrue(Arrays.equals(content, result.content()));
            assertEquals(sha256(content), result.digest());
        }
    }

    private static void aResultThatDoesNotHashToItsDigestIsNotReturned() {
        final byte[] served = "what the store held".getBytes(StandardCharsets.UTF_8);
        final Script script = new Script(request -> octets(request, 200, served, sha256("what the job recorded".getBytes(StandardCharsets.UTF_8))));
        final YogabaseJobsException exception = assertThrows(YogabaseJobsException.class, () -> script.client().result(JOB));
        assertEquals(502, exception.status());
        assertEquals("job_result_digest_mismatch", exception.code());
        assertTrue(!exception.getMessage().contains("what the store held"));
    }

    private static void aResultWithoutAWellFormedDigestIsNotTrusted() {
        final byte[] content = "hello".getBytes(StandardCharsets.UTF_8);
        final String right = sha256(content).substring("sha256:".length());
        final List<String> digests = new ArrayList<>(Arrays.asList(
            null, "", "sha256:", "SHA256:" + right, "sha256:" + right.toUpperCase(java.util.Locale.ROOT),
            "sha256:" + right.substring(0, right.length() - 1), "sha256:" + right + "4", "sha1:" + right, right,
            "sha256:z" + right.substring(1)));
        for (final String digest : digests) {
            final Script script = new Script(request -> octets(request, 200, content, digest));
            final YogabaseJobsException exception = assertThrows(YogabaseJobsException.class, () -> script.client().result(JOB));
            assertEquals(502, exception.status());
            assertEquals("job_response_invalid", exception.code());
        }
    }

    private static void aResultWithTwoDigestHeadersIsNotTrusted() {
        final byte[] content = "hello".getBytes(StandardCharsets.UTF_8);
        final String digest = sha256(content);
        final Script script = new Script(request -> new Response.Builder()
            .request(request).protocol(Protocol.HTTP_1_1).code(200).message("OK")
            .addHeader(YogabaseJobsClient.RESULT_DIGEST_HEADER, digest)
            .addHeader(YogabaseJobsClient.RESULT_DIGEST_HEADER, digest)
            .body(ResponseBody.create(content, OCTETS)).build());
        final YogabaseJobsException exception = assertThrows(YogabaseJobsException.class, () -> script.client().result(JOB));
        assertEquals("job_response_invalid", exception.code());
    }

    private static void aResultLargerThanTheBoundIsRefusedDeclaredOrStreamed() {
        final byte[] one = {1};
        final Script declared = new Script(request -> new Response.Builder()
            .request(request).protocol(Protocol.HTTP_1_1).code(200).message("OK")
            .header(YogabaseJobsClient.RESULT_DIGEST_HEADER, sha256(one))
            .body(new ResponseBody() {
                @Override
                public MediaType contentType() {
                    return OCTETS;
                }

                @Override
                public long contentLength() {
                    return YogabaseJobsClient.MAXIMUM_RESULT_BYTES + 1L;
                }

                @Override
                public BufferedSource source() {
                    return new Buffer().write(one);
                }
            }).build());
        assertEquals("job_response_invalid", assertThrows(YogabaseJobsException.class, () -> declared.client().result(JOB)).code());

        final byte[] oversized = new byte[YogabaseJobsClient.MAXIMUM_RESULT_BYTES + 1];
        final Script streamed = new Script(request -> new Response.Builder()
            .request(request).protocol(Protocol.HTTP_1_1).code(200).message("OK")
            .header(YogabaseJobsClient.RESULT_DIGEST_HEADER, sha256(one))
            .body(new ResponseBody() {
                @Override
                public MediaType contentType() {
                    return OCTETS;
                }

                @Override
                public long contentLength() {
                    return -1L;
                }

                @Override
                public BufferedSource source() {
                    return new Buffer().write(oversized);
                }
            }).build());
        assertEquals("job_response_invalid", assertThrows(YogabaseJobsException.class, () -> streamed.client().result(JOB)).code());
    }

    private static void aSuccessThatIsNotA200IsNotAResult() {
        final byte[] content = "hello".getBytes(StandardCharsets.UTF_8);
        final Script script = new Script(request -> octets(request, 202, content, sha256(content)));
        assertEquals("job_response_invalid", assertThrows(YogabaseJobsException.class, () -> script.client().result(JOB)).code());
    }

    private static void aResultTheGatewayRefusesSurfacesItsStatusAndCodeAndNoBytes() {
        final Object[][] refusals = {
            { 409, YogabaseJobErrorCodes.RESULT_NOT_READY, "The job has not settled, so it has no result yet." },
            { 404, YogabaseJobErrorCodes.RESULT_NOT_FOUND, "Job result not found." },
            { 502, YogabaseJobErrorCodes.RESULT_INTEGRITY_FAILED, "The stored result does not match what the job recorded." },
            { 500, YogabaseJobErrorCodes.RESULT_REFERENCE_INVALID, "The job's stored result is not its own." },
            { 503, YogabaseJobErrorCodes.UNAVAILABLE, "Job persistence unavailable." },
        };
        for (final Object[] refusal : refusals) {
            final Script script = new Script(request -> problem(request, (Integer) refusal[0], (String) refusal[2], (String) refusal[1], Map.of()));
            final YogabaseJobsException exception = assertThrows(YogabaseJobsException.class, () -> script.client().result(JOB));
            assertEquals(refusal[0], exception.status());
            assertEquals(refusal[1], exception.code());
            assertEquals(refusal[2], exception.getMessage());
        }
    }

    // ---------------------------------------------------------------------------------------
    // Construction
    // ---------------------------------------------------------------------------------------

    private static void theOriginAndTheProjectAreValidatedAndTheApplicationClientHandsOutTheClient() {
        final Script script = new Script(request -> json(request, 200, jobJson(Map.of())));
        final RequestAdapter adapter = script.adapter();
        for (final String origin : new String[] { "https://apps.example.test/not-an-origin", "https://user:pass@apps.example.test", "https://apps.example.test/?q=1", "ftp://apps.example.test" }) {
            assertThrows(IllegalArgumentException.class, () -> new YogabaseJobsClient(adapter, URI.create(origin), PROJECT));
        }
        assertThrows(IllegalArgumentException.class, () -> new YogabaseJobsClient(adapter, URI.create(GATEWAY), new UUID(0L, 0L)));
        assertThrows(NullPointerException.class, () -> new YogabaseJobsClient(null, URI.create(GATEWAY), PROJECT));
        assertThrows(NullPointerException.class, () -> new YogabaseJobsClient(adapter, URI.create(GATEWAY), null));

        final YogabaseJobsClient direct = new YogabaseJobsClient(adapter, URI.create(GATEWAY + "/"), PROJECT);
        assertEquals(URI.create(GATEWAY), direct.origin());
        assertEquals(PROJECT, direct.projectId());

        // The application client hands out a jobs client over its own adapter and origin.
        final YogabaseApplicationClient application = new YogabaseApplicationClient(adapter, URI.create(GATEWAY));
        final YogabaseJobsClient shared = application.jobs(PROJECT);
        assertEquals(application.origin(), shared.origin());
        shared.get(JOB);
        assertEquals(JOBS + "/" + JOB, script.requests.get(0).url().toString());
    }

    private static void anAdapterThatDoesNotHandBackItsNativeResponseIsNamed() {
        final AtomicReference<String> baseUrl = new AtomicReference<>();
        final RequestAdapter adapter = (RequestAdapter) Proxy.newProxyInstance(
            RequestAdapter.class.getClassLoader(),
            new Class<?>[] { RequestAdapter.class },
            (proxy, method, arguments) -> switch (method.getName()) {
                case "setBaseUrl" -> { baseUrl.set((String) arguments[0]); yield null; }
                case "getBaseUrl" -> baseUrl.get();
                default -> null;
            });
        final IllegalStateException exception = assertThrows(
            IllegalStateException.class, () -> new YogabaseJobsClient(adapter, URI.create(GATEWAY), PROJECT).get(JOB));
        assertTrue(exception.getMessage().contains("OkHttpRequestAdapter"));
    }

    // ---------------------------------------------------------------------------------------
    // The authored requests are the generated builders' requests
    // ---------------------------------------------------------------------------------------

    private static void theRequestsAreTheOnesTheGeneratedBuildersBuild() {
        final io.yogabase.client.models.JobSubmitRequest body = new io.yogabase.client.models.JobSubmitRequest();
        body.setJobType("orders-create");
        body.setIdempotencyKey("order-42");
        body.setPayloadBase64("aGVsbG8=");
        body.setMaxAttempts(3);
        assertSameRequest(
            sentBy(client -> client.submit(new YogabaseJobSubmitRequest("orders-create", "order-42", "hello".getBytes(StandardCharsets.UTF_8), 3))),
            builtBy(api -> api.v1().projects().byProjectId(PROJECT).jobs().post(body)));
        assertSameRequest(
            sentBy(client -> client.get(JOB)),
            builtBy(api -> api.v1().projects().byProjectId(PROJECT).jobs().byJobId(JOB).get()));
        assertSameRequest(
            sentBy(client -> client.cancel(JOB)),
            builtBy(api -> api.v1().projects().byProjectId(PROJECT).jobs().byJobId(JOB).cancel().post()));
        assertSameRequest(
            sentBy(client -> client.result(JOB)),
            builtBy(api -> api.v1().projects().byProjectId(PROJECT).jobs().byJobId(JOB).result().get()));
    }

    private record Sent(Request request, String body) {
    }

    private static Sent sentBy(java.util.function.Consumer<YogabaseJobsClient> call) {
        final Script script = new Script(request -> json(request, 200, "{}"));
        try {
            call.accept(script.client());
        } catch (RuntimeException expected) {
            // The canned answer is not a job; only the request is compared.
        }
        assertEquals(1, script.requests.size());
        return new Sent(script.requests.get(0), script.bodyText(0));
    }

    private static Sent builtBy(java.util.function.Consumer<YogabaseApiClient> call) {
        final Script script = new Script(request -> json(request, 200, "{}"));
        final RequestAdapter adapter = script.adapter();
        adapter.setBaseUrl(GATEWAY);
        try {
            call.accept(new YogabaseApiClient(adapter));
        } catch (RuntimeException expected) {
            // The canned answer is not a job; only the request is compared.
        }
        assertEquals(1, script.requests.size());
        return new Sent(script.requests.get(0), script.bodyText(0));
    }

    private static void assertSameRequest(Sent sent, Sent built) {
        assertEquals(built.request().method(), sent.request().method());
        assertEquals(built.request().url().toString(), sent.request().url().toString());
        if (!built.body().isEmpty() || !sent.body().isEmpty()) {
            assertJson(built.body(), sent.body());
        }
        // The authored client sends exactly what the generated builder asks to accept.
        assertEquals(built.request().header("Accept"), sent.request().header("Accept"));
    }

    // ---------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------

    /** A scripted OkHttp client behind a real Kiota adapter: each call is recorded and answered by the next responder. */
    private static final class Script {
        final List<Request> requests = new ArrayList<>();
        final List<byte[]> bodies = new ArrayList<>();
        private final Deque<Function<Request, Response>> responders = new ArrayDeque<>();

        @SafeVarargs
        Script(Function<Request, Response>... responders) {
            this.responders.addAll(Arrays.asList(responders));
        }

        RequestAdapter adapter() {
            final OkHttpClient http = new OkHttpClient.Builder().addInterceptor(chain -> {
                final Request request = chain.request();
                requests.add(request);
                final Buffer buffer = new Buffer();
                if (request.body() != null) {
                    request.body().writeTo(buffer);
                }
                bodies.add(buffer.readByteArray());
                final Function<Request, Response> responder = responders.size() > 1 ? responders.removeFirst() : responders.peekFirst();
                return responder.apply(request);
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
            return new OkHttpRequestAdapter(new BaseBearerTokenAuthenticationProvider(tokens), null, null, http);
        }

        YogabaseJobsClient client() {
            return new YogabaseJobsClient(adapter(), URI.create(GATEWAY), PROJECT);
        }

        String bodyText(int index) {
            return new String(bodies.get(index), StandardCharsets.UTF_8);
        }
    }

    private static String jobJson(Map<String, Object> overrides) {
        final Map<String, Object> document = new LinkedHashMap<>();
        document.put("jobId", JOB.toString());
        document.put("jobType", "orders-create");
        document.put("status", "Queued");
        document.put("attempt", 0);
        document.put("maxAttempts", 5);
        document.put("progressPercent", 0);
        document.put("cancellationRequested", false);
        document.put("resultReference", null);
        document.put("failureCode", null);
        document.put("createdAt", "2026-10-03T05:00:00+00:00");
        document.put("updatedAt", "2026-10-03T05:00:01+00:00");
        document.put("completedAt", null);
        document.putAll(overrides);
        return new GsonBuilder().serializeNulls().create().toJson(document);
    }

    private static Response json(Request request, int code, String body) {
        return new Response.Builder().request(request).protocol(Protocol.HTTP_1_1).code(code).message("x")
            .body(ResponseBody.create(body.getBytes(StandardCharsets.UTF_8), JSON)).build();
    }

    // What ASP.NET Core writes for Results.Problem(statusCode, title, detail, extensions): the code and the
    // limits are top-level members beside type, title, status and traceId.
    private static Response problem(Request request, int status, String title, String code, Map<String, Object> extras) {
        final Map<String, Object> document = new LinkedHashMap<>();
        document.put("type", "https://tools.ietf.org/html/rfc9110");
        document.put("title", title);
        document.put("status", status);
        document.put("traceId", "00-trace");
        document.put("code", code);
        document.putAll(extras);
        return new Response.Builder().request(request).protocol(Protocol.HTTP_1_1).code(status).message("x")
            .body(ResponseBody.create(new GsonBuilder().create().toJson(document).getBytes(StandardCharsets.UTF_8), PROBLEM)).build();
    }

    private static Response empty(Request request, int code) {
        return new Response.Builder().request(request).protocol(Protocol.HTTP_1_1).code(code).message("x")
            .body(ResponseBody.create(new byte[0], null)).build();
    }

    private static Response octets(Request request, int code, byte[] content, String digest) {
        final Response.Builder builder = new Response.Builder().request(request).protocol(Protocol.HTTP_1_1).code(code).message("x")
            .header("Cache-Control", "private, no-store")
            .body(ResponseBody.create(content, OCTETS));
        if (digest != null) {
            builder.header(YogabaseJobsClient.RESULT_DIGEST_HEADER, digest);
        }
        return builder.build();
    }

    private static String sha256(byte[] content) {
        try {
            return "sha256:" + HexFormat.of().formatHex(MessageDigest.getInstance("SHA-256").digest(content));
        } catch (java.security.NoSuchAlgorithmException exception) {
            throw new IllegalStateException(exception);
        }
    }

    private static void assertTrue(boolean value) {
        cases++;
        if (!value) {
            throw new AssertionError("Expected true");
        }
    }

    private static void assertJson(String expected, String actual) {
        cases++;
        if (!com.google.gson.JsonParser.parseString(expected).equals(com.google.gson.JsonParser.parseString(actual))) {
            throw new AssertionError("Expected the JSON document " + expected + " but got " + actual);
        }
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
