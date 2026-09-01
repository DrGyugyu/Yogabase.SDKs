# Dependency licenses

Status: local preview inventory
Last reviewed: 2026-09-01

This inventory covers direct SDK and test dependencies. Exact transitive versions and integrity
values remain locked in `packages.lock.json`, `package-lock.json`, generated Kiota lock files, and
the ecosystem package manifests. A release must generate an SBOM and re-audit the complete
transitive graph before publication.

| Dependency | Locked version | Use | License | Obligation / boundary |
|---|---:|---|---|---|
| `Microsoft.AspNetCore.SignalR.Client` | 10.0.11 | .NET realtime transport | MIT | Retain the MIT notice when redistributed. Unity remains unqualified. |
| `System.Text.Json` | 10.0.11 | .NET JSON transport | MIT | Retain the MIT notice when redistributed. |
| `xunit.v3` | 4.0.0 | .NET tests only | Apache-2.0 | Build/test-only; retain required notice if redistributed. |
| `@microsoft/kiota-bundle` | 1.0.0-preview.103 | TypeScript runtime | MIT | Retain the MIT notice; transitive licenses are recorded in `package-lock.json`. |
| `typescript` | 7.0.2 | TypeScript compiler, development only | Apache-2.0 | Build-only; do not bundle the compiler with the client. |
| `microsoft-kiota-bundle` | 1.9.10 | Python runtime | MIT | Retain the MIT notice; resolve from the locked Python manifest during qualification. |
| `setuptools` | 80 or newer | Python package build backend | MIT | Build-only; pin exactly before a release build. |
| `com.microsoft.kiota:microsoft-kiota-bundle` | 1.9.3 | Java runtime | MIT | Retain the MIT notice. |
| `com.microsoft.kiota:microsoft-kiota-serialization-*` | 1.9.3 | Java serializers | MIT | Retain the MIT notice. |
| `jakarta.annotation:jakarta.annotation-api` | 2.1.1 | Java annotations | EPL-2.0 or GPL-2.0 with Classpath Exception | Preserve the selected license notices and confirm the release SBOM selection. |
| `microsoft_kiota_bundle` | 0.1.1 | Dart runtime | MIT | Retain the MIT notice; Dart and Flutter remain unqualified. |
| Microsoft Kiota generator | 1.34.1 | External source generation | MIT | Generation tool only; source commit and NuGet digest are bound in `MANIFEST.json`. |

The SDK implementations and package-specific `LICENSE.md` files are proprietary. Third-party
dependencies do not receive rights to Yogabase source beyond their own license terms.
