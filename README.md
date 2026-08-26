# StackYoga YogaBase SDKs

This is the standalone local development repository for the StackYoga YogaBase client SDKs. It is
deliberately separate from the StackYoga product repository, solution, images, Helm chart, and
Marketplace build graph.

All packages are unpublished previews. Nothing in this repository authorizes publication to
NuGet, npm, PyPI, Maven Central, pub.dev, GitHub, or another registry.

## Packages

| Platform | Location | Package identity | Qualification |
|---|---|---|---|
| .NET | `src/dotnet/StackYoga.YogaBase.Client` | `StackYoga.YogaBase.Client` `0.1.0-preview.1` | Authored; `netstandard2.1` and `net10.0`; local tests and package build |
| TypeScript | `languages/typescript` | `@stackyoga/yogabase-client-preview` `0.1.0-preview.1` | Kiota-generated preview plus StackYoga storage-key adapter |
| Python | `languages/python` | `stackyoga-yogabase-client-preview` `0.1.0.dev1` | Kiota-generated preview plus StackYoga storage-key adapter |
| Java/JVM | `languages/java` | `io.stackyoga:yogabase-client-preview` `0.1.0-preview.1` | Kiota-generated preview and current Kotlin/JVM interop path |
| Dart | `languages/dart` | `stackyoga_yogabase_client_preview` `0.1.0-dev.1` | Experimental generated source; local Dart compilation is unqualified |

The immutable API snapshot is `contracts/yogabase-client-v1.openapi.json`. Its adjacent checksum
and `MANIFEST.json` bind this repository to the exact contract and imported artifacts.

## Local verification

```text
dotnet restore StackYoga.SDKs.slnx --locked-mode
dotnet build StackYoga.SDKs.slnx -c Release --no-restore
dotnet test --solution StackYoga.SDKs.slnx -c Release --no-restore --no-build
dotnet pack src/dotnet/StackYoga.YogaBase.Client/StackYoga.YogaBase.Client.csproj -c Release --no-restore
```

Each generated-language folder retains its native package manifest, dependency lock where the
ecosystem provides one, generated Kiota lock, source, documentation, and conformance helper tests.
Dependency trees, downloaded tools, caches, and build intermediates are intentionally excluded.
Previously built packages and clean source archives are retained unchanged under
`artifacts/imported`; sibling-repository builds go under `artifacts/local`.

## Scope and security

The SDK surface is only the YogaBase application gateway: bootstrap, signup, approved Data,
Storage, Functions, and the authored .NET SignalR subscription client. It contains no StackYoga
management, Kubernetes, cloud provisioning, delivery, arbitrary SQL, GameSessions, or provider
credentials. Public apps use authorization code with PKCE; the SDK does not contain confidential
client credentials. The default .NET token store is memory-only.

Targeting .NET Standard 2.1 does not by itself qualify Unity or AOT. Android, Flutter, Dart,
Kotlin/JVM, Unity IL2CPP, native secure storage, redirect handling, and live-gateway conformance
require their own platform evidence before support can be claimed.
