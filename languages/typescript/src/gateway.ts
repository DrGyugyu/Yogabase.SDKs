/**
 * Plumbing the authored application-gateway clients share (durable Jobs and schedules): the gateway origin
 * and project checks, the call's own timeout and abort scope, and the reading of a refusal. It is
 * package-internal: the clients' own modules (`jobs.ts`, `schedules.ts`) are the public surface, and this
 * file is not re-exported from the index.
 */

import type { Uuid } from "./delivery.js";

export const DEFAULT_TIMEOUT_MS = 30_000;
export const MAX_TIMEOUT_MS = 15 * 60_000;
export const CANONICAL_UUID = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;
export const CONTROL_CHARACTER = /[\u0000-\u001f\u007f-\u009f]/;
const LOOPBACK_HOSTS = new Set(["localhost", "127.0.0.1", "[::1]"]);

/**
 * The code a refusal carries when the gateway named none, derived from the HTTP status so a caller can match on
 * it without a status table. A refusal that names its own code (a Jobs `job_not_found`, a schedule's
 * `schedule_payload_too_large`) keeps it.
 */
export const YOGABASE_STATUS_ERROR_CODES = {
    /** 401: no signed-in user, or a token the gateway cannot authenticate. */
    authenticationRequired: "authentication_required",
    /** 403: the token may not call the route, or the caller may not do this to that resource. */
    forbidden: "forbidden",
    /** 404 with no body: no such resource, another user's resource, or an installation that does not serve the route. */
    notFound: "not_found",
    /** 409 with no body: the request conflicts with what the gateway holds. */
    conflict: "conflict",
    /** 412: the ETag sent is stale, or the precondition does not hold for what the gateway holds. */
    preconditionFailed: "precondition_failed",
    /** 413 with no body: a body over its listener's limit. */
    payloadTooLarge: "payload_too_large",
    /** 415: the request body is not JSON. */
    unsupportedMediaType: "unsupported_media_type",
    /** 428: the route needs an `If-Match` or `If-None-Match` precondition and the request sent none. */
    preconditionRequired: "precondition_required",
    /** 429: too many requests. */
    rateLimited: "rate_limited",
    /** Any other refusal, which a caller reads from the status. */
    requestFailed: "request_failed",
} as const;

const CODE_BY_STATUS: Readonly<Record<number, string>> = {
    401: YOGABASE_STATUS_ERROR_CODES.authenticationRequired,
    403: YOGABASE_STATUS_ERROR_CODES.forbidden,
    404: YOGABASE_STATUS_ERROR_CODES.notFound,
    409: YOGABASE_STATUS_ERROR_CODES.conflict,
    412: YOGABASE_STATUS_ERROR_CODES.preconditionFailed,
    // A gateway that predates the named refusal answers a bare 413 for a body over its listener's limit.
    413: YOGABASE_STATUS_ERROR_CODES.payloadTooLarge,
    415: YOGABASE_STATUS_ERROR_CODES.unsupportedMediaType,
    428: YOGABASE_STATUS_ERROR_CODES.preconditionRequired,
    429: YOGABASE_STATUS_ERROR_CODES.rateLimited,
};

export function normalizeHost(value: string, allowInsecureLoopback: boolean): string {
    if (!value) {
        throw new TypeError("gatewayHost is required");
    }
    const url = new URL(value);
    const loopback = LOOPBACK_HOSTS.has(url.hostname);
    const secure = url.protocol === "https:" || (allowInsecureLoopback && loopback && url.protocol === "http:");
    if (!secure || url.username || url.password || url.search || url.hash || url.pathname !== "/") {
        throw new TypeError("gatewayHost must be an origin-only HTTPS URL (plain http only for a loopback host with allowInsecureLoopback)");
    }
    return url.origin;
}

/** True for a canonical hyphenated UUID that is not the all-zero one. */
export function isNonZeroCanonicalUuid(value: unknown): value is string {
    return typeof value === "string" && CANONICAL_UUID.test(value) && !/^[0-]+$/.test(value.replaceAll("-", ""));
}

export function requireUuid(value: string, name: string): Uuid {
    if (!isNonZeroCanonicalUuid(value)) {
        throw new TypeError(`${name} must be a non-empty canonical UUID`);
    }
    return value;
}

export function boundedTimeout(value: number): number {
    if (!Number.isFinite(value) || value < 1 || value > MAX_TIMEOUT_MS) {
        throw new RangeError(`timeoutMs must be between 1 and ${MAX_TIMEOUT_MS}`);
    }
    return value;
}

export function toBase64(bytes: Uint8Array): string {
    let binary = "";
    for (let offset = 0; offset < bytes.length; offset += 0x8000) {
        binary += String.fromCharCode(...bytes.subarray(offset, offset + 0x8000));
    }
    return btoa(binary);
}

/**
 * A call's own timeout and abort scope: the caller's signal and the timeout, joined, with the disposer the call
 * runs once it has read what it needs. `what` names the client in the timeout's message.
 */
export function scopedSignal(parent: AbortSignal | undefined, timeoutMs: number | undefined, what: string) {
    if (timeoutMs === undefined && !parent) {
        return { signal: undefined, dispose: () => undefined };
    }
    const controller = new AbortController();
    const abort = () => controller.abort(parent?.reason);
    if (parent) {
        if (parent.aborted) {
            abort();
        } else {
            parent.addEventListener("abort", abort, { once: true });
        }
    }
    const timer = timeoutMs === undefined ? undefined : setTimeout(() => controller.abort(new Error(`Yogabase ${what} request timed out`)), timeoutMs);
    return {
        signal: controller.signal,
        dispose: () => {
            if (timer !== undefined) {
                clearTimeout(timer);
            }
            parent?.removeEventListener("abort", abort);
        },
    };
}

/** What a refusal says, read once from the response; each client wraps it in its own error class. */
export interface GatewayProblem {
    message: string;
    status: number;
    /** The gateway's stable code, or the one derived from the status when the body names none. */
    code: string;
    problem?: Record<string, unknown>;
    correlationId?: string;
    limitBytes?: number;
    maxBodyBytes?: number;
}

export async function readProblem(response: Response): Promise<GatewayProblem> {
    let problem: Record<string, unknown> | undefined;
    try {
        const parsed = await response.json() as unknown;
        if (parsed !== null && typeof parsed === "object" && !Array.isArray(parsed)) {
            problem = parsed as Record<string, unknown>;
        }
    } catch { /* preserve the status when the body is not a problem document */ }
    const named = (name: string): number | undefined => {
        const value = problem?.[name];
        return typeof value === "number" && Number.isSafeInteger(value) && value > 0 ? value : undefined;
    };
    const detail = typeof problem?.detail === "string" ? problem.detail : undefined;
    const title = typeof problem?.title === "string" ? problem.title : undefined;
    const traceId = typeof problem?.traceId === "string" ? problem.traceId : undefined;
    return {
        message: detail ?? title ?? `Yogabase returned HTTP ${response.status}.`,
        status: response.status,
        code: typeof problem?.code === "string" ? problem.code : CODE_BY_STATUS[response.status] ?? YOGABASE_STATUS_ERROR_CODES.requestFailed,
        problem,
        correlationId: response.headers.get("X-Correlation-ID") ?? traceId,
        limitBytes: named("limitBytes"),
        maxBodyBytes: named("maxBodyBytes"),
    };
}

export type GatewayAccessToken = string | (() => string | Promise<string>);
export type GatewayFetch = (input: RequestInfo | URL, init?: RequestInit) => Promise<Response>;

/** The options every authored application-gateway client takes. */
export interface GatewayClientOptions {
    gatewayHost: string;
    projectId: Uuid;
    accessToken: GatewayAccessToken;
    fetch?: GatewayFetch;
    headers?: Record<string, string>;
    timeoutMs?: number | null;
    allowInsecureLoopback?: boolean;
}

/** What one call adds to a request: its body, what it accepts, and the headers it carries beside the token. */
export interface GatewayRequestInit {
    body?: string;
    accept?: string;
    headers?: Record<string, string>;
}

/**
 * The channel an authored client calls the application gateway through: the validated origin and project, the
 * user's access token, and the one place a request is built and sent, so the Jobs and schedule clients cannot
 * drift apart in how they authenticate, refuse redirects or scope a call's timeout.
 */
export class GatewayChannel {
    public readonly gatewayHost: string;
    public readonly projectId: Uuid;
    private readonly fetcher: GatewayFetch;
    private readonly defaultHeaders: Record<string, string>;
    private readonly token: GatewayAccessToken;
    private readonly timeoutMs: number;

    /**
     * @param what names the client in messages ("jobs", "schedules")
     * @param refuse turns a non-successful response into the client's own error
     */
    public constructor(
        options: GatewayClientOptions,
        private readonly what: string,
        private readonly refuse: (response: Response) => Promise<Error>,
    ) {
        this.gatewayHost = normalizeHost(options.gatewayHost, options.allowInsecureLoopback === true);
        this.projectId = requireUuid(options.projectId, "projectId");
        if (!options.accessToken) {
            throw new TypeError(`accessToken is required for the ${what} API`);
        }
        this.token = options.accessToken;
        this.fetcher = options.fetch ?? globalThis.fetch.bind(globalThis);
        this.defaultHeaders = { ...(options.headers ?? {}) };
        this.timeoutMs = options.timeoutMs === null ? 0 : boundedTimeout(options.timeoutMs ?? DEFAULT_TIMEOUT_MS);
    }

    /**
     * Sends one request and hands back its successful response with the disposer of the call's own timeout and
     * abort scope, which the caller runs once it has read the body. A refusal is read and thrown inside the scope.
     */
    public async dispatch(
        method: string,
        path: string,
        signal: AbortSignal | undefined,
        init: GatewayRequestInit = {},
    ): Promise<{ response: Response; dispose: () => void }> {
        const token = typeof this.token === "function" ? await this.token() : this.token;
        if (!token) {
            throw new TypeError(`The ${this.what} access token provider returned an empty token`);
        }
        const url = new URL(path, `${this.gatewayHost}/`);
        const headers = new Headers(this.defaultHeaders);
        headers.set("Accept", init.accept ?? "application/json, application/problem+json");
        headers.set("Authorization", `Bearer ${token}`);
        for (const [name, value] of Object.entries(init.headers ?? {})) {
            headers.set(name, value);
        }
        if (init.body !== undefined) {
            headers.set("Content-Type", "application/json");
        }
        const scoped = scopedSignal(signal, this.timeoutMs === 0 ? undefined : this.timeoutMs, this.what);
        try {
            const response = await this.fetcher(url, { method, headers, body: init.body, signal: scoped.signal, redirect: "error" });
            if (!response.ok) {
                throw await this.refuse(response);
            }
            return { response, dispose: scoped.dispose };
        } catch (error) {
            scoped.dispose();
            throw error;
        }
    }
}
