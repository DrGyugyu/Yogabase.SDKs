# Yogabase.ServerPathProbe

An Editor-free walk of the Unity demo's server-side path, for the owner to run from their own
terminal with their own player sign-in, before spending time on a Unity Editor build. It exercises
the same five things a Unity demo client would exercise, through `Yogabase.Yogabase.Client`:

1. loads sign-in context from environment variables (never the command line);
2. `POST /v1/servers/start` through `Servers.StartAsync`, printing `serverId` and `networkPorts`;
3. sends one raw UDP datagram to the allocated `host:externalPort`, with an `--address-override`
   option for a macOS run against local k3d (whose allocations carry a node-internal address the
   host cannot route to — the run says plainly when the override was applied);
4. invokes one named Lambda through `Functions.InvokeAsync` and prints the outcome, correlation id,
   and a bounded preview of the response;
5. optionally subscribes to one realtime topic for a bounded window and logs received event names
   with a bounded payload preview — skipped, not failed, when the installation does not report the
   `realtime-subscribe` capability as available.

This is a sample and a diagnostic tool, not a published package. `IsPackable` is `false`, so even a
`dotnet pack` of the whole solution produces no package for it, and the root
[README.md](../../../README.md#local-verification) packs only the three library `.csproj` paths. It also never
signs in, claims a seat, or creates an account: it only consumes a player access token the owner
already holds.

## Sign-in context

Read from environment variables only. Neither is ever accepted as a command-line argument, printed,
logged, or written to disk, and the tokens live only in an in-memory session store for the length
of the run.

| Variable | Required | Meaning |
| --- | --- | --- |
| `YOGABASE_PLAYER_ACCESS_TOKEN` | Yes | The signed-in player's OIDC access token. This tool does not sign in itself — no browser, no PKCE, no password grant — the owner obtains the token out of band (for example from a real client's own sign-in flow) and exports it before running the probe. |
| `YOGABASE_PLAYER_REFRESH_TOKEN` | No | Lets the client refresh the access token if a run outlives it. |

No other credential is read. In particular a developer token is never needed: every step here is a
player-authenticated gateway call, not a Control Plane call.

The Lambda result and realtime payload previews print whatever the function or event carried, cut
to 200 characters. Do not point the probe at a function whose response carries a secret.

## Running it

```console
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"
export YOGABASE_PLAYER_ACCESS_TOKEN="..."   # obtained out of band; never typed as an argument

dotnet run --project samples/dotnet/Yogabase.ServerPathProbe -- \
  --gateway-url https://api.example.com \
  --publishable-key sy_pub_example_project \
  --service adaptive-sync-demo \
  --function-id bp_claim_reward \
  --realtime-topic inventory.updated \
  --realtime-seconds 10
```

Against a local k3d installation, where the application gateway is plain `http://` on loopback and
an allocation's `Host` is a node-internal address the macOS host cannot route to:

```console
dotnet run --project samples/dotnet/Yogabase.ServerPathProbe -- \
  --gateway-url http://127.0.0.1:8082 \
  --allow-insecure-loopback \
  --address-override 127.0.0.1 \
  --publishable-key sy_pub_example_project \
  --service adaptive-sync-demo \
  --function-id bp_claim_reward
```

Run `dotnet run --project samples/dotnet/Yogabase.ServerPathProbe -- --help` for the full option
list, the exact environment variable names, and the exit-code table (one code per step, documented
in [`CliOptions.cs`](CliOptions.cs) and [`ExitCodes.cs`](ExitCodes.cs) — keep both in sync with the
help text when either changes). A step whose own request times out exits with that step's code; the
timeout code is reserved for the overall `--timeout-seconds` bound and Ctrl+C.

## What this does and does not prove

A clean run through step 4 proves the server-side surfaces the Unity demo depends on — allocation,
address delivery, and Lambda invocation — respond correctly to a real player identity, without
building or running Unity. It does **not** prove:

- the Unity/IL2CPP packaging of the SignalR realtime client (`README.md` at the client package root
  already flags this as unqualified) — step 5 here runs the same managed SignalR client this
  console process hosts, not the Unity build;
- that a game workload on the allocated port actually understands the demo's own protocol — step 3
  sends one opaque UDP datagram and reports whether an unsolicited reply arrived within a bounded
  window, which most UDP workloads never send, so no reply is expected and not a failure;
- that an addressed event reaches only its recipient — this probe drives one player identity through
  one process, and that proof needs two signed-in clients.

## Tests

`tests/dotnet/Yogabase.ServerPathProbe.Tests` covers the pure logic — argument parsing, environment
variable handling, and bounded-preview formatting — with no network access. It does not exercise
`Program.RunAsync`, which needs a live (or locally mocked) gateway; that was exercised by hand
against a throwaway local `HttpListener` mock during development, not committed to this repository.
