// Signs in to a real installation and uses what the token opens. Skipped unless these are set:
// YOGABASE_SMOKE_GATEWAY (the gateway origin), YOGABASE_SMOKE_PUBLISHABLE_KEY, YOGABASE_SMOKE_EMAIL and
// YOGABASE_SMOKE_PASSWORD (an existing, confirmed user without MFA), YOGABASE_SMOKE_FUNCTION_ID and
// YOGABASE_SMOKE_FUNCTION_VERSION (a deployed stateless function that accepts `{}`).
import assert from "node:assert/strict";
import { createHash, randomUUID } from "node:crypto";
import test from "node:test";
import { DefaultRequestAdapter } from "@microsoft/kiota-bundle";
import { createYogabaseApiClient, YogabaseSession } from "../dist/index.js";

const env = name => process.env[`YOGABASE_SMOKE_${name}`];
const missing = ["GATEWAY", "PUBLISHABLE_KEY", "EMAIL", "PASSWORD", "FUNCTION_ID", "FUNCTION_VERSION"].filter(name => !env(name));
const digest = bytes => `sha256:${createHash("sha256").update(bytes).digest("hex")}`;

test("sign in, invoke a function, put and get an object", { skip: missing.length > 0 && `Set the YOGABASE_SMOKE_* variables to run against an installation (missing: ${missing.join(", ")}).` }, async () => {
    const session = new YogabaseSession({ gatewayHost: env("GATEWAY"), publishableKey: env("PUBLISHABLE_KEY"), allowInsecureLoopback: true });
    await session.signInWithPassword(env("EMAIL"), env("PASSWORD"));
    const adapter = new DefaultRequestAdapter(session);
    adapter.baseUrl = session.gatewayHost;
    const api = createYogabaseApiClient(adapter);

    const payload = new TextEncoder().encode("{}");
    const invoked = await api.v1.projects.byProjectId(session.projectId).functions.byFunctionId(env("FUNCTION_ID")).versions.byFunctionVersion(env("FUNCTION_VERSION")).invoke.post({
        idempotencyKey: randomUUID(),
        correlationId: "live-smoke",
        deadline: new Date(Date.now() + 20_000),
        mode: 0, // synchronous
        payloadDigest: digest(payload),
        inlinePayloadBase64: Buffer.from(payload).toString("base64"),
    });
    assert.equal(invoked.outcome, 4); // Completed

    const bytes = new TextEncoder().encode(`live smoke ${randomUUID()}`);
    const object = api.v1.storage.objects.byKey(`smoke-${randomUUID()}.txt`);
    await object.put(bytes.slice().buffer, { headers: { "X-Yogabase-Content-Sha256": digest(bytes), "Idempotency-Key": randomUUID() } });
    assert.deepEqual(new Uint8Array(await object.get()), bytes);
});
