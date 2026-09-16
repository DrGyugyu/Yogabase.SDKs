# Lambda wrapper audit — 2026-09-12

The requested handoff audit was checked against the current SDK source, based on
`0523c26212cebba405753649a5fc3674d9c3fbfc`. The paging, create-if-absent, scope-count and refusal-code
work described as pending in the old handoff is already implemented and tested here.

| Contract | Existing implementation and evidence |
|---|---|
| Storage pages | `LambdaStorage.ListAsync` reads the gateway's object/cursor envelope, preserves the opaque cursor, enforces page size 1–500, and reports malformed responses. `ListAllAsync` follows successive pages. |
| Create-if-absent | `PutAsync` sends the exact logical key and bytes; `object_conflict` remains a typed 409 refusal. `ReplaceAsync` explicitly performs delete then put and validates the proposed write before deleting. The README describes the non-atomic replacement and same-invocation retry behavior. |
| Scope cap | `LambdaUsers.IssueTokenAsync` accepts 1–16 distinct scopes, collapses duplicates, and refuses `openid`/`offline_access` before transport. Tests cover 16 distinct, 17 distinct, and 17 entries containing one duplicate. |
| Refusal codes | `LambdaApiException` preserves gateway status/code/detail; the wrapper also returns the same reserved-key and never-issuable-scope refusals locally. Tests cover storage, cursor and user issuance codes. |

One actual remaining failure was fixed: `ListAllAsync` previously rejected only an immediately
repeated cursor. A server returning A → B → A could keep it requesting pages indefinitely.
The wrapper now records the cursors it has visited and refuses any cycle with the existing
`502/response_invalid` result. A bounded negative test proves the cycle is rejected before a
fourth request, alongside the existing successful multi-page and adjacent-repeat tests.

Validation:

- `dotnet build Yogabase.SDKs.slnx --no-restore -c Release -nologo -v q`: 0 warnings, 0 errors.
- Release executable `Yogabase.Lambda.Tests`: 135 passed, 0 failed.
- Release executable `Yogabase.Yogabase.Client.Tests`: 106 passed, 0 failed.
- Release executable `Yogabase.Yogabase.RuntimeHost.Tests`: 14 passed, 0 failed.
- Existing Python 3.13.12 virtual environment: `PYTHONPATH=src .venv/bin/python -m unittest discover -s tests -v`
  from `languages/python`: 10 passed. No runtime or package was installed.
- Dart was unavailable on PATH and no SDK executable was found in the inspected local tool/cache
  locations or indexed filesystem matches. The established check is
  `dart run test/yogabase_delivery_source_test.dart` from `languages/dart`; it was not run.

These are local wrapper, transport-fixture and source-level tests. They do not claim registry
publication, a standing-installation live walk, Dart qualification, or a full application session.
Preexisting untracked Unity metadata was preserved.
