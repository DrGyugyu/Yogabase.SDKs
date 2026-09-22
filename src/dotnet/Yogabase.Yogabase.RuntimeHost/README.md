# Yogabase.Yogabase.RuntimeHost

Preview .NET/Unity runtime-host client for a Yogabase hosted self-contained service workload — a
dedicated server that carries its own runtime and participates in lifecycle only through the
runtime-host wire contract. The package targets .NET Standard 2.1 and .NET 10.

It is deliberately **separate** from `Yogabase.Yogabase.Client`. A headless dedicated server does
not want a SignalR dependency, and it does not want reflection-based JSON: this package depends on
nothing beyond `HttpClient` and `System.Text.Json`, and it reads every response through the JSON
DOM (`JsonNode`/`JsonObject`) rather than `JsonSerializer.Deserialize<T>`, so an IL2CPP build has
nothing to strip.

```csharp
using Yogabase.Yogabase.RuntimeHost;

await using var host = new YogabaseRuntimeHostClient();
host.Connect();                       // starts the background heartbeat loop
await host.ReadyAsync();              // request readiness once the server can accept players

await foreach (var assignment in host.WatchAssignmentsAsync(TimeSpan.FromSeconds(2), stopping))
{
    if (assignment.Kind == "allocated")
    {
        // the match has been placed on this instance
    }
}

await host.ShutdownAsync();           // begin an auditable drain
```

## Refusals and the startup window

A refused call throws `YogabaseRuntimeHostException`, carrying the HTTP status, the relay's `code`
and the raw body. One code is not terminal. A freshly started Pod's first calls can reach the relay
before the member has reported that Pod, and the relay then answers
`503 RUNTIME_HOST_INSTANCE_PENDING` with a `retryAfterSeconds` hint. `HeartbeatOnceAsync` and
`ReadyAsync` wait that out, each wait being the hint (2 seconds without one), until
`YogabaseRuntimeHostOptions.StartupPendingBudget` is spent (default 60 seconds; zero disables it),
and then throw the last pending refusal. Every other code is thrown on the first attempt, and
`ReserveAsync`, `ShutdownAsync` and the assignment watch never wait. Tests substitute
`StartupPendingDelay` so nothing sleeps.

The background loop that `Connect` starts keeps going after a failed heartbeat. Pass
`Connect(onHeartbeatFailure)` to see those failures instead of losing them.

## What this client talks to

The workload calls its member's Pod-local relay sidecar over loopback — by default
`http://127.0.0.1:8090`, overridden by the `YOGABASE_RUNTIME_HOST_ENDPOINT` environment variable
the platform injects. Exactly five routes exist:

| Call | Route |
| --- | --- |
| `HeartbeatOnceAsync` | `POST /runtime-host/v1/self/heartbeat` |
| `ReadyAsync` | `POST /runtime-host/v1/self/ready` |
| `ReserveAsync` | `POST /runtime-host/v1/self/reserve` |
| `ShutdownAsync` | `POST /runtime-host/v1/self/shutdown` |
| `WatchAssignmentsOnceAsync` | `GET /runtime-host/v1/self/assignments` |

A page also states the head when the Control Plane does: `HeadState` is the instance's current
lifecycle state as the Control Plane observes it (a lapsed lease reads `Unknown` although no event
records the lapse) and `HeadSequence` the newest event's sequence. `ReadCurrentStateAsync` uses it
to answer the current state in one read, and against a Control Plane that predates the head it
pages the stream to its end instead, which is what a consumer had to do before. A head in a shape
outside the contract reads as no head. `RuntimeHostCurrentState.IsReady` is true for `Ready`,
`Reserved` and `Allocated` only; a state name the client does not know is not ready.

A Unity project takes this client as a built assembly from `src/unity/Yogabase.Yogabase.RuntimeHost.Unity`:
the same sources, compiled for netstandard2.1 against System.Text.Json 8.0.5, so it binds Unity's own
8.0 copy.

## Deliberate omissions

- **No instance id.** Nothing here takes, stores, or sends one. The Control Plane resolves the
  instance from the binding the member Agent stamps from its own Pod observation, so a workload can
  never assert an identity that is not its own.
- **No credential.** This client never sets an `Authorization` header or sends an API key. The relay
  accepts neither application nor delivery credentials, and it holds none itself.
- **No allocation method.** Allocation placement and release are backend-only Control Plane
  operations; the relay refuses a self-allocate with `403 RUNTIME_HOST_RELAY_ALLOCATION_BACKEND_ONLY`.
  The absence of an allocate call is contract, not an oversight.
- **Polling, not streaming.** The relay is strictly request/response, so the assignment watch is a
  resume-token poll. An empty resume token in a page means "no progress" and does not reset the
  cursor.

`YogabaseRuntimeHostClient` is not a general-purpose HTTP client: it pins its base address, uses a
short request timeout, and disables redirects, proxies, cookies, and default credentials.

## Third-party notices

The client loop shape is a faithful, modified derivative of Agones' `sdks/csharp/sdk/AgonesSDK.cs`
(Apache-2.0, v1.60.0, commit `67b44a783218f7811084adaa5d9b334fb28e44ba`). Copyright Contributors to
Agones a Series of LF Projects, LLC. The unmodified licence ships in this package at
`licenses/Apache-2.0-Agones.txt`, and every derived source file records the exact modifications.
No Agones wire contract, type name, or resource model is reproduced.
