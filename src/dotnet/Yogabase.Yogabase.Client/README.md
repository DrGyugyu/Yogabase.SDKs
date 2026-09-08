# Yogabase.Yogabase.Client

Preview .NET clients for the versioned Yogabase application-gateway, game-server, build, and
workload Delivery wire contracts. The package
targets .NET Standard 2.1 and .NET 10; Unity remains an unqualified candidate platform pending
IL2CPP/AOT and dependency-closure testing. It supports non-secret publishable-key bootstrap,
OIDC authorization-code/PKCE, mediated object
storage, stateless Function invocation, Actor and Workflow Lambdas, SignalR realtime, and
game-server allocation.

Every route this package calls lives in one place, `YogabaseRoutes`, grouped by the host and
credential it belongs to. Nothing else in the package builds a path from string literals.

The publishable key identifies a client configuration only. It is never sent as application
authorization; authenticated calls use an OIDC access token. The default session store is memory
only. Supply an `IYogabaseSessionStore` backed by the host platform's secure credential facility if
sessions must survive process restarts.

```csharp
using Yogabase.Yogabase;

await using var yogabase = await YogabaseClient.CreateAsync(
    new Uri("https://api.example.com"),
    "sy_pub_example_project");

var server = await yogabase.Servers.StartAsync("battle");
var game = server.FindPort("game");           // game.Endpoint is "node-ip:hostPort"
```

## Reading and writing data

**There is no Data API client, and there is no client-side data surface at all.** The client-facing
Data API module is withdrawn: an application reaches its project's data through a Lambda, which runs
server-side with a full-access database context, and through Auth, Storage, and Realtime. Call a
Lambda with `yogabase.Functions`, and write the Lambda itself against the `Yogabase.Lambda` package
in this repository.

A bootstrap document from an installation that predates the removal may still carry a `data-rest`
capability row. The client accepts that document and ignores the row;
`Configuration.IsAvailable(YogabaseCapability.WithdrawnDataRest)` answers `false` either way.

## Game servers

`yogabase.Servers` allocates hosted-service instances for the signed-in player against
`/v1/servers/*` on the application gateway. `StartAsync` returns a `YogabaseServer` whose
`NetworkPorts` carry the node `Host` and the `ExternalPort` a client dials; an allocation missing a
usable address is refused rather than returned, because a caller that trusts an empty host dials its
own machine. `ListAsync` reads the bare array the contract pins — an instance whose address no
member has observed yet is listed with empty `NetworkPorts`, and must not be dialed — and
`StopAsync` answers the instance's state after the release, never a deletion. The `service` is
the hosted service's canonical id (`[a-z0-9]([-a-z0-9]{0,61}[a-z0-9])?`); `CustomData` is at most
32 entries, 64-byte keys, 1024-byte values, and 4 KiB as canonical JSON, checked here before the
gateway checks it again. A refusal surfaces as `YogabaseApiException` with the gateway's own code:
`server_none_available`, `server_contended`, `server_not_found`, `server_address_not_observed`,
`server_stop_not_available`, `server_request_invalid`, `server_authentication_required` or
`server_unavailable`. The bootstrap document carries no capability identifier for this surface, so
the client does not pretend to know whether an installation enables it — an installation without it
answers the gateway's own refusal.

## Uploading a build

`YogabaseClient.Builds(...)` returns a `YogabaseBuildsClient` for the **Control Plane** origin, not
the application gateway, authenticated with a **project-scoped developer token** issued from the
console's Settings page — a `ybt_`-prefixed value of at most 128 characters, which the client
checks before sending anything, because the Control Plane answers any other bearer with a bare
`401`. A developer tool calls it; a shipped game client never does. The token is a secret: keep it
in the host's credential store, never in source control and never in a shipped build.

```csharp
using var builds = YogabaseClient.Builds(
    new Uri("https://control-plane.example.com/"),
    developerToken);

var build = await builds.UploadDirectoryAsync(
    projectId,
    environmentId,
    "Builds/LinuxServer",
    new YogabaseBuildUploadRequest
    {
        Executable = "Server.x86_64",
        Name = "battle",
        Version = "1.4.0",
        Ports = new[] { new YogabaseBuildPort { Name = "game", Port = 7777, Protocol = "udp" } },
    });

var checked_ = await builds.WaitForCheckAsync(projectId, environmentId, build.BuildId);
if (!checked_.IsAdmitted)
{
    foreach (var finding in checked_.Check.Findings)
    {
        Console.WriteLine($"{finding.Severity} {finding.Id} {finding.Package} {finding.Version} -> {finding.FixedIn}");
    }
    foreach (var refusal in checked_.Check.Refusals)
    {
        Console.WriteLine($"{refusal.Code} field={refusal.Field} cause={refusal.Cause}");
    }
}

// A build refused by an advisory snapshot that has since moved on can be checked again in place.
var again = await builds.RecheckAsync(projectId, environmentId, build.BuildId);
```

A `YogabaseBuild` is exactly what the Control Plane writes: `BuildId` (a UUID), `Digest`,
`State` (`checking`, `admitted`, `refused`), `Identity` (`name:version`), `Attempt` (a corrected
re-upload takes the next attempt of the same identity), `Kind`, and a `Check` that is always
present — `EvidenceOrigin` (`customer-supplied` or `yogabase-derived`), the inventory, scan and
advisory-database digests, `Passed` (false until the member reports; an absent result is never a
clean one), `ObservedAt`, `Findings` and `Refusals`. A refusal carries a stable `Code`, the
`Field` it is about, and a bounded `Cause` token — never prose. A request the Control Plane refuses
outright surfaces as `YogabaseApiException` with an upper-case code such as
`BUILD_BUNDLE_UNRECOGNIZED`, `BUILD_TOKEN_SCOPE_REQUIRED` or `BUILD_NOT_FOUND` and the
`X-Correlation-ID` the response carried.

`UploadDirectoryAsync` zips the directory in memory with forward-slash entry names and keeps the
Linux player binary and native libraries executable (`0755`), because a Unity dedicated server will
not start if the executable bit is lost in transit. `UploadAsync` takes an already-built zip stream
and never disposes it. The multipart form carries the `bundle` part plus the optional `executable`,
`name`, `version` and repeated `ports` hints; the Control Plane also accepts an optional pre-signed
`manifest` part, which this client never sends because Yogabase derives the manifest, the inventory
and the scan from the bundle itself.

"Build check" is Yogabase's own pass over the bundle: it derives the layout manifest and a CycloneDX
inventory, and matches the catalogued components against a digest-pinned OSV snapshot. A pass means
no Critical/High finding among the catalogued components in that snapshot on its recorded date. It
is never a claim that the build is secure, and anything uncatalogued is reported as uncatalogued.

`YogabaseClientConfiguration` carries separate `GatewayEndpoint` and `RealtimeEndpoint` origins.
The client rejects malformed or insecure non-loopback origins and never derives realtime traffic
from the REST gateway. Storage uploads send `X-Yogabase-Content-Sha256`; Actor and Workflow inline
payloads are digest-checked before transport. Realtime Lambda completions are at-least-once and the
client suppresses duplicate event IDs within a bounded in-memory window.

## Workload delivery

`YogabaseDeliveryClient` is deliberately independent from `YogabaseClient`. Give it the TLS-only
Delivery host and a callback that obtains a short-lived workload `DeliveryBearer` token. It exposes
the implemented template, environment, artifact-admission, release, promotion, rollback,
decommission, durable-operation, abandon, polling, and SSE surfaces while preserving `If-Match`,
`Idempotency-Key`, `Location`, `Retry-After`, and opaque SSE resume IDs.

```csharp
using var delivery = new YogabaseDeliveryClient(
    new Uri("https://delivery.example.com/"),
    cancellationToken => workloadTokens.GetAsync(cancellationToken));

var preview = await delivery.PreviewEnvironmentAsync(
    projectId,
    new YogabaseDeliveryEnvironmentPreviewRequest
    {
        Name = "staging",
        TemplateId = templateId,
        TemplateVersion = 1,
    },
    currentETag);
```

The delivery client submits credential-free artifact locators and immutable evidence; it does not
receive Kubernetes credentials or add Agones/PlayFlow compatibility calls. The current Yogabase
contract does not expose runtime health, ready, allocation, reservation, or shutdown operations.

Interactive login/MFA methods use the protected cookie session maintained by the client's HTTP
handler. PKCE methods manage bearer-token sessions through `IYogabaseSessionStore`. Hosts that
replace the default handler are responsible for equivalent cookie behavior and redirect safety.

Yogabase and this package remain preview capabilities until the repository's release gates pass.
The signed bootstrap capability list is authoritative for each deployment; unknown capability
identifiers are treated as unavailable.
