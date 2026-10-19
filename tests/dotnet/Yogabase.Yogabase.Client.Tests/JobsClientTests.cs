using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Yogabase.Yogabase;

namespace Yogabase.Tests;

/// <summary>
/// Wire-shape coverage for the durable Jobs surface and for the 413 of the function invoke, actor invoke
/// and workflow routes, asserted against a fake handler: these tests pin the exact request the SDK emits
/// and how it reads and refuses what the gateway answers, and say nothing about the server's behaviour.
/// The shapes are the ones the mirrored client OpenAPI document declares for the Jobs routes
/// (<c>x-yogabase-jobs</c>) and the product's gateway writes (<c>DurableJobsGateway</c>);
/// <see cref="ContractOperationParityTests"/> holds the operations to the document and
/// <see cref="ContractLimitsAndCodesTests"/> its limits and codes.
/// </summary>
public sealed class JobsClientTests
{
    private static readonly Guid ProjectId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid EnvironmentId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid JobId = Guid.Parse("5d0a8f0e-3c63-4d5e-9a0b-7c1f3a9e2b41");
    private const string JobsRoute = "http://localhost/v1/projects/11111111-1111-4111-8111-111111111111/jobs";
    private const string DigestHeader = "X-Yogabase-Content-Sha256";

    // ---------------------------------------------------------------------------------------
    // Submit
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task SubmitSendsTheExactRouteBodyAndUserTokenAndReadsTheAcceptedJob()
    {
        HttpRequestMessage? observed = null;
        string? body = null;
        string? contentType = null;
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            observed = Clone(request);
            body = await request.Content!.ReadAsStringAsync(cancellationToken);
            contentType = request.Content.Headers.ContentType?.MediaType;
            return Json(HttpStatusCode.Accepted, JobJson());
        });
        await using var client = await ClientAsync(handler);

        var job = await client.Jobs.SubmitAsync(
            new YogabaseJobSubmitRequest
            {
                JobType = "orders-create",
                IdempotencyKey = "order-42",
                Payload = Encoding.UTF8.GetBytes("hello"),
                MaxAttempts = 3,
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, observed!.Method);
        Assert.Equal(JobsRoute, observed.RequestUri!.AbsoluteUri);
        Assert.Equal(new AuthenticationHeaderValue("Bearer", "access-token"), observed.Headers.Authorization);
        Assert.Equal("application/json", contentType);
        // The member order and the names are the product's DurableJobSubmission, which refuses any other member.
        Assert.Equal(
            "{\"jobType\":\"orders-create\",\"idempotencyKey\":\"order-42\",\"payloadBase64\":\"aGVsbG8=\",\"maxAttempts\":3}",
            body);
        // The key rides in the body: the gateway reads no Idempotency-Key header on this route.
        Assert.False(observed.Headers.Contains("Idempotency-Key"));

        Assert.Equal(JobId, job.JobId);
        Assert.Equal("orders-create", job.JobType);
        Assert.Equal(YogabaseJobStatus.Queued, job.Status);
        Assert.Equal(0, job.Attempt);
        Assert.Equal(5, job.MaxAttempts);
        Assert.Equal(0, job.ProgressPercent);
        Assert.False(job.CancellationRequested);
        Assert.Null(job.ResultReference);
        Assert.Null(job.FailureCode);
        Assert.Equal(DateTimeOffset.Parse("2026-10-03T05:00:00+00:00", System.Globalization.CultureInfo.InvariantCulture), job.CreatedAt);
        Assert.Null(job.CompletedAt);
        Assert.False(job.IsTerminal);
    }

    [Fact]
    public async Task SubmitOmitsAnAbsentOrEmptyPayloadAndAnUnsetAttemptBudget()
    {
        var bodies = new List<string>();
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            bodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return Json(HttpStatusCode.Accepted, JobJson(jobType: "emails-send"));
        });
        await using var client = await ClientAsync(handler);

        await client.Jobs.SubmitAsync(
            new YogabaseJobSubmitRequest { JobType = "emails-send", IdempotencyKey = "k" },
            TestContext.Current.CancellationToken);
        await client.Jobs.SubmitAsync(
            new YogabaseJobSubmitRequest { JobType = "emails-send", IdempotencyKey = "k", Payload = [] },
            TestContext.Current.CancellationToken);

        // A job with no payload and one with an empty payload are the same job to the gateway.
        Assert.Equal(["{\"jobType\":\"emails-send\",\"idempotencyKey\":\"k\"}", "{\"jobType\":\"emails-send\",\"idempotencyKey\":\"k\"}"], bodies);
    }

    [Fact]
    public async Task ARetriedSubmitIsTheSameRequestOnTheWireSoTheGatewayCanReplayIt()
    {
        var bodies = new List<string>();
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            bodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            // 202 again, whatever the job has become: the replay is the job it already made.
            return Json(HttpStatusCode.Accepted, JobJson(status: bodies.Count == 1 ? "Queued" : "Running"));
        });
        await using var client = await ClientAsync(handler);
        var request = new YogabaseJobSubmitRequest
        {
            JobType = "orders-create",
            IdempotencyKey = "order-42",
            Payload = [1, 2, 3],
        };

        var first = await client.Jobs.SubmitAsync(request, TestContext.Current.CancellationToken);
        var replay = await client.Jobs.SubmitAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(bodies[0], bodies[1]);
        Assert.Equal(first.JobId, replay.JobId);
        Assert.Equal(YogabaseJobStatus.Running, replay.Status);
    }

    [Fact]
    public async Task ASubmitAtExactlyTheInlineLimitIsSentAndOneByteMoreIsRefusedBeforeTransport()
    {
        string? body = null;
        var calls = 0;
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            calls++;
            body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return Json(HttpStatusCode.Accepted, JobJson());
        });
        await using var client = await ClientAsync(handler);

        await client.Jobs.SubmitAsync(
            new YogabaseJobSubmitRequest
            {
                JobType = "orders-create",
                IdempotencyKey = "k-max",
                Payload = new byte[YogabaseJobsLimits.MaximumPayloadBytes],
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(1, calls);
        using var document = JsonDocument.Parse(body!);
        Assert.Equal(1_398_104, document.RootElement.GetProperty("payloadBase64").GetString()!.Length);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.Jobs.SubmitAsync(
            new YogabaseJobSubmitRequest
            {
                JobType = "orders-create",
                IdempotencyKey = "k-over",
                Payload = new byte[YogabaseJobsLimits.MaximumPayloadBytes + 1],
            },
            TestContext.Current.CancellationToken));
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" orders-create")]
    [InlineData("orders-create ")]
    [InlineData("orders create")]
    [InlineData("orders/create")]
    [InlineData("orders\ncreate")]
    [InlineData("ordérs")]
    public async Task AMalformedJobTypeIsRefusedBeforeTransport(string jobType)
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(Json(HttpStatusCode.Accepted, JobJson()));
        });
        await using var client = await ClientAsync(handler);

        await Assert.ThrowsAsync<ArgumentException>(() => client.Jobs.SubmitAsync(
            new YogabaseJobSubmitRequest { JobType = jobType, IdempotencyKey = "k" },
            TestContext.Current.CancellationToken));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task AJobTypeOfTheLongestLengthIsSentAndALongerOneIsRefused()
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(Json(HttpStatusCode.Accepted, JobJson(jobType: new string('a', 128))));
        });
        await using var client = await ClientAsync(handler);

        await client.Jobs.SubmitAsync(
            new YogabaseJobSubmitRequest { JobType = new string('a', 128), IdempotencyKey = "k" },
            TestContext.Current.CancellationToken);
        Assert.Equal(1, calls);

        await Assert.ThrowsAsync<ArgumentException>(() => client.Jobs.SubmitAsync(
            new YogabaseJobSubmitRequest { JobType = new string('a', 129), IdempotencyKey = "k" },
            TestContext.Current.CancellationToken));
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" padded")]
    [InlineData("padded ")]
    [InlineData("has\ncontrol")]
    public async Task AMalformedIdempotencyKeyIsRefusedBeforeTransport(string key)
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(Json(HttpStatusCode.Accepted, JobJson()));
        });
        await using var client = await ClientAsync(handler);

        await Assert.ThrowsAsync<ArgumentException>(() => client.Jobs.SubmitAsync(
            new YogabaseJobSubmitRequest { JobType = "orders-create", IdempotencyKey = key },
            TestContext.Current.CancellationToken));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task AnIdempotencyKeyOfTheLongestLengthIsSentAndALongerOneIsRefused()
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(Json(HttpStatusCode.Accepted, JobJson()));
        });
        await using var client = await ClientAsync(handler);

        await client.Jobs.SubmitAsync(
            new YogabaseJobSubmitRequest { JobType = "orders-create", IdempotencyKey = new string('k', 256) },
            TestContext.Current.CancellationToken);
        Assert.Equal(1, calls);

        await Assert.ThrowsAsync<ArgumentException>(() => client.Jobs.SubmitAsync(
            new YogabaseJobSubmitRequest { JobType = "orders-create", IdempotencyKey = new string('k', 257) },
            TestContext.Current.CancellationToken));
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    public async Task AnAttemptBudgetOutsideOneToTenIsRefusedBeforeTransport(int maxAttempts)
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(Json(HttpStatusCode.Accepted, JobJson()));
        });
        await using var client = await ClientAsync(handler);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.Jobs.SubmitAsync(
            new YogabaseJobSubmitRequest { JobType = "orders-create", IdempotencyKey = "k", MaxAttempts = maxAttempts },
            TestContext.Current.CancellationToken));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task ASubmitWithoutARequestIsRefusedBeforeTransport()
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Json(HttpStatusCode.Accepted, JobJson())));
        await using var client = await ClientAsync(handler);

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.Jobs.SubmitAsync(null!, TestContext.Current.CancellationToken));
    }

    // ---------------------------------------------------------------------------------------
    // Refusals
    // ---------------------------------------------------------------------------------------

    [Theory]
    [InlineData(409, "job_idempotency_conflict", "The idempotency key belongs to a different job request.")]
    [InlineData(422, "job_type_not_registered", "The job type is not registered.")]
    [InlineData(400, "job_request_invalid", "Invalid job request.")]
    [InlineData(403, "job_access_denied", "Job access denied.")]
    [InlineData(503, "job_unavailable", "Job persistence unavailable.")]
    public async Task ASubmitTheGatewayRefusesSurfacesItsStatusAndCode(int status, string code, string title)
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Problem((HttpStatusCode)status, title, code)));
        await using var client = await ClientAsync(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Jobs.SubmitAsync(
            new YogabaseJobSubmitRequest { JobType = "orders-create", IdempotencyKey = "k" },
            TestContext.Current.CancellationToken));

        Assert.Equal(status, exception.StatusCode);
        Assert.Equal(code, exception.Code);
        Assert.Equal(title, exception.Message);
        Assert.Null(exception.LimitBytes);
        Assert.Null(exception.MaxBodyBytes);
    }

    [Fact]
    public async Task APayloadTooLargeRefusalNamesTheLimitsItCarries()
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Problem(
            HttpStatusCode.RequestEntityTooLarge,
            "The job payload is too large.",
            YogabaseJobErrors.PayloadTooLarge,
            detail: "The request is larger than this route accepts. A job payload may be at most 1048576 bytes before base64 encoding, and the whole request body at most 8391225 bytes.",
            limitBytes: 1_048_576,
            maxBodyBytes: 8_391_225)));
        await using var client = await ClientAsync(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Jobs.SubmitAsync(
            new YogabaseJobSubmitRequest { JobType = "orders-create", IdempotencyKey = "k", Payload = [1] },
            TestContext.Current.CancellationToken));

        Assert.Equal(413, exception.StatusCode);
        Assert.Equal("job_payload_too_large", exception.Code);
        Assert.Equal(1_048_576, exception.LimitBytes);
        Assert.Equal(8_391_225, exception.MaxBodyBytes);
        Assert.Contains("8391225", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ARefusedSubmitWithNoTokenIsAnAuthenticationRefusalBeforeTransport()
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(Json(HttpStatusCode.Accepted, JobJson()));
        });
        await using var client = YogabaseClient.FromConfiguration(
            Configuration(YogabaseClientTests.Capabilities()),
            new YogabaseClientOptions { AllowInsecureLoopback = true, TransportHandler = handler });

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Jobs.SubmitAsync(
            new YogabaseJobSubmitRequest { JobType = "orders-create", IdempotencyKey = "k" },
            TestContext.Current.CancellationToken));

        Assert.Equal(401, exception.StatusCode);
        Assert.Equal("authentication_required", exception.Code);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task JobsAreNotGatedOnTheFunctionsCapabilityBecauseTheBootstrapNamesNoJobsCapability()
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(Json(HttpStatusCode.Accepted, JobJson()));
        });
        IReadOnlyList<YogabaseCapability> capabilities =
        [
            new() { Id = "auth", Available = true },
            new() { Id = "storage", Available = true },
            new() { Id = "functions-rest", Available = false, UnavailableReason = "preview-disabled" },
            new() { Id = "realtime-subscribe", Available = false, UnavailableReason = "preview-disabled" },
        ];
        await using var client = await ClientAsync(handler, capabilities);

        // Invoking a function is refused on the capability; a job is not, and the gateway decides.
        await Assert.ThrowsAsync<NotSupportedException>(() => client.Functions.InvokeAsync(
            "resize",
            "1",
            new YogabaseInvocationRequest
            {
                IdempotencyKey = "request-1",
                CorrelationId = "correlation-1",
                Deadline = DateTimeOffset.UtcNow.AddSeconds(20),
                InlinePayload = [1],
            },
            TestContext.Current.CancellationToken));
        await client.Jobs.SubmitAsync(
            new YogabaseJobSubmitRequest { JobType = "orders-create", IdempotencyKey = "k" },
            TestContext.Current.CancellationToken);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task AnInstallationWithoutJobsAnswersAPlainNotFoundThatIsNotTheJobNotFoundCode()
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        await using var client = await ClientAsync(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Jobs.GetAsync(
            JobId,
            TestContext.Current.CancellationToken));

        Assert.Equal(404, exception.StatusCode);
        Assert.Equal("not_found", exception.Code);
        Assert.NotEqual(YogabaseJobErrors.NotFound, exception.Code);
    }

    // ---------------------------------------------------------------------------------------
    // Get and cancel
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task GetReadsOneJobByItsRouteAndMapsEveryField()
    {
        HttpRequestMessage? observed = null;
        var handler = new ScriptedHandler((request, _) =>
        {
            observed = Clone(request);
            return Task.FromResult(Json(HttpStatusCode.OK, JobJson(
                status: "Succeeded",
                attempt: 2,
                maxAttempts: 3,
                progress: 100,
                resultReference: "object://invocation-payload/abc/def",
                failureCode: "handler_retry",
                completedAt: "2026-10-03T05:00:09+00:00")));
        });
        await using var client = await ClientAsync(handler);

        var job = await client.Jobs.GetAsync(JobId, TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, observed!.Method);
        Assert.Equal($"{JobsRoute}/5d0a8f0e-3c63-4d5e-9a0b-7c1f3a9e2b41", observed.RequestUri!.AbsoluteUri);
        Assert.Equal(new AuthenticationHeaderValue("Bearer", "access-token"), observed.Headers.Authorization);
        Assert.Null(observed.Content);
        Assert.Equal(YogabaseJobStatus.Succeeded, job.Status);
        Assert.Equal(2, job.Attempt);
        Assert.Equal(3, job.MaxAttempts);
        Assert.Equal(100, job.ProgressPercent);
        Assert.Equal("object://invocation-payload/abc/def", job.ResultReference);
        Assert.Equal("handler_retry", job.FailureCode);
        Assert.Equal(DateTimeOffset.Parse("2026-10-03T05:00:09+00:00", System.Globalization.CultureInfo.InvariantCulture), job.CompletedAt);
        Assert.True(job.IsTerminal);
    }

    [Theory]
    [InlineData("Queued", false)]
    [InlineData("Running", false)]
    [InlineData("Failed", false)]
    [InlineData("Succeeded", true)]
    [InlineData("Cancelled", true)]
    [InlineData("DeadLettered", true)]
    public async Task EveryStatusMapsAndOnlyTheSettledOnesAreTerminal(string status, bool terminal)
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, JobJson(status: status))));
        await using var client = await ClientAsync(handler);

        var job = await client.Jobs.GetAsync(JobId, TestContext.Current.CancellationToken);

        Assert.Equal(status, job.Status.ToString());
        Assert.Equal(terminal, job.IsTerminal);
    }

    [Fact]
    public async Task AnotherUsersJobAndAnUnknownJobAreTheSameNotFound()
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Problem(
            HttpStatusCode.NotFound,
            "Job not found.",
            YogabaseJobErrors.NotFound)));
        await using var client = await ClientAsync(handler);

        var read = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Jobs.GetAsync(
            JobId,
            TestContext.Current.CancellationToken));
        var cancel = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Jobs.CancelAsync(
            JobId,
            TestContext.Current.CancellationToken));

        Assert.Equal((404, "job_not_found"), (read.StatusCode, read.Code));
        Assert.Equal((404, "job_not_found"), (cancel.StatusCode, cancel.Code));
    }

    [Fact]
    public async Task CancelPostsToTheCancelRouteWithNoBodyAndReadsTheJob()
    {
        HttpRequestMessage? observed = null;
        var handler = new ScriptedHandler((request, _) =>
        {
            observed = Clone(request);
            return Task.FromResult(Json(HttpStatusCode.OK, JobJson(status: "Running", cancellation: true)));
        });
        await using var client = await ClientAsync(handler);

        var job = await client.Jobs.CancelAsync(JobId, TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, observed!.Method);
        Assert.Equal($"{JobsRoute}/5d0a8f0e-3c63-4d5e-9a0b-7c1f3a9e2b41/cancel", observed.RequestUri!.AbsoluteUri);
        Assert.Equal(new AuthenticationHeaderValue("Bearer", "access-token"), observed.Headers.Authorization);
        Assert.Null(observed.Content);
        // A running job is flagged and stopped by its worker, so the flag can be set while it still runs.
        Assert.True(job.CancellationRequested);
        Assert.Equal(YogabaseJobStatus.Running, job.Status);
    }

    [Fact]
    public async Task AnEmptyJobIdIsRefusedBeforeTransportOnEveryRoute()
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(Json(HttpStatusCode.OK, JobJson()));
        });
        await using var client = await ClientAsync(handler);

        await Assert.ThrowsAsync<ArgumentException>(() => client.Jobs.GetAsync(Guid.Empty, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => client.Jobs.CancelAsync(Guid.Empty, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => client.Jobs.GetResultAsync(Guid.Empty, TestContext.Current.CancellationToken));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task ACancelledTokenStopsAJobCallBeforeItReachesTheGateway()
    {
        var calls = 0;
        var handler = new ScriptedHandler((_, cancellationToken) =>
        {
            calls++;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Json(HttpStatusCode.OK, JobJson()));
        });
        await using var client = await ClientAsync(handler);
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.Jobs.GetAsync(JobId, source.Token));
        Assert.True(calls <= 1);
    }

    // ---------------------------------------------------------------------------------------
    // Response validation
    // ---------------------------------------------------------------------------------------

    public static TheoryData<string> InvalidJobs() => new()
    {
        // A state this client was never told of, and a number read as a state.
        JobJson(status: "Paused"),
        JobJson(status: "1"),
        JobJson(status: "queued"),
        // Bounds the gateway's own ledger enforces.
        JobJson(maxAttempts: 0),
        JobJson(maxAttempts: 11),
        JobJson(attempt: -1),
        JobJson(progress: 101),
        JobJson(progress: -1),
        // Identity and shape.
        JobJson(jobId: "not-a-guid"),
        JobJson(jobId: "00000000-0000-0000-0000-000000000000"),
        JobJson(jobId: "5D0A8F0E-3C63-4D5E-9A0B-7C1F3A9E2B41".Replace("-", string.Empty, StringComparison.Ordinal)),
        JobJson(jobType: "has space"),
        JobJson(jobType: ""),
        JobJson(failureCode: "Upper_Case"),
        JobJson(resultReference: "https://elsewhere.example/result"),
        JobJson(resultReference: "object://"),
        JobJson(resultReference: "object://with space"),
    };

    [Theory]
    [MemberData(nameof(InvalidJobs))]
    public async Task AJobTheRouteDoesNotDeclareIsRefusedNotGuessedAt(string json)
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, json)));
        await using var client = await ClientAsync(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Jobs.GetAsync(
            JobId,
            TestContext.Current.CancellationToken));

        Assert.Equal(502, exception.StatusCode);
        Assert.Equal("job_response_invalid", exception.Code);
    }

    public static TheoryData<string> UnreadableJobs() => new()
    {
        // A member the route does not declare (the client refuses members it does not know, so a
        // field the gateway adds waits for a client that accepts it), and a document that is not an object.
        "{\"jobId\":\"5d0a8f0e-3c63-4d5e-9a0b-7c1f3a9e2b41\",\"unexpected\":true}",
        "[]",
        "null",
        "not json",
    };

    [Theory]
    [MemberData(nameof(UnreadableJobs))]
    public async Task AnAnswerThatIsNotAJobDocumentIsRefused(string json)
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, json)));
        await using var client = await ClientAsync(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Jobs.GetAsync(
            JobId,
            TestContext.Current.CancellationToken));

        Assert.Equal("response_invalid", exception.Code);
    }

    // ---------------------------------------------------------------------------------------
    // Result
    // ---------------------------------------------------------------------------------------

    [Theory]
    [InlineData(1)]
    [InlineData(81_920)]
    [InlineData(300_001)]
    public async Task TheResultIsReadFromItsRouteAndCheckedAgainstTheDigestTheGatewaySent(int length)
    {
        HttpRequestMessage? observed = null;
        var bytes = RandomNumberGenerator.GetBytes(length);
        var digest = "sha256:" + LowerHex(SHA256.HashData(bytes));
        var handler = new ScriptedHandler((request, _) =>
        {
            observed = Clone(request);
            return Task.FromResult(Result(bytes, digest));
        });
        await using var client = await ClientAsync(handler);

        var result = await client.Jobs.GetResultAsync(JobId, TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, observed!.Method);
        Assert.Equal($"{JobsRoute}/5d0a8f0e-3c63-4d5e-9a0b-7c1f3a9e2b41/result", observed.RequestUri!.AbsoluteUri);
        Assert.Equal(new AuthenticationHeaderValue("Bearer", "access-token"), observed.Headers.Authorization);
        // The route answers the bytes as octet-stream and a refusal as a problem document.
        Assert.Equal(
            ["application/octet-stream", "application/problem+json"],
            observed.Headers.GetValues("Accept").SelectMany(static value => value.Split(',', StringSplitOptions.TrimEntries)).ToArray());
        Assert.Equal(bytes, result.Content);
        Assert.Equal(digest, result.Digest);
    }

    [Fact]
    public async Task AResultThatDoesNotHashToItsDigestIsNotReturned()
    {
        var served = Encoding.UTF8.GetBytes("what the store held");
        var claimed = "sha256:" + LowerHex(SHA256.HashData(Encoding.UTF8.GetBytes("what the job recorded")));
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Result(served, claimed)));
        await using var client = await ClientAsync(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Jobs.GetResultAsync(
            JobId,
            TestContext.Current.CancellationToken));

        Assert.Equal(502, exception.StatusCode);
        Assert.Equal("job_result_digest_mismatch", exception.Code);
        Assert.DoesNotContain("what the store held", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("sha256:")]
    // The prefix, the case and the length are each part of the shape.
    [InlineData("SHA256:2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824")]
    [InlineData("sha256:2CF24DBA5FB0A30E26E83B2AC5B9E29E1B161E5C1FA7425E73043362938B9824")]
    [InlineData("sha256:2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b982")]
    [InlineData("sha256:2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b98244")]
    [InlineData("sha1:2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824")]
    [InlineData("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824")]
    [InlineData("sha256:zcf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824")]
    public async Task AResultWithoutAWellFormedDigestIsNotTrustedEvenIfItsBytesAreRight(string? digest)
    {
        var bytes = Encoding.UTF8.GetBytes("hello");
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Result(bytes, digest)));
        await using var client = await ClientAsync(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Jobs.GetResultAsync(
            JobId,
            TestContext.Current.CancellationToken));

        Assert.Equal(502, exception.StatusCode);
        Assert.Equal("job_response_invalid", exception.Code);
    }

    [Fact]
    public async Task AResultWithTwoDigestHeadersIsNotTrusted()
    {
        var bytes = Encoding.UTF8.GetBytes("hello");
        var digest = "sha256:" + LowerHex(SHA256.HashData(bytes));
        var handler = new ScriptedHandler((_, _) =>
        {
            var response = Result(bytes, digest);
            response.Headers.TryAddWithoutValidation(DigestHeader, digest);
            return Task.FromResult(response);
        });
        await using var client = await ClientAsync(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Jobs.GetResultAsync(
            JobId,
            TestContext.Current.CancellationToken));

        Assert.Equal("job_response_invalid", exception.Code);
    }

    [Fact]
    public async Task AResultTheGatewayDeclaresLargerThanTheBoundIsRefusedWithoutReadingIt()
    {
        var digest = "sha256:" + LowerHex(SHA256.HashData([1]));
        var handler = new ScriptedHandler((_, _) =>
        {
            var response = Result([1], digest);
            response.Content.Headers.ContentLength = YogabaseJobsLimits.MaximumResultBytes + 1L;
            return Task.FromResult(response);
        });
        await using var client = await ClientAsync(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Jobs.GetResultAsync(
            JobId,
            TestContext.Current.CancellationToken));

        Assert.Equal(502, exception.StatusCode);
        Assert.Equal("job_response_invalid", exception.Code);
    }

    [Fact]
    public async Task ASuccessThatIsNotA200IsNotAResult()
    {
        var bytes = Encoding.UTF8.GetBytes("hello");
        var digest = "sha256:" + LowerHex(SHA256.HashData(bytes));
        var handler = new ScriptedHandler((_, _) =>
        {
            var response = Result(bytes, digest);
            response.StatusCode = HttpStatusCode.Accepted;
            return Task.FromResult(response);
        });
        await using var client = await ClientAsync(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Jobs.GetResultAsync(
            JobId,
            TestContext.Current.CancellationToken));

        Assert.Equal("job_response_invalid", exception.Code);
    }

    [Theory]
    [InlineData(409, "job_result_not_ready", "The job has not settled, so it has no result yet.")]
    [InlineData(404, "job_result_not_found", "Job result not found.")]
    [InlineData(502, "job_result_integrity_failed", "The stored result does not match what the job recorded.")]
    [InlineData(500, "job_result_reference_invalid", "The job's stored result is not its own.")]
    [InlineData(503, "job_unavailable", "Job persistence unavailable.")]
    public async Task AResultTheGatewayRefusesSurfacesItsStatusAndCodeAndNoBytes(int status, string code, string title)
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Problem((HttpStatusCode)status, title, code)));
        await using var client = await ClientAsync(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Jobs.GetResultAsync(
            JobId,
            TestContext.Current.CancellationToken));

        Assert.Equal(status, exception.StatusCode);
        Assert.Equal(code, exception.Code);
        Assert.Equal(title, exception.Message);
    }

    // ---------------------------------------------------------------------------------------
    // The function invoke route's 413 (the same payload limit, a different surface)
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task AnInvokeAtExactlyTheInlineLimitIsSentAndOneByteMoreIsRefusedBeforeTransport()
    {
        string? body = null;
        var calls = 0;
        var handler = new ScriptedHandler(async (request, cancellationToken) =>
        {
            calls++;
            body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return Json(HttpStatusCode.Accepted, JsonSerializer.Serialize(new
            {
                outcome = 0,
                invocationId = "33333333-3333-8333-8333-333333333333",
                correlationId = "correlation-1",
            }));
        });
        await using var client = await ClientAsync(handler);

        await client.Functions.InvokeAsync("resize", "1", Invocation(new byte[1_048_576]), TestContext.Current.CancellationToken);

        Assert.Equal(1, calls);
        using var document = JsonDocument.Parse(body!);
        Assert.Equal(1_398_104, document.RootElement.GetProperty("inlinePayloadBase64").GetString()!.Length);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.Functions.InvokeAsync(
            "resize",
            "1",
            Invocation(new byte[1_048_577]),
            TestContext.Current.CancellationToken));
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task AnInvokeTheGatewayRefusesAsTooLargeIsA413PayloadTooLargeThatNamesBothLimits()
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(Problem(
            HttpStatusCode.RequestEntityTooLarge,
            "Payload too large.",
            YogabaseInvocationErrors.PayloadTooLarge,
            detail: "The request is larger than this route accepts.",
            limitBytes: 1_048_576,
            maxBodyBytes: 8_392_166)));
        await using var client = await ClientAsync(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Functions.InvokeAsync(
            "resize",
            "1",
            Invocation([1, 2, 3]),
            TestContext.Current.CancellationToken));

        Assert.Equal(413, exception.StatusCode);
        Assert.Equal("payload_too_large", exception.Code);
        Assert.Equal(1_048_576, exception.LimitBytes);
        Assert.Equal(8_392_166, exception.MaxBodyBytes);
    }

    [Fact]
    public async Task ABare413FromAGatewayThatPredatesTheNamedRefusalStillReadsAsPayloadTooLarge()
    {
        var handler = new ScriptedHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.RequestEntityTooLarge)));
        await using var client = await ClientAsync(handler);

        var exception = await Assert.ThrowsAsync<YogabaseApiException>(() => client.Functions.InvokeAsync(
            "resize",
            "1",
            Invocation([1, 2, 3]),
            TestContext.Current.CancellationToken));

        Assert.Equal(413, exception.StatusCode);
        Assert.Equal(YogabaseInvocationErrors.PayloadTooLarge, exception.Code);
        Assert.Null(exception.LimitBytes);
        Assert.Null(exception.MaxBodyBytes);
    }

    [Fact]
    public async Task AnActorOrWorkflowCallTheGatewayRefusesAsTooLargeIsA413PayloadTooLargeThatNamesBothLimits()
    {
        // The contract's x-yogabase-orleans-payload: 480 KiB of payload, and a body limit for each route.
        var calls = new (string Route, long MaxBodyBytes, Func<YogabaseClient, Task> Call)[]
        {
            (
                "actor invoke",
                3_935_575,
                client => client.Actors.InvokeAsync(
                    "counter",
                    "1",
                    "customer",
                    new YogabaseActorInvocationRequest
                    {
                        IdempotencyKey = "actor-request-1",
                        CorrelationId = "actor-correlation",
                        Deadline = DateTimeOffset.UtcNow.AddSeconds(20),
                        InlinePayload = [1, 2, 3],
                    },
                    TestContext.Current.CancellationToken)
            ),
            (
                "workflow start",
                3_934_405,
                client => client.Workflows.StartAsync(
                    "onboard",
                    "1",
                    new YogabaseWorkflowStartRequest { IdempotencyKey = "start-1", InlineInput = [1] },
                    TestContext.Current.CancellationToken)
            ),
            (
                "workflow cancel",
                3_934_417,
                client => client.Workflows.CancelAsync(
                    JobId,
                    new YogabaseWorkflowCancelRequest { IdempotencyKey = "cancel-1", InlinePayload = [] },
                    TestContext.Current.CancellationToken)
            ),
        };

        foreach (var (route, maxBodyBytes, call) in calls)
        {
            var handler = new ScriptedHandler((_, _) => Task.FromResult(Problem(
                HttpStatusCode.RequestEntityTooLarge,
                "Payload too large.",
                YogabaseInvocationErrors.PayloadTooLarge,
                detail: "The request is larger than this route accepts.",
                limitBytes: 491_520,
                maxBodyBytes: maxBodyBytes)));
            await using var client = await ClientAsync(handler);

            var exception = await Assert.ThrowsAsync<YogabaseApiException>(() => call(client));

            Assert.Equal(413, exception.StatusCode);
            Assert.Equal("payload_too_large", exception.Code);
            Assert.True(exception.LimitBytes == 491_520, route);
            Assert.True(exception.MaxBodyBytes == maxBodyBytes, route);
        }
    }

    // ---------------------------------------------------------------------------------------
    // Routes
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void TheJobsRoutesAreDefinedOnceInYogabaseRoutes()
    {
        Assert.Equal("v1/projects/{projectId}/jobs", YogabaseRoutes.JobsTemplate);
        Assert.Equal("v1/projects/{projectId}/jobs/{jobId}", YogabaseRoutes.JobTemplate);
        Assert.Equal("v1/projects/{projectId}/jobs/{jobId}/cancel", YogabaseRoutes.JobCancelTemplate);
        Assert.Equal("v1/projects/{projectId}/jobs/{jobId}/result", YogabaseRoutes.JobResultTemplate);
        Assert.Equal("v1/projects/11111111-1111-4111-8111-111111111111/jobs", YogabaseRoutes.Jobs(ProjectId));
        Assert.Equal(
            "v1/projects/11111111-1111-4111-8111-111111111111/jobs/5d0a8f0e-3c63-4d5e-9a0b-7c1f3a9e2b41",
            YogabaseRoutes.Job(ProjectId, JobId));
        Assert.Equal(
            "v1/projects/11111111-1111-4111-8111-111111111111/jobs/5d0a8f0e-3c63-4d5e-9a0b-7c1f3a9e2b41/cancel",
            YogabaseRoutes.JobCancel(ProjectId, JobId));
        Assert.Equal(
            "v1/projects/11111111-1111-4111-8111-111111111111/jobs/5d0a8f0e-3c63-4d5e-9a0b-7c1f3a9e2b41/result",
            YogabaseRoutes.JobResult(ProjectId, JobId));
        Assert.Throws<ArgumentException>(() => YogabaseRoutes.Jobs(Guid.Empty));
        Assert.Throws<ArgumentException>(() => YogabaseRoutes.Job(ProjectId, Guid.Empty));
    }

    // ---------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------

    private static YogabaseInvocationRequest Invocation(byte[] payload) => new()
    {
        IdempotencyKey = "request-1",
        CorrelationId = "correlation-1",
        Deadline = DateTimeOffset.UtcNow.AddSeconds(20),
        Mode = YogabaseInvocationMode.Asynchronous,
        InlinePayload = payload,
    };

    private static string JobJson(
        string status = "Queued",
        int attempt = 0,
        int maxAttempts = 5,
        int progress = 0,
        bool cancellation = false,
        string? resultReference = null,
        string? failureCode = null,
        string? completedAt = null,
        string? jobId = null,
        string jobType = "orders-create") => JsonSerializer.Serialize(new
        {
            jobId = jobId ?? "5d0a8f0e-3c63-4d5e-9a0b-7c1f3a9e2b41",
            jobType,
            status,
            attempt,
            maxAttempts,
            progressPercent = progress,
            cancellationRequested = cancellation,
            resultReference,
            failureCode,
            createdAt = "2026-10-03T05:00:00+00:00",
            updatedAt = "2026-10-03T05:00:01+00:00",
            completedAt,
        });

    private static HttpResponseMessage Result(byte[] bytes, string? digest)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        response.Headers.CacheControl = new CacheControlHeaderValue { NoStore = true, Private = true };
        response.Headers.TryAddWithoutValidation("X-Content-Type-Options", "nosniff");
        if (digest is not null)
        {
            response.Headers.TryAddWithoutValidation(DigestHeader, digest);
        }

        return response;
    }

    // Exactly the problem ASP.NET Core writes for Results.Problem(statusCode, title, detail, extensions): the
    // code and the limits are top-level members beside type, title, status and traceId.
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

    private static string LowerHex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();

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
        OidcPublicClientId = "yogabase-native-preview",
        OidcScopes = ["openid", "offline_access"],
        OidcRedirectUris = ["io.yogabase.app:/oauth/callback"],
        RealtimePath = "/v1/realtime",
        Capabilities = capabilities,
        IssuedAt = DateTimeOffset.Parse("2026-10-03T00:00:00+00:00", System.Globalization.CultureInfo.InvariantCulture),
    };

    private sealed class ScriptedHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
