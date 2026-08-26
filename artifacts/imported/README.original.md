# StackYoga YogaBase SDK preview artifacts

These are local, unpublished preview artifacts generated on 2026-08-26 from
`docs/api/yogabase-client-v1.openapi.json`. They are not releases and must not be published to a
package registry without a separate owner decision.

- `StackYoga.YogaBase.Client.0.1.0-preview.1.nupkg` is the authored .NET package.
- `packages/typescript` is a Kiota-generated TypeScript preview; its compiler build and local npm
  package creation have been verified.
- `packages/python` is a Kiota-generated Python preview requiring Python 3.10 or newer; its wheel
  installs and imports in a clean Python 3.12 environment.
- `packages/java` is a Kiota-generated Java/JVM preview and is the current Kotlin/JVM interop path;
  its Maven package build and resulting JAR have been verified.
- `packages/dart` is an experimental Kiota-generated Dart preview. It remains generated-only
  because a Dart SDK was not available for local compilation.

Clean source/artifact archives for TypeScript, Python, Java, and Dart are stored beside this file.
They exclude dependency trees, tool downloads, generator logs, caches, and build intermediates;
they also exclude agent/memory instruction files and Unity `.meta` files to avoid workspace
collisions. The archives include each package's authored logical-key helper and helper tests;
the TypeScript archive includes compiled `dist` output and its npm package, the Python archive
includes its wheel, and the Java archive includes its JAR. The earlier raw generator directories
remain untouched. `MANIFEST.json` records the final SHA-256 checksums. Its C# NuGet checksum is
recorded after dual-target compilation, package inspection, and a clean local-feed consumer test.

## Hierarchical Storage adapter

The final structured clients were regenerated from the current contract, including signup `201`
success plus `403`, `409`, and `503` Problem mappings and the storage catch-all path-tail metadata.
Kiota still expands `{key}` as a normal path parameter, so direct generated `byKey`/`by_key` calls
are documented as single-segment-only. Each structured package therefore adds a clearly marked,
authored StackYoga adapter that validates the same logical-key grammar as the C# client, encodes
every segment independently, preserves literal `/`, and switches the generated
`WithKeyItemRequestBuilder` to its raw `withUrl`/`with_url` path.

TypeScript and Python each pass three executable helper tests. Java passes its executable
main-style conformance test, including inspection of the generated request URI. Dart remains
source-inspected only because no Dart SDK is installed.

All generated clients contain only the YogaBase application-plane routes in the checked-in
contract. They include bootstrap, signup, Data, Storage, and Functions request builders; OAuth
endpoints are security-scheme metadata rather than generated operations, and Realtime is not in the
OpenAPI-generated clients. A host-platform OAuth adapter is therefore still required. The previews
do not contain StackYoga management, delivery, provisioning, GameSessions, provider coordinates,
or raw SQL APIs. Authentication remains unavailable in current runtime bootstrap documents until
public-client registration and the browser login journey are provisioned and qualified.

The generated sources depend on the language-specific Microsoft Kiota runtime packages recorded
in each package manifest. The sources and manifests are proprietary StackYoga artifacts; Kiota and
its runtime packages remain under their own MIT licenses.
