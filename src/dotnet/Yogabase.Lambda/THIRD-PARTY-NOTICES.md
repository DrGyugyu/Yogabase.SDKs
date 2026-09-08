# Third-party notices — Yogabase.Lambda

This package contains no third-party code. One third-party notice is retained anyway, because the
shape of its public surface was translated from a permissively licensed reference.

## Nakama Plus runtime modules

| Field | Record |
|---|---|
| Reference | Nakama Plus (`https://github.com/doublemo/nakama-plus.git`) |
| Revision | commit `18e4d2cd53d5f546d5684d6f70996c08234afc45`, observed on `main` 2026-08-22 |
| Licence | Apache-2.0. The unmodified upstream `LICENSE` (SHA-256 `cfc7749b96f63bd31c3c42b5c471bf756814053e847c10f3eb003417bc523d30`) ships at `licenses/Apache-2.0-Nakama.txt` inside this package. |
| Attribution | Copyright 2018 The Nakama Authors. No `NOTICE` file exists in the pinned tree. |
| Adopted | The *shape* of the runtime-module context surface only: one context object handed to customer code that exposes a database handle, an object store, a user directory with ban and token-issuance operations, a topic publish call, the caller's identity, a logger, and per-invocation facts — the grouping Nakama's `nk.*` runtime module presents. |
| Not adopted | No Nakama source, Go/Lua/JavaScript runtime, type or method name, wire contract, storage/collection model, match, session, party, tournament, leaderboard, wallet, notification, or social concept, and no Nakama dependency. Nothing from Nakama is compiled, linked, or shipped. |
| Modifications | The surface is C#, BCL-typed, and bound to Yogabase's own internal Lambda runtime API (`/v1/lambda-runtime/…`) and to Yogabase's own identity and capability boundaries. The database is `System.Data.Common.DbDataSource` under the project's `yogabase_lambda` identity rather than a Nakama storage engine; there is no key/value collection API, no schemaless storage index, and no runtime-module registration surface in this package. |
| Replacement strategy | The surface is an authored wrapper over an HTTP contract and a BCL type that Yogabase owns. Were the Apache grant ever unusable, it could be rewritten directly from the runtime API route list in `LambdaRoutes` without reference to Nakama. |

Apache-2.0 imposes no source-disclosure, network-use, or commercial restriction. The copying
boundary above is also recorded in the repository's `docs/REFERENCE_REVIEW.md` and
`docs/DEPENDENCY_LICENSES.md`.
