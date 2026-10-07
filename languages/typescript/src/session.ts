/**
 * Headless password sign-in for Node: log in for the identity cookie, then run the OIDC code flow with PKCE using
 * that cookie in place of a browser. The session keeps the tokens and is the Kiota authentication provider.
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
 * `authorization_response_invalid` / `authorization_redirect_mismatch` for an answer that is not this call's.
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
        const cookies = new Map<string, string>();
        const call = async (url: string, init: RequestInit = {}) => {
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

        const login = await call(`${this.gatewayHost}/auth/login`, jsonPost({ email, password }));
        if (login.status === 202 && mfaCode !== undefined) {
            const verify = await call(`${this.gatewayHost}/auth/mfa/verify`, jsonPost({ code: mfaCode }));
            if (verify.status !== 204) {
                throw new YogabaseAuthError(await readProblem(verify));
            }
        } else if (login.status !== 204) {
            throw new YogabaseAuthError(await readProblem(login));
        }

        const verifier = random(48);
        const state = random(32);
        const challenge = base64Url(new Uint8Array(await crypto.subtle.digest("SHA-256", new TextEncoder().encode(verifier))));
        const authorize = await call(`${authorizeEndpoint}?${new URLSearchParams({
            response_type: "code", client_id: clientId, redirect_uri: redirect.href, scope,
            code_challenge: challenge, code_challenge_method: "S256", state,
        })}`);
        if (authorize.status !== 302) {
            throw new YogabaseAuthError(await readProblem(authorize));
        }
        const back = new URL(authorize.headers.get("Location") ?? "", redirect);
        if (back.protocol !== redirect.protocol || back.host !== redirect.host || back.pathname !== redirect.pathname) {
            throw refused(400, "authorization_redirect_mismatch", "The authorization response used another redirect URI.");
        }
        if (back.searchParams.get("state") !== state) {
            throw refused(400, "authorization_response_invalid", "The authorization response failed state validation.");
        }
        const code = back.searchParams.get("code");
        const error = back.searchParams.get("error");
        if (error || !code) {
            throw refused(400, error ?? "authorization_response_invalid", "The authorization server returned no code.");
        }

        this.oidc = { clientId, tokenEndpoint };
        await this.exchange({ grant_type: "authorization_code", client_id: clientId, code, redirect_uri: redirect.href, code_verifier: verifier });
        this.projectId = config.projectId;
    }

    /** The access token, refreshed first when it expires within a minute. Pass it as `accessToken` to the authored clients. */
    public readonly accessToken = async (): Promise<string> => {
        const tokens = this.tokens;
        if (!tokens) {
            throw refused(401, "authentication_required", "Sign in first: call signInWithPassword.");
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
