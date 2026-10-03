// Checks of the Kiota-generated application client against the mirrored contract's newer routes. The client
// under test is generated, not authored; what is authored here is only the fake transport.
import assert from "node:assert/strict";
import test from "node:test";
import { AnonymousAuthenticationProvider } from "@microsoft/kiota-abstractions";
import { FetchRequestAdapter, HttpClient } from "@microsoft/kiota-http-fetchlibrary";
import { createYogabaseApiClient } from "../dist/index.js";

const PROJECT = "11111111-1111-4111-8111-111111111111";

function generatedClient(respond) {
    const calls = [];
    const fakeFetch = async (url, init) => {
        calls.push({ url: String(url), init });
        return respond(url, init);
    };
    const adapter = new FetchRequestAdapter(new AnonymousAuthenticationProvider(), undefined, undefined, new HttpClient(fakeFetch));
    adapter.baseUrl = "https://gateway.example.test";
    return { client: createYogabaseApiClient(adapter), calls };
}

test("the invoke route's 413 is a payload_too_large problem that names both limits", async () => {
    const { client, calls } = generatedClient(() => new Response(JSON.stringify({
        type: "https://tools.ietf.org/html/rfc9110#section-15.5.14",
        title: "Payload too large.",
        status: 413,
        detail: "The request is larger than this route accepts.",
        code: "payload_too_large",
        limitBytes: 1_048_576,
        maxBodyBytes: 8_392_166,
    }), { status: 413, headers: { "Content-Type": "application/problem+json" } }));

    await assert.rejects(
        client.v1.projects.byProjectId(PROJECT).functions.byFunctionId("resize").versions.byFunctionVersion("1").invoke.post({
            idempotencyKey: "request-1",
            correlationId: "correlation-1",
            deadline: new Date(Date.now() + 20_000),
            mode: 1,
            payloadDigest: `sha256:${"a".repeat(64)}`,
            inlinePayloadBase64: "AQID",
        }),
        error => error.responseStatusCode === 413
            && error.code === "payload_too_large"
            && error.additionalData.limitBytes === 1_048_576
            && error.additionalData.maxBodyBytes === 8_392_166,
    );
    assert.equal(calls[0].url, `https://gateway.example.test/v1/projects/${PROJECT}/functions/resize/versions/1/invoke`);
    assert.equal(calls[0].init.method, "POST");
});

test("the restored Data API is generated: a select posts to the table's route as the signed-in user", async () => {
    const { client, calls } = generatedClient(() => new Response(JSON.stringify({ rows: [{ id: 1 }], affectedRows: 1, hasMore: false }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
    }));

    const result = await client.v1.data.byTable("notes").select.post({ limit: 10 });

    assert.equal(calls[0].url, "https://gateway.example.test/v1/data/notes/select");
    assert.equal(calls[0].init.method, "POST");
    // The Kiota adapter hands fetch the body as bytes.
    assert.equal(JSON.parse(new TextDecoder().decode(calls[0].init.body)).limit, 10);
    assert.equal(result.affectedRows, 1);
    assert.equal(result.hasMore, false);
});

function tooLarge(code, limitBytes, maxBodyBytes) {
    return () => new Response(JSON.stringify({
        type: "https://tools.ietf.org/html/rfc9110#section-15.5.14",
        title: "Payload too large.",
        status: 413,
        detail: "The request is larger than this route accepts.",
        code,
        limitBytes,
        maxBodyBytes,
    }), { status: 413, headers: { "Content-Type": "application/problem+json" } });
}

test("the actor invoke and workflow routes' 413 is a payload_too_large problem that names both limits", async () => {
    const instance = "55555555-5555-4555-8555-555555555555";
    const routes = [
        [3_935_575, "actors/counter/versions/1/instances/customer/invoke", project => project.actors.byFunctionId("counter").versions.byFunctionVersion("1").instances.byInstanceKey("customer").invoke.post({})],
        [3_934_405, "workflows/onboard/versions/1/start", project => project.workflows.byWorkflowId("onboard").versions.byDefinitionVersion("1").start.post({})],
        [3_934_417, `workflows/instances/${instance}/cancel`, project => project.workflows.instances.byWorkflowInstanceId(instance).cancel.post({})],
    ];
    for (const [maxBodyBytes, path, send] of routes) {
        const { client, calls } = generatedClient(tooLarge("payload_too_large", 491_520, maxBodyBytes));
        await assert.rejects(
            send(client.v1.projects.byProjectId(PROJECT)),
            error => error.responseStatusCode === 413
                && error.code === "payload_too_large"
                && error.additionalData.limitBytes === 491_520
                && error.additionalData.maxBodyBytes === maxBodyBytes,
            path,
        );
        assert.equal(calls[0].url, `https://gateway.example.test/v1/projects/${PROJECT}/${path}`);
        assert.equal(calls[0].init.method, "POST");
    }
});

test("the Data API's 413 is a data_request_too_large problem that names both limits", async () => {
    const { client, calls } = generatedClient(tooLarge("data_request_too_large", 1_048_576, 1_114_112));
    await assert.rejects(
        client.v1.data.byTable("notes").insert.post({ rows: [{ id: 1 }] }),
        error => error.responseStatusCode === 413
            && error.code === "data_request_too_large"
            && error.additionalData.limitBytes === 1_048_576
            && error.additionalData.maxBodyBytes === 1_114_112,
    );
    assert.equal(calls[0].url, "https://gateway.example.test/v1/data/notes/insert");
});
