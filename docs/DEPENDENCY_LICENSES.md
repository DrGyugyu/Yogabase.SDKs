# Dependency licenses

Status: public-source inventory (the source is public on GitHub under Apache-2.0; no package
has been published to a registry)
Last reviewed: 2026-10-20

This inventory covers direct SDK and test dependencies. Exact transitive versions and integrity
values remain locked in `packages.lock.json`, `package-lock.json`, `pubspec.lock`, the hash-pinned
Python `requirements.lock` and `build-requirements.lock`, generated Kiota lock files, and the
ecosystem package manifests. Publishing this repository's source on GitHub redistributes none of
the dependencies listed here: the repository carries lock files and manifests, not the packages
themselves, so the public source carries no third-party notice obligation beyond the ones the
repository already keeps for ported material. A release to a package registry (NuGet, npm, PyPI,
Maven Central, pub.dev or a UPM registry) must generate an SBOM and re-audit the complete
transitive graph before that publication; the two review items below (MPL-2.0 `certifi`,
EPL/GPL-CPE `jakarta.annotation-api`) are registry-publication items and stay open until one is
prepared.

| Dependency | Locked version | Use | License | Obligation / boundary |
|---|---:|---|---|---|
| `Microsoft.AspNetCore.SignalR.Client` | 10.0.13 | .NET realtime transport | MIT | Retain the MIT notice when redistributed. Unity remains unqualified. |
| `Microsoft.Extensions.Logging.Abstractions` | 10.0.13 | `Yogabase.Lambda` logging | MIT | Retain the MIT notice when redistributed. A Lambda bundle shares the sandbox host's load context, so this version must stay compatible with the host's. |
| `Microsoft.Extensions.Caching.Abstractions` | 10.0.13 | `Yogabase.Lambda` `IDistributedCache` type the runtime registers for the Valkey cache capability | MIT | Retain the MIT notice. Brings `Microsoft.Extensions.Primitives` 10.0.11 (MIT). The sandbox host carries the same abstractions from the ASP.NET Core shared framework and shadows `Microsoft.Extensions.*` for a bundle, so this version must stay compatible with the host's; replacing it means resolving the cache by type name instead. |
| `StackExchange.Redis` | 3.1.31 | `Yogabase.Lambda` direct project Valkey data and pub/sub client | MIT | Retain the MIT notice. Matches the existing product host pin and unifies with its assembly; Valkey ACLs enforce isolation. Replacement requires updating the direct client contract in both repositories. |
| `Microsoft.Extensions.Http` | 10.0.13 | `Yogabase.Lambda.Tests` only | MIT | Test-only. `Yogabase.Lambda` deliberately does not reference it and resolves `IHttpClientFactory` by type name; the test reference exists to prove that lookup binds the real interface. |
| `System.Text.Json` | 10.0.13 | .NET JSON transport | MIT | Retain the MIT notice when redistributed. |
| `xunit.v3` | 4.0.0 | .NET tests only | Apache-2.0 | Build/test-only; retain required notice if redistributed. |
| `@microsoft/kiota-bundle` | 1.0.0-preview.103 | TypeScript runtime | MIT | Retain the MIT notice; transitive licenses are recorded in `package-lock.json`. |
| `typescript` | 7.0.2 | TypeScript compiler, development only | Apache-2.0 | Build-only; do not bundle the compiler with the client. |
| `microsoft-kiota-bundle` | 1.9.10 | Python runtime | MIT | Retain the MIT notice. Install the runtime closure from `languages/python/requirements.lock` with `--require-hashes`; its transitive licences are listed below the table. |
| `httpx` | 0.28.1 | Authored Python async Delivery transport and SSE | BSD-3-Clause | Retain copyright and licence text when redistributed. Redirect following remains disabled. |
| `setuptools` | 84.0.0 (`build-requirements.lock`; `pyproject.toml` pins exactly `setuptools==84.0.0`) | Python package build backend | MIT | Build-only; not shipped in the wheel. The locked build runs `python -m build --no-isolation` over `build-requirements.lock`. |
| `build` | 1.6.1 (`build-requirements.lock`) | Python wheel and sdist build frontend | MIT | Build-only; not shipped. Its lock also pins `packaging` 26.3 (Apache-2.0 OR BSD-2-Clause) and `pyproject_hooks` 1.3.3 (MIT), and, only where their markers hold, `tomli` 2.4.1 (MIT), `importlib_metadata` 9.0.1 (Apache-2.0), `zipp` 4.1.0 (MIT) and `colorama` 0.4.6 (BSD classifier). |
| `com.microsoft.kiota:microsoft-kiota-bundle` | 1.9.3 | Java runtime | MIT | Retain the MIT notice. |
| `com.microsoft.kiota:microsoft-kiota-serialization-*` | 1.9.3 | Java serializers | MIT | Retain the MIT notice. |
| `com.google.code.gson:gson` | 2.14.0 (transitive compile dependency of `microsoft-kiota-serialization-json` 1.9.3) | Java: JSON reading in the authored Jobs client and the sign-in | Apache-2.0 | Retain the Apache-2.0 notice. Used directly by `YogabaseJobsClient` and `YogabaseSignIn` though it arrives with the Kiota JSON module; replacement is Kiota's own `ParseNode`, at the cost of the strict member-type checks the clients make. |
| `com.squareup.okhttp3:okhttp` | 4.12.0 (compile dependency of `microsoft-kiota-http-okHttp` 1.9.3) | Java: the Jobs client reads the OkHttp `Response` the Kiota adapter hands back | Apache-2.0 | Retain the Apache-2.0 notice. Used directly by `YogabaseJobsClient`, which therefore needs the Kiota bundle's `OkHttpRequestAdapter` and names any other adapter; the result route's digest header is not reachable through the adapter interface. |
| `jakarta.annotation:jakarta.annotation-api` | 2.1.1 | Java annotations | EPL-2.0 or GPL-2.0 with Classpath Exception | Preserve the selected license notices and confirm the selection in the SBOM of a registry publication. Open registry-publication item; the public source redistributes nothing of it. |
| `microsoft_kiota_bundle` | 0.1.1 | Dart runtime | MIT | Retain the MIT notice; live Dart transport and Flutter integration remain unqualified. |
| `uuid` | 4.6.0 | Dart delivery UUID value type | MIT | Retain the MIT notice. |
| `http` | 1.6.0 | Dart sign-in transport (also the Kiota adapter's own client type) | BSD-3-Clause | Retain the BSD-3-Clause notice. Already locked as a Kiota dependency; now declared directly. Redirect following is disabled per request. |
| `crypto` | 3.0.7 | Dart sign-in PKCE `S256` challenge | BSD-3-Clause | Retain the BSD-3-Clause notice. Already locked as a transitive dependency; now declared directly. |
| Microsoft Kiota generator | 1.34.1 | External source generation | MIT | Generation tool only; source commit and NuGet digest are bound in `MANIFEST.json`. |
| `org.apache.maven.plugins:maven-dependency-plugin` | 3.8.1 (named on the command line, not in `pom.xml`) | Java: writes the compile classpath the check programs run on | Apache-2.0 | Build tool only; nothing of it enters the jar. |
| Agones client SDK loop shape (`v1.60.0`, `67b44a783218f7811084adaa5d9b334fb28e44ba`) | Source-ported, not a package reference | Client loop shape for `Yogabase.Yogabase.RuntimeHost` and its Unity package | Apache-2.0 | Retain the Apache-2.0 licence and the "Copyright Contributors to Agones a Series of LF Projects, LLC" attribution; ship the unmodified licence text at `licenses/Apache-2.0-Agones.txt` and pack it into the package; mark every translated file prominently as a modified derivative. Apache-2.0 imposes no source-disclosure, network-use, or commercial restriction. Copying boundary and modifications are recorded in [`REFERENCE_REVIEW.md`](REFERENCE_REVIEW.md). |
| Nakama Plus runtime-module surface shape (`18e4d2cd53d5f546d5684d6f70996c08234afc45`) | Shape-ported, not a package reference and not a source copy | Grouping of the `Yogabase.Lambda` runtime context | Apache-2.0 | Retain the Apache-2.0 licence and the "Copyright 2018 The Nakama Authors" attribution; ship the unmodified licence text at `licenses/Apache-2.0-Nakama.txt` (SHA-256 `cfc7749b96f63bd31c3c42b5c471bf756814053e847c10f3eb003417bc523d30`) and pack it into the package alongside `THIRD-PARTY-NOTICES.md`. No Nakama source, type name, wire contract, or storage model is adopted. Copying boundary is recorded in [`REFERENCE_REVIEW.md`](REFERENCE_REVIEW.md). |

The 2026-10-20 additions add no dependency: the .NET Data client and the `Yogabase.Lambda` data
wrapper use `System.Text.Json` and the BCL already listed, and the Java sign-in uses the JDK's own
`java.net.http` client and Gson, already listed (its check program's fake gateway uses the JDK's
`com.sun.net.httpserver`, test-only). The toolchains the suites ran on are recorded in
[`CLOUD_ENVIRONMENT.md`](CLOUD_ENVIRONMENT.md); none is a dependency of a package.

## The Unity build line (8.0)

The owner decided on 2026-09-17 that the Unity SDK is a separate netstandard2.1 build pinned to the
8.0 package line. `src/unity/Yogabase.Yogabase.Client.Unity` and
`src/unity/Yogabase.Yogabase.RuntimeHost.Unity` compile the same sources as the NuGet builds against
`src/unity/Directory.Packages.props`, which does not import the root pins. Both locks
(`src/unity/*/packages.lock.json`) resolve nothing at 9.x or above; `UnityBuildTests` pins that and
the references the built assemblies carry. The versions are exactly the ones the AI Game Developer
(MCP) Unity package restores, so a Unity project that also has that package holds byte-identical
copies of each.

| Dependency | Locked version | Use | License | Obligation / boundary |
|---|---:|---|---|---|
| `Microsoft.AspNetCore.SignalR.Client` and its closure: `SignalR.Client.Core`, `SignalR.Common`, `SignalR.Protocols.Json`, `Http.Connections.Client`, `Http.Connections.Common`, `Connections.Abstractions`, `Microsoft.Extensions.Features` | 8.0.32 | Unity client realtime transport | MIT | Retain the MIT notice (each package's `LICENSE.TXT`) wherever the DLLs are redistributed, including a Unity project that vendors them. |
| `System.Text.Json` | 8.0.5 | Unity client and runtime-host JSON | MIT | Retain the MIT notice. Unity 6000.5.7f1 binds its own 8.0.0.0 copy (a BCL extension); the package is a build input only there. |
| `Microsoft.Extensions.DependencyInjection` / `.Abstractions`, `Logging` / `.Abstractions`, `Options`, `Primitives` | 8.0.1 / 8.0.2, 8.0.1 / 8.0.3, 8.0.2, 8.0.0 | SignalR client dependencies | MIT | Retain the MIT notice. |
| `Microsoft.Bcl.TimeProvider`, `Microsoft.Bcl.AsyncInterfaces`, `System.IO.Pipelines`, `System.Threading.Channels`, `System.Text.Encodings.Web`, `System.Diagnostics.DiagnosticSource` | 8.0.1, 8.0.0, 8.0.0, 8.0.0, 8.0.0, 8.0.1 | SignalR and JSON dependencies | MIT | Retain the MIT notice. |
| `System.ComponentModel.Annotations`, `System.Runtime.CompilerServices.Unsafe` | 5.0.0, 6.0.0 | Options and JSON dependencies | MIT | Retain the MIT notice. |
| `System.Memory`, `System.Buffers`, `System.Numerics.Vectors`, `System.Threading.Tasks.Extensions` | 4.5.5, 4.5.1, 4.4.0, 4.5.4 | netstandard2.0 support packages `System.Text.Json` 8.0.5 names (it has no netstandard2.1 group) | MIT (the corefx `LICENSE.TXT` their nuspecs point at) | Build inputs only: netstandard2.1 and Unity's profile carry these types, so no Unity project needs the package DLLs. |

The Python runtime lock resolves these transitive packages, with the licence each wheel's own
metadata declares (pip 26.0 report, 2026-09-17): `anyio` 4.15.1, `h11` 0.16.0, `h2` 4.4.1,
`hpack` 4.2.0, `hyperframe` 6.1.0 and the six transitive `microsoft-kiota-*` 1.9.10 packages are MIT;
`exceptiongroup` 1.3.1 (below Python 3.11 only) declares the MIT classifier; `httpcore` 1.0.9 and
`idna` 3.19 are BSD-3-Clause; `opentelemetry-api` and `opentelemetry-sdk` 1.44.0,
`opentelemetry-semantic-conventions` 0.65b0 and `std-uritemplate` 2.0.12 are Apache-2.0;
`typing_extensions` 4.16.0 is PSF-2.0; and `certifi` 2026.7.22 is MPL-2.0, a file-level
weak-copyleft licence. The package does not vendor or modify any of them; the SBOM audit of a
registry publication still has to confirm these declarations and review the MPL-2.0 obligation
explicitly. It is an open registry-publication item; the public source redistributes nothing of it.

The owner decided on 2026-09-17 to drop `certifi` in favour of the platform trust store. The trust
half is done: the package's own HTTP clients verify against `ssl.create_default_context()`
(`yogabase_client.platform_ssl_context`), and the README tells callers to build the Kiota adapter's
client the same way. The dependency half is not possible with this stack: `httpx` 0.28.1 and
`httpcore` 1.0.9 both declare `Requires-Dist: certifi` unconditionally and import it (re-checked on
2026-09-22), so `certifi` stays in `requirements.lock` until the Kiota Python runtime stops depending
on httpx or an owner-approved replacement transport is recorded here.

The SDK implementations are licensed under Apache-2.0 with Yogabase as the copyright holder (owner
decision, 2026-10-19): the root `LICENSE.md` is the licence text, `NOTICE` the attribution notice,
and the .NET packages declare `Apache-2.0` as their licence expression and carry both files.
Third-party dependencies receive no rights to Yogabase source beyond that licence's terms, and
their own notices stay with them as the table records.

Reference-only SDKs are not dependencies or release inputs. Their immutable identities, licences,
and copying boundaries are recorded in [`REFERENCE_REVIEW.md`](REFERENCE_REVIEW.md).

## Advisory-driven bumps

- 2026-10-19: `Microsoft.AspNetCore.SignalR.Client` 10.0.11 → 10.0.13 and (Unity line) 8.0.15 → 8.0.32
  for CVE-2026-70159 / GHSA-334f-vvp4-7g6p (SignalR denial of service in `SignalR.Common`), which
  NuGet audit reported as NU1903 and the build treats as an error. The sibling
  `Microsoft.Extensions.*` and `System.Text.Json` pins moved to 10.0.13 with it; the Unity
  closure's eight SignalR packages resolved to 8.0.32 and nothing else in the Unity lock changed.
