# Dependency licenses

Status: local preview inventory
Last reviewed: 2026-09-22

This inventory covers direct SDK and test dependencies. Exact transitive versions and integrity
values remain locked in `packages.lock.json`, `package-lock.json`, `pubspec.lock`, the hash-pinned
Python `requirements.lock` and `build-requirements.lock`, generated Kiota lock files, and the
ecosystem package manifests. A release must generate an SBOM and re-audit the complete
transitive graph before publication.

| Dependency | Locked version | Use | License | Obligation / boundary |
|---|---:|---|---|---|
| `Microsoft.AspNetCore.SignalR.Client` | 10.0.11 | .NET realtime transport | MIT | Retain the MIT notice when redistributed. Unity remains unqualified. |
| `Microsoft.Extensions.Logging.Abstractions` | 10.0.11 | `Yogabase.Lambda` logging | MIT | Retain the MIT notice when redistributed. A Lambda bundle shares the sandbox host's load context, so this version must stay compatible with the host's. |
| `Microsoft.Extensions.Caching.Abstractions` | 10.0.11 | `Yogabase.Lambda` `IDistributedCache` type the runtime registers for the Valkey cache capability | MIT | Retain the MIT notice. Brings `Microsoft.Extensions.Primitives` 10.0.11 (MIT). The sandbox host carries the same abstractions from the ASP.NET Core shared framework and shadows `Microsoft.Extensions.*` for a bundle, so this version must stay compatible with the host's; replacing it means resolving the cache by type name instead. |
| `StackExchange.Redis` | 3.1.31 | `Yogabase.Lambda` direct project Valkey data and pub/sub client | MIT | Retain the MIT notice. Matches the existing product host pin and unifies with its assembly; Valkey ACLs enforce isolation. Replacement requires updating the direct client contract in both repositories. |
| `Microsoft.Extensions.Http` | 10.0.10 | `Yogabase.Lambda.Tests` only | MIT | Test-only. `Yogabase.Lambda` deliberately does not reference it and resolves `IHttpClientFactory` by type name; the test reference exists to prove that lookup binds the real interface. |
| `System.Text.Json` | 10.0.11 | .NET JSON transport | MIT | Retain the MIT notice when redistributed. |
| `xunit.v3` | 4.0.0 | .NET tests only | Apache-2.0 | Build/test-only; retain required notice if redistributed. |
| `@microsoft/kiota-bundle` | 1.0.0-preview.103 | TypeScript runtime | MIT | Retain the MIT notice; transitive licenses are recorded in `package-lock.json`. |
| `typescript` | 7.0.2 | TypeScript compiler, development only | Apache-2.0 | Build-only; do not bundle the compiler with the client. |
| `microsoft-kiota-bundle` | 1.9.10 | Python runtime | MIT | Retain the MIT notice. Install the runtime closure from `languages/python/requirements.lock` with `--require-hashes`; its transitive licences are listed below the table. |
| `httpx` | 0.28.1 | Authored Python async Delivery transport and SSE | BSD-3-Clause | Retain copyright and licence text when redistributed. Redirect following remains disabled. |
| `setuptools` | 84.0.0 (`build-requirements.lock`; `pyproject.toml` pins exactly `setuptools==84.0.0`) | Python package build backend | MIT | Build-only; not shipped in the wheel. The locked build runs `python -m build --no-isolation` over `build-requirements.lock`. |
| `build` | 1.6.1 (`build-requirements.lock`) | Python wheel and sdist build frontend | MIT | Build-only; not shipped. Its lock also pins `packaging` 26.3 (Apache-2.0 OR BSD-2-Clause) and `pyproject_hooks` 1.3.3 (MIT), and, only where their markers hold, `tomli` 2.4.1 (MIT), `importlib_metadata` 9.0.1 (Apache-2.0), `zipp` 4.1.0 (MIT) and `colorama` 0.4.6 (BSD classifier). |
| `com.microsoft.kiota:microsoft-kiota-bundle` | 1.9.3 | Java runtime | MIT | Retain the MIT notice. |
| `com.microsoft.kiota:microsoft-kiota-serialization-*` | 1.9.3 | Java serializers | MIT | Retain the MIT notice. |
| `jakarta.annotation:jakarta.annotation-api` | 2.1.1 | Java annotations | EPL-2.0 or GPL-2.0 with Classpath Exception | Preserve the selected license notices and confirm the release SBOM selection. |
| `microsoft_kiota_bundle` | 0.1.1 | Dart runtime | MIT | Retain the MIT notice; live Dart transport and Flutter integration remain unqualified. |
| `uuid` | 4.6.0 | Dart delivery UUID value type | MIT | Retain the MIT notice. |
| Microsoft Kiota generator | 1.34.1 | External source generation | MIT | Generation tool only; source commit and NuGet digest are bound in `MANIFEST.json`. |
| Agones client SDK loop shape (`v1.60.0`, `67b44a783218f7811084adaa5d9b334fb28e44ba`) | Source-ported, not a package reference | Client loop shape for `Yogabase.Yogabase.RuntimeHost` and its Unity package | Apache-2.0 | Retain the Apache-2.0 licence and the "Copyright Contributors to Agones a Series of LF Projects, LLC" attribution; ship the unmodified licence text at `licenses/Apache-2.0-Agones.txt` and pack it into the package; mark every translated file prominently as a modified derivative. Apache-2.0 imposes no source-disclosure, network-use, or commercial restriction. Copying boundary and modifications are recorded in [`REFERENCE_REVIEW.md`](REFERENCE_REVIEW.md). |
| Nakama Plus runtime-module surface shape (`18e4d2cd53d5f546d5684d6f70996c08234afc45`) | Shape-ported, not a package reference and not a source copy | Grouping of the `Yogabase.Lambda` runtime context | Apache-2.0 | Retain the Apache-2.0 licence and the "Copyright 2018 The Nakama Authors" attribution; ship the unmodified licence text at `licenses/Apache-2.0-Nakama.txt` (SHA-256 `cfc7749b96f63bd31c3c42b5c471bf756814053e847c10f3eb003417bc523d30`) and pack it into the package alongside `THIRD-PARTY-NOTICES.md`. No Nakama source, type name, wire contract, or storage model is adopted. Copying boundary is recorded in [`REFERENCE_REVIEW.md`](REFERENCE_REVIEW.md). |

The Python runtime lock resolves these transitive packages, with the licence each wheel's own
metadata declares (pip 26.0 report, 2026-09-17): `anyio` 4.15.1, `h11` 0.16.0, `h2` 4.4.1,
`hpack` 4.2.0, `hyperframe` 6.1.0 and the six transitive `microsoft-kiota-*` 1.9.10 packages are MIT;
`exceptiongroup` 1.3.1 (below Python 3.11 only) declares the MIT classifier; `httpcore` 1.0.9 and
`idna` 3.19 are BSD-3-Clause; `opentelemetry-api` and `opentelemetry-sdk` 1.44.0,
`opentelemetry-semantic-conventions` 0.65b0 and `std-uritemplate` 2.0.12 are Apache-2.0;
`typing_extensions` 4.16.0 is PSF-2.0; and `certifi` 2026.7.22 is MPL-2.0, a file-level
weak-copyleft licence. The package does not vendor or modify any of them; the release SBOM audit
still has to confirm these declarations and review the MPL-2.0 obligation explicitly.

The owner decided on 2026-09-17 to drop `certifi` in favour of the platform trust store. The trust
half is done: the package's own HTTP clients verify against `ssl.create_default_context()`
(`stackyoga_yogabase.platform_ssl_context`), and the README tells callers to build the Kiota adapter's
client the same way. The dependency half is not possible with this stack: `httpx` 0.28.1 and
`httpcore` 1.0.9 both declare `Requires-Dist: certifi` unconditionally and import it (re-checked on
2026-09-22), so `certifi` stays in `requirements.lock` until the Kiota Python runtime stops depending
on httpx or an owner-approved replacement transport is recorded here.

The SDK implementations and package-specific `LICENSE.md` files are proprietary. Third-party
dependencies do not receive rights to Yogabase source beyond their own license terms.

Reference-only SDKs are not dependencies or release inputs. Their immutable identities, licences,
and copying boundaries are recorded in [`REFERENCE_REVIEW.md`](REFERENCE_REVIEW.md).
