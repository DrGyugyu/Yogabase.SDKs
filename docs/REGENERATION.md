# Generated client regeneration

`scripts/regenerate-clients.sh` is the single regeneration entry point for the generated protocol
clients. It verifies all five checked-in OpenAPI checksums, restores the repository-local
Microsoft Kiota 1.34.1 tool, generates into an isolated repository-local staging directory, and
synchronizes only generated source roots. The application contract — which carries the
`/v1/servers/*` game-server surface — generates TypeScript, Python, Java, and Dart clients. The
delivery contract additionally generates the Java and Dart low-level protocol clients used by their
authored delivery facades.

Every document under `contracts/` is a byte-identical copy of the product repository's
`docs/api/` file of the same name. Re-syncing one means copying the product file, regenerating its
`.sha256` sidecar with `shasum -a 256`, and updating the digest wherever `ContractDigestTests` pins
it (`MANIFEST.json`, both READMEs, and the `YOGABASE_DELIVERY_CONTRACT_SHA256` constants for the
delivery contract). A contract is never edited by hand here.

### `contracts/yogabase-builds-v1.openapi.json` is checked in but not generated

The Control Plane's build-ingest surface (upload, read, re-check; developer token) is mirrored and
checksum-verified but deliberately not wired into a `generate_*` call. It is a developer-tool
surface — the Unity Editor, a CLI, CI — rather than something a shipped game client calls, and the
two consumers that exist (`src/dotnet/Yogabase.Yogabase.Client/BuildsClient.cs` and the Unity
package's `YogabaseBuildUploader`) are hand-authored because the multipart upload with a streamed
bundle, the bounded polling, and the UUID build-id rule are simpler to state directly than through
a generated multipart body. Generate a client from it only when a language actually needs one.

### `contracts/yogabase-runtime-host-v1.openapi.json` is checked in but not generated

This third contract describes the Control Plane's server-side runtime-host surface. It is recorded
here (contract file plus `.sha256` sidecar, and its checksum is verified by this script) purely as
a pinned reference artifact — **it is deliberately not wired into any `generate_*` call above.**

The reason is a mismatch between how the contract addresses resources and how a workload actually
calls them. Every path in this contract is instance-addressed, e.g.
`/instances/{instanceId}/...`. But a workload SDK running inside a member instance calls the
self-scoped relay routes exposed to it, e.g. `/runtime-host/v1/self/...`, and it never learns its
own `instanceId` (that is instance-side operator/relay state, not something exposed to the
workload). If Kiota generated a client from this contract, every generated method would require an
`instanceId` parameter the caller cannot supply, and callers would end up hand-rolling the real
`self/...` calls anyway — the generated surface would be actively wrong, not merely unused.

Do not "fix" this by wiring the contract into the regeneration script, changing its base path, or
generating a client against it. If a workload-facing self-scoped client is ever needed, it must be
generated from a contract whose paths are actually shaped as `/runtime-host/v1/self/...` — that is
a new/updated contract, not a reinterpretation of this one. The product has since published such a
contract (the fifth, described next); the SDK checks it in and does not generate from it either.

### `contracts/yogabase-runtime-host-self-v1.openapi.json` is checked in but not generated

The fifth contract is that new, self-scoped document. Its paths are shaped as
`/runtime-host/v1/self/{heartbeat|ready|reserve|shutdown|assignments}` (four `POST`s and a `GET`),
which are exactly the loopback relay routes a workload's runtime-host client speaks, and it carries
the lease and page schemas, each route's refusal codes and the relay's own limits (64 KiB request
body, 64 pending calls, 30 s answer timeout, 100 events per page, reservations clamped to 1-600 s,
30 s lease, 300 s drain window, 2 s retry hint) in its `x-yogabase-runtime-host-relay` extension.
It was mirrored on 2026-10-20.

**The SDK does not generate from it**, as it does not from the instance-addressed document above:
the contract and its `.sha256` sidecar are recorded here, and this script verifies the checksum, but
no `generate_*` call reads it. The only runtime-host client is the authored one in
`src/dotnet/Yogabase.Yogabase.RuntimeHost` and the Unity package under
`languages/unity/io.yogabase.runtime-host`, which carry the heartbeat loop, the wait-out of
`RUNTIME_HOST_INSTANCE_PENDING` and the resume-token polling; `YogabaseRuntimeHostContractTests`
holds that client's routes, limits and refusal codes to this document instead, so a re-mirrored
document that adds, moves or renumbers any of them fails a test. Re-syncing it follows the note
above: copy the product file, regenerate the `.sha256` sidecar, and update the digest in
`MANIFEST.json` (`runtimeHostSelfContract`) and both READMEs, where `ContractDigestTests` pins it.

```text
./scripts/regenerate-clients.sh
```

The tool version, source commit, and NuGet package digest are recorded in `MANIFEST.json`. Each
generated source root also contains a Kiota lock binding it to the exact contract description hash.

The following convenience adapters are authored and deliberately preserved during regeneration:

- `languages/typescript/src/delivery.ts`
- `languages/typescript/src/gateway.ts`
- `languages/typescript/src/jobs.ts`
- `languages/typescript/src/schedules.ts`
- `languages/typescript/src/session.ts`
- `languages/typescript/src/storageLogicalKey.ts`
- `languages/python/src/yogabase_client/_gateway.py`
- `languages/python/src/yogabase_client/delivery.py`
- `languages/python/src/yogabase_client/jobs.py`
- `languages/python/src/yogabase_client/schedules.py`
- `languages/python/src/yogabase_client/sign_in.py`
- `languages/python/src/yogabase_client/storage_logical_key.py`
- `languages/java/src/main/java/io/yogabase/client/YogabaseApplicationClient.java`
- `languages/java/src/main/java/io/yogabase/client/YogabaseAuthException.java`
- `languages/java/src/main/java/io/yogabase/client/YogabaseDelivery*.java`
- `languages/java/src/main/java/io/yogabase/client/YogabaseGateway*.java`
- `languages/java/src/main/java/io/yogabase/client/YogabaseJob*.java`
- `languages/java/src/main/java/io/yogabase/client/YogabaseSchedule*.java`
- `languages/java/src/main/java/io/yogabase/client/YogabaseExternalSignIn.java`, `YogabaseSession.java` and `YogabaseSignIn.java`
- `languages/java/src/main/java/io/yogabase/client/YogabaseStatusErrorCodes.java`
- `languages/java/src/main/java/io/yogabase/client/YogabaseStorageLogicalKey.java`
- `languages/dart/lib/src/yogabase_application_client.dart`
- `languages/dart/lib/src/yogabase_delivery_*.dart`
- `languages/dart/lib/src/yogabase_gateway_support.dart`
- `languages/dart/lib/src/yogabase_jobs_client.dart`
- `languages/dart/lib/src/yogabase_octet_stream.dart`
- `languages/dart/lib/src/yogabase_schedules_client.dart`
- `languages/dart/lib/src/yogabase_sign_in.dart`
- `languages/dart/lib/src/yogabase_storage_logical_key.dart`

The TypeScript root export, Python package initializer, and Dart package library are likewise
authored integration seams and are preserved by the script. The authored .NET application and
delivery clients are not Kiota outputs, and neither is the authored runtime-host module in
`src/dotnet/Yogabase.Yogabase.RuntimeHost` or its Unity package under `languages/unity` — the
script never touches them. Review generated diffs before committing; regeneration does not
authorize publication or replace platform-specific build and live-gateway qualification.

### The Jobs and schedule clients: the contract describes the routes, the convenience layer stays authored

The product's client OpenAPI document describes the durable Jobs routes (`/v1/projects/{projectId}/jobs`,
`x-yogabase-jobs`) and the schedule routes (`/v1/projects/{projectId}/schedules/{scheduleId}`,
`x-yogabase-schedules`), so regenerating produces `jobs` and `schedules` request builders and their models
(`Job`, `JobSubmitRequest`, `ScheduleDefinitionRequest`, `ScheduleTriggerRequest`, `MisfirePolicy`, ...) in the
TypeScript, Python, Java and Dart trees. The authored clients above stay the front door because they carry what a
generated builder cannot: the strict response checks, the result route's digest header, the status-derived
refusal codes, the ETag derived from the schedule's version and instance, and the bounds checked before anything
is sent. How each language relates to the generated code:

- **Java and Dart** build every Jobs and schedule request from the generated builders
  (`toGetRequestInformation()`, `toPutRequestInformation(...)`, ...), send it through the same Kiota request
  adapter and read the answer with the authored strict readers, so a contract change to a route, a verb or a
  body member surfaces in the next regeneration instead of silently drifting. `YogabaseGatewayChannel.java`
  and `yogabase_gateway_support.dart` hold what the two clients share.
- **TypeScript and Python** stay standalone (`fetch` / `httpx`, one `GatewayChannel` in `gateway.ts` and
  `_gateway.py`), the way their delivery clients do, and are held to the generated builders by tests that build
  the same request both ways and compare verb, URL, headers and body.
- **.NET** has no generated code at all; its transport is authored.

Three checks keep the clients and the document together, and a re-mirrored contract that changes any of them
fails a test instead of passing quietly:

- `ContractOperationParityTests` lists every operation of the mirrored application contract with the SDK surface
  that serves it in each shipped language. The .NET column is exercised (the first request of each of the 32
  non-excluded operations is compared with the contract's verb and path); the TypeScript, Python, Java and Dart
  generated trees are scanned for exactly the contract's verbs and paths; each language's authored Jobs and
  schedule client must still spell the operation. The only exclusions are the five Data operations of the .NET
  client, with the reason recorded in its README, and the two provider return routes (`/auth/external/{provider}/return`,
  a GET and a POST), which a browser or a provider calls and an application never does. A new operation in a
  re-mirrored document therefore fails until every language has a surface for it or an explicit, reasoned exclusion.
- `ContractLimitsAndCodesTests` compares the contract's bounds, statuses and refusal codes with each client's
  constants (the schedule write's `schedule_payload_too_large` and its limits, the job codes, the policy names,
  the ETag pattern, and the schedule id: the definition member's pattern, and the path parameter's lack of one,
  because a schedule written before the rule is still read, run and deleted by its id).
- `JobsSurfaceParityTests` and `SchedulesSurfaceParityTests` keep the five authored clients spelling the same
  route tails, request members, error codes and bounds, and `SchedulesSurfaceParityTests` holds the pattern each
  client compiles for a schedule write's id to the contract's text; they read the sources as text, which is how
  the .NET suite holds Dart to them without a Dart SDK. The .NET, TypeScript, Python and Java tests also run
  every Latin-1 character and the edge lengths through both a write and the pattern read from the mirrored
  document.

When a Kiota upgrade or a contract change renames a generated builder, the Java and Dart clients stop
compiling at that exact call, which is the point of wrapping
them; the TypeScript and Python equivalence tests fail the same way. Keep the authored names clear of the
generated ones: the TypeScript root export re-exports both modules, so a duplicate export name is a compile
error. `MANIFEST.json` records, per mirrored document, the contract digest beside the Kiota description hash
that Kiota wrote for it into every `kiota-lock.json` (`generator.descriptionHashes`); the hash is a function of the
document but not the SHA-256 of its bytes, so the pair is what proves a generated tree belongs to the mirrored
contract.

### Every rsync `--exclude` must stay anchored with a leading slash

The preservation list above is implemented as `rsync --exclude` patterns. An rsync pattern with no
leading slash matches by **basename at every depth**, not just at the transfer root. That is a trap
here, because Kiota's TypeScript emitter writes almost all generated code into per-directory
`index.ts` files: an unanchored `--exclude index.ts` silently excluded 56 of the 60 generated
TypeScript files — protecting them from update *and* from `--delete` — so TypeScript regeneration
was a no-op that still refreshed `kiota-lock.json`, leaving the lock claiming a contract the source
did not match. The same latent bug applied to the Python and Java/Dart entries.

Every exclude is therefore written as `/index.ts`, `/delivery/`, and so on. When adding an authored
file to the list, anchor it the same way, and confirm afterwards that a regeneration run leaves the
authored files untouched while the generated tree actually changes.

The `--exclude /delivery/` entries in the Java and Dart parent blocks are intentional and must not
be removed: they stop the parent sync's `--delete` from wiping the generated delivery subtree,
which its own rsync populates immediately afterwards.
