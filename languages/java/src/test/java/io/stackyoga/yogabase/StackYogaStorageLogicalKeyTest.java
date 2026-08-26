package io.stackyoga.yogabase;

// Executable test for the authored StackYoga adapter; no test framework is required.

import com.microsoft.kiota.RequestAdapter;
import io.stackyoga.yogabase.v1.storage.objectsescaped.ObjectsRequestBuilder;
import java.lang.reflect.Proxy;
import java.util.HashMap;
import java.util.List;

public final class StackYogaStorageLogicalKeyTest {
    private StackYogaStorageLogicalKeyTest() {
    }

    public static void main(String[] args) throws Exception {
        assertEquals(
            "https://apps.example.test:8443/v1/storage/objects/avatars/My%20photo%20%281%29.png",
            StackYogaStorageLogicalKey.buildStorageObjectUrl(
                "https://apps.example.test:8443/",
                "avatars/My photo (1).png"));
        assertEquals(
            "https://apps.example.test/v1/storage/objects/caf%C3%A9/%E7%8C%AB.json",
            StackYogaStorageLogicalKey.buildStorageObjectUrl(
                "https://apps.example.test/",
                "café/猫.json"));

        for (String value : List.of(
            "", " ", "/a", "projects/a", "https://example.test/a", "a\\b", "a%b", "a\0b",
            "a//b", ".", "..", "a/./b", "a/../b", "x".repeat(1025))) {
            expectInvalid(value);
        }

        final String gateway = "https://apps.example.test/";
        final RequestAdapter adapter = (RequestAdapter)Proxy.newProxyInstance(
            RequestAdapter.class.getClassLoader(),
            new Class<?>[] { RequestAdapter.class },
            (proxy, method, arguments) -> method.getName().equals("getBaseUrl") ? gateway : null);
        final HashMap<String, Object> pathParameters = new HashMap<>();
        pathParameters.put("baseurl", gateway);
        final ObjectsRequestBuilder objects = new ObjectsRequestBuilder(pathParameters, adapter);
        final var builder = StackYogaStorageLogicalKey.requestBuilder(
            objects,
            gateway,
            "avatars/user 1/photo.png");
        assertEquals(
            "https://apps.example.test/v1/storage/objects/avatars/user%201/photo.png",
            builder.toGetRequestInformation().getUri().toString());

        System.out.println("java logical-key helper tests: PASS");
    }

    private static void expectInvalid(String value) {
        try {
            StackYogaStorageLogicalKey.buildStorageObjectUrl("https://apps.example.test/", value);
            throw new AssertionError("Expected invalid key: " + value);
        } catch (IllegalArgumentException expected) {
        }
    }

    private static void assertEquals(String expected, String actual) {
        if (!expected.equals(actual)) {
            throw new AssertionError("Expected " + expected + " but got " + actual);
        }
    }
}
