using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Yogabase.Yogabase;

namespace Yogabase.Tests;

/// <summary>
/// Wire-shape coverage for the schedule surface, asserted against a fake handler: these tests pin the exact
/// request the SDK emits (the route, the conditional headers, the body) and how it reads and refuses what the
/// gateway answers, and say nothing about the server's behaviour. The shapes are the ones the mirrored client
/// OpenAPI document declares under <c>x-yogabase-schedules</c> and for the four schedule operations;
/// <see cref="ContractOperationParityTests"/> holds the surface to the document, and the end-to-end harness
/// recorded in <c>MANIFEST.json</c> holds it to the product's real routes.
/// </summary>
public sealed class SchedulesClientTests
{
    private static readonly Guid ProjectId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid EnvironmentId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private const string Instance = "01234567-89ab-cdef-0123-456789abcdef";
    private const string InstanceHex = "0123456789abcdef0123456789abcdef";
    private const string ItemRoute = "http://localhost/v1/projects/11111111-1111-4111-8111-111111111111/schedules/nightly";
    private const string RunRoute = ItemRoute + "/trigger";
    private static readonly string ETag3 = "\"schedule-v3-" + InstanceHex + "\"";

    // ---------------------------------------------------------------------------------------
    // Create and update
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task CreateSendsAConditionalPutOfTheExactBodyAndReadsTheScheduleAndItsETag()
    {
        HttpRequestMessage? observed = null;
        string? body = null;
        string? contentType = null;
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            observed = Clone(request);
            body = await request.Content!.ReadAsStringAsync(cancellationToken);
            contentType = request.Content.Headers.ContentType?.MediaType;
            return Schedule(HttpStatusCode.Created, ScheduleJson(version: "1"), "\"schedule-v1-" + InstanceHex + "\"");
        });
        await using var client = await ClientAsync(handler);

        var schedule = await client.Schedules.CreateAsync(Definition(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Put, observed!.Method);
        Assert.Equal(ItemRoute, observed.RequestUri!.AbsoluteUri);
        Assert.Equal(new AuthenticationHeaderValue("Bearer", "access-token"), observed.Headers.Authorization);
        Assert.Equal("application/json", contentType);
        // Exactly one of the two preconditions: a create is conditional on there being nothing there.
        Assert.Equal(["*"], observed.Headers.GetValues("If-None-Match"));
        Assert.False(observed.Headers.Contains("If-Match"));
        // The names are the gateway's ScheduledLambdaDefinitionRequest, which refuses any other member.
        Assert.Equal(
            "{\"scheduleId\":\"nightly\",\"functionId\":\"orders-nightly\",\"functionVersion\":\"1\",\"cron\":\"*/5 * * * *\","
            + "\"timeZone\":\"UTC\",\"misfirePolicy\":\"SkipToNextOccurrence\",\"concurrencyPolicy\":\"Allow\","
            + "\"maxCatchUp\":1,\"maxAttempts\":3,\"deadlineSeconds\":300,\"payloadBase64\":\"aGVsbG8=\"}",
            body);

        Assert.Equal("nightly", schedule.ScheduleId);
        Assert.Equal(ProjectId, schedule.ProjectId);
        Assert.Equal(Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"), schedule.Subject);
        Assert.Equal("orders-nightly", schedule.FunctionId);
        Assert.Equal("1", schedule.FunctionVersion);
        Assert.Equal("1", schedule.Version);
        Assert.Equal(Guid.Parse(Instance), schedule.InstanceId);
        Assert.Equal(DateTimeOffset.Parse("2026-10-03T05:00:00+00:00", CultureInfo.InvariantCulture), schedule.LastOccurrence);
        Assert.Null(schedule.OneShotAtUtc);
        Assert.False(schedule.IsOneShot);
        Assert.Equal("\"schedule-v1-" + InstanceHex + "\"", schedule.ETag);
    }

    [Fact]
    public async Task UpdateSendsTheETagAsIfMatchAndNeverIfNoneMatch()
    {
        HttpRequestMessage? observed = null;
        var handler = new ScriptedHandler((request, _) =>
        {
            observed = Clone(request);
            return Task.FromResult(Schedule(HttpStatusCode.OK, ScheduleJson(version: "4"), "\"schedule-v4-" + InstanceHex + "\""));
        });
        await using var client = await ClientAsync(handler);

        var schedule = await client.Schedules.UpdateAsync(Definition(), ETag3, TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Put, observed!.Method);
        Assert.Equal(ItemRoute, observed.RequestUri!.AbsoluteUri);
        Assert.Equal([ETag3], observed.Headers.GetValues("If-Match"));
        Assert.False(observed.Headers.Contains("If-None-Match"));
        Assert.Equal("4", schedule.Version);
        Assert.Equal("\"schedule-v4-" + InstanceHex + "\"", schedule.ETag);
    }

    [Fact]
    public async Task ADefinitionOmitsAnAbsentOrEmptyPayloadAndSendsAOneShotsInstantInUtc()
    {
        var bodies = new List<string>();
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            bodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return Schedule(HttpStatusCode.Created, ScheduleJson(), null);
        });
        await using var client = await ClientAsync(handler);

        var noPayload = Definition();
        noPayload.Payload = null;
        var emptyPayload = Definition();
        emptyPayload.Payload = [];
        var oneShot = Definition();
        oneShot.Payload = null;
        oneShot.OneShotAtUtc = new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.FromHours(9));
        foreach (var definition in new[] { noPayload, emptyPayload, oneShot })
        {
            await client.Schedules.CreateAsync(definition, TestContext.Current.CancellationToken);
        }

        // A schedule with no payload and one with an empty payload are the same schedule to the gateway.
        Assert.DoesNotContain("payloadBase64", bodies[0], StringComparison.Ordinal);
        Assert.Equal(bodies[0], bodies[1]);
        Assert.DoesNotContain("oneShotAtUtc", bodies[0], StringComparison.Ordinal);
        // 12:00 at +09:00 is 03:00 UTC, and the gateway requires a UTC instant.
        Assert.EndsWith(",\"oneShotAtUtc\":\"2026-10-03T03:00:00+00:00\"}", bodies[2], StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("RunImmediately", "Allow")]
    [InlineData("SkipToNextOccurrence", "Forbid")]
    [InlineData("RunAllMissed", "Replace")]
    public async Task EveryPolicyIsSentByItsName(string misfire, string concurrency)
    {
        string? body = null;
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return Schedule(HttpStatusCode.Created, ScheduleJson(), null);
        });
        await using var client = await ClientAsync(handler);
        var definition = Definition();
        definition.MisfirePolicy = Enum.Parse<YogabaseScheduleMisfirePolicy>(misfire);
        definition.ConcurrencyPolicy = Enum.Parse<YogabaseScheduleConcurrencyPolicy>(concurrency);

        await client.Schedules.CreateAsync(definition, TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(body!);
        Assert.Equal(misfire, document.RootElement.GetProperty("misfirePolicy").GetString());
        Assert.Equal(concurrency, document.RootElement.GetProperty("concurrencyPolicy").GetString());
    }

    [Fact]
    public async Task APayloadAtExactlyTheInlineLimitIsSentAndOneByteMoreIsRefusedBeforeTransport()
    {
        string? body = null;
        var calls = 0;
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            calls++;
            body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return Schedule(HttpStatusCode.Created, ScheduleJson(), null);
        });
        await using var client = await ClientAsync(handler);
        var definition = Definition();
        definition.Payload = new byte[YogabaseSchedulesLimits.MaximumPayloadBytes];

        await client.Schedules.CreateAsync(definition, TestContext.Current.CancellationToken);

        Assert.Equal(1, calls);
        using var document = JsonDocument.Parse(body!);
        Assert.Equal(1_398_104, document.RootElement.GetProperty("payloadBase64").GetString()!.Length);

        definition.Payload = new byte[YogabaseSchedulesLimits.MaximumPayloadBytes + 1];
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.Schedules.CreateAsync(definition, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.Schedules.UpdateAsync(definition, ETag3, TestContext.Current.CancellationToken));
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData("blank-schedule-id")]
    [InlineData("padded-schedule-id")]
    [InlineData("control-schedule-id")]
    [InlineData("long-schedule-id")]
    [InlineData("dot-schedule-id")]
    [InlineData("dots-schedule-id")]
    [InlineData("trailing-space-schedule-id")]
    [InlineData("slash-schedule-id")]
    [InlineData("space-schedule-id")]
    [InlineData("dash-first-schedule-id")]
    [InlineData("dot-first-schedule-id")]
    [InlineData("underscore-first-schedule-id")]
    [InlineData("tilde-first-schedule-id")]
    [InlineData("colon-first-schedule-id")]
    [InlineData("percent-schedule-id")]
    [InlineData("question-schedule-id")]
    [InlineData("hash-schedule-id")]
    [InlineData("non-ascii-letter-schedule-id")]
    [InlineData("non-ascii-digit-schedule-id")]
    [InlineData("trailing-newline-schedule-id")]
    [InlineData("blank-function-id")]
    [InlineData("long-function-id")]
    [InlineData("blank-function-version")]
    [InlineData("long-function-version")]
    [InlineData("blank-cron")]
    [InlineData("long-cron")]
    [InlineData("blank-time-zone")]
    [InlineData("long-time-zone")]
    [InlineData("no-misfire-policy")]
    [InlineData("undefined-misfire-policy")]
    [InlineData("no-concurrency-policy")]
    [InlineData("undefined-concurrency-policy")]
    [InlineData("catch-up-zero")]
    [InlineData("catch-up-over")]
    [InlineData("attempts-zero")]
    [InlineData("attempts-over")]
    [InlineData("deadline-zero")]
    [InlineData("deadline-over")]
    [InlineData("one-shot-unset-instant")]
    public async Task AMalformedDefinitionIsRefusedBeforeTransport(string mutation)
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(Schedule(HttpStatusCode.Created, ScheduleJson(), null));
        });
        await using var client = await ClientAsync(handler);
        var definition = Mutate(Definition(), mutation);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => client.Schedules.CreateAsync(definition, TestContext.Current.CancellationToken));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => client.Schedules.UpdateAsync(definition, ETag3, TestContext.Current.CancellationToken));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task TheLongestAcceptedValuesOfEveryBoundedMemberAreSent()
    {
        string? body = null;
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return Schedule(HttpStatusCode.Created, ScheduleJson(scheduleId: new string('s', 128)), null);
        });
        await using var client = await ClientAsync(handler);
        var definition = Definition();
        definition.ScheduleId = new string('s', 128);
        definition.FunctionId = new string('f', 256);
        definition.FunctionVersion = new string('v', 64);
        definition.Cron = new string('1', 128);
        definition.TimeZone = new string('z', 128);
        definition.MaxCatchUp = 128;
        definition.MaxAttempts = 16;
        definition.DeadlineSeconds = 86_400;

        await client.Schedules.CreateAsync(definition, TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(body!);
        Assert.Equal(128, document.RootElement.GetProperty("scheduleId").GetString()!.Length);
        Assert.Equal(256, document.RootElement.GetProperty("functionId").GetString()!.Length);
        Assert.Equal(86_400, document.RootElement.GetProperty("deadlineSeconds").GetInt32());
    }

    [Theory]
    [InlineData("a")]
    [InlineData("Z")]
    [InlineData("0")]
    [InlineData("nightly")]
    [InlineData("a.b_c~d:e-f")]
    [InlineData("orders-nightly.v2:eu~west_1")]
    public async Task AScheduleIdThatFollowsTheRuleIsSentUnchangedInTheBodyAndAsOneSegmentOfThePath(string id)
    {
        var uris = new List<string>();
        var bodies = new List<string>();
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            uris.Add(request.RequestUri!.AbsoluteUri);
            bodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
            return Schedule(HttpStatusCode.OK, ScheduleJson(scheduleId: id), null);
        });
        await using var client = await ClientAsync(handler);
        var definition = Definition();
        definition.ScheduleId = id;

        await client.Schedules.CreateAsync(definition, TestContext.Current.CancellationToken);
        await client.Schedules.GetAsync(id, TestContext.Current.CancellationToken);

        var route = "http://localhost/v1/projects/11111111-1111-4111-8111-111111111111/schedules/" + Uri.EscapeDataString(id);
        Assert.Equal([route, route], uris);
        // The id in the body is the id in the path, as it was written.
        using var document = JsonDocument.Parse(bodies[0]);
        Assert.Equal(id, document.RootElement.GetProperty("scheduleId").GetString());
    }

    [Fact]
    public async Task AScheduleWrittenBeforeTheIdRuleIsStillReadRunAndDeletedByItsIdAndNeverWritten()
    {
        // An id the contract keeps addressable by a read, a run and a delete, percent-encoded as any path segment is,
        // though a write of it is refused: the schedule was written before the pattern was enforced.
        const string legacy = "weekly report/é?#";
        var uris = new List<string>();
        var handler = new ScriptedHandler((request, _) =>
        {
            uris.Add(request.RequestUri!.AbsoluteUri);
            return Task.FromResult(
                request.Method == HttpMethod.Get ? Schedule(HttpStatusCode.OK, ScheduleJson(scheduleId: legacy), null)
                : request.Method == HttpMethod.Delete ? new HttpResponseMessage(HttpStatusCode.NoContent)
                : Json(HttpStatusCode.Accepted, "{\"outcome\":0,\"invocationId\":\"9b9e2f3a-0d6e-4e6c-a4f1-2c6a7a5f0b11\"}"));
        });
        await using var client = await ClientAsync(handler);

        var read = await client.Schedules.GetAsync(legacy, TestContext.Current.CancellationToken);
        await client.Schedules.TriggerAsync(legacy, ETag3, Run(), TestContext.Current.CancellationToken);
        await client.Schedules.DeleteAsync(legacy, ETag3, TestContext.Current.CancellationToken);

        const string escaped = "http://localhost/v1/projects/11111111-1111-4111-8111-111111111111/schedules/weekly%20report%2F%C3%A9%3F%23";
        Assert.Equal(legacy, read.ScheduleId);
        Assert.Equal([escaped, escaped + "/trigger", escaped], uris);

        // A write of it is refused before transport: delete it and create it again under an id that follows the rule.
        var definition = Definition();
        definition.ScheduleId = legacy;
        await Assert.ThrowsAsync<ArgumentException>(() => client.Schedules.CreateAsync(definition, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => client.Schedules.UpdateAsync(definition, ETag3, TestContext.Current.CancellationToken));
        Assert.Equal(3, uris.Count);
    }

    // The contract's pattern is JSON Schema's (ECMA-262): its $ matches only at the end of the input, where .NET's
    // also matches before a final newline, so the oracle swaps in \z. Every character of Latin-1 alone, after a letter
    // and before one, a few characters beyond it that look like letters or digits, and the lengths at the edge go
    // through both the oracle and a write.
    [Fact]
    public async Task AWriteAcceptsExactlyTheIdsThePatternOfTheContractsScheduleIdMemberAccepts()
    {
        var pattern = await ReadContractScheduleIdPatternAsync();
        Assert.StartsWith("^", pattern, StringComparison.Ordinal);
        Assert.EndsWith("$", pattern, StringComparison.Ordinal);
        var oracle = new Regex(pattern[..^1] + "\\z", RegexOptions.CultureInvariant);

        var candidates = new List<string> { string.Empty, " ", "٣", "a٣", "K", "aK", "０", "a０", "\U0001F600", "a\U0001F600" };
        for (var code = 0; code <= 0xFF; code++)
        {
            var character = ((char)code).ToString();
            candidates.Add(character);
            candidates.Add("a" + character);
            candidates.Add(character + "a");
        }

        foreach (var length in new[] { 127, 128, 129 })
        {
            candidates.Add(new string('a', length));
            candidates.Add("a" + new string('.', length - 1));
            candidates.Add("a" + new string(':', length - 1));
        }

        var current = string.Empty;
        var reached = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            reached++;
            return Task.FromResult(Schedule(HttpStatusCode.Created, ScheduleJson(scheduleId: current), null));
        });
        await using var client = await ClientAsync(handler);

        var accepted = 0;
        foreach (var id in candidates)
        {
            current = id;
            var before = reached;
            var definition = Definition();
            definition.ScheduleId = id;
            if (oracle.IsMatch(id))
            {
                accepted++;
                await client.Schedules.CreateAsync(definition, TestContext.Current.CancellationToken);
                Assert.True(reached == before + 1, $"The contract's pattern accepts the id {Describe(id)} and the client refused it.");
            }
            else
            {
                await Assert.ThrowsAsync<ArgumentException>(() => client.Schedules.CreateAsync(definition, TestContext.Current.CancellationToken));
                Assert.True(reached == before, $"The contract's pattern refuses the id {Describe(id)} and the client sent it.");
            }
        }

        // The corpus holds both kinds, so the comparison is not vacuous.
        Assert.InRange(accepted, 60, candidates.Count - 60);
    }

    private static string Describe(string id) =>
        "\"" + string.Concat(id.Select(static character => character is >= ' ' and <= '~' ? character.ToString() : "\\u" + ((int)character).ToString("x4", CultureInfo.InvariantCulture))) + "\"";

    private static async Task<string> ReadContractScheduleIdPatternAsync()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Yogabase.SDKs.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(
            Path.Combine(directory.FullName, "contracts", "yogabase-client-v1.openapi.json"),
            TestContext.Current.CancellationToken));
        return document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("ScheduleDefinitionRequest")
            .GetProperty("properties").GetProperty("scheduleId").GetProperty("pattern").GetString()!;
    }

    [Fact]
    public async Task AWriteWithoutADefinitionIsRefusedBeforeTransport()
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(Schedule(HttpStatusCode.Created, ScheduleJson(), null));
        });
        await using var client = await ClientAsync(handler);

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.Schedules.CreateAsync(null!, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.Schedules.UpdateAsync(null!, ETag3, TestContext.Current.CancellationToken));
        Assert.Equal(0, calls);
    }

    // ---------------------------------------------------------------------------------------
    // The ETag a caller passes back
    // ---------------------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("*")]
    [InlineData("schedule-v3-0123456789abcdef0123456789abcdef")]
    [InlineData("W/\"schedule-v3-0123456789abcdef0123456789abcdef\"")]
    [InlineData("\"schedule-v3-0123456789ABCDEF0123456789abcdef\"")]
    [InlineData("\"schedule-v3-0123456789abcdef0123456789abcde\"")]
    [InlineData("\"schedule-v3-0123456789abcdef0123456789abcdef0\"")]
    [InlineData("\"schedule-v3-01234567-89ab-cdef-0123-456789abcdef\"")]
    [InlineData("\"schedule-v0-0123456789abcdef0123456789abcdef\"")]
    [InlineData("\"schedule-v03-0123456789abcdef0123456789abcdef\"")]
    [InlineData("\"schedule-v-0123456789abcdef0123456789abcdef\"")]
    [InlineData("\"schedule-v-1-0123456789abcdef0123456789abcdef\"")]
    [InlineData("\"schedule-v9223372036854775808-0123456789abcdef0123456789abcdef\"")]
    [InlineData("\"schedule-v3-00000000000000000000000000000000\"")]
    [InlineData("\"v3-0123456789abcdef0123456789abcdef\"")]
    [InlineData("\"schedule-v3-0123456789abcdef0123456789abcdef\", \"schedule-v4-0123456789abcdef0123456789abcdef\"")]
    public async Task AnETagThatIsNotTheGatewaysTokenIsRefusedBeforeTransportOnEveryConditionalCall(string eTag)
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(Schedule(HttpStatusCode.OK, ScheduleJson(), null));
        });
        await using var client = await ClientAsync(handler);

        await Assert.ThrowsAsync<ArgumentException>(() => client.Schedules.UpdateAsync(Definition(), eTag, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => client.Schedules.DeleteAsync("nightly", eTag, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => client.Schedules.TriggerAsync("nightly", eTag, Run(), TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => client.Schedules.UpdateAsync(Definition(), null!, TestContext.Current.CancellationToken));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task TheLargestVersionThatFitsA64BitIntegerIsAnAcceptedETag()
    {
        HttpRequestMessage? observed = null;
        var handler = new ScriptedHandler((request, _) =>
        {
            observed = Clone(request);
            return Task.FromResult(Schedule(HttpStatusCode.NoContent, string.Empty, null));
        });
        await using var client = await ClientAsync(handler);
        var eTag = "\"schedule-v9223372036854775807-" + InstanceHex + "\"";

        await client.Schedules.DeleteAsync("nightly", eTag, TestContext.Current.CancellationToken);

        Assert.Equal([eTag], observed!.Headers.GetValues("If-Match"));
    }

    // ---------------------------------------------------------------------------------------
    // Get
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task GetReadsOneScheduleByItsRouteAndBuildsItsETagFromTheBody()
    {
        HttpRequestMessage? observed = null;
        var handler = new ScriptedHandler((request, _) =>
        {
            observed = Clone(request);
            return Task.FromResult(Schedule(HttpStatusCode.OK, ScheduleJson(), ETag3));
        });
        await using var client = await ClientAsync(handler);

        var schedule = await client.Schedules.GetAsync("nightly", TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, observed!.Method);
        Assert.Equal(ItemRoute, observed.RequestUri!.AbsoluteUri);
        Assert.Equal(new AuthenticationHeaderValue("Bearer", "access-token"), observed.Headers.Authorization);
        Assert.Null(observed.Content);
        Assert.False(observed.Headers.Contains("If-Match"));
        Assert.Equal(ETag3, schedule.ETag);
        Assert.Equal("3", schedule.Version);
    }

    [Fact]
    public async Task GetMapsAOneShotsInstantAndTheNullMembersOfARecurringSchedule()
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(calls == 1
                ? Schedule(HttpStatusCode.OK, ScheduleJson(lastOccurrence: null, oneShotAtUtc: "2026-10-04T01:02:03+00:00"), ETag3)
                : Schedule(HttpStatusCode.OK, ScheduleJson(), ETag3));
        });
        await using var client = await ClientAsync(handler);

        var oneShot = await client.Schedules.GetAsync("nightly", TestContext.Current.CancellationToken);
        var recurring = await client.Schedules.GetAsync("nightly", TestContext.Current.CancellationToken);

        Assert.True(oneShot.IsOneShot);
        Assert.Equal(DateTimeOffset.Parse("2026-10-04T01:02:03+00:00", CultureInfo.InvariantCulture), oneShot.OneShotAtUtc);
        Assert.Null(oneShot.LastOccurrence);
        Assert.False(recurring.IsOneShot);
        Assert.NotNull(recurring.LastOccurrence);
    }

    [Fact]
    public async Task AStrongETagHeaderThatDisagreesWithTheBodyIsRefusedAndAWeakOrMissingOneIsIgnored()
    {
        var headers = new Queue<string?>(new string?[]
        {
            "\"schedule-v4-" + InstanceHex + "\"",
            "\"schedule-v3-ffffffffffffffffffffffffffffffff\"",
            "W/\"schedule-v9-" + InstanceHex + "\"",
            null,
        });
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Schedule(HttpStatusCode.OK, ScheduleJson(), headers.Dequeue())));
        await using var client = await ClientAsync(handler);

        // Two strong headers that are not the body's token: the version is another, and so is the instance.
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var exception = await Assert.ThrowsAsync<YogabaseApiException>(
                () => client.Schedules.GetAsync("nightly", TestContext.Current.CancellationToken));
            Assert.Equal(YogabaseScheduleErrors.ResponseInvalid, exception.Code);
        }

        // A proxy that compresses a response may weaken its ETag, and the body carries the same facts.
        Assert.Equal(ETag3, (await client.Schedules.GetAsync("nightly", TestContext.Current.CancellationToken)).ETag);
        Assert.Equal(ETag3, (await client.Schedules.GetAsync("nightly", TestContext.Current.CancellationToken)).ETag);
    }

    [Theory]
    [InlineData("scheduleId", "\"other\"")]
    [InlineData("scheduleId", "null")]
    [InlineData("projectId", "\"33333333-3333-4333-8333-333333333333\"")]
    [InlineData("projectId", "\"not-a-uuid\"")]
    [InlineData("subject", "\"00000000-0000-0000-0000-000000000000\"")]
    [InlineData("subject", "\"not-a-uuid\"")]
    [InlineData("functionId", "\"\"")]
    [InlineData("functionVersion", "null")]
    [InlineData("version", "\"0\"")]
    [InlineData("version", "\"03\"")]
    [InlineData("version", "\"abc\"")]
    [InlineData("version", "\"9223372036854775808\"")]
    [InlineData("version", "3")]
    [InlineData("instanceId", "\"00000000-0000-0000-0000-000000000000\"")]
    [InlineData("instanceId", "\"0123456789abcdef0123456789abcdef\"")]
    [InlineData("instanceId", "null")]
    public async Task AScheduleTheGatewayDescribesWronglyIsRefusedNotGuessedAt(string member, string replacement)
    {
        var json = ReplaceMember(ScheduleJson(), member, replacement);
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Schedule(HttpStatusCode.OK, json, null)));
        await using var client = await ClientAsync(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(
            () => client.Schedules.GetAsync("nightly", TestContext.Current.CancellationToken));

        // A number where the version's text belongs does not even deserialize, and reads as response_invalid.
        Assert.True(
            exception.Code is YogabaseScheduleErrors.ResponseInvalid or "response_invalid",
            $"Unexpected code {exception.Code}.");
    }

    [Theory]
    [InlineData("{\"scheduleId\":\"nightly\"}")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("not json")]
    public async Task AnAnswerThatIsNotAScheduleDocumentIsRefused(string json)
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Schedule(HttpStatusCode.OK, json, null)));
        await using var client = await ClientAsync(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(
            () => client.Schedules.GetAsync("nightly", TestContext.Current.CancellationToken));

        Assert.True(exception.Code is YogabaseScheduleErrors.ResponseInvalid or "response_invalid");
    }

    [Fact]
    public async Task AScheduleWithAMemberTheRouteDoesNotDeclareIsRefused()
    {
        var json = ScheduleJson().TrimEnd('}') + ",\"extra\":1}";
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Schedule(HttpStatusCode.OK, json, null)));
        await using var client = await ClientAsync(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(
            () => client.Schedules.GetAsync("nightly", TestContext.Current.CancellationToken));

        Assert.Equal("response_invalid", exception.Code);
    }

    // ---------------------------------------------------------------------------------------
    // Delete
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task DeleteSendsIfMatchWithNoBodyAndAnswers204()
    {
        HttpRequestMessage? observed = null;
        var handler = new ScriptedHandler((request, _) =>
        {
            observed = Clone(request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });
        await using var client = await ClientAsync(handler);

        await client.Schedules.DeleteAsync("nightly", ETag3, TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Delete, observed!.Method);
        Assert.Equal(ItemRoute, observed.RequestUri!.AbsoluteUri);
        Assert.Equal([ETag3], observed.Headers.GetValues("If-Match"));
        Assert.Null(observed.Content);
    }

    // ---------------------------------------------------------------------------------------
    // Trigger
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task TriggerSendsTheExactBodyWithIfMatchAndReadsBothOutcomes()
    {
        HttpRequestMessage? observed = null;
        string? body = null;
        var calls = 0;
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            calls++;
            observed = Clone(request);
            body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return Json(HttpStatusCode.Accepted, "{\"outcome\":" + (calls == 1 ? 0 : 1) + ",\"invocationId\":\"9b9e2f3a-0d6e-4e6c-a4f1-2c6a7a5f0b11\"}");
        });
        await using var client = await ClientAsync(handler);
        var run = new YogabaseScheduleTriggerRequest
        {
            IdempotencyKey = "run-1",
            Deadline = new DateTimeOffset(2026, 10, 3, 21, 30, 0, TimeSpan.FromHours(9)),
        };

        var first = await client.Schedules.TriggerAsync("nightly", ETag3, run, TestContext.Current.CancellationToken);
        var repeat = await client.Schedules.TriggerAsync("nightly", ETag3, run, TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, observed!.Method);
        Assert.Equal(RunRoute, observed.RequestUri!.AbsoluteUri);
        Assert.Equal([ETag3], observed.Headers.GetValues("If-Match"));
        Assert.False(observed.Headers.Contains("If-None-Match"));
        // 21:30 at +09:00 is 12:30 UTC.
        Assert.Equal("{\"idempotencyKey\":\"run-1\",\"deadline\":\"2026-10-03T12:30:00+00:00\"}", body);
        Assert.Equal(YogabaseScheduleTriggerOutcome.Enqueued, first.Outcome);
        Assert.Equal(YogabaseScheduleTriggerOutcome.Replayed, repeat.Outcome);
        Assert.Equal(first.InvocationId, repeat.InvocationId);
        Assert.Equal(Guid.Parse("9b9e2f3a-0d6e-4e6c-a4f1-2c6a7a5f0b11"), first.InvocationId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" padded")]
    [InlineData("padded ")]
    [InlineData("long")]
    public async Task AMalformedRunKeyIsRefusedBeforeTransport(string key)
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(Json(HttpStatusCode.Accepted, "{}"));
        });
        await using var client = await ClientAsync(handler);
        var request = Run();
        request.IdempotencyKey = key == "long" ? new string('k', 257) : key;

        await Assert.ThrowsAsync<ArgumentException>(
            () => client.Schedules.TriggerAsync("nightly", ETag3, request, TestContext.Current.CancellationToken));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task ARunNeedsADeadlineAndARequestAndAKeyOfTheLongestLengthIsSent()
    {
        string? body = null;
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return Json(HttpStatusCode.Accepted, "{\"outcome\":0,\"invocationId\":\"9b9e2f3a-0d6e-4e6c-a4f1-2c6a7a5f0b11\"}");
        });
        await using var client = await ClientAsync(handler);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => client.Schedules.TriggerAsync("nightly", ETag3, null!, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => client.Schedules.TriggerAsync(
            "nightly",
            ETag3,
            new YogabaseScheduleTriggerRequest { IdempotencyKey = "k" },
            TestContext.Current.CancellationToken));
        var longest = Run();
        longest.IdempotencyKey = new string('k', 256);
        await client.Schedules.TriggerAsync("nightly", ETag3, longest, TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(body!);
        Assert.Equal(256, document.RootElement.GetProperty("idempotencyKey").GetString()!.Length);
    }

    [Theory]
    [InlineData("{\"outcome\":2,\"invocationId\":\"9b9e2f3a-0d6e-4e6c-a4f1-2c6a7a5f0b11\"}")]
    [InlineData("{\"outcome\":-1,\"invocationId\":\"9b9e2f3a-0d6e-4e6c-a4f1-2c6a7a5f0b11\"}")]
    [InlineData("{\"invocationId\":\"9b9e2f3a-0d6e-4e6c-a4f1-2c6a7a5f0b11\"}")]
    [InlineData("{\"outcome\":0,\"invocationId\":\"00000000-0000-0000-0000-000000000000\"}")]
    [InlineData("{\"outcome\":0,\"invocationId\":\"nope\"}")]
    [InlineData("{\"outcome\":0}")]
    public async Task ARunTheGatewayDescribesWronglyIsRefused(string json)
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Json(HttpStatusCode.Accepted, json)));
        await using var client = await ClientAsync(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(
            () => client.Schedules.TriggerAsync("nightly", ETag3, Run(), TestContext.Current.CancellationToken));

        Assert.Equal(YogabaseScheduleErrors.ResponseInvalid, exception.Code);
    }

    // ---------------------------------------------------------------------------------------
    // Refusals
    // ---------------------------------------------------------------------------------------

    [Theory]
    [InlineData(400, "request_failed")]
    [InlineData(401, "authentication_required")]
    [InlineData(403, "forbidden")]
    [InlineData(404, "not_found")]
    [InlineData(409, "conflict")]
    [InlineData(412, "precondition_failed")]
    [InlineData(415, "unsupported_media_type")]
    [InlineData(428, "precondition_required")]
    [InlineData(503, "request_failed")]
    public async Task EveryRefusalOfAWriteReadsAsItsStatusWhereTheGatewayNamesNoCode(int status, string code)
    {
        // The gateway answers a refusal with a fixed title (or with no body at all) and names no code.
        var handler = new ScriptedHandler((_, _) => Task.FromResult(
            status is 404 or 409 or 415
                ? new HttpResponseMessage((HttpStatusCode)status)
                : ProblemWithoutCode((HttpStatusCode)status, "Fixed title.")));
        await using var client = await ClientAsync(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(
            () => client.Schedules.UpdateAsync(Definition(), ETag3, TestContext.Current.CancellationToken));

        Assert.Equal(status, exception.StatusCode);
        Assert.Equal(code, exception.Code);
        Assert.Null(exception.LimitBytes);
        Assert.Null(exception.MaxBodyBytes);
    }

    [Theory]
    [InlineData(412, "precondition_failed")]
    [InlineData(428, "precondition_required")]
    [InlineData(404, "not_found")]
    public async Task EveryRefusalOfADeleteReadsAsItsStatus(int status, string code)
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(
            status == 404 ? new HttpResponseMessage(HttpStatusCode.NotFound) : ProblemWithoutCode((HttpStatusCode)status, "Fixed title.")));
        await using var client = await ClientAsync(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(
            () => client.Schedules.DeleteAsync("nightly", ETag3, TestContext.Current.CancellationToken));

        Assert.Equal(status, exception.StatusCode);
        Assert.Equal(code, exception.Code);
    }

    [Theory]
    [InlineData(400, "request_failed")]
    [InlineData(404, "not_found")]
    [InlineData(409, "conflict")]
    [InlineData(412, "precondition_failed")]
    [InlineData(428, "precondition_required")]
    [InlineData(503, "request_failed")]
    public async Task EveryRefusalOfARunReadsAsItsStatus(int status, string code)
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(
            status is 404 or 409 ? new HttpResponseMessage((HttpStatusCode)status) : ProblemWithoutCode((HttpStatusCode)status, "Fixed title.")));
        await using var client = await ClientAsync(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(
            () => client.Schedules.TriggerAsync("nightly", ETag3, Run(), TestContext.Current.CancellationToken));

        Assert.Equal(status, exception.StatusCode);
        Assert.Equal(code, exception.Code);
    }

    [Fact]
    public async Task APayloadTooLargeRefusalNamesTheCodeAndBothLimits()
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Problem(
            HttpStatusCode.RequestEntityTooLarge,
            "The schedule payload is too large.",
            YogabaseScheduleErrors.PayloadTooLarge,
            "The request is larger than this route accepts. A schedule's payload may be at most 1048576 bytes before base64 encoding, and the whole request body at most 8394167 bytes.",
            1_048_576,
            8_394_167)));
        await using var client = await ClientAsync(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(
            () => client.Schedules.CreateAsync(Definition(), TestContext.Current.CancellationToken));

        Assert.Equal(413, exception.StatusCode);
        Assert.Equal("schedule_payload_too_large", exception.Code);
        Assert.Equal(1_048_576, exception.LimitBytes);
        Assert.Equal(8_394_167, exception.MaxBodyBytes);
        Assert.Contains("8394167", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ABare413FromAGatewayThatPredatesTheNamedRefusalStillReadsAsPayloadTooLarge()
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.RequestEntityTooLarge)));
        await using var client = await ClientAsync(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(
            () => client.Schedules.CreateAsync(Definition(), TestContext.Current.CancellationToken));

        Assert.Equal(413, exception.StatusCode);
        Assert.Equal(YogabaseStatusErrors.PayloadTooLarge, exception.Code);
        Assert.NotEqual(YogabaseScheduleErrors.PayloadTooLarge, exception.Code);
        Assert.Null(exception.LimitBytes);
    }

    [Fact]
    public async Task AnInstallationWithoutTheSchedulerAnswersABareNotFoundOnEveryRoute()
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        await using var client = await ClientAsync(handler);

        foreach (var call in new Func<Task>[]
        {
            () => client.Schedules.GetAsync("nightly", TestContext.Current.CancellationToken),
            () => client.Schedules.DeleteAsync("nightly", ETag3, TestContext.Current.CancellationToken),
            () => client.Schedules.TriggerAsync("nightly", ETag3, Run(), TestContext.Current.CancellationToken),
        })
        {
            var exception = await Assert.ThrowsAsync<YogabaseApiException>(call);
            Assert.Equal(404, exception.StatusCode);
            Assert.Equal(YogabaseStatusErrors.NotFound, exception.Code);
        }
    }

    [Fact]
    public async Task ASignedOutCallIsAnAuthenticationRefusalBeforeTransport()
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(Schedule(HttpStatusCode.OK, ScheduleJson(), null));
        });
        await using var client = YogabaseClient.FromConfiguration(
            Configuration(YogabaseClientTests.Capabilities()),
            new YogabaseClientOptions { AllowInsecureLoopback = true, TransportHandler = handler });

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(
            () => client.Schedules.GetAsync("nightly", TestContext.Current.CancellationToken));

        Assert.Equal(401, exception.StatusCode);
        Assert.Equal("authentication_required", exception.Code);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task SchedulesAreNotGatedOnAnyCapabilityBecauseTheBootstrapNamesNoSchedulesCapability()
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(Schedule(HttpStatusCode.OK, ScheduleJson(), null));
        });
        IReadOnlyList<YogabaseCapability> capabilities =
        [
            new() { Id = "auth", Available = true },
            new() { Id = "storage", Available = true },
            new() { Id = "functions-rest", Available = false, UnavailableReason = "preview-disabled" },
            new() { Id = "realtime-subscribe", Available = false, UnavailableReason = "preview-disabled" },
        ];
        await using var client = await ClientAsync(handler, capabilities);

        await client.Schedules.GetAsync("nightly", TestContext.Current.CancellationToken);

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task ACancelledTokenStopsAScheduleCallBeforeItReachesTheGateway()
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(Schedule(HttpStatusCode.OK, ScheduleJson(), null));
        });
        await using var client = await ClientAsync(handler);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.Schedules.GetAsync("nightly", cancelled.Token));
        Assert.Equal(0, calls);
    }

    // ---------------------------------------------------------------------------------------
    // Routes and limits
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void TheScheduleRoutesAreDefinedOnceInYogabaseRoutes()
    {
        Assert.Equal("v1/projects/{projectId}/schedules/{scheduleId}", YogabaseRoutes.ScheduleTemplate);
        Assert.Equal("v1/projects/{projectId}/schedules/{scheduleId}/trigger", YogabaseRoutes.ScheduleTriggerTemplate);
        Assert.Equal(
            "v1/projects/11111111-1111-4111-8111-111111111111/schedules/nightly",
            YogabaseRoutes.Schedule(ProjectId, "nightly"));
        Assert.Equal(
            "v1/projects/11111111-1111-4111-8111-111111111111/schedules/nightly/trigger",
            YogabaseRoutes.ScheduleTrigger(ProjectId, "nightly"));
        Assert.Throws<ArgumentException>(() => YogabaseRoutes.Schedule(Guid.Empty, "nightly"));
    }

    [Fact]
    public void TheLimitsAreTheOnesTheContractStates()
    {
        // The contract-derived pins live in ContractOperationParityTests; these are the figures a caller reads.
        Assert.Equal(1_048_576, YogabaseSchedulesLimits.MaximumPayloadBytes);
        Assert.Equal(128, YogabaseSchedulesLimits.MaximumScheduleIdLength);
        Assert.Equal(16, YogabaseSchedulesLimits.MaximumAttempts);
        Assert.Equal(86_400, YogabaseSchedulesLimits.MaximumDeadlineSeconds);
        Assert.Equal(3_600, YogabaseSchedulesLimits.MaximumManualDeadlineSeconds);
    }

    // ---------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------

    private static YogabaseScheduleDefinition Definition() => new()
    {
        ScheduleId = "nightly",
        FunctionId = "orders-nightly",
        FunctionVersion = "1",
        Cron = "*/5 * * * *",
        TimeZone = "UTC",
        MisfirePolicy = YogabaseScheduleMisfirePolicy.SkipToNextOccurrence,
        ConcurrencyPolicy = YogabaseScheduleConcurrencyPolicy.Allow,
        MaxCatchUp = 1,
        MaxAttempts = 3,
        DeadlineSeconds = 300,
        Payload = Encoding.UTF8.GetBytes("hello"),
    };

    private static YogabaseScheduleTriggerRequest Run() => new()
    {
        IdempotencyKey = "run-1",
        Deadline = DateTimeOffset.UtcNow.AddMinutes(10),
    };

    private static YogabaseScheduleDefinition Mutate(YogabaseScheduleDefinition definition, string mutation)
    {
        switch (mutation)
        {
            case "blank-schedule-id": definition.ScheduleId = "  "; break;
            case "padded-schedule-id": definition.ScheduleId = " nightly"; break;
            case "control-schedule-id": definition.ScheduleId = "night\nly"; break;
            case "long-schedule-id": definition.ScheduleId = new string('s', 129); break;
            case "dot-schedule-id": definition.ScheduleId = "."; break;
            case "dots-schedule-id": definition.ScheduleId = ".."; break;
            case "trailing-space-schedule-id": definition.ScheduleId = "nightly "; break;
            case "slash-schedule-id": definition.ScheduleId = "weekly/report"; break;
            case "space-schedule-id": definition.ScheduleId = "weekly report"; break;
            case "dash-first-schedule-id": definition.ScheduleId = "-nightly"; break;
            case "dot-first-schedule-id": definition.ScheduleId = ".nightly"; break;
            case "underscore-first-schedule-id": definition.ScheduleId = "_nightly"; break;
            case "tilde-first-schedule-id": definition.ScheduleId = "~nightly"; break;
            case "colon-first-schedule-id": definition.ScheduleId = ":nightly"; break;
            case "percent-schedule-id": definition.ScheduleId = "weekly%2Freport"; break;
            case "question-schedule-id": definition.ScheduleId = "nightly?"; break;
            case "hash-schedule-id": definition.ScheduleId = "night#ly"; break;
            case "non-ascii-letter-schedule-id": definition.ScheduleId = "nightlé"; break;
            // ARABIC-INDIC DIGIT THREE: a digit to char.IsDigit, but not to the contract's [0-9].
            case "non-ascii-digit-schedule-id": definition.ScheduleId = "night٣"; break;
            // .NET's $ would let this through; the contract's pattern does not.
            case "trailing-newline-schedule-id": definition.ScheduleId = "nightly\n"; break;
            case "blank-function-id": definition.FunctionId = " "; break;
            case "long-function-id": definition.FunctionId = new string('f', 257); break;
            case "blank-function-version": definition.FunctionVersion = string.Empty; break;
            case "long-function-version": definition.FunctionVersion = new string('v', 65); break;
            case "blank-cron": definition.Cron = string.Empty; break;
            case "long-cron": definition.Cron = new string('1', 129); break;
            case "blank-time-zone": definition.TimeZone = string.Empty; break;
            case "long-time-zone": definition.TimeZone = new string('z', 129); break;
            case "no-misfire-policy": definition.MisfirePolicy = null; break;
            case "undefined-misfire-policy": definition.MisfirePolicy = (YogabaseScheduleMisfirePolicy)9; break;
            case "no-concurrency-policy": definition.ConcurrencyPolicy = null; break;
            case "undefined-concurrency-policy": definition.ConcurrencyPolicy = (YogabaseScheduleConcurrencyPolicy)9; break;
            case "catch-up-zero": definition.MaxCatchUp = 0; break;
            case "catch-up-over": definition.MaxCatchUp = 129; break;
            case "attempts-zero": definition.MaxAttempts = 0; break;
            case "attempts-over": definition.MaxAttempts = 17; break;
            case "deadline-zero": definition.DeadlineSeconds = 0; break;
            case "deadline-over": definition.DeadlineSeconds = 86_401; break;
            case "one-shot-unset-instant": definition.OneShotAtUtc = default(DateTimeOffset); break;
            default: throw new ArgumentOutOfRangeException(nameof(mutation), mutation, "Unknown mutation.");
        }

        return definition;
    }

    private static string ScheduleJson(
        string scheduleId = "nightly",
        string? projectId = null,
        string version = "3",
        string instanceId = Instance,
        string? lastOccurrence = "2026-10-03T05:00:00+00:00",
        string? oneShotAtUtc = null) => JsonSerializer.Serialize(new
        {
            scheduleId,
            projectId = projectId ?? ProjectId.ToString("D"),
            subject = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
            functionId = "orders-nightly",
            functionVersion = "1",
            version,
            instanceId,
            lastOccurrence,
            oneShotAtUtc,
        });

    // Replaces one top-level member of a flat JSON object with raw JSON text, keeping the others.
    private static string ReplaceMember(string json, string member, string replacementJson)
    {
        using var document = JsonDocument.Parse(json);
        var builder = new StringBuilder("{");
        var first = true;
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (!first)
            {
                builder.Append(',');
            }

            first = false;
            builder.Append(JsonSerializer.Serialize(property.Name)).Append(':')
                .Append(property.Name == member ? replacementJson : property.Value.GetRawText());
        }

        return builder.Append('}').ToString();
    }

    private static HttpResponseMessage Schedule(HttpStatusCode status, string json, string? eTag)
    {
        var response = new HttpResponseMessage(status);
        if (json.Length > 0)
        {
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        if (eTag is not null)
        {
            response.Headers.TryAddWithoutValidation("ETag", eTag);
        }

        return response;
    }

    // The problem ASP.NET Core writes for Results.Problem(statusCode, title): no code and no limits.
    private static HttpResponseMessage ProblemWithoutCode(HttpStatusCode status, string title) => new(status)
    {
        Content = new StringContent(
            JsonSerializer.Serialize(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["type"] = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                ["title"] = title,
                ["status"] = (int)status,
                ["traceId"] = "00-0123456789abcdef0123456789abcdef-0123456789abcdef-00",
            }),
            Encoding.UTF8,
            "application/problem+json"),
    };

    private static HttpResponseMessage Problem(
        HttpStatusCode status,
        string title,
        string code,
        string? detail = null,
        long? limitBytes = null,
        long? maxBodyBytes = null)
    {
        var members = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["type"] = "https://tools.ietf.org/html/rfc9110#section-15.5.14",
            ["title"] = title,
            ["status"] = (int)status,
        };
        if (detail is not null)
        {
            members["detail"] = detail;
        }

        members["traceId"] = "00-0123456789abcdef0123456789abcdef-0123456789abcdef-00";
        members["code"] = code;
        if (limitBytes is not null)
        {
            members["limitBytes"] = limitBytes;
        }

        if (maxBodyBytes is not null)
        {
            members["maxBodyBytes"] = maxBodyBytes;
        }

        return new HttpResponseMessage(status)
        {
            Content = new StringContent(JsonSerializer.Serialize(members), Encoding.UTF8, "application/problem+json"),
        };
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string json) => new(status)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static HttpRequestMessage Clone(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // The content itself is not cloned (a test that needs the body reads it in the handler), but its
        // presence is, so a call that must send none can say so.
        if (request.Content is not null)
        {
            clone.Content = new ByteArrayContent([]);
        }

        return clone;
    }

    private static async Task<YogabaseClient> ClientAsync(
        HttpMessageHandler handler,
        IReadOnlyList<YogabaseCapability>? capabilities = null)
    {
        var store = new InMemoryYogabaseSessionStore();
        await store.SaveAsync(
            new YogabaseTokenSet
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            },
            TestContext.Current.CancellationToken);
        return YogabaseClient.FromConfiguration(
            Configuration(capabilities ?? YogabaseClientTests.Capabilities()),
            new YogabaseClientOptions
            {
                AllowInsecureLoopback = true,
                SessionStore = store,
                TransportHandler = handler,
            });
    }

    private static YogabaseClientConfiguration Configuration(IReadOnlyList<YogabaseCapability> capabilities) => new()
    {
        Schema = YogabaseClient.ClientConfigSchema,
        ProjectId = ProjectId,
        EnvironmentId = EnvironmentId,
        GatewayEndpoint = "http://localhost/",
        RealtimeEndpoint = "http://localhost:5001/",
        OidcAuthority = "http://localhost/p/11111111-1111-4111-8111-111111111111",
        OidcDiscoveryEndpoint = "http://localhost/.well-known/openid-configuration",
        OidcPublicClientId = "stackyoga-native-preview",
        OidcScopes = ["openid", "offline_access"],
        OidcRedirectUris = ["com.stackyoga.app:/oauth/callback"],
        RealtimePath = "/v1/realtime",
        Capabilities = capabilities,
        IssuedAt = DateTimeOffset.Parse("2026-10-03T00:00:00+00:00", CultureInfo.InvariantCulture),
    };

    private sealed class ScriptedHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
