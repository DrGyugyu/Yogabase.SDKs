import assert from "node:assert/strict";
import test from "node:test";
import { DeliveryPollingTimeoutError, YogabaseDeliveryClient, YogabaseDeliveryError } from "../dist/index.js";

function fakeFetch(responses) {
    const calls = [];
    const fetcher = async (url, init) => {
        calls.push({ url: String(url), init });
        const next = responses.shift();
        if (typeof next === "function") return next(url, init);
        return next;
    };
    return { calls, fetcher };
}

function jsonResponse(body, status = 200, headers = {}) {
    return new Response(JSON.stringify(body), { status, headers: { "Content-Type": "application/json", ...headers } });
}

test("delivery client keeps application and delivery hosts separate and sends bearer/header guards", async () => {
    const fake = fakeFetch([jsonResponse({ previewId: "p", previewDigest: "sha256:p", action: "create", expectedRevision: 0, approvalRequired: false, expiresAt: "2026-09-01T00:00:00Z" })]);
    const client = new YogabaseDeliveryClient({ deliveryHost: "https://delivery.example.test/", accessToken: "token", fetch: fake.fetcher });
    const result = await client.previewEnvironment("project", { name: "dev", templateId: "template", templateVersion: 1 }, { ifMatch: 'W/"7"' });
    assert.equal(result.data.action, "create");
    assert.equal(fake.calls[0].url, "https://delivery.example.test/api/v1/delivery/projects/project/environments/previews");
    assert.equal(fake.calls[0].init.headers.get("Authorization"), "Bearer token");
    assert.equal(fake.calls[0].init.headers.get("If-Match"), 'W/"7"');
    assert.equal(fake.calls[0].init.headers.get("Content-Type"), "application/json");
});

test("delivery client rejects a host URL with a path", () => {
    assert.throws(
        () => new YogabaseDeliveryClient({ deliveryHost: "https://delivery.example.test/not-an-origin", accessToken: "token" }),
        /origin-only/,
    );
    assert.throws(
        () => new YogabaseDeliveryClient({ deliveryHost: "http://delivery.example.test", accessToken: "token" }),
        /HTTPS/,
    );
});

test("submit operations carry idempotency and expose response metadata", async () => {
    const fake = fakeFetch([jsonResponse({ operationId: "op", state: "Queued", kind: "admit", revision: 1, updatedAt: "2026-09-01T00:00:00Z" }, 202, { Location: "/operations/op", "Retry-After": "3", ETag: 'W/"1"' })]);
    const client = new YogabaseDeliveryClient({ deliveryHost: "https://delivery.example.test", accessToken: "token", fetch: fake.fetcher });
    const result = await client.submitArtifactAdmission("p", "e", { previewId: "preview", previewDigest: "sha256:preview" }, { idempotencyKey: "request-1" });
    assert.equal(result.data.operationId, "op");
    assert.equal(result.location, "/operations/op");
    assert.equal(result.retryAfter, 3);
    assert.equal(result.etag, 'W/"1"');
    assert.equal(fake.calls[0].init.headers.get("Idempotency-Key"), "request-1");
});

test("polling is bounded and cancellable", async () => {
    const fake = fakeFetch([jsonResponse({ operationId: "op", state: "Running", kind: "release", revision: 1, updatedAt: "2026-09-01T00:00:00Z" })]);
    const client = new YogabaseDeliveryClient({ deliveryHost: "https://delivery.example.test", accessToken: "token", fetch: fake.fetcher });
    await assert.rejects(client.waitForOperation("p", "op", { maxAttempts: 1, intervalMs: 0, timeoutMs: 100 }), DeliveryPollingTimeoutError);

    for (const state of ["AwaitingApproval", "Queued", "Dispatched", "Running"]) {
        const pending = fakeFetch([jsonResponse({ operationId: "op", state, kind: "release", revision: 1, updatedAt: "2026-09-01T00:00:00Z" })]);
        const pendingClient = new YogabaseDeliveryClient({ deliveryHost: "https://delivery.example.test", accessToken: "token", fetch: pending.fetcher });
        await assert.rejects(pendingClient.waitForOperation("p", "op", { maxAttempts: 1, intervalMs: 0, timeoutMs: 100 }), DeliveryPollingTimeoutError, state);
    }

    const blocked = fakeFetch([jsonResponse({ operationId: "op", state: "Blocked", kind: "release", revision: 1, updatedAt: "2026-09-01T00:00:00Z" })]);
    const blockedClient = new YogabaseDeliveryClient({ deliveryHost: "https://delivery.example.test", accessToken: "token", fetch: blocked.fetcher });
    assert.equal((await blockedClient.waitForOperation("p", "op", { maxAttempts: 1, intervalMs: 0, timeoutMs: 100 })).state, "Blocked");

    const controller = new AbortController();
    controller.abort();
    await assert.rejects(client.waitForOperation("p", "op", { signal: controller.signal }), error => error.name === "AbortError");

    const slow = fakeFetch([async () => {
        await new Promise(resolve => setTimeout(resolve, 20));
        return jsonResponse({ operationId: "op", state: "Running", kind: "release", revision: 1, updatedAt: "2026-09-01T00:00:00Z" });
    }]);
    const slowClient = new YogabaseDeliveryClient({ deliveryHost: "https://delivery.example.test", accessToken: "token", fetch: slow.fetcher });
    await assert.rejects(slowClient.waitForOperation("p", "op", { maxAttempts: 10, intervalMs: 0, timeoutMs: 5 }), DeliveryPollingTimeoutError);
    assert.equal(slow.calls.length, 1);
});

test("problem responses become typed delivery errors", async () => {
    const fake = fakeFetch([jsonResponse({ title: "Conflict", status: 409, code: "stale-preview", correlationId: "c-1" }, 409)]);
    const client = new YogabaseDeliveryClient({ deliveryHost: "https://delivery.example.test", accessToken: "token", fetch: fake.fetcher });
    await assert.rejects(client.getOperation("p", "op"), error => error instanceof YogabaseDeliveryError && error.status === 409 && error.correlationId === "c-1");
});

test("SSE operation events surface opaque IDs and ignore heartbeat comments", async () => {
    const fake = fakeFetch([new Response(": heartbeat\n\nid: 1\nevent: delivery-operation\ndata: {\"operationId\":\"op\",\"state\":\"Succeeded\",\"detail\":\"done\",\"occurredAt\":\"2026-09-01T00:00:00Z\"}\n\n", { status: 200, headers: { "Content-Type": "text/event-stream" } })]);
    const client = new YogabaseDeliveryClient({ deliveryHost: "https://delivery.example.test", accessToken: "token", fetch: fake.fetcher });
    const events = [];
    for await (const event of client.streamOperationEvents("p", { lastEventId: "0" })) events.push(event);
    assert.deepEqual(events, [{ id: "1", event: "delivery-operation", data: { operationId: "op", state: "Succeeded", detail: "done", occurredAt: "2026-09-01T00:00:00Z" } }]);
    assert.equal(fake.calls[0].init.headers.get("Accept"), "text/event-stream");
    assert.equal(fake.calls[0].init.headers.get("Last-Event-ID"), "0");
});

test("getTemplate deserializes a response without hostedServicePolicyDigest as absent", async () => {
    const fake = fakeFetch([jsonResponse({
        id: "t",
        name: "web",
        version: 1,
        state: "Active",
        targetId: "target",
        templateDigest: "sha256:aaaa",
        revision: 1,
    })]);
    const client = new YogabaseDeliveryClient({ deliveryHost: "https://delivery.example.test", accessToken: "token", fetch: fake.fetcher });
    const result = await client.getTemplate("p", "t");
    assert.equal(result.data.hostedServicePolicyDigest, undefined);
});

test("getTemplate surfaces the exact hostedServicePolicyDigest value when present", async () => {
    const fake = fakeFetch([jsonResponse({
        id: "t",
        name: "web",
        version: 1,
        state: "Active",
        targetId: "target",
        templateDigest: "sha256:aaaa",
        hostedServicePolicyDigest: "sha256:bbbb",
        revision: 1,
    })]);
    const client = new YogabaseDeliveryClient({ deliveryHost: "https://delivery.example.test", accessToken: "token", fetch: fake.fetcher });
    const result = await client.getTemplate("p", "t");
    assert.equal(result.data.hostedServicePolicyDigest, "sha256:bbbb");
});

test("SSE operation events reject a successful response with the wrong content type", async () => {
    const fake = fakeFetch([new Response("{}", { status: 200, headers: { "Content-Type": "application/json" } })]);
    const client = new YogabaseDeliveryClient({ deliveryHost: "https://delivery.example.test", accessToken: "token", fetch: fake.fetcher });
    await assert.rejects(async () => {
        for await (const _event of client.streamOperationEvents("p")) { /* validate response before reading */ }
    }, error => error instanceof YogabaseDeliveryError && error.status === 200);
});
