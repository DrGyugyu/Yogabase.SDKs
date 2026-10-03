// Test support, not a test: the request a Kiota-generated builder builds, captured on a fake transport, so an
// authored client can be held to it. The generated client is built over the same fetch adapter a consumer would
// use, with a fetch that records the call and answers with what the test gives it.
import { AnonymousAuthenticationProvider } from "@microsoft/kiota-abstractions";
import { FetchRequestAdapter, HttpClient } from "@microsoft/kiota-http-fetchlibrary";
import { createYogabaseApiClient } from "../dist/index.js";

const BASE = "https://apps.example.test";

/**
 * Runs `call(client)` on a generated client and returns the one request it sent: its URL, method, headers
 * (names lowercased) and body (decoded as JSON when it is, as text otherwise, null when there is none).
 */
export async function generatedRequest(call, answer = () => new Response("{}", { status: 200, headers: { "Content-Type": "application/json" } })) {
    const calls = [];
    const fakeFetch = async (url, init) => {
        calls.push({ url: String(url), init });
        return answer(url, init);
    };
    const adapter = new FetchRequestAdapter(new AnonymousAuthenticationProvider(), undefined, undefined, new HttpClient(fakeFetch));
    adapter.baseUrl = BASE;
    try {
        await call(createYogabaseApiClient(adapter));
    } catch {
        // Only the request is under test: the canned answer is rarely one the generated model accepts.
    }
    if (calls.length !== 1) {
        throw new Error(`the generated builder sent ${calls.length} requests, not one`);
    }
    const { url, init } = calls[0];
    const headers = {};
    const source = init.headers;
    if (source instanceof Headers) {
        for (const [name, value] of source) headers[name.toLowerCase()] = value;
    } else if (source) {
        for (const [name, value] of Object.entries(source)) headers[name.toLowerCase()] = String(value);
    }
    let body = null;
    if (init.body !== undefined && init.body !== null) {
        const text = typeof init.body === "string" ? init.body : new TextDecoder().decode(init.body);
        try {
            body = JSON.parse(text);
        } catch {
            body = text;
        }
    }
    return { url, method: init.method, headers, body };
}

/** The request an authored client sent, taken from its fake fetch call, in the same shape as `generatedRequest`. */
export function authoredRequest(call) {
    const headers = {};
    for (const [name, value] of call.init.headers) headers[name.toLowerCase()] = value;
    let body = null;
    if (call.init.body !== undefined && call.init.body !== null) {
        body = JSON.parse(call.init.body);
    }
    return { url: call.url, method: call.init.method, headers, body };
}
