# Third Party Notices

This package's `Runtime/YogabaseRuntimeHostClient.cs`, `Runtime/YogabaseRuntimeHostOptions.cs`, and
`Runtime/RuntimeHostModels.cs` are a faithful, minimal, **modified** C# derivative of the client
loop shape in Agones' `sdks/csharp/sdk/AgonesSDK.cs`:

- **Project:** Agones
- **Version:** v1.60.0
- **Commit:** `67b44a783218f7811084adaa5d9b334fb28e44ba`
- **Copyright:** Copyright Contributors to Agones a Series of LF Projects, LLC.
- **License:** Apache License, Version 2.0

## What was adopted

Only the *shape* of Agones' client loop — heartbeat, readiness signal, assignment watch, shutdown —
was ported, retargeted end to end at Yogabase's own self-scoped runtime-host relay routes
(`/runtime-host/v1/self/{heartbeat|ready|reserve|shutdown|assignments}`). Each of the three source
files above carries its own header describing the specific modifications Yogabase made:

1. Self-scoped loopback routes instead of Agones' SDK sidecar contract.
2. No instance id and no credential of any kind (the Control Plane resolves identity from the
   Agent-stamped Pod binding, not from anything the workload asserts).
3. A resume-token poll instead of Agones' server-streamed watch, because the Yogabase relay is
   strictly request/response.

No Agones proto/sdk wire contract, type or method name, GameServer/Fleet/allocation resource model,
or host-port policy is reproduced. `Runtime/Compatibility.cs` is original Yogabase code (an
`IsExternalInit` shim for the `netstandard2.1` target) and carries no Agones attribution because it
is not derived from Agones material.

## License text

The unmodified Apache License, Version 2.0 text ships inside this package, self-contained, at
[`licenses/Apache-2.0-Agones.txt`](licenses/Apache-2.0-Agones.txt).
