package io.stackyoga.yogabase;

import com.google.gson.GsonBuilder;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import com.microsoft.kiota.RequestAdapter;
import com.microsoft.kiota.authentication.AccessTokenProvider;
import com.microsoft.kiota.authentication.AllowedHostsValidator;
import com.microsoft.kiota.authentication.BaseBearerTokenAuthenticationProvider;
import com.microsoft.kiota.http.OkHttpRequestAdapter;
import io.stackyoga.yogabase.models.ConcurrencyPolicy;
import io.stackyoga.yogabase.models.MisfirePolicy;
import io.stackyoga.yogabase.models.ScheduleDefinitionRequest;
import io.stackyoga.yogabase.models.ScheduleTriggerRequest;
import java.lang.reflect.Proxy;
import java.net.URI;
import java.nio.charset.StandardCharsets;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.util.ArrayDeque;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Deque;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import java.util.function.Function;
import okhttp3.MediaType;
import okhttp3.OkHttpClient;
import okhttp3.Protocol;
import okhttp3.Request;
import okhttp3.Response;
import okhttp3.ResponseBody;
import okio.Buffer;

/**
 * Executable conformance checks for the authored Java schedule client. The shapes pinned here are the ones the
 * mirrored client OpenAPI document declares for the schedule routes ({@code x-yogabase-schedules} and the four
 * operations) and the product's gateway writes. The client builds each request with the generated Kiota request
 * builders and runs over a real Kiota {@code OkHttpRequestAdapter} whose OkHttp client answers every call from a
 * script, so the request that reaches the wire is the one asserted, and it is held to the request the generated
 * builder builds on its own.
 */
public final class YogabaseSchedulesClientTest {
    private static final String GATEWAY = "https://apps.example.test";
    private static final UUID PROJECT = UUID.fromString("11111111-1111-4111-8111-111111111111");
    private static final String INSTANCE = "01234567-89ab-cdef-0123-456789abcdef";
    private static final String INSTANCE_HEX = "0123456789abcdef0123456789abcdef";
    private static final String ETAG3 = "\"schedule-v3-" + INSTANCE_HEX + "\"";
    private static final String ITEM = GATEWAY + "/v1/projects/" + PROJECT + "/schedules/nightly";
    private static final String RUN = ITEM + "/trigger";
    private static final String INVOCATION = "9b9e2f3a-0d6e-4e6c-a4f1-2c6a7a5f0b11";
    private static final MediaType JSON = MediaType.get("application/json");
    private static final MediaType PROBLEM = MediaType.get("application/problem+json");
    private static int cases;

    private YogabaseSchedulesClientTest() {
    }

    public static void main(String[] args) throws Exception {
        createSendsAConditionalPutOfTheExactBodyAndReadsTheScheduleAndItsETag();
        updateSendsTheETagAsIfMatchAndNeverIfNoneMatch();
        aDefinitionOmitsAnAbsentOrEmptyPayloadAndSendsAOneShotsInstantInUtc();
        everyPolicyIsSentByItsName();
        aPayloadAtExactlyTheInlineLimitIsSentAndOneByteMoreIsRefused();
        aMalformedDefinitionIsRefusedBeforeTheNetwork();
        theLongestAcceptedValuesOfEveryBoundedMemberAreSent();
        aScheduleIdThatNeedsEscapingTravelsAsOneEscapedPathSegment();
        anETagThatIsNotTheGatewaysTokenIsRefusedBeforeTheNetwork();
        theLargestVersionThatFitsA64BitIntegerIsAnAcceptedETag();
        getReadsOneScheduleByItsRouteAndBuildsItsETagFromTheBody();
        getMapsAOneShotsInstantAndTheNullMembersOfARecurringSchedule();
        aStrongETagHeaderThatDisagreesWithTheBodyIsRefusedAndAWeakRepeatedOrMissingOneIsIgnored();
        aScheduleTheGatewayDescribesWronglyIsRefused();
        anAnswerThatIsNotAScheduleDocumentIsRefused();
        aMemberThisClientDoesNotKnowIsIgnored();
        deleteSendsIfMatchWithNoBodyAndAnswers204();
        triggerSendsTheExactBodyWithIfMatchAndReadsBothOutcomes();
        aMalformedRunIsRefusedBeforeTheNetwork();
        aRunTheGatewayDescribesWronglyIsRefused();
        everyRefusalReadsAsItsStatusWhereTheGatewayNamesNoCode();
        aPayloadTooLargeRefusalNamesTheCodeAndBothLimits();
        aBare413ReadsAsPayloadTooLarge();
        limitsTheRefusalStatesBadlyAreNotTrusted();
        anInstallationWithoutTheSchedulerAnswersABareNotFoundOnEveryRoute();
        theOriginAndTheProjectAreValidatedAndTheApplicationClientHandsOutTheClient();
        anAdapterThatDoesNotHandBackItsNativeResponseIsNamed();
        theRequestsAreTheOnesTheGeneratedBuildersBuild();
        System.out.println("java schedules client tests: PASS (" + cases + " checks)");
    }

    // ---------------------------------------------------------------------------------------
    // Create and update
    // ---------------------------------------------------------------------------------------

    private static void createSendsAConditionalPutOfTheExactBodyAndReadsTheScheduleAndItsETag() {
        final Script script = new Script(request -> schedule(request, 201, scheduleJson(Map.of("version", "1")), "\"schedule-v1-" + INSTANCE_HEX + "\""));
        final YogabaseSchedule created = script.client().create(definition(Map.of()));

        final Request request = script.requests.get(0);
        assertEquals("PUT", request.method());
        assertEquals(ITEM, request.url().toString());
        assertEquals("Bearer user-token", request.header("Authorization"));
        assertEquals("application/json", request.header("Content-Type").split(";")[0]);
        // Exactly one of the two preconditions: a create is conditional on there being nothing there.
        assertEquals("*", request.header("If-None-Match"));
        assertEquals(null, request.header("If-Match"));
        // The names are the gateway's ScheduledLambdaDefinitionRequest, which refuses any other member; the member
        // order is the generated serializer's, so the body is compared as the JSON document it is.
        assertJson(
            "{\"scheduleId\":\"nightly\",\"functionId\":\"orders-nightly\",\"functionVersion\":\"1\",\"cron\":\"*/5 * * * *\","
                + "\"timeZone\":\"UTC\",\"misfirePolicy\":\"SkipToNextOccurrence\",\"concurrencyPolicy\":\"Allow\","
                + "\"maxCatchUp\":1,\"maxAttempts\":3,\"deadlineSeconds\":300,\"payloadBase64\":\"aGVsbG8=\"}",
            script.bodyText(0));

        assertEquals("nightly", created.scheduleId());
        assertEquals(PROJECT, created.projectId());
        assertEquals(UUID.fromString("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"), created.subject());
        assertEquals("orders-nightly", created.functionId());
        assertEquals("1", created.functionVersion());
        assertEquals("1", created.version());
        assertEquals(UUID.fromString(INSTANCE), created.instanceId());
        assertEquals(OffsetDateTime.parse("2026-10-03T05:00:00+00:00"), created.lastOccurrence());
        assertEquals(null, created.oneShotAtUtc());
        assertEquals(false, created.isOneShot());
        assertEquals("\"schedule-v1-" + INSTANCE_HEX + "\"", created.eTag());
    }

    private static void updateSendsTheETagAsIfMatchAndNeverIfNoneMatch() {
        final Script script = new Script(request -> schedule(request, 200, scheduleJson(Map.of("version", "4")), "\"schedule-v4-" + INSTANCE_HEX + "\""));
        final YogabaseSchedule updated = script.client().update(definition(Map.of()), ETAG3);

        final Request request = script.requests.get(0);
        assertEquals("PUT", request.method());
        assertEquals(ITEM, request.url().toString());
        assertEquals(ETAG3, request.header("If-Match"));
        assertEquals(null, request.header("If-None-Match"));
        assertEquals("4", updated.version());
        assertEquals("\"schedule-v4-" + INSTANCE_HEX + "\"", updated.eTag());
    }

    private static void aDefinitionOmitsAnAbsentOrEmptyPayloadAndSendsAOneShotsInstantInUtc() {
        final Script script = new Script(request -> schedule(request, 201, scheduleJson(Map.of()), null));
        final YogabaseSchedulesClient schedules = script.client();
        schedules.create(definition(mapOf("payload", null)));
        schedules.create(definition(Map.of("payload", new byte[0])));
        schedules.create(definition(mapOf("payload", null, "oneShotAtUtc", OffsetDateTime.parse("2026-10-03T12:00:00+09:00"))));
        // A schedule with no payload and one with an empty payload are the same schedule to the gateway.
        assertTrue(!script.bodyText(0).contains("payloadBase64"));
        assertJson(script.bodyText(0), script.bodyText(1));
        assertTrue(!script.bodyText(0).contains("oneShotAtUtc"));
        // 12:00 at +09:00 is 03:00 UTC, and the gateway requires a UTC instant.
        assertEquals(
            OffsetDateTime.parse("2026-10-03T03:00:00Z"),
            OffsetDateTime.parse(JsonParser.parseString(script.bodyText(2)).getAsJsonObject().get("oneShotAtUtc").getAsString()));
        final String instant = JsonParser.parseString(script.bodyText(2)).getAsJsonObject().get("oneShotAtUtc").getAsString();
        assertTrue(instant.endsWith("Z") || instant.endsWith("+00:00"));
    }

    private static void everyPolicyIsSentByItsName() {
        final Script script = new Script(request -> schedule(request, 201, scheduleJson(Map.of()), null));
        final YogabaseSchedulesClient schedules = script.client();
        final YogabaseScheduleMisfirePolicy[] misfires = YogabaseScheduleMisfirePolicy.values();
        final YogabaseScheduleConcurrencyPolicy[] concurrencies = YogabaseScheduleConcurrencyPolicy.values();
        for (int index = 0; index < misfires.length; index++) {
            schedules.create(definition(Map.of("misfirePolicy", misfires[index], "concurrencyPolicy", concurrencies[index])));
        }
        final String[] misfireNames = {"RunImmediately", "SkipToNextOccurrence", "RunAllMissed"};
        final String[] concurrencyNames = {"Allow", "Forbid", "Replace"};
        for (int index = 0; index < misfires.length; index++) {
            final JsonObject body = JsonParser.parseString(script.bodyText(index)).getAsJsonObject();
            assertEquals(misfireNames[index], body.get("misfirePolicy").getAsString());
            assertEquals(concurrencyNames[index], body.get("concurrencyPolicy").getAsString());
        }
    }

    private static void aPayloadAtExactlyTheInlineLimitIsSentAndOneByteMoreIsRefused() {
        final Script script = new Script(request -> schedule(request, 201, scheduleJson(Map.of()), null));
        final YogabaseSchedulesClient schedules = script.client();
        schedules.create(definition(Map.of("payload", new byte[1_048_576])));
        final String body = script.bodyText(0);
        final int start = body.indexOf("\"payloadBase64\":\"") + "\"payloadBase64\":\"".length();
        assertEquals(1_398_104, body.indexOf('"', start) - start);
        final YogabaseScheduleDefinition over = definition(Map.of("payload", new byte[1_048_577]));
        assertThrows(IllegalArgumentException.class, () -> schedules.create(over));
        assertThrows(IllegalArgumentException.class, () -> schedules.update(over, ETAG3));
        assertEquals(1, script.requests.size());
    }

    private static void aMalformedDefinitionIsRefusedBeforeTheNetwork() {
        final Script script = new Script(request -> schedule(request, 201, scheduleJson(Map.of()), null));
        final YogabaseSchedulesClient schedules = script.client();
        final List<Map<String, Object>> malformed = new ArrayList<>();
        for (final String id : new String[] {"  ", " nightly", "night\nly", "x".repeat(129), ".", ".."}) {
            malformed.add(Map.of("scheduleId", id));
        }
        malformed.add(mapOf("scheduleId", null));
        malformed.add(Map.of("functionId", " "));
        malformed.add(Map.of("functionId", "x".repeat(257)));
        malformed.add(mapOf("functionVersion", null));
        malformed.add(Map.of("functionVersion", "x".repeat(65)));
        malformed.add(Map.of("cron", ""));
        malformed.add(Map.of("cron", "1".repeat(129)));
        malformed.add(Map.of("timeZone", ""));
        malformed.add(Map.of("timeZone", "z".repeat(129)));
        malformed.add(mapOf("misfirePolicy", null));
        malformed.add(mapOf("concurrencyPolicy", null));
        malformed.add(Map.of("maxCatchUp", 0));
        malformed.add(Map.of("maxCatchUp", 129));
        malformed.add(Map.of("maxAttempts", 0));
        malformed.add(Map.of("maxAttempts", 17));
        malformed.add(Map.of("deadlineSeconds", 0));
        malformed.add(Map.of("deadlineSeconds", 86_401));
        for (final Map<String, Object> overrides : malformed) {
            assertThrows(IllegalArgumentException.class, () -> schedules.create(definition(overrides)));
            assertThrows(IllegalArgumentException.class, () -> schedules.update(definition(overrides), ETAG3));
        }
        assertThrows(NullPointerException.class, () -> schedules.create(null));
        assertThrows(NullPointerException.class, () -> schedules.update(null, ETAG3));
        assertEquals(0, script.requests.size());
    }

    private static void theLongestAcceptedValuesOfEveryBoundedMemberAreSent() {
        final Script script = new Script(request -> schedule(request, 201, scheduleJson(Map.of("scheduleId", "s".repeat(128))), null));
        final Map<String, Object> overrides = new LinkedHashMap<>();
        overrides.put("scheduleId", "s".repeat(128));
        overrides.put("functionId", "f".repeat(256));
        overrides.put("functionVersion", "v".repeat(64));
        overrides.put("cron", "1".repeat(128));
        overrides.put("timeZone", "z".repeat(128));
        overrides.put("maxCatchUp", 128);
        overrides.put("maxAttempts", 16);
        overrides.put("deadlineSeconds", 86_400);
        script.client().create(definition(overrides));
        final JsonObject body = JsonParser.parseString(script.bodyText(0)).getAsJsonObject();
        assertEquals(128, body.get("scheduleId").getAsString().length());
        assertEquals(256, body.get("functionId").getAsString().length());
        assertEquals(86_400, body.get("deadlineSeconds").getAsInt());
    }

    private static void aScheduleIdThatNeedsEscapingTravelsAsOneEscapedPathSegment() {
        final String id = "weekly report/é?#";
        final Script script = new Script(request -> schedule(request, 200, scheduleJson(Map.of("scheduleId", id)), null));
        final YogabaseSchedulesClient schedules = script.client();
        schedules.create(definition(Map.of("scheduleId", id)));
        schedules.get(id);
        final String escaped = GATEWAY + "/v1/projects/" + PROJECT + "/schedules/weekly%20report%2F%C3%A9%3F%23";
        assertEquals(escaped, script.requests.get(0).url().toString());
        assertEquals(escaped, script.requests.get(1).url().toString());
        // The id in the body is the id in the path, unescaped.
        assertEquals(id, JsonParser.parseString(script.bodyText(0)).getAsJsonObject().get("scheduleId").getAsString());
    }

    // ---------------------------------------------------------------------------------------
    // The ETag a caller passes back
    // ---------------------------------------------------------------------------------------

    private static void anETagThatIsNotTheGatewaysTokenIsRefusedBeforeTheNetwork() {
        final Script script = new Script(request -> schedule(request, 200, scheduleJson(Map.of()), null));
        final YogabaseSchedulesClient schedules = script.client();
        final YogabaseScheduleTriggerRequest run = new YogabaseScheduleTriggerRequest("run-1", OffsetDateTime.now().plusMinutes(10));
        final String[] malformed = {
            "",
            "*",
            "schedule-v3-" + INSTANCE_HEX,
            "W/\"schedule-v3-" + INSTANCE_HEX + "\"",
            "\"schedule-v3-" + INSTANCE_HEX.toUpperCase(java.util.Locale.ROOT) + "\"",
            "\"schedule-v3-" + INSTANCE_HEX.substring(1) + "\"",
            "\"schedule-v3-" + INSTANCE_HEX + "0\"",
            "\"schedule-v3-" + INSTANCE + "\"",
            "\"schedule-v0-" + INSTANCE_HEX + "\"",
            "\"schedule-v03-" + INSTANCE_HEX + "\"",
            "\"schedule-v-" + INSTANCE_HEX + "\"",
            "\"schedule-v9223372036854775808-" + INSTANCE_HEX + "\"",
            "\"schedule-v3-" + "0".repeat(32) + "\"",
            "\"v3-" + INSTANCE_HEX + "\"",
            ETAG3 + ", \"schedule-v4-" + INSTANCE_HEX + "\"",
            null,
        };
        for (final String eTag : malformed) {
            assertThrows(IllegalArgumentException.class, () -> schedules.update(definition(Map.of()), eTag));
            assertThrows(IllegalArgumentException.class, () -> schedules.delete("nightly", eTag));
            assertThrows(IllegalArgumentException.class, () -> schedules.trigger("nightly", eTag, run));
        }
        assertEquals(0, script.requests.size());
    }

    private static void theLargestVersionThatFitsA64BitIntegerIsAnAcceptedETag() {
        final Script script = new Script(request -> empty(request, 204));
        final String eTag = "\"schedule-v9223372036854775807-" + INSTANCE_HEX + "\"";
        script.client().delete("nightly", eTag);
        assertEquals(eTag, script.requests.get(0).header("If-Match"));
    }

    // ---------------------------------------------------------------------------------------
    // Get
    // ---------------------------------------------------------------------------------------

    private static void getReadsOneScheduleByItsRouteAndBuildsItsETagFromTheBody() {
        final Script script = new Script(request -> schedule(request, 200, scheduleJson(Map.of()), ETAG3));
        final YogabaseSchedule read = script.client().get("nightly");
        final Request request = script.requests.get(0);
        assertEquals("GET", request.method());
        assertEquals(ITEM, request.url().toString());
        assertEquals("Bearer user-token", request.header("Authorization"));
        assertEquals(0, script.bodies.get(0).length);
        assertEquals(null, request.header("If-Match"));
        assertEquals(ETAG3, read.eTag());
        assertEquals("3", read.version());
    }

    private static void getMapsAOneShotsInstantAndTheNullMembersOfARecurringSchedule() {
        final Map<String, Object> oneShot = new LinkedHashMap<>();
        oneShot.put("lastOccurrence", null);
        oneShot.put("oneShotAtUtc", "2026-10-04T01:02:03+00:00");
        final Script script = new Script(
            request -> schedule(request, 200, scheduleJson(oneShot), ETAG3),
            request -> schedule(request, 200, scheduleJson(Map.of()), ETAG3),
            request -> schedule(request, 200, scheduleJsonWithout("lastOccurrence", "oneShotAtUtc"), ETAG3));
        final YogabaseSchedulesClient schedules = script.client();
        final YogabaseSchedule first = schedules.get("nightly");
        final YogabaseSchedule recurring = schedules.get("nightly");
        final YogabaseSchedule absent = schedules.get("nightly");
        assertEquals(OffsetDateTime.parse("2026-10-04T01:02:03+00:00"), first.oneShotAtUtc());
        assertEquals(null, first.lastOccurrence());
        assertEquals(true, first.isOneShot());
        assertEquals(null, recurring.oneShotAtUtc());
        assertEquals(OffsetDateTime.parse("2026-10-03T05:00:00+00:00"), recurring.lastOccurrence());
        // A member the gateway left out reads as null, as the contract makes both members nullable.
        assertEquals(null, absent.oneShotAtUtc());
        assertEquals(null, absent.lastOccurrence());
    }

    private static void aStrongETagHeaderThatDisagreesWithTheBodyIsRefusedAndAWeakRepeatedOrMissingOneIsIgnored() {
        final Script script = new Script(
            request -> schedule(request, 200, scheduleJson(Map.of()), "\"schedule-v4-" + INSTANCE_HEX + "\""),
            request -> schedule(request, 200, scheduleJson(Map.of()), "\"schedule-v3-" + "f".repeat(32) + "\""),
            request -> schedule(request, 200, scheduleJson(Map.of()), "W/\"schedule-v9-" + INSTANCE_HEX + "\""),
            request -> schedule(request, 200, scheduleJson(Map.of()), ETAG3 + ", \"schedule-v4-" + INSTANCE_HEX + "\""),
            request -> schedule(request, 200, scheduleJson(Map.of()), null),
            request -> schedule(request, 200, scheduleJson(Map.of()), ETAG3));
        final YogabaseSchedulesClient schedules = script.client();
        // Two strong headers that are not the body's token: the version is another, and so is the instance.
        for (int attempt = 0; attempt < 2; attempt++) {
            final YogabaseSchedulesException exception = assertThrows(YogabaseSchedulesException.class, () -> schedules.get("nightly"));
            assertEquals(YogabaseScheduleErrorCodes.RESPONSE_INVALID, exception.code());
        }
        // A proxy that compresses a response may weaken its ETag, and the body carries the same facts.
        for (int attempt = 0; attempt < 4; attempt++) {
            assertEquals(ETAG3, schedules.get("nightly").eTag());
        }
    }

    private static void aScheduleTheGatewayDescribesWronglyIsRefused() {
        final Map<String, Object> wrong = new LinkedHashMap<>();
        wrong.put("another schedule", Map.of("scheduleId", "other"));
        wrong.put("another project", Map.of("projectId", "33333333-3333-4333-8333-333333333333"));
        wrong.put("a project that is not a uuid", Map.of("projectId", "not-a-uuid"));
        wrong.put("an empty owner", Map.of("subject", "00000000-0000-0000-0000-000000000000"));
        wrong.put("an owner that is not a uuid", Map.of("subject", "not-a-uuid"));
        wrong.put("an empty function id", Map.of("functionId", ""));
        wrong.put("a version of zero", Map.of("version", "0"));
        wrong.put("a version with a leading zero", Map.of("version", "03"));
        wrong.put("a version that is not a number", Map.of("version", "abc"));
        wrong.put("a version over 64 bits", Map.of("version", "9223372036854775808"));
        wrong.put("a version as a number", Map.of("version", 3));
        wrong.put("an empty instance", Map.of("instanceId", "00000000-0000-0000-0000-000000000000"));
        wrong.put("an instance without hyphens", Map.of("instanceId", INSTANCE_HEX));
        wrong.put("a last occurrence that is not a time", Map.of("lastOccurrence", "yesterday"));
        wrong.put("a one-shot instant that is not a time", Map.of("oneShotAtUtc", 12));
        for (final Map.Entry<String, Object> entry : wrong.entrySet()) {
            @SuppressWarnings("unchecked")
            final Map<String, Object> overrides = (Map<String, Object>) entry.getValue();
            final Script script = new Script(request -> schedule(request, 200, scheduleJson(overrides), null));
            final YogabaseSchedulesException exception = assertThrows(YogabaseSchedulesException.class, () -> script.client().get("nightly"));
            assertEquals(502, exception.status());
            assertEquals(YogabaseScheduleErrorCodes.RESPONSE_INVALID, exception.code());
        }
        for (final String name : new String[] {"scheduleId", "functionVersion", "instanceId", "projectId", "subject", "version"}) {
            final Script script = new Script(request -> schedule(request, 200, scheduleJsonWithout(name), null));
            final YogabaseSchedulesException exception = assertThrows(YogabaseSchedulesException.class, () -> script.client().get("nightly"));
            assertEquals(YogabaseScheduleErrorCodes.RESPONSE_INVALID, exception.code());
        }
    }

    private static void anAnswerThatIsNotAScheduleDocumentIsRefused() {
        for (final String body : new String[] {"{\"scheduleId\":\"nightly\"}", "[]", "null", "not json"}) {
            final Script script = new Script(request -> schedule(request, 200, body, null));
            final YogabaseSchedulesException exception = assertThrows(YogabaseSchedulesException.class, () -> script.client().get("nightly"));
            assertEquals(YogabaseScheduleErrorCodes.RESPONSE_INVALID, exception.code());
        }
    }

    private static void aMemberThisClientDoesNotKnowIsIgnored() {
        final String document = scheduleJson(Map.of("extra", 1));
        final Script script = new Script(request -> schedule(request, 200, document, null));
        assertEquals("nightly", script.client().get("nightly").scheduleId());
    }

    // ---------------------------------------------------------------------------------------
    // Delete and trigger
    // ---------------------------------------------------------------------------------------

    private static void deleteSendsIfMatchWithNoBodyAndAnswers204() {
        final Script script = new Script(request -> empty(request, 204));
        script.client().delete("nightly", ETAG3);
        final Request request = script.requests.get(0);
        assertEquals("DELETE", request.method());
        assertEquals(ITEM, request.url().toString());
        assertEquals(ETAG3, request.header("If-Match"));
        assertEquals(0, script.bodies.get(0).length);
    }

    private static void triggerSendsTheExactBodyWithIfMatchAndReadsBothOutcomes() {
        final Script script = new Script(
            request -> json(request, 202, "{\"outcome\":0,\"invocationId\":\"" + INVOCATION + "\"}"),
            request -> json(request, 202, "{\"outcome\":1,\"invocationId\":\"" + INVOCATION + "\"}"));
        final YogabaseSchedulesClient schedules = script.client();
        final YogabaseScheduleTriggerRequest run =
            new YogabaseScheduleTriggerRequest("run-1", OffsetDateTime.parse("2026-10-03T21:30:00+09:00"));
        final YogabaseScheduleTriggerResult first = schedules.trigger("nightly", ETAG3, run);
        final YogabaseScheduleTriggerResult repeat = schedules.trigger("nightly", ETAG3, run);

        final Request request = script.requests.get(0);
        assertEquals("POST", request.method());
        assertEquals(RUN, request.url().toString());
        assertEquals(ETAG3, request.header("If-Match"));
        assertEquals(null, request.header("If-None-Match"));
        // 21:30 at +09:00 is 12:30 UTC.
        final JsonObject body = JsonParser.parseString(script.bodyText(0)).getAsJsonObject();
        assertEquals("run-1", body.get("idempotencyKey").getAsString());
        assertEquals(OffsetDateTime.parse("2026-10-03T12:30:00Z"), OffsetDateTime.parse(body.get("deadline").getAsString()));
        assertEquals(YogabaseScheduleTriggerOutcome.ENQUEUED, first.outcome());
        assertEquals(YogabaseScheduleTriggerOutcome.REPLAYED, repeat.outcome());
        assertEquals(UUID.fromString(INVOCATION), first.invocationId());
        assertEquals(first.invocationId(), repeat.invocationId());
    }

    private static void aMalformedRunIsRefusedBeforeTheNetwork() {
        final Script script = new Script(request -> json(request, 202, "{\"outcome\":0,\"invocationId\":\"" + INVOCATION + "\"}"));
        final YogabaseSchedulesClient schedules = script.client();
        final OffsetDateTime deadline = OffsetDateTime.now().plusMinutes(10);
        for (final String key : new String[] {"", "   ", " padded", "padded ", "k".repeat(257)}) {
            assertThrows(IllegalArgumentException.class, () -> schedules.trigger("nightly", ETAG3, new YogabaseScheduleTriggerRequest(key, deadline)));
        }
        assertThrows(IllegalArgumentException.class, () -> schedules.trigger("nightly", ETAG3, new YogabaseScheduleTriggerRequest(null, deadline)));
        assertThrows(IllegalArgumentException.class, () -> schedules.trigger("nightly", ETAG3, new YogabaseScheduleTriggerRequest("k", null)));
        assertThrows(NullPointerException.class, () -> schedules.trigger("nightly", ETAG3, null));
        assertEquals(0, script.requests.size());
        schedules.trigger("nightly", ETAG3, new YogabaseScheduleTriggerRequest("k".repeat(256), deadline));
        assertEquals(256, JsonParser.parseString(script.bodyText(0)).getAsJsonObject().get("idempotencyKey").getAsString().length());
    }

    private static void aRunTheGatewayDescribesWronglyIsRefused() {
        for (final String document : new String[] {
            "{\"outcome\":2,\"invocationId\":\"" + INVOCATION + "\"}",
            "{\"outcome\":-1,\"invocationId\":\"" + INVOCATION + "\"}",
            "{\"invocationId\":\"" + INVOCATION + "\"}",
            "{\"outcome\":\"0\",\"invocationId\":\"" + INVOCATION + "\"}",
            "{\"outcome\":0,\"invocationId\":\"00000000-0000-0000-0000-000000000000\"}",
            "{\"outcome\":0,\"invocationId\":\"nope\"}",
            "{\"outcome\":0}",
        }) {
            final Script script = new Script(request -> json(request, 202, document));
            final YogabaseSchedulesException exception = assertThrows(
                YogabaseSchedulesException.class,
                () -> script.client().trigger("nightly", ETAG3, new YogabaseScheduleTriggerRequest("k", OffsetDateTime.now().plusMinutes(5))));
            assertEquals(YogabaseScheduleErrorCodes.RESPONSE_INVALID, exception.code());
        }
    }

    // ---------------------------------------------------------------------------------------
    // Refusals
    // ---------------------------------------------------------------------------------------

    private static void everyRefusalReadsAsItsStatusWhereTheGatewayNamesNoCode() {
        final Object[][] expectations = {
            {400, YogabaseStatusErrorCodes.REQUEST_FAILED},
            {401, YogabaseStatusErrorCodes.AUTHENTICATION_REQUIRED},
            {403, YogabaseStatusErrorCodes.FORBIDDEN},
            {404, YogabaseStatusErrorCodes.NOT_FOUND},
            {409, YogabaseStatusErrorCodes.CONFLICT},
            {412, YogabaseStatusErrorCodes.PRECONDITION_FAILED},
            {415, YogabaseStatusErrorCodes.UNSUPPORTED_MEDIA_TYPE},
            {428, YogabaseStatusErrorCodes.PRECONDITION_REQUIRED},
            {429, YogabaseStatusErrorCodes.RATE_LIMITED},
            {503, YogabaseStatusErrorCodes.REQUEST_FAILED},
        };
        final List<Integer> bodiless = List.of(404, 409, 415);
        for (final Object[] expectation : expectations) {
            final int status = (Integer) expectation[0];
            final List<java.util.function.Consumer<YogabaseSchedulesClient>> calls = List.of(
                client -> client.update(definition(Map.of()), ETAG3),
                client -> client.delete("nightly", ETAG3),
                client -> client.trigger("nightly", ETAG3, new YogabaseScheduleTriggerRequest("k", OffsetDateTime.now().plusMinutes(5))),
                client -> client.get("nightly"));
            for (final java.util.function.Consumer<YogabaseSchedulesClient> call : calls) {
                final Script script = new Script(request -> bodiless.contains(status)
                    ? empty(request, status)
                    : problem(request, status, "Fixed title.", null, Map.of()));
                final YogabaseSchedulesException exception = assertThrows(YogabaseSchedulesException.class, () -> call.accept(script.client()));
                assertEquals(status, exception.status());
                assertEquals(expectation[1], exception.code());
                assertEquals(null, exception.limitBytes());
                assertEquals(null, exception.maxBodyBytes());
            }
        }
        // A refusal's title is its message and its trace reaches the error.
        final Script script = new Script(request -> problem(request, 412, "The schedule concurrency precondition is stale.", null, Map.of()));
        final YogabaseSchedulesException stale = assertThrows(YogabaseSchedulesException.class, () -> script.client().update(definition(Map.of()), ETAG3));
        assertEquals("The schedule concurrency precondition is stale.", stale.getMessage());
        assertEquals("00-trace", stale.correlationId());
    }

    private static void aPayloadTooLargeRefusalNamesTheCodeAndBothLimits() {
        final String detail = "A schedule's payload may be at most 1048576 bytes before base64 encoding, and the whole request body at most 8394167 bytes.";
        final Script script = new Script(request -> problem(
            request, 413, "The schedule payload is too large.", YogabaseScheduleErrorCodes.PAYLOAD_TOO_LARGE,
            Map.of("detail", detail, "limitBytes", 1_048_576, "maxBodyBytes", 8_394_167)));
        final YogabaseSchedulesException exception = assertThrows(YogabaseSchedulesException.class, () -> script.client().create(definition(Map.of())));
        assertEquals(413, exception.status());
        assertEquals("schedule_payload_too_large", exception.code());
        assertEquals(1_048_576L, exception.limitBytes());
        assertEquals(8_394_167L, exception.maxBodyBytes());
        assertTrue(exception.getMessage().contains("8394167"));
    }

    private static void aBare413ReadsAsPayloadTooLarge() {
        final Script script = new Script(request -> empty(request, 413));
        final YogabaseSchedulesException exception = assertThrows(YogabaseSchedulesException.class, () -> script.client().create(definition(Map.of())));
        assertEquals(413, exception.status());
        assertEquals(YogabaseStatusErrorCodes.PAYLOAD_TOO_LARGE, exception.code());
        assertTrue(!YogabaseScheduleErrorCodes.PAYLOAD_TOO_LARGE.equals(exception.code()));
        assertEquals(null, exception.limitBytes());
    }

    private static void limitsTheRefusalStatesBadlyAreNotTrusted() {
        final Script script = new Script(request -> problem(
            request, 413, "x", YogabaseScheduleErrorCodes.PAYLOAD_TOO_LARGE, Map.of("limitBytes", -1, "maxBodyBytes", "8394167")));
        final YogabaseSchedulesException exception = assertThrows(YogabaseSchedulesException.class, () -> script.client().create(definition(Map.of())));
        assertEquals(null, exception.limitBytes());
        assertEquals(null, exception.maxBodyBytes());
    }

    private static void anInstallationWithoutTheSchedulerAnswersABareNotFoundOnEveryRoute() {
        final Script script = new Script(request -> empty(request, 404));
        final YogabaseSchedulesClient schedules = script.client();
        final List<Runnable> calls = List.of(
            () -> schedules.get("nightly"),
            () -> schedules.delete("nightly", ETAG3),
            () -> schedules.trigger("nightly", ETAG3, new YogabaseScheduleTriggerRequest("k", OffsetDateTime.now().plusMinutes(5))));
        for (final Runnable call : calls) {
            final YogabaseSchedulesException exception = assertThrows(YogabaseSchedulesException.class, call::run);
            assertEquals(404, exception.status());
            assertEquals(YogabaseStatusErrorCodes.NOT_FOUND, exception.code());
        }
        // Both authored clients refuse as a gateway exception, so a caller may catch the one type.
        assertTrue(YogabaseGatewayException.class.isAssignableFrom(YogabaseSchedulesException.class));
        assertTrue(YogabaseGatewayException.class.isAssignableFrom(YogabaseJobsException.class));
    }

    // ---------------------------------------------------------------------------------------
    // Construction
    // ---------------------------------------------------------------------------------------

    private static void theOriginAndTheProjectAreValidatedAndTheApplicationClientHandsOutTheClient() {
        final RequestAdapter adapter = new Script(request -> empty(request, 204)).adapter();
        for (final String origin : new String[] {"https://apps.example.test/path", "https://user@apps.example.test", "ftp://apps.example.test", "https://apps.example.test?x=1"}) {
            assertThrows(IllegalArgumentException.class, () -> new YogabaseSchedulesClient(adapter, URI.create(origin), PROJECT));
        }
        assertThrows(IllegalArgumentException.class, () -> new YogabaseSchedulesClient(adapter, URI.create(GATEWAY), new UUID(0L, 0L)));
        assertThrows(NullPointerException.class, () -> new YogabaseSchedulesClient(adapter, URI.create(GATEWAY), null));
        assertThrows(NullPointerException.class, () -> new YogabaseSchedulesClient(null, URI.create(GATEWAY), PROJECT));
        final YogabaseSchedulesClient direct = new YogabaseSchedulesClient(adapter, URI.create(GATEWAY + "/"), PROJECT);
        assertEquals(URI.create(GATEWAY), direct.origin());
        assertEquals(PROJECT, direct.projectId());

        final YogabaseApplicationClient application = new YogabaseApplicationClient(adapter, URI.create(GATEWAY));
        final YogabaseSchedulesClient handedOut = application.schedules(PROJECT);
        assertEquals(PROJECT, handedOut.projectId());
        assertEquals(application.origin(), handedOut.origin());
    }

    private static void anAdapterThatDoesNotHandBackItsNativeResponseIsNamed() {
        final RequestAdapter adapter = (RequestAdapter) Proxy.newProxyInstance(
            YogabaseSchedulesClientTest.class.getClassLoader(),
            new Class<?>[] {RequestAdapter.class},
            (proxy, method, arguments) -> switch (method.getName()) {
                case "getBaseUrl" -> GATEWAY;
                case "getSerializationWriterFactory" -> new com.microsoft.kiota.serialization.SerializationWriterFactoryRegistry();
                case "getParseNodeFactory" -> new com.microsoft.kiota.serialization.ParseNodeFactoryRegistry();
                default -> null;
            });
        final IllegalStateException exception = assertThrows(
            IllegalStateException.class, () -> new YogabaseSchedulesClient(adapter, URI.create(GATEWAY), PROJECT).get("nightly"));
        assertTrue(exception.getMessage().contains("OkHttpRequestAdapter"));
    }

    // ---------------------------------------------------------------------------------------
    // The authored requests are the generated builders' requests
    // ---------------------------------------------------------------------------------------

    private static void theRequestsAreTheOnesTheGeneratedBuildersBuild() {
        // create and update: the generated put builder, with the precondition a caller adds by hand.
        assertSameRequest(
            sentBy(client -> client.create(definition(Map.of()))),
            builtBy(api -> api.v1().projects().byProjectId(PROJECT).schedules().byScheduleId("nightly").put(
                generatedDefinition(), configuration -> configuration.headers.add("If-None-Match", "*"))),
            "If-None-Match", "*");
        assertSameRequest(
            sentBy(client -> client.update(definition(Map.of()), ETAG3)),
            builtBy(api -> api.v1().projects().byProjectId(PROJECT).schedules().byScheduleId("nightly").put(
                generatedDefinition(), configuration -> configuration.headers.add("If-Match", ETAG3))),
            "If-Match", ETAG3);
        assertSameRequest(
            sentBy(client -> client.get("nightly")),
            builtBy(api -> api.v1().projects().byProjectId(PROJECT).schedules().byScheduleId("nightly").get()),
            null, null);
        assertSameRequest(
            sentBy(client -> client.delete("nightly", ETAG3)),
            builtBy(api -> api.v1().projects().byProjectId(PROJECT).schedules().byScheduleId("nightly").delete(
                configuration -> configuration.headers.add("If-Match", ETAG3))),
            "If-Match", ETAG3);
        final OffsetDateTime deadline = OffsetDateTime.parse("2026-10-03T12:30:00Z");
        final ScheduleTriggerRequest body = new ScheduleTriggerRequest();
        body.setIdempotencyKey("run-1");
        body.setDeadline(deadline);
        assertSameRequest(
            sentBy(client -> client.trigger("nightly", ETAG3, new YogabaseScheduleTriggerRequest("run-1", deadline))),
            builtBy(api -> api.v1().projects().byProjectId(PROJECT).schedules().byScheduleId("nightly").trigger().post(
                body, configuration -> configuration.headers.add("If-Match", ETAG3))),
            "If-Match", ETAG3);
    }

    private record Sent(Request request, String body) {
    }

    private static Sent sentBy(java.util.function.Consumer<YogabaseSchedulesClient> call) {
        final Script script = new Script(request -> json(request, 200, "{}"));
        try {
            call.accept(script.client());
        } catch (RuntimeException expected) {
            // The canned answer is not a schedule; only the request is compared.
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
            // The canned answer is not a schedule; only the request is compared.
        }
        assertEquals(1, script.requests.size());
        return new Sent(script.requests.get(0), script.bodyText(0));
    }

    private static void assertSameRequest(Sent sent, Sent built, String header, String value) {
        assertEquals(built.request().method(), sent.request().method());
        assertEquals(built.request().url().toString(), sent.request().url().toString());
        if (!built.body().isEmpty() || !sent.body().isEmpty()) {
            assertJson(built.body(), sent.body());
        }
        // The authored client accepts at least what the generated builder asks for.
        for (final String accepted : built.request().header("Accept").split(",")) {
            assertTrue(sent.request().header("Accept").contains(accepted.trim()));
        }
        if (header != null) {
            assertEquals(value, sent.request().header(header));
            assertEquals(value, built.request().header(header));
        }
    }

    private static ScheduleDefinitionRequest generatedDefinition() {
        final ScheduleDefinitionRequest body = new ScheduleDefinitionRequest();
        body.setScheduleId("nightly");
        body.setFunctionId("orders-nightly");
        body.setFunctionVersion("1");
        body.setCron("*/5 * * * *");
        body.setTimeZone("UTC");
        body.setMisfirePolicy(MisfirePolicy.SkipToNextOccurrence);
        body.setConcurrencyPolicy(ConcurrencyPolicy.Allow);
        body.setMaxCatchUp(1);
        body.setMaxAttempts(3);
        body.setDeadlineSeconds(300);
        body.setPayloadBase64("aGVsbG8=");
        return body;
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

        YogabaseSchedulesClient client() {
            return new YogabaseSchedulesClient(adapter(), URI.create(GATEWAY), PROJECT);
        }

        String bodyText(int index) {
            return new String(bodies.get(index), StandardCharsets.UTF_8);
        }
    }

    /** A Map that may hold a null, which Map.of cannot. */
    private static Map<String, Object> mapOf(Object... keysAndValues) {
        final Map<String, Object> map = new LinkedHashMap<>();
        for (int index = 0; index < keysAndValues.length; index += 2) {
            map.put((String) keysAndValues[index], keysAndValues[index + 1]);
        }
        return map;
    }

    private static YogabaseScheduleDefinition definition(Map<String, Object> overrides) {
        final Map<String, Object> values = new LinkedHashMap<>();
        values.put("scheduleId", "nightly");
        values.put("functionId", "orders-nightly");
        values.put("functionVersion", "1");
        values.put("cron", "*/5 * * * *");
        values.put("timeZone", "UTC");
        values.put("misfirePolicy", YogabaseScheduleMisfirePolicy.SKIP_TO_NEXT_OCCURRENCE);
        values.put("concurrencyPolicy", YogabaseScheduleConcurrencyPolicy.ALLOW);
        values.put("maxCatchUp", 1);
        values.put("maxAttempts", 3);
        values.put("deadlineSeconds", 300);
        values.put("payload", "hello".getBytes(StandardCharsets.UTF_8));
        values.put("oneShotAtUtc", null);
        values.putAll(overrides);
        return new YogabaseScheduleDefinition(
            (String) values.get("scheduleId"),
            (String) values.get("functionId"),
            (String) values.get("functionVersion"),
            (String) values.get("cron"),
            (String) values.get("timeZone"),
            (YogabaseScheduleMisfirePolicy) values.get("misfirePolicy"),
            (YogabaseScheduleConcurrencyPolicy) values.get("concurrencyPolicy"),
            (Integer) values.get("maxCatchUp"),
            (Integer) values.get("maxAttempts"),
            (Integer) values.get("deadlineSeconds"),
            (byte[]) values.get("payload"),
            (OffsetDateTime) values.get("oneShotAtUtc"));
    }

    private static Map<String, Object> scheduleDocument(Map<String, Object> overrides) {
        final Map<String, Object> document = new LinkedHashMap<>();
        document.put("scheduleId", "nightly");
        document.put("projectId", PROJECT.toString());
        document.put("subject", "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
        document.put("functionId", "orders-nightly");
        document.put("functionVersion", "1");
        document.put("version", "3");
        document.put("instanceId", INSTANCE);
        document.put("lastOccurrence", "2026-10-03T05:00:00+00:00");
        document.put("oneShotAtUtc", null);
        document.putAll(overrides);
        return document;
    }

    private static String scheduleJson(Map<String, Object> overrides) {
        return new GsonBuilder().serializeNulls().create().toJson(scheduleDocument(overrides));
    }

    private static String scheduleJsonWithout(String... names) {
        final Map<String, Object> document = scheduleDocument(Map.of());
        for (final String name : names) {
            document.remove(name);
        }
        return new GsonBuilder().serializeNulls().create().toJson(document);
    }

    private static Response schedule(Request request, int code, String body, String eTag) {
        final Response.Builder builder = new Response.Builder().request(request).protocol(Protocol.HTTP_1_1).code(code).message("x")
            .body(ResponseBody.create(body.getBytes(StandardCharsets.UTF_8), JSON));
        if (eTag != null) {
            builder.header("ETag", eTag);
        }
        return builder.build();
    }

    private static Response json(Request request, int code, String body) {
        return new Response.Builder().request(request).protocol(Protocol.HTTP_1_1).code(code).message("x")
            .body(ResponseBody.create(body.getBytes(StandardCharsets.UTF_8), JSON)).build();
    }

    // What ASP.NET Core writes for Results.Problem(statusCode, title, detail, extensions): the code and the
    // limits are top-level members beside type, title, status and traceId. The schedule routes name a code only
    // on the 413.
    private static Response problem(Request request, int status, String title, String code, Map<String, Object> extras) {
        final Map<String, Object> document = new LinkedHashMap<>();
        document.put("type", "https://tools.ietf.org/html/rfc9110");
        document.put("title", title);
        document.put("status", status);
        document.put("traceId", "00-trace");
        if (code != null) {
            document.put("code", code);
        }
        document.putAll(extras);
        return new Response.Builder().request(request).protocol(Protocol.HTTP_1_1).code(status).message("x")
            .body(ResponseBody.create(new GsonBuilder().create().toJson(document).getBytes(StandardCharsets.UTF_8), PROBLEM)).build();
    }

    private static Response empty(Request request, int code) {
        return new Response.Builder().request(request).protocol(Protocol.HTTP_1_1).code(code).message("x")
            .body(ResponseBody.create(new byte[0], null)).build();
    }

    private static void assertTrue(boolean value) {
        cases++;
        if (!value) {
            throw new AssertionError("Expected true");
        }
    }

    private static void assertJson(String expected, String actual) {
        cases++;
        if (!JsonParser.parseString(expected).equals(JsonParser.parseString(actual))) {
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
