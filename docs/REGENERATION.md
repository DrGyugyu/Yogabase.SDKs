# Generated client regeneration

`scripts/regenerate-clients.sh` is the single regeneration entry point for the TypeScript,
Python, Java, and Dart REST clients. It verifies the checked-in OpenAPI checksum, restores the
repository-local Microsoft Kiota 1.34.1 tool, generates into an isolated repository-local staging
directory, and synchronizes only the generated source roots.

```text
./scripts/regenerate-clients.sh
```

The tool version, source commit, and NuGet package digest are recorded in `MANIFEST.json`. Each
generated source root also contains a Kiota lock binding it to the exact contract description hash.

The following convenience adapters are authored and deliberately preserved during regeneration:

- `languages/typescript/src/storageLogicalKey.ts`
- `languages/python/src/stackyoga_yogabase/storage_logical_key.py`
- `languages/java/src/main/java/io/stackyoga/yogabase/YogabaseStorageLogicalKey.java`
- `languages/dart/lib/src/yogabase_storage_logical_key.dart`

The TypeScript root export, Python package initializer, and Dart package library are likewise
authored integration seams and are preserved by the script. Review generated diffs before
committing; regeneration does not authorize publication or replace platform-specific build and
live-gateway qualification.
