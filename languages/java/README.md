# Yogabase Java/JVM client preview

Generated with Microsoft Kiota 1.34.1 from the current Yogabase v1-preview contract. The source
targets the Java/JVM Kiota runtime and is the current Kotlin/JVM interoperability path; no native
Kotlin or Android qualification is claimed. Callers must provide a request adapter with the exact
Yogabase REST gateway and a project bearer-token authentication provider.

The generated REST surface includes bootstrap, signup and interactive auth endpoints, Data,
Storage, stateless Function invocation, Actor Lambda invocation, and Workflow start/cancel. The
contract returns a separate realtime origin, but this package does not yet include or qualify a
JVM SignalR wrapper.

The serialization modules are direct compile dependencies because the generated client registers
their factories; the Kiota bundle currently marks those transitive modules as runtime scope.

This source preview is local, unpublished, and not production-qualified.

## Hierarchical storage keys

Generated `ObjectsRequestBuilder.byKey(...)` is single-segment-only. For logical keys containing
`/`, call the authored `YogabaseStorageLogicalKey.requestBuilder(...)` adapter with the generated
objects builder, validated gateway origin, and logical key. It applies the C# key grammar, encodes
each segment separately, and returns the generated item builder through `withUrl(...)`.

```java
var item = YogabaseStorageLogicalKey.requestBuilder(
    client.v1().storage().objectsEscaped(),
    requestAdapter.getBaseUrl(),
    "avatars/user-1/photo.png");
```

`YogabaseStorageLogicalKey` is authored Yogabase code, not Kiota output.

For `PUT`, add both required contract headers in the generated `PutRequestConfiguration` consumer:
`X-Yogabase-Content-Sha256: sha256:<64 lowercase hex>` and a bounded `Idempotency-Key`. Kiota does
not emit dedicated Java method arguments for these headers.

## Workload delivery

The package also contains a delivery client generated from the separate workload Delivery v1
contract under `io.stackyoga.yogabase.delivery`. Wrap a delivery-only `RequestAdapter` with
`YogabaseDeliveryClient`; do not reuse the application adapter or its credentials. The authored
facade validates the delivery origin, provides a rotating bearer-token provider and header helpers,
recognizes the exact terminal operation states, supports cancellable bounded polling, and parses
SSE while preserving opaque resume IDs.

```java
try (var delivery = new YogabaseDeliveryClient(
        deliveryRequestAdapter,
        URI.create("https://delivery.example.com"))) {
    var operations = delivery.api().api().v1().delivery().projects()
        .byProjectId(projectId).operations();
}
```

Artifact admission carries credential-free source and evidence metadata. No Kubernetes credential,
Agones/PlayFlow compatibility call, or unimplemented runtime health/allocation API is added.
