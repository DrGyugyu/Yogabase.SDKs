/**
 * Headless sign-in for Node, by password or with an external provider: sign in for the identity cookie, then run the OIDC
 * code flow with PKCE using that cookie in place of a browser. The session keeps the tokens and is the Kiota authentication provider.
 * Node only: a browser forbids a script to set `Cookie`, and the gateway sends no CORS headers.
 */

import type { AuthenticationProvider, RequestInformation } from "@microsoft/kiota-abstractions";
import type { Uuid } from "./delivery.js";
import { DEFAULT_TIMEOUT_MS, type GatewayFetch, type GatewayProblem, normalizeHost, readProblem, toBase64 } from "./gateway.js";

export interface YogabaseSessionOptions {
    gatewayHost: string;
    /** The project's `sy_pub_` publishable key; it identifies the project and authenticates nothing. */
    publishableKey: string;
    fetch?: GatewayFetch;
    allowInsecureLoopback?: boolean;
}

/**
 * A refusal of sign-in, with the gateway's `status` and `code`: `invalid_credentials` (401: a wrong password and an
 * unknown, unconfirmed or locked account alike), `mfa_enrollment_required` (403), `authentication_rate_limited` (429),
 * `mfa_required` (202), `interactive_login_required` (401), an OAuth error such as `invalid_grant`, or
 * `authorization_response_invalid` / `authorization_redirect_mismatch` for an answer that is not this call's. An
 * external sign-in adds the gateway's `external_*` codes and `external_start_invalid` (no https address and state).
 */
export class YogabaseAuthError extends Error {
    public readonly status: number;
    public readonly code: string;
    public readonly correlationId?: string;

    public constructor(refusal: Pick<GatewayProblem, "message" | "status" | "code" | "correlationId">) {
        super(refusal.message);
        this.name = "YogabaseAuthError";
        this.status = refusal.status;
        this.code = refusal.code;
        this.correlationId = refusal.correlationId;
    }
}

const refused = (status: number, code: string, message: string) => new YogabaseAuthError({ status, code, message });
const jsonPost = (body: unknown): RequestInit => ({ method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
const base64Url = (bytes: Uint8Array) => toBase64(bytes).replaceAll("+", "-").replaceAll("/", "_").replaceAll("=", "");
const random = (count: number) => base64Url(crypto.getRandomValues(new Uint8Array(count)));
const s256 = async (verifier: string) => base64Url(new Uint8Array(await crypto.subtle.digest("SHA-256", new TextEncoder().encode(verifier))));
type Call = (url: string, init?: RequestInit) => Promise<Response>;

// What a browser came back with, held to the redirect URI it was sent to and the state sent: its code, or a refusal.
function readRedirect(expected: URL, back: URL, state: string): string {
    if (back.protocol !== expected.protocol || back.host !== expected.host || back.pathname !== expected.pathname
        || back.username || back.password || back.href.includes("#")) {
        throw refused(400, "authorization_redirect_mismatch", "The authorization response used another redirect URI.");
    }
    const names = [...back.searchParams.keys()];
    if (new Set(names).size !== names.length || back.searchParams.get("state") !== state) {
        throw refused(400, "authorization_response_invalid", "The authorization response failed state validation.");
    }
    const code = back.searchParams.get("code");
    const error = back.searchParams.get("error");
    if (error || !code) {
        throw refused(400, error || "authorization_response_invalid", "The authorization server returned no code.");
    }
    return code;
}

/**
 * A started external sign-in: open `authorizationUrl` in the system browser, then pass the address the browser is
 * redirected to, with this object, to `completeExternalSignIn`. Its state and verifier are not readable from outside.
 */
export class YogabaseExternalSignIn {
    public constructor(public readonly authorizationUrl: string) {}
}
const started = new WeakMap<YogabaseExternalSignIn, {
    provider: string; redirectUri: URL; state: string; verifier: string; cookies: Map<string, string>; step?: "mfa" | "cookie";
}>();

export class YogabaseSession implements AuthenticationProvider {
    public readonly gatewayHost: string;
    /** The project's id from its client configuration, known once `signInWithPassword` has returned. */
    public projectId?: Uuid;
    private readonly publishableKey: string;
    private readonly fetcher: GatewayFetch;
    private oidc?: { clientId: string; tokenEndpoint: string };
    private tokens?: { accessToken: string; refreshToken?: string; expiresAt: number };
    private refreshing?: Promise<void>;

    public constructor(options: YogabaseSessionOptions) {
        this.gatewayHost = normalizeHost(options.gatewayHost, options.allowInsecureLoopback === true);
        if (!options.publishableKey) {
            throw new TypeError("publishableKey is required");
        }
        this.publishableKey = options.publishableKey;
        this.fetcher = options.fetch ?? globalThis.fetch.bind(globalThis);
    }

    /**
     * Signs the user in and keeps the tokens. An account with MFA throws `mfa_required` until the call is repeated
     * with the authenticator code as `mfaCode`. Every refusal is a `YogabaseAuthError`, and no token is requested
     * unless the authorize step answered with this call's state and a code.
     */
    public async signInWithPassword(email: string, password: string, mfaCode?: string): Promise<void> {
        await this.signIn(new Map(), async call => {
            const login = await call(`${this.gatewayHost}/auth/login`, jsonPost({ email, password }));
            if (login.status === 202 && mfaCode !== undefined) {
                await this.verifyMfa(call, mfaCode);
            } else if (login.status !== 204) {
                throw new YogabaseAuthError(await readProblem(login));
            }
        });
    }

    /**
     * Starts signing a user in with an external provider (`google`, `microsoft`, `apple`: the id the project gave it).
     * `redirectUri` is one the project registered. Only the S256 challenge of a fresh verifier is sent. Opening the
     * returned address and receiving the redirect is the application's: this opens no browser and listens on no port.
     */
    public async startExternalSignIn(provider: string, redirectUri: string): Promise<YogabaseExternalSignIn> {
        if (!/^[a-z0-9._-]{1,64}$/.test(provider)) {
            throw new TypeError("provider must be 1 to 64 lowercase letters, digits, '.', '_' or '-'");
        }
        const registered = new URL(redirectUri);
        const verifier = random(48);
        const cookies = new Map<string, string>();
        const response = await this.withCookies(cookies)(`${this.gatewayHost}/auth/external/${provider}/start`,
            jsonPost({ redirectUri, codeChallenge: await s256(verifier) }));
        if (!response.ok) {
            throw new YogabaseAuthError(await readProblem(response));
        }
        const answer = await response.json().catch(() => undefined);
        if (!URL.canParse(answer?.authorizationUrl) || new URL(answer.authorizationUrl).protocol !== "https:" || typeof answer.state !== "string" || !answer.state) {
            throw refused(502, "external_start_invalid", "Yogabase returned an invalid external sign-in start.");
        }
        const pending = new YogabaseExternalSignIn(answer.authorizationUrl);
        started.set(pending, { provider, redirectUri: registered, state: answer.state, verifier, cookies });
        return pending;
    }

    /**
     * Completes `pending` from the address the browser was redirected to and keeps the tokens. A redirect with another
     * state or address, an `error` or no `code` is a refusal before any request. An account with MFA throws
     * `mfa_required`; call again with the same `pending` and the authenticator code, and the spent code is not re-sent.
     */
    public async completeExternalSignIn(pending: YogabaseExternalSignIn, redirect: string | URL, mfaCode?: string): Promise<void> {
        const held = started.get(pending);
        if (!held) {
            throw new TypeError("pending must be the result of startExternalSignIn");
        }
        const code = readRedirect(held.redirectUri, new URL(redirect), held.state);
        await this.signIn(held.cookies, async call => {
            if (!held.step) {
                const response = await call(`${this.gatewayHost}/auth/external/${held.provider}/callback`,
                    jsonPost({ code, state: held.state, codeVerifier: held.verifier }));
                if (response.status !== 204 && response.status !== 202) {
                    throw new YogabaseAuthError(await readProblem(response));
                }
                held.step = response.status === 204 ? "cookie" : "mfa";
            }
            if (held.step === "mfa") {
                if (mfaCode === undefined) {
                    throw refused(202, "mfa_required", "Complete the second authentication factor: call again with mfaCode.");
                }
                await this.verifyMfa(call, mfaCode);
                held.step = "cookie";
            }
        });
    }

    // One request with the cookies held so far; the cookies its answer sets are held for the next.
    private withCookies(cookies: Map<string, string>): Call {
        return async (url: string, init: RequestInit = {}) => {
            const headers = new Headers(init.headers);
            if (cookies.size > 0) {
                headers.set("Cookie", [...cookies].map(([name, value]) => `${name}=${value}`).join("; "));
            }
            const response = await this.send(url, { ...init, headers });
            for (const line of response.headers.getSetCookie()) {
                const [name, value] = line.split(";")[0].split(/=(.*)/s);   // an empty value is a deletion
                if (value) {
                    cookies.set(name, value);
                } else {
                    cookies.delete(name);
                }
            }
            return response;
        };
    }

    private async verifyMfa(call: Call, code: string): Promise<void> {
        const verify = await call(`${this.gatewayHost}/auth/mfa/verify`, jsonPost({ code }));
        if (verify.status !== 204) {
            throw new YogabaseAuthError(await readProblem(verify));
        }
    }

    // The code flow with the identity cookie standing in for the browser; `identify` makes the gateway set the cookie.
    private async signIn(cookies: Map<string, string>, identify: (call: Call) => Promise<void>): Promise<void> {
        const call = this.withCookies(cookies);
        const read = async (url: string): Promise<Record<string, any>> => {
            const response = await call(url);
            if (!response.ok) {
                throw new YogabaseAuthError(await readProblem(response));
            }
            return await response.json();
        };
        const need = (value: unknown, name: string) => {
            if (typeof value !== "string" || !value) {
                throw refused(502, "oidc_discovery_invalid", `Yogabase returned no ${name}.`);
            }
            return value;
        };
        // The identity cookie goes to the endpoints the gateway names, so they must be the gateway's own.
        const own = (url: unknown, name: string) => {
            if (new URL(need(url, name)).origin !== this.gatewayHost) {
                throw refused(502, "oidc_discovery_invalid", `Yogabase named ${name} on another origin.`);
            }
            return url as string;
        };

        const config = await read(`${this.gatewayHost}/v1/client-config/${encodeURIComponent(this.publishableKey)}`);
        const clientId = need(config.oidcPublicClientId, "oidcPublicClientId");
        const redirect = new URL(need(config.oidcRedirectUris?.[0], "oidcRedirectUris"));
        const scope = need(config.oidcScopes?.join?.(" "), "oidcScopes");
        const discovery = await read(own(config.oidcDiscoveryEndpoint, "oidcDiscoveryEndpoint"));
        const authorizeEndpoint = own(discovery.authorization_endpoint, "authorization_endpoint");
        const tokenEndpoint = own(discovery.token_endpoint, "token_endpoint");

        await identify(call);

        const verifier = random(48);
        const state = random(32);
        const authorize = await call(`${authorizeEndpoint}?${new URLSearchParams({
            response_type: "code", client_id: clientId, redirect_uri: redirect.href, scope,
            code_challenge: await s256(verifier), code_challenge_method: "S256", state,
        })}`);
        if (authorize.status !== 302) {
            throw new YogabaseAuthError(await readProblem(authorize));
        }
        const code = readRedirect(redirect, new URL(authorize.headers.get("Location") ?? "", redirect), state);

        this.oidc = { clientId, tokenEndpoint };
        await this.exchange({ grant_type: "authorization_code", client_id: clientId, code, redirect_uri: redirect.href, code_verifier: verifier });
        this.projectId = config.projectId;
    }

    /** The access token, refreshed first when it expires within a minute. Pass it as `accessToken` to the authored clients. */
    public readonly accessToken = async (): Promise<string> => {
        const tokens = this.tokens;
        if (!tokens) {
            throw refused(401, "authentication_required", "Sign in first: call signInWithPassword or completeExternalSignIn.");
        }
        if (tokens.refreshToken && tokens.expiresAt - Date.now() < 60_000) {
            // The refresh token rotates, so concurrent callers share one refresh.
            this.refreshing ??= this.exchange({ grant_type: "refresh_token", client_id: this.oidc!.clientId, refresh_token: tokens.refreshToken })
                .finally(() => { this.refreshing = undefined; });
            await this.refreshing;
        }
        return this.tokens!.accessToken;
    };

    /** The Kiota authentication provider: the stored token goes to the gateway and nowhere else. */
    public readonly authenticateRequest = async (request: RequestInformation): Promise<void> => {
        if (new URL(request.URL).origin === this.gatewayHost) {
            request.headers.tryAdd("Authorization", `Bearer ${await this.accessToken()}`);
        }
    };

    private send(url: string, init: RequestInit): Promise<Response> {
        return this.fetcher(url, { ...init, redirect: "manual", signal: AbortSignal.timeout(DEFAULT_TIMEOUT_MS) });
    }

    private async exchange(form: Record<string, string>): Promise<void> {
        const response = await this.send(this.oidc!.tokenEndpoint, { method: "POST", body: new URLSearchParams(form) });
        if (!response.ok) {
            const refusal = await readProblem(response);
            const oauth = refusal.problem?.error;
            throw new YogabaseAuthError({ ...refusal, code: typeof oauth === "string" ? oauth : refusal.code });
        }
        const token = await response.json() as Record<string, any>;
        if (typeof token.access_token !== "string" || !token.access_token || String(token.token_type).toLowerCase() !== "bearer" || !(token.expires_in > 0)) {
            throw refused(502, "token_response_invalid", "Yogabase returned an invalid token response.");
        }
        this.tokens = {
            accessToken: token.access_token,
            refreshToken: typeof token.refresh_token === "string" ? token.refresh_token : undefined,
            expiresAt: Date.now() + token.expires_in * 1000,
        };
    }
}
