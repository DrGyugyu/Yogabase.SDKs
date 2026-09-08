# Generated client regeneration

`scripts/regenerate-clients.sh` is the single regeneration entry point for the generated protocol
clients. It verifies all four checked-in OpenAPI checksums, restores the repository-local
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
a new/updated contract, not a reinterpretation of this one.

```text
./scripts/regenerate-clients.sh
```

The tool version, source commit, and NuGet package digest are recorded in `MANIFEST.json`. Each
generated source root also contains a Kiota lock binding it to the exact contract description hash.

The following convenience adapters are authored and deliberately preserved during regeneration:

- `languages/typescript/src/delivery.ts`
- `languages/typescript/src/storageLogicalKey.ts`
- `languages/python/src/stackyoga_yogabase/delivery.py`
- `languages/python/src/stackyoga_yogabase/storage_logical_key.py`
- `languages/java/src/main/java/io/stackyoga/yogabase/YogabaseApplicationClient.java`
- `languages/java/src/main/java/io/stackyoga/yogabase/YogabaseDelivery*.java`
- `languages/java/src/main/java/io/stackyoga/yogabase/YogabaseStorageLogicalKey.java`
- `languages/dart/lib/src/yogabase_application_client.dart`
- `languages/dart/lib/src/yogabase_delivery_*.dart`
- `languages/dart/lib/src/yogabase_storage_logical_key.dart`

The TypeScript root export, Python package initializer, and Dart package library are likewise
authored integration seams and are preserved by the script. The authored .NET application and
delivery clients are not Kiota outputs, and neither is the authored runtime-host module in
`src/dotnet/Yogabase.Yogabase.RuntimeHost` or its Unity package under `languages/unity` — the
script never touches them. Review generated diffs before committing; regeneration does not
authorize publication or replace platform-specific build and live-gateway qualification.

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
