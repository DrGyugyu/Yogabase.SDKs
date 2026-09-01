# Artifact provenance

The `imported` directory preserves the distributable preview artifacts assembled in an external
temporary generation workspace on 2026-08-26, together with its normalized manifest and README.
The structured source packages were copied separately into `languages` so
that dependency trees, tools, caches, and build intermediates could be excluded. The nested
`MANIFEST.json` retains the original artifact SHA-256 values, generator version, generator source
commit, and source-contract digest while replacing developer-local absolute paths with logical
provenance. Its artifact paths are interpreted relative
to `artifacts/imported`; its source-package paths identify the original output layout, while this
repository uses `languages/<language>`.

The current API document is snapshotted under `contracts` and independently checksummed. The root
`MANIFEST.json` records this sibling repository's current contract, source provenance, imported
historical artifact manifest, per-platform qualification, and locally rebuilt NuGet package. The
current TypeScript, Python, Java, and Dart source trees are reproducibly generated with the pinned
Kiota tool; `artifacts/imported` remains historical and unmodified.

No artifact is published. The configured origin is the owner's private local Gitea server and is a
development remote, not a publication channel.
