# YogaBase Java/JVM client preview

Generated with Microsoft Kiota 1.34.1 from the StackYoga YogaBase v1-preview contract. The source
targets the Java/JVM Kiota runtime and is the current Kotlin/JVM interoperability path; no native
Kotlin or Android qualification is claimed. Callers must provide a request adapter with the exact
YogaBase gateway and a project bearer-token authentication provider.

The serialization modules are direct compile dependencies because the generated client registers
their factories; the Kiota bundle currently marks those transitive modules as runtime scope.

This source preview is local, unpublished, and not production-qualified.

## Hierarchical storage keys

Generated `ObjectsRequestBuilder.byKey(...)` is single-segment-only. For logical keys containing
`/`, call the authored `StackYogaStorageLogicalKey.requestBuilder(...)` adapter with the generated
objects builder, validated gateway origin, and logical key. It applies the C# key grammar, encodes
each segment separately, and returns the generated item builder through `withUrl(...)`.

```java
var item = StackYogaStorageLogicalKey.requestBuilder(
    client.v1().storage().objectsEscaped(),
    requestAdapter.getBaseUrl(),
    "avatars/user-1/photo.png");
```

`StackYogaStorageLogicalKey` is authored StackYoga code, not Kiota output.
