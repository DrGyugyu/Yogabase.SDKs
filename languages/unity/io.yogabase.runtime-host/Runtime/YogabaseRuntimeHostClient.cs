// A faithful, minimal C# port of the client loop shape in Agones' sdks/csharp/sdk/AgonesSDK.cs
// (Apache-2.0, v1.60.0, commit 67b44a783218f7811084adaa5d9b334fb28e44ba). Copyright Contributors
// to Agones a Series of LF Projects, LLC. Licensed under the Apache License, Version 2.0; the
// unmodified licence text ships with this package at licenses/Apache-2.0-Agones.txt.
//
// THIS FILE IS A MODIFIED DERIVATIVE. The modifications Yogabase made to the Agones shape are:
//   1. Self-scoped loopback routes. Every call targets
//      /runtime-host/v1/self/{heartbeat|ready|reserve|shutdown|assignments} on the Pod-local
//      relay (default http://127.0.0.1:8090), not Agones' SDK sidecar contract.
//   2. No instance id and no credential. Agones' SDK is scoped by its sidecar to one GameServer;
//      here the Control Plane resolves identity from the Agent-stamped binding alone, so this
//      client presents no id, no API key, and no Authorization header of any kind.
//   3. Polling instead of streaming. Agones' watch holds a server stream; the Yogabase relay is
//      strictly request/response, so the assignment watch is a resume-token poll.
//
// Nothing from Agones' proto/sdk wire contract, type or method names, GameServer/Fleet/allocation
// resource model, or host-port policy is reproduced here.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Yogabase.Yogabase.RuntimeHost
{

/// <summary>
/// Mirrors AgonesSDK.cs's client loop shape against Yogabase's self-scoped runtime-host relay
/// contract: a background heartbeat loop started by <see cref="Connect"/>, one-shot
/// Ready/Reserve/Shutdown calls, and a polling assignment watch.
///
/// Every call targets <c>/runtime-host/v1/self/{verb}</c> on the Pod-local relay -- nothing here
/// ever learns, presents, or asserts an instance id or a credential of any kind; the Control Plane
/// resolves identity solely from the Agent-stamped binding. There is deliberately no allocation
/// method: allocation placement is a backend-only Control Plane operation, so the relay refuses
/// <c>/runtime-host/v1/self/allocate</c> (under either method) with 403
/// <c>RUNTIME_HOST_RELAY_ALLOCATION_BACKEND_ONLY</c>, and no <c>/allocations</c> route is mapped
/// on it at all.
///
/// A refused call throws <see cref="YogabaseRuntimeHostException"/>. Most refusals carry a
/// <c>{code, detail}</c> body, but the relay answers 413, 429 and 504 with no body at all; those
/// throw the same exception with the status, the code <c>UNKNOWN</c> and an empty
/// <see cref="YogabaseRuntimeHostException.BodyJson"/>, and
/// <see cref="YogabaseRuntimeHostException.IsRetryable"/> says whether the contract marks the
/// answer worth repeating (429 and 504 yes, 413 no).
///
/// This is not a general-purpose HTTP client. The <see cref="HttpClient"/> it owns is built with
/// redirects, proxying, cookies, default credentials, and pre-authentication all disabled, and it
/// never sets an <c>Authorization</c> header or sends any other credential.
/// </summary>
public sealed class YogabaseRuntimeHostClient : IAsyncDisposable
{
    /// <summary>
    /// The one refusal code <see cref="HeartbeatOnceAsync"/> and <see cref="ReadyAsync"/> wait out
    /// (within <see cref="YogabaseRuntimeHostOptions.StartupPendingBudget"/>) rather than throw at
    /// once. It means the member's readiness observation for this Pod incarnation has not reached
    /// the Control Plane yet -- a platform timing fact, not a caller error -- and the identical call
    /// succeeds once that observation arrives. Every other code (for example
    /// <c>RUNTIME_HOST_STALE_EPOCH</c>, <c>RUNTIME_HOST_SHUTDOWN_IN_PROGRESS</c>,
    /// <c>RUNTIME_HOST_TERMINATED</c>) is terminal and is thrown immediately.
    /// </summary>
    private const string InstancePendingCode = YogabaseRuntimeHostException.InstancePendingCode;

    /// <summary>The wait between retries when a pending refusal's body carries no <c>retryAfterSeconds</c> hint.</summary>
    private static readonly TimeSpan DefaultInstancePendingDelay = TimeSpan.FromSeconds(2);

    private readonly HttpClient http;
    private readonly TimeSpan heartbeatInterval;
    private readonly TimeSpan startupPendingBudget;
    private readonly Func<TimeSpan, CancellationToken, Task> pendingDelay;
    private readonly CancellationTokenSource stopping = new();
    private Task? heartbeatLoop;
    private Action<Exception>? heartbeatFailureHandler;
    private long sequence;
    private bool disposed;

    /// <summary>Initializes a new instance of the <see cref="YogabaseRuntimeHostClient"/> class using default options.</summary>
    public YogabaseRuntimeHostClient()
        : this(new YogabaseRuntimeHostOptions())
    {
    }

    /// <summary>Initializes a new instance of the <see cref="YogabaseRuntimeHostClient"/> class with the given options.</summary>
    /// <param name="options">The options that configure the endpoint, heartbeat interval, and request timeout.</param>
    public YogabaseRuntimeHostClient(YogabaseRuntimeHostOptions options)
        : this(options, CreateDefaultHandler())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YogabaseRuntimeHostClient"/> class with the given
    /// options and a caller-supplied transport handler, primarily for tests.
    /// </summary>
    /// <param name="options">The options that configure the endpoint, heartbeat interval, and request timeout.</param>
    /// <param name="handler">The transport handler to use. The client takes ownership and disposes it.</param>
    public YogabaseRuntimeHostClient(YogabaseRuntimeHostOptions options, HttpMessageHandler handler)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        heartbeatInterval = options.HeartbeatInterval;
        startupPendingBudget = options.StartupPendingBudget;
        pendingDelay = options.StartupPendingDelay ?? Task.Delay;
        http = new HttpClient(handler)
        {
            BaseAddress = options.Endpoint,
            Timeout = options.RequestTimeout,
        };
    }

    /// <summary>
    /// Starts the background heartbeat loop; mirrors Agones' Connect() + Health(). Calling this
    /// more than once has no additional effect (including a second, different
    /// <paramref name="onHeartbeatFailure"/>).
    /// </summary>
    /// <param name="onHeartbeatFailure">
    /// Invoked, best-effort, with each exception the background loop would otherwise swallow
    /// silently. A throwing callback is itself swallowed so it can never break the loop's
    /// resilience contract. A <c>RUNTIME_HOST_INSTANCE_PENDING</c> refusal reaches it only once
    /// <see cref="YogabaseRuntimeHostOptions.StartupPendingBudget"/> is spent, because each tick's
    /// heartbeat waits it out first. The callback runs on the loop's thread and must not block.
    /// Loop cancellation during disposal is never reported.
    /// </param>
    public void Connect(Action<Exception>? onHeartbeatFailure = null)
    {
        if (heartbeatLoop is not null)
        {
            return;
        }

        heartbeatFailureHandler = onHeartbeatFailure;
        heartbeatLoop = RunHeartbeatLoopAsync(stopping.Token);
    }

    /// <summary>
    /// Sends a single heartbeat with the next monotonic sequence number. A
    /// <c>RUNTIME_HOST_INSTANCE_PENDING</c> refusal is retried, waiting each time the refusal's
    /// hint (or 2 seconds when it carries none), until <see cref="YogabaseRuntimeHostOptions.StartupPendingBudget"/>
    /// would be exceeded; every other refusal is thrown at once.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The lease reported by the relay.</returns>
    public Task<RuntimeHostLease> HeartbeatOnceAsync(CancellationToken cancellationToken = default) =>
        WaitingOutInstancePendingAsync(
            () => PostLeaseAsync("heartbeat", new JsonObject { ["sequence"] = Interlocked.Increment(ref sequence) }, cancellationToken),
            cancellationToken);

    /// <summary>
    /// Marks this host ready to receive an assignment. A <c>RUNTIME_HOST_INSTANCE_PENDING</c>
    /// refusal is retried the same way as <see cref="HeartbeatOnceAsync"/>; every other refusal is
    /// thrown at once.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The lease reported by the relay.</returns>
    public Task<RuntimeHostLease> ReadyAsync(CancellationToken cancellationToken = default) =>
        WaitingOutInstancePendingAsync(
            () => PostLeaseAsync("ready", new JsonObject(), cancellationToken),
            cancellationToken);

    /// <summary>Reserves this host for the given window, clamped to between 1 and 600 seconds.</summary>
    /// <param name="window">The requested reservation window.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The lease reported by the relay.</returns>
    public Task<RuntimeHostLease> ReserveAsync(TimeSpan window, CancellationToken cancellationToken = default) =>
        PostLeaseAsync(
            "reserve",
            new JsonObject { ["seconds"] = (int)Math.Clamp(window.TotalSeconds, 1, 600) },
            cancellationToken);

    /// <summary>Requests a graceful shutdown of this host's lease.</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The lease reported by the relay.</returns>
    public Task<RuntimeHostLease> ShutdownAsync(CancellationToken cancellationToken = default) =>
        PostLeaseAsync("shutdown", new JsonObject(), cancellationToken);

    /// <summary>One poll of the assignment watch; the caller loops this on its own cadence, or uses <see cref="WatchAssignmentsAsync"/>.</summary>
    /// <param name="resumeToken">The resume token from a previous page, or null/empty to start from the beginning.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The next page of assignment events and the resume token for the following poll.</returns>
    public async Task<RuntimeHostAssignmentPage> WatchAssignmentsOnceAsync(
        string? resumeToken = null,
        CancellationToken cancellationToken = default)
    {
        var query = string.IsNullOrEmpty(resumeToken) ? string.Empty : $"?resumeToken={Uri.EscapeDataString(resumeToken)}";
        using var response = await http.GetAsync($"/runtime-host/v1/self/assignments{query}", cancellationToken)
            .ConfigureAwait(false);
        var text = await ReadBodyAsync(response, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw ToRuntimeHostException(response.StatusCode, text);
        }

        var node = JsonNode.Parse(text) as JsonObject
            ?? throw new InvalidOperationException("The assignments route returned no JSON object body.");
        var events = (node["events"] as JsonArray ?? new JsonArray())
            .Select(item => new RuntimeHostAssignmentEvent(
                item!["sequence"]!.GetValue<long>(),
                item["kind"]!.GetValue<string>(),
                item["state"]!.GetValue<string>(),
                item["allocationId"]?.GetValue<string>(),
                item["occurredAt"]!.GetValue<DateTimeOffset>()))
            .ToArray();
        // Both head fields are additive: a Control Plane that predates them omits them, and a value
        // outside the contract is read as no head rather than failing the page, so the caller keeps
        // today's paging behaviour instead of trusting a malformed state.
        var headState = ReadHeadState(node["headState"]);
        var headSequence = ReadHeadSequence(node["headSequence"]);
        return new RuntimeHostAssignmentPage(
            events,
            node["resumeToken"]?.GetValue<string>() ?? string.Empty,
            headState,
            headState is null ? null : headSequence);
    }

    /// <summary>
    /// Reads the instance's current lifecycle state. When the Control Plane states the head on its
    /// pages this is one request (one-read readiness); against one that predates it, this pages the
    /// event stream from the start until it reaches the head (a page shorter than
    /// <see cref="RuntimeHostAssignmentPage.MaximumEventsPerPage"/>) and answers the last event's
    /// state, which is what a consumer had to do before the head existed. It never advances a
    /// caller's own assignment cursor.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The current state, or a null state when nothing has been recorded yet.</returns>
    public async Task<RuntimeHostCurrentState> ReadCurrentStateAsync(CancellationToken cancellationToken = default)
    {
        const int MaximumPages = 10_000;
        string? resumeToken = null;
        RuntimeHostAssignmentEvent? last = null;
        for (var pages = 0; pages < MaximumPages; pages++)
        {
            var page = await WatchAssignmentsOnceAsync(resumeToken, cancellationToken).ConfigureAwait(false);
            if (page.HasHead)
            {
                return new RuntimeHostCurrentState(page.HeadState, page.HeadSequence, ReadFromHead: true);
            }

            if (page.Events.Count > 0)
            {
                last = page.Events[page.Events.Count - 1];
            }

            if (page.Events.Count < RuntimeHostAssignmentPage.MaximumEventsPerPage
                || string.IsNullOrEmpty(page.ResumeToken)
                || string.Equals(page.ResumeToken, resumeToken, StringComparison.Ordinal))
            {
                break;
            }

            resumeToken = page.ResumeToken;
        }

        return new RuntimeHostCurrentState(last?.State, last?.Sequence, ReadFromHead: false);
    }

    private static string? ReadHeadState(JsonNode? value) =>
        value is JsonValue scalar && scalar.TryGetValue<string>(out var state) && !string.IsNullOrEmpty(state)
            ? state
            : null;

    private static long? ReadHeadSequence(JsonNode? value) =>
        value is JsonValue scalar && scalar.TryGetValue<long>(out var sequence) && sequence >= 0
            ? sequence
            : null;

    /// <summary>
    /// Polls <see cref="WatchAssignmentsOnceAsync"/> on the given interval and yields each event in
    /// order. A page whose resume token is empty or absent does not advance the cursor -- the
    /// previously held token is presented again on the next poll; only a non-empty token replaces
    /// it. Like the heartbeat loop, a transient failure does not stop the poll; the sequence ends
    /// cleanly when <paramref name="cancellationToken"/> is canceled.
    /// </summary>
    /// <param name="pollInterval">The delay between polls.</param>
    /// <param name="cancellationToken">A token to end the watch.</param>
    /// <returns>An asynchronous sequence of assignment events.</returns>
    public async IAsyncEnumerable<RuntimeHostAssignmentEvent> WatchAssignmentsAsync(
        TimeSpan pollInterval,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? resumeToken = null;
        while (!cancellationToken.IsCancellationRequested)
        {
            RuntimeHostAssignmentPage? page = null;
            try
            {
                page = await WatchAssignmentsOnceAsync(resumeToken, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                // A transient relay/backend failure does not stop the watch: like the heartbeat
                // loop, it keeps polling on the next tick rather than exiting.
            }

            if (page is not null)
            {
                foreach (var assignmentEvent in page.Events)
                {
                    yield return assignmentEvent;
                }

                if (!string.IsNullOrEmpty(page.ResumeToken))
                {
                    resumeToken = page.ResumeToken;
                }
            }

            var canceled = false;
            try
            {
                await Task.Delay(pollInterval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                canceled = true;
            }

            if (canceled)
            {
                yield break;
            }
        }
    }

    /// <summary>Cancels the background heartbeat loop, awaits it, and releases the underlying <see cref="HttpClient"/>. Safe to call more than once.</summary>
    /// <returns>A task that completes once disposal finishes.</returns>
    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        stopping.Cancel();
        if (heartbeatLoop is not null)
        {
            try
            {
                await heartbeatLoop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        stopping.Dispose();
        http.Dispose();
    }

    // netstandard2.1's HttpContent carries no CancellationToken overload of ReadAsStringAsync, so
    // the Unity leg checks the token itself before reading rather than dropping cancellation
    // silently; the net10 leg forwards the token into the read.
    private static Task<string> ReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
#if NETSTANDARD2_1
        cancellationToken.ThrowIfCancellationRequested();
        return response.Content.ReadAsStringAsync();
#else
        return response.Content.ReadAsStringAsync(cancellationToken);
#endif
    }

    // Every assignment here is a "do not" -- no redirect, no proxy, no cookie jar, no ambient
    // credential -- because this handler talks to a loopback runtime host and must not inherit any
    // machine-wide HTTP behaviour. The flags say all of that on their own; the matching `Proxy` and
    // `Credentials` nulls said it a second time, and Mono's HttpClientHandler throws
    // InvalidOperationException from set_Proxy once UseProxy is false. On CoreCLR the redundant
    // assignment is merely ignored, so this only ever failed in an IL2CPP player -- the Unity
    // dedicated server died in its own constructor before it could bind a port.
    private static HttpClientHandler CreateDefaultHandler() =>
        new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            UseCookies = false,
            UseDefaultCredentials = false,
            PreAuthenticate = false,
        };

    private async Task<RuntimeHostLease> PostLeaseAsync(string verb, JsonObject body, CancellationToken cancellationToken)
    {
        using var content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        using var response = await http.PostAsync($"/runtime-host/v1/self/{verb}", content, cancellationToken)
            .ConfigureAwait(false);
        var text = await ReadBodyAsync(response, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw ToRuntimeHostException(response.StatusCode, text);
        }

        var node = JsonNode.Parse(text) as JsonObject
            ?? throw new InvalidOperationException($"The {verb} route returned no JSON object body.");
        return new RuntimeHostLease(
            node["state"]!.GetValue<string>(),
            node["heartbeatSequence"]!.GetValue<long>(),
            node["leaseExpiresAt"]!.GetValue<DateTimeOffset>(),
            node["eventSequence"]!.GetValue<long>());
    }

    // A refusal the Control Plane or the relay chose is a JSON {code, detail} body, but the relay
    // answers 413, 429 and 504 with no body at all, and a hop in front of it could answer with
    // something that is not JSON. None of those is a parse failure: the caller gets the status
    // (and the raw body, when there is one) on a YogabaseRuntimeHostException, whose code is
    // "UNKNOWN" because no body named one.
    private static YogabaseRuntimeHostException ToRuntimeHostException(HttpStatusCode status, string raw) =>
        new YogabaseRuntimeHostException(status, ReadRefusalCode(raw), raw);

    private static string ReadRefusalCode(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "UNKNOWN";
        }

        // JsonDocument rather than JsonNode: a JsonObject throws ArgumentException on first access
        // to a body with a repeated property name, and a refusal must never fail to surface.
        try
        {
            using var document = JsonDocument.Parse(raw);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var name in new[] { "code", "title" })
                {
                    if (document.RootElement.TryGetProperty(name, out var value)
                        && value.ValueKind == JsonValueKind.String
                        && !string.IsNullOrEmpty(value.GetString()))
                    {
                        return value.GetString()!;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Not JSON: the status is all the body says.
        }

        return "UNKNOWN";
    }

    // Retries only RUNTIME_HOST_INSTANCE_PENDING, and only for as long as
    // YogabaseRuntimeHostOptions.StartupPendingBudget allows. `elapsed` tracks time already spent
    // waiting rather than a wall-clock deadline, so an injected `pendingDelay` that never actually
    // sleeps still converges deterministically in tests. Every other exception -- including a
    // pending refusal whose next wait would exceed the remaining budget -- propagates unchanged.
    private async Task<RuntimeHostLease> WaitingOutInstancePendingAsync(
        Func<Task<RuntimeHostLease>> attempt,
        CancellationToken cancellationToken)
    {
        var elapsed = TimeSpan.Zero;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await attempt().ConfigureAwait(false);
            }
            catch (YogabaseRuntimeHostException exception) when (exception.Code == InstancePendingCode)
            {
                var wait = exception.RetryAfterSeconds is { } hintSeconds && hintSeconds > 0
                    ? TimeSpan.FromSeconds(hintSeconds)
                    : DefaultInstancePendingDelay;
                if (elapsed + wait > startupPendingBudget)
                {
                    throw;
                }

                elapsed += wait;
                await pendingDelay(wait, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task RunHeartbeatLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await HeartbeatOnceAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                // A transient relay/backend failure does not stop the loop: like Agones' Health(),
                // a hosted service keeps trying on the next tick rather than exiting. It was
                // previously swallowed with no trace at all; a caller that wants to observe it now
                // can, via the callback Connect() accepts.
                try
                {
                    heartbeatFailureHandler?.Invoke(exception);
                }
                catch (Exception)
                {
                    // The callback's own failure must not break the loop's resilience contract.
                }
            }

            try
            {
                await Task.Delay(heartbeatInterval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}

}
