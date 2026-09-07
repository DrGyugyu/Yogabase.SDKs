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
using System.Net;

namespace Yogabase.Yogabase.RuntimeHost
{

/// <summary>
/// One heartbeat/ready/reserve/shutdown lease answer, loosely parsed from the runtime-host relay
/// response body.
/// </summary>
/// <param name="State">The lease state reported by the relay (e.g. <c>"Ready"</c>, <c>"Reserved"</c>).</param>
/// <param name="HeartbeatSequence">The most recently acknowledged heartbeat sequence number.</param>
/// <param name="LeaseExpiresAt">The instant at which the current lease expires absent a further heartbeat.</param>
/// <param name="EventSequence">The sequence number of the most recent assignment event known to the relay.</param>
public sealed record RuntimeHostLease(
    string State,
    long HeartbeatSequence,
    DateTimeOffset LeaseExpiresAt,
    long EventSequence);

/// <summary>One assignment-lifecycle event returned by the assignments watch route.</summary>
/// <param name="Sequence">The monotonic sequence number of this event.</param>
/// <param name="Kind">The event kind reported by the relay (e.g. <c>"Allocated"</c>, <c>"Released"</c>).</param>
/// <param name="State">The lease state that resulted from this event.</param>
/// <param name="AllocationId">The backend-issued allocation identifier, when this event carries one.</param>
/// <param name="OccurredAt">The instant the relay recorded this event.</param>
public sealed record RuntimeHostAssignmentEvent(
    long Sequence,
    string Kind,
    string State,
    string? AllocationId,
    DateTimeOffset OccurredAt);

/// <summary>One page of assignment events plus the resume token for the next poll.</summary>
/// <param name="Events">The events returned by this poll, in relay order.</param>
/// <param name="ResumeToken">
/// The token to present on the next poll. An empty value means the caller should keep presenting
/// whichever token it already holds rather than treat this page as resetting the cursor.
/// </param>
public sealed record RuntimeHostAssignmentPage(
    IReadOnlyList<RuntimeHostAssignmentEvent> Events,
    string ResumeToken);

/// <summary>Raised when a self-scoped runtime-host call is refused; the caller decides whether that is expected.</summary>
public sealed class YogabaseRuntimeHostException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="YogabaseRuntimeHostException"/> class.</summary>
    public YogabaseRuntimeHostException()
        : base("runtime-host call refused.")
    {
        Code = "UNKNOWN";
        BodyJson = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="YogabaseRuntimeHostException"/> class with a message.</summary>
    /// <param name="message">The exception message.</param>
    public YogabaseRuntimeHostException(string message)
        : base(message)
    {
        Code = "UNKNOWN";
        BodyJson = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="YogabaseRuntimeHostException"/> class with a message and inner exception.</summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public YogabaseRuntimeHostException(string message, Exception innerException)
        : base(message, innerException)
    {
        Code = "UNKNOWN";
        BodyJson = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="YogabaseRuntimeHostException"/> class describing a refused relay call.</summary>
    /// <param name="statusCode">The HTTP status code the relay returned.</param>
    /// <param name="code">The relay-reported error code, or <c>"UNKNOWN"</c> when the body names none.</param>
    /// <param name="bodyJson">The raw response body.</param>
    public YogabaseRuntimeHostException(HttpStatusCode statusCode, string code, string bodyJson)
        : base($"runtime-host call refused: {(int)statusCode} {code}")
    {
        StatusCode = statusCode;
        Code = code;
        BodyJson = bodyJson;
    }

    /// <summary>The HTTP status code the relay returned.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>The relay-reported error code. Never null; <c>"UNKNOWN"</c> when the body names none.</summary>
    public string Code { get; }

    /// <summary>The raw response body. Never null.</summary>
    public string BodyJson { get; }
}

}
