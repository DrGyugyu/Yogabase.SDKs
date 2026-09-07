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

namespace Yogabase.Yogabase.RuntimeHost
{

/// <summary>Configures a <see cref="YogabaseRuntimeHostClient"/>.</summary>
public sealed class YogabaseRuntimeHostOptions
{
    /// <summary>
    /// The environment variable that, when set to a well-formed absolute URI, overrides
    /// <see cref="DefaultEndpoint"/> for <see cref="ResolveEndpoint"/>.
    /// </summary>
    public const string EndpointVariable = "YOGABASE_RUNTIME_HOST_ENDPOINT";

    /// <summary>The default Pod-local runtime-host relay endpoint.</summary>
    public const string DefaultEndpoint = "http://127.0.0.1:8090";

    /// <summary>Initializes a new instance of the <see cref="YogabaseRuntimeHostOptions"/> class.</summary>
    public YogabaseRuntimeHostOptions()
    {
        Endpoint = ResolveEndpoint();
        HeartbeatInterval = TimeSpan.FromSeconds(2);
        RequestTimeout = TimeSpan.FromSeconds(10);
    }

    /// <summary>The runtime-host relay endpoint this client connects to.</summary>
    public Uri Endpoint { get; set; }

    /// <summary>The interval between heartbeats sent by the background heartbeat loop.</summary>
    public TimeSpan HeartbeatInterval { get; set; }

    /// <summary>The per-request timeout applied to every call this client makes.</summary>
    public TimeSpan RequestTimeout { get; set; }

    /// <summary>
    /// Resolves the runtime-host relay endpoint from <see cref="EndpointVariable"/> when it is set
    /// to a well-formed absolute URI, falling back to <see cref="DefaultEndpoint"/> otherwise. This
    /// never throws on a malformed environment value.
    /// </summary>
    /// <returns>The resolved endpoint URI.</returns>
    public static Uri ResolveEndpoint()
    {
        var value = Environment.GetEnvironmentVariable(EndpointVariable);
        if (!string.IsNullOrWhiteSpace(value)
            && Uri.TryCreate(value, UriKind.Absolute, out var parsed))
        {
            return parsed;
        }

        return new Uri(DefaultEndpoint, UriKind.Absolute);
    }
}

}
