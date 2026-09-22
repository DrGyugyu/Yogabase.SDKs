/**
 * Authored Yogabase Delivery API facade.
 *
 * This module is deliberately separate from the generated application gateway client. It is
 * maintained from contracts/yogabase-delivery-v1.openapi.json and exposes only that contract's
 * workload-delivery operations.
 */

export type Uuid = string;
export type Sha256Digest = string;
export const YOGABASE_DELIVERY_CONTRACT_SHA256 = "1eca152807ed2c6ab834b6ad7fea5fedb514ce359bd8f02f028635b03b8b6cc4";
export const DELIVERY_TERMINAL_OPERATION_STATES = ["Succeeded", "Failed", "Abandoned", "Cancelled", "Blocked"] as const;

export interface DeliveryEnvironmentPreviewRequest {
    name: string;
    templateId: Uuid;
    templateVersion: number;
}

export interface DeliveryArtifactPreviewRequest {
    sourceLocator: string;
    bundleDigest: Sha256Digest;
    manifestDigest: Sha256Digest;
    sbomDigest: Sha256Digest;
    scanEvidenceDigest: Sha256Digest;
    artifactVersion: number;
    sourceSizeBytes: number;
    sourceMediaType: string;
    manifestUri: string;
    sbomUri: string;
    sbomFormat: string;
    scanUri: string;
    scanner: string;
    scannedAt: string;
    criticalFindings: number;
    highFindings: number;
}

export interface DeliveryReleaseConfiguration {
    statelessFunctions: boolean;
    scheduler: boolean;
    jobs: boolean;
    maxConcurrentExecutions?: number;
    maxCatchUp?: number;
    allowedTimeZones?: string[];
}

export interface DeliveryReleasePreviewRequest {
    artifactAdmissionIds: Uuid[];
    configuration: DeliveryReleaseConfiguration;
    migrationEvidenceDigest: Sha256Digest;
}

export interface DeliveryPromotionPreviewRequest {
    sourceEnvironmentId: Uuid;
    sourceReleaseId: Uuid;
    migrationEvidenceDigest: Sha256Digest;
}

export interface DeliveryRollbackPreviewRequest {
    releaseId: Uuid;
    migrationEvidenceDigest: Sha256Digest;
}

export interface DeliverySubmitRequest {
    previewId: Uuid;
    previewDigest: Sha256Digest;
}

export interface DeliveryPreviewResponse {
    previewId: Uuid;
    previewDigest: Sha256Digest;
    action: string;
    expectedRevision: number;
    approvalRequired: boolean;
    expiresAt: string;
}

export interface DeliveryTemplateResponse {
    id: Uuid;
    name: string;
    version: number;
    state: string;
    targetId: Uuid;
    templateDigest: Sha256Digest;
    hostedServicePolicyDigest?: Sha256Digest | null;
    revision: number;
}

export interface DeliveryEnvironmentResponse {
    id: Uuid;
    name: string;
    slug: string;
    lifecycle: string;
    isProduction: boolean;
    templateId: Uuid;
    templateVersion: number;
    currentReleaseId?: Uuid | null;
    runtimeRevision: number;
    revision: number;
}

export interface DeliveryArtifactResponse {
    id: Uuid;
    status: string;
    artifactVersion: number;
    bundleDigest: Sha256Digest;
    manifestDigest: Sha256Digest;
    sbomDigest: Sha256Digest;
    scanResultDigest: Sha256Digest;
    signerId: string;
    keyId: string;
    revision: number;
}

export interface DeliveryReleaseResponse {
    id: Uuid;
    releaseType: string;
    sourceReleaseId?: Uuid | null;
    desiredRuntimeRevision: number;
    artifactSetDigest: Sha256Digest;
    configurationDigest: Sha256Digest;
    migrationEvidenceDigest: Sha256Digest;
    status: string;
    revision: number;
}

export interface DeliveryOperationEnvelope {
    operationId: Uuid;
    state: string;
    kind: string;
    resourceType?: string | null;
    resourceId?: Uuid | null;
    revision: number;
    updatedAt: string;
}

export interface DeliveryOperationEvent {
    operationId: Uuid;
    state: string;
    detail: string;
    occurredAt: string;
}

export interface DeliveryOperationStreamEvent {
    /** Opaque protected resume token; pass it back explicitly as `lastEventId` when reconnecting. */
    id?: string;
    event?: string;
    data: DeliveryOperationEvent;
}

export interface DeliveryTemplateList {
    items: DeliveryTemplateResponse[];
    nextCursor?: string | null;
}

export interface DeliveryEnvironmentList {
    items: DeliveryEnvironmentResponse[];
    nextCursor?: string | null;
}

export interface DeliveryArtifactList {
    items: DeliveryArtifactResponse[];
    nextCursor?: string | null;
}

export interface DeliveryReleaseList {
    items: DeliveryReleaseResponse[];
    nextCursor?: string | null;
}

export interface DeliveryOperationList {
    items: DeliveryOperationEnvelope[];
    nextCursor?: string | null;
}

export interface ProblemDetails {
    type?: string;
    title: string;
    status: number;
    detail?: string;
    instance?: string;
    code: string;
    correlationId: string;
    [key: string]: unknown;
}

export interface DeliveryResponse<T> {
    data: T;
    status: number;
    etag?: string;
    location?: string;
    retryAfter?: number;
    correlationId?: string;
}

export type DeliveryAccessToken = string | (() => string | Promise<string>);
export type DeliveryFetch = (input: RequestInfo | URL, init?: RequestInit) => Promise<Response>;

export interface DeliveryClientOptions {
    deliveryHost: string;
    accessToken: DeliveryAccessToken;
    fetch?: DeliveryFetch;
    headers?: Record<string, string>;
    timeoutMs?: number | null;
}

export interface DeliveryListOptions {
    limit?: number;
    cursor?: string;
    signal?: AbortSignal;
}

export interface DeliveryRequestOptions {
    signal?: AbortSignal;
}

export interface DeliveryPreviewOptions extends DeliveryRequestOptions {
    ifMatch: string;
}

export interface DeliverySubmitOptions extends DeliveryRequestOptions {
    idempotencyKey: string;
}

export interface DeliveryOperationEventOptions extends DeliveryRequestOptions {
    environmentId?: Uuid;
    operationId?: Uuid;
    lastEventId?: string;
}

export interface WaitForDeliveryOperationOptions extends DeliveryRequestOptions {
    terminalStates?: readonly string[];
    intervalMs?: number;
    timeoutMs?: number;
    maxAttempts?: number;
}

export class YogabaseDeliveryError extends Error {
    public readonly status: number;
    public readonly problem?: ProblemDetails;
    public readonly correlationId?: string;

    public constructor(message: string, status: number, problem?: ProblemDetails, correlationId?: string) {
        super(message);
        this.name = "YogabaseDeliveryError";
        this.status = status;
        this.problem = problem;
        this.correlationId = correlationId;
    }
}

export class DeliveryPollingTimeoutError extends Error {
    public readonly lastOperation?: DeliveryOperationEnvelope;

    public constructor(lastOperation?: DeliveryOperationEnvelope) {
        super("The Yogabase delivery operation did not reach a terminal state within the polling bounds.");
        this.name = "DeliveryPollingTimeoutError";
        this.lastOperation = lastOperation;
    }
}

type RequestOptions = {
    query?: Record<string, string | number | undefined>;
    body?: unknown;
    headers?: Record<string, string>;
    signal?: AbortSignal;
    timeoutMs?: number | null;
};

type InternalResponse<T> = DeliveryResponse<T> & { response: Response };

const DEFAULT_TIMEOUT_MS = 30_000;
const MAX_TIMEOUT_MS = 15 * 60_000;
const MAX_POLL_INTERVAL_MS = 60_000;

/** Client for the workload-only Yogabase Delivery API. */
export class YogabaseDeliveryClient {
    public readonly deliveryHost: string;
    private readonly fetcher: DeliveryFetch;
    private readonly defaultHeaders: Record<string, string>;
    private readonly token: DeliveryAccessToken;
    private readonly timeoutMs: number;

    public constructor(options: DeliveryClientOptions) {
        this.deliveryHost = normalizeHost(options.deliveryHost);
        if (!options.accessToken) {
            throw new TypeError("accessToken is required for the delivery API");
        }
        this.token = options.accessToken;
        this.fetcher = options.fetch ?? globalThis.fetch.bind(globalThis);
        this.defaultHeaders = { ...(options.headers ?? {}) };
        this.timeoutMs = boundedTimeout(options.timeoutMs ?? DEFAULT_TIMEOUT_MS);
    }

    public listTemplates(projectId: Uuid, options: DeliveryListOptions = {}) {
        return this.request<DeliveryTemplateList>("GET", `/projects/${segment(projectId)}/templates`, listQuery(options));
    }

    public getTemplate(projectId: Uuid, templateId: Uuid, options: DeliveryRequestOptions = {}) {
        return this.request<DeliveryTemplateResponse>("GET", `/projects/${segment(projectId)}/templates/${segment(templateId)}`, options);
    }

    public listEnvironments(projectId: Uuid, options: DeliveryListOptions = {}) {
        return this.request<DeliveryEnvironmentList>("GET", `/projects/${segment(projectId)}/environments`, listQuery(options));
    }

    public previewEnvironment(projectId: Uuid, body: DeliveryEnvironmentPreviewRequest, options: DeliveryPreviewOptions) {
        return this.request<DeliveryPreviewResponse>("POST", `/projects/${segment(projectId)}/environments/previews`, {
            body, headers: { "If-Match": checkedEtag(options.ifMatch) }, signal: options.signal,
        });
    }

    public submitEnvironment(projectId: Uuid, body: DeliverySubmitRequest, options: DeliverySubmitOptions) {
        return this.request<DeliveryOperationEnvelope>("POST", `/projects/${segment(projectId)}/environments`, {
            body, headers: { "Idempotency-Key": checkedIdempotencyKey(options.idempotencyKey) }, signal: options.signal,
        });
    }

    public getEnvironment(projectId: Uuid, environmentId: Uuid, options: DeliveryRequestOptions = {}) {
        return this.request<DeliveryEnvironmentResponse>("GET", `/projects/${segment(projectId)}/environments/${segment(environmentId)}`, options);
    }

    public listArtifacts(projectId: Uuid, environmentId: Uuid, options: DeliveryListOptions = {}) {
        return this.request<DeliveryArtifactList>("GET", `/projects/${segment(projectId)}/environments/${segment(environmentId)}/artifacts`, listQuery(options));
    }

    public previewArtifactAdmission(projectId: Uuid, environmentId: Uuid, body: DeliveryArtifactPreviewRequest, options: DeliveryPreviewOptions) {
        return this.request<DeliveryPreviewResponse>("POST", `/projects/${segment(projectId)}/environments/${segment(environmentId)}/artifacts/previews`, {
            body, headers: { "If-Match": checkedEtag(options.ifMatch) }, signal: options.signal,
        });
    }

    public submitArtifactAdmission(projectId: Uuid, environmentId: Uuid, body: DeliverySubmitRequest, options: DeliverySubmitOptions) {
        return this.request<DeliveryOperationEnvelope>("POST", `/projects/${segment(projectId)}/environments/${segment(environmentId)}/artifacts`, {
            body, headers: { "Idempotency-Key": checkedIdempotencyKey(options.idempotencyKey) }, signal: options.signal,
        });
    }

    public getArtifact(projectId: Uuid, environmentId: Uuid, admissionId: Uuid, options: DeliveryRequestOptions = {}) {
        return this.request<DeliveryArtifactResponse>("GET", `/projects/${segment(projectId)}/environments/${segment(environmentId)}/artifacts/${segment(admissionId)}`, options);
    }

    public listReleases(projectId: Uuid, environmentId: Uuid, options: DeliveryListOptions = {}) {
        return this.request<DeliveryReleaseList>("GET", `/projects/${segment(projectId)}/environments/${segment(environmentId)}/releases`, listQuery(options));
    }

    public previewRelease(projectId: Uuid, environmentId: Uuid, body: DeliveryReleasePreviewRequest, options: DeliveryPreviewOptions) {
        return this.request<DeliveryPreviewResponse>("POST", `/projects/${segment(projectId)}/environments/${segment(environmentId)}/releases/previews`, {
            body, headers: { "If-Match": checkedEtag(options.ifMatch) }, signal: options.signal,
        });
    }

    public submitRelease(projectId: Uuid, environmentId: Uuid, body: DeliverySubmitRequest, options: DeliverySubmitOptions) {
        return this.request<DeliveryOperationEnvelope>("POST", `/projects/${segment(projectId)}/environments/${segment(environmentId)}/releases`, {
            body, headers: { "Idempotency-Key": checkedIdempotencyKey(options.idempotencyKey) }, signal: options.signal,
        });
    }

    public getRelease(projectId: Uuid, environmentId: Uuid, releaseId: Uuid, options: DeliveryRequestOptions = {}) {
        return this.request<DeliveryReleaseResponse>("GET", `/projects/${segment(projectId)}/environments/${segment(environmentId)}/releases/${segment(releaseId)}`, options);
    }

    public previewPromotion(projectId: Uuid, environmentId: Uuid, body: DeliveryPromotionPreviewRequest, options: DeliveryPreviewOptions) {
        return this.request<DeliveryPreviewResponse>("POST", `/projects/${segment(projectId)}/environments/${segment(environmentId)}/promotions/previews`, {
            body, headers: { "If-Match": checkedEtag(options.ifMatch) }, signal: options.signal,
        });
    }

    public submitPromotion(projectId: Uuid, environmentId: Uuid, body: DeliverySubmitRequest, options: DeliverySubmitOptions) {
        return this.request<DeliveryOperationEnvelope>("POST", `/projects/${segment(projectId)}/environments/${segment(environmentId)}/promotions`, {
            body, headers: { "Idempotency-Key": checkedIdempotencyKey(options.idempotencyKey) }, signal: options.signal,
        });
    }

    public previewRollback(projectId: Uuid, environmentId: Uuid, body: DeliveryRollbackPreviewRequest, options: DeliveryPreviewOptions) {
        return this.request<DeliveryPreviewResponse>("POST", `/projects/${segment(projectId)}/environments/${segment(environmentId)}/rollbacks/previews`, {
            body, headers: { "If-Match": checkedEtag(options.ifMatch) }, signal: options.signal,
        });
    }

    public submitRollback(projectId: Uuid, environmentId: Uuid, body: DeliverySubmitRequest, options: DeliverySubmitOptions) {
        return this.request<DeliveryOperationEnvelope>("POST", `/projects/${segment(projectId)}/environments/${segment(environmentId)}/rollbacks`, {
            body, headers: { "Idempotency-Key": checkedIdempotencyKey(options.idempotencyKey) }, signal: options.signal,
        });
    }

    public previewDecommission(projectId: Uuid, environmentId: Uuid, options: DeliveryPreviewOptions) {
        return this.request<DeliveryPreviewResponse>("POST", `/projects/${segment(projectId)}/environments/${segment(environmentId)}/decommissions/previews`, {
            headers: { "If-Match": checkedEtag(options.ifMatch) }, signal: options.signal,
        });
    }

    public submitDecommission(projectId: Uuid, environmentId: Uuid, body: DeliverySubmitRequest, options: DeliverySubmitOptions) {
        return this.request<DeliveryOperationEnvelope>("POST", `/projects/${segment(projectId)}/environments/${segment(environmentId)}/decommissions`, {
            body, headers: { "Idempotency-Key": checkedIdempotencyKey(options.idempotencyKey) }, signal: options.signal,
        });
    }

    public listOperations(projectId: Uuid, options: DeliveryListOptions = {}) {
        return this.request<DeliveryOperationList>("GET", `/projects/${segment(projectId)}/operations`, listQuery(options));
    }

    public getOperation(projectId: Uuid, operationId: Uuid, options: DeliveryRequestOptions = {}) {
        return this.request<DeliveryOperationEnvelope>("GET", `/projects/${segment(projectId)}/operations/${segment(operationId)}`, options);
    }

    public previewAbandon(projectId: Uuid, operationId: Uuid, options: DeliveryPreviewOptions) {
        return this.request<DeliveryPreviewResponse>("POST", `/projects/${segment(projectId)}/operations/${segment(operationId)}/abandon/previews`, {
            headers: { "If-Match": checkedEtag(options.ifMatch) }, signal: options.signal,
        });
    }

    public submitAbandon(projectId: Uuid, operationId: Uuid, body: DeliverySubmitRequest, options: DeliverySubmitOptions) {
        return this.request<DeliveryOperationEnvelope>("POST", `/projects/${segment(projectId)}/operations/${segment(operationId)}/abandon`, {
            body, headers: { "Idempotency-Key": checkedIdempotencyKey(options.idempotencyKey) }, signal: options.signal,
        });
    }

    /** Stream the contract's text/event-stream operation events using the platform Web Streams API. */
    public async *streamOperationEvents(projectId: Uuid, options: DeliveryOperationEventOptions = {}): AsyncGenerator<DeliveryOperationStreamEvent> {
        const query: Record<string, string | number | undefined> = {
            environmentId: options.environmentId,
            operationId: options.operationId,
        };
        const headers: Record<string, string> = { Accept: "text/event-stream" };
        if (options.lastEventId !== undefined) {
            if (!options.lastEventId || options.lastEventId.length > 4096) {
                throw new RangeError("lastEventId must contain 1 through 4096 characters");
            }
            headers["Last-Event-ID"] = options.lastEventId;
        }
        const response = await this.rawFetch("GET", `/projects/${segment(projectId)}/operations/events`, {
            query, headers,
            signal: options.signal, timeoutMs: null,
        });
        if (!response.headers.get("Content-Type")?.toLowerCase().startsWith("text/event-stream")) {
            throw new YogabaseDeliveryError("The delivery event endpoint did not return text/event-stream", response.status);
        }
        if (!response.body) {
            throw new Error("The delivery event stream did not provide a readable response body.");
        }
        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        let buffer = "";
        let eventLines: string[] = [];
        try {
            while (true) {
                const chunk = await reader.read();
                if (chunk.done) {
                    buffer += decoder.decode();
                    if (buffer.trim() || eventLines.length > 0) {
                        eventLines.push(buffer);
                        const event = parseSseEvent(eventLines.join("\n"));
                        if (event) yield event;
                    }
                    return;
                }
                buffer += decoder.decode(chunk.value, { stream: true });
                const lines = buffer.split(/\r?\n/);
                buffer = lines.pop() ?? "";
                for (const line of lines) {
                    if (line === "") {
                        const event = parseSseEvent(eventLines.join("\n"));
                        eventLines = [];
                        if (event) yield event;
                    } else {
                        eventLines.push(line);
                    }
                }
            }
        } finally {
            await reader.cancel().catch(() => undefined);
        }
    }

    /** Poll an operation with explicit terminal states and hard attempt/time bounds. */
    public async waitForOperation(projectId: Uuid, operationId: Uuid, options: WaitForDeliveryOperationOptions): Promise<DeliveryOperationEnvelope> {
        const terminalStates = options.terminalStates ?? DELIVERY_TERMINAL_OPERATION_STATES;
        if (terminalStates.length === 0) throw new TypeError("terminalStates must not be empty");
        const intervalMs = boundedPollInterval(options.intervalMs ?? 1_000);
        const maxAttempts = positiveInteger(options.maxAttempts ?? 60, "maxAttempts");
        const timeoutMs = boundedTimeout(options.timeoutMs ?? 300_000);
        const deadline = Date.now() + timeoutMs;
        let last: DeliveryOperationEnvelope | undefined;
        for (let attempt = 0; attempt < maxAttempts; attempt++) {
            if (options.signal?.aborted) throw abortError(options.signal.reason);
            if (Date.now() >= deadline) throw new DeliveryPollingTimeoutError(last);
            const result = await this.rawRequest<DeliveryOperationEnvelope>("GET", `/projects/${segment(projectId)}/operations/${segment(operationId)}`, {
                signal: options.signal, timeoutMs: Math.min(this.timeoutMs, Math.max(1, deadline - Date.now())),
            });
            last = result.data;
            if (Date.now() >= deadline) throw new DeliveryPollingTimeoutError(last);
            if (terminalStates.includes(last.state)) return last;
            const remaining = deadline - Date.now();
            if (attempt + 1 < maxAttempts && remaining > 0) await delay(Math.min(intervalMs, remaining), options.signal);
        }
        throw new DeliveryPollingTimeoutError(last);
    }

    private async request<T>(method: string, path: string, options: RequestOptions = {}): Promise<DeliveryResponse<T>> {
        const result = await this.rawRequest<T>(method, path, options);
        return result;
    }

    private async rawRequest<T = never>(method: string, path: string, options: RequestOptions = {}): Promise<InternalResponse<T>> {
        const response = await this.rawFetch(method, path, options);
        const value = response.status === 204 ? undefined as T : await response.json() as T;
        return {
            data: value,
            status: response.status,
            etag: response.headers.get("ETag") ?? undefined,
            location: response.headers.get("Location") ?? undefined,
            retryAfter: parseRetryAfter(response.headers.get("Retry-After")),
            correlationId: response.headers.get("X-Yogabase-Correlation-Id") ?? undefined,
            response,
        };
    }

    private async rawFetch(method: string, path: string, options: RequestOptions = {}): Promise<Response> {
        const token = typeof this.token === "function" ? await this.token() : this.token;
        if (!token) throw new TypeError("The delivery access token provider returned an empty token");
        const url = new URL(`/api/v1/delivery${path}`, `${this.deliveryHost}/`);
        for (const [key, value] of Object.entries(options.query ?? {})) {
            if (value !== undefined) url.searchParams.set(key, String(value));
        }
        const headers = new Headers(this.defaultHeaders);
        headers.set("Accept", "application/json");
        headers.set("Authorization", `Bearer ${token}`);
        if (options.body !== undefined) {
            headers.set("Content-Type", "application/json");
        }
        for (const [key, value] of Object.entries(options.headers ?? {})) headers.set(key, value);
        const timeoutMs = options.timeoutMs === null ? undefined : options.timeoutMs === undefined ? this.timeoutMs : options.timeoutMs;
        const scoped = scopedSignal(options.signal, timeoutMs === undefined ? undefined : boundedTimeout(timeoutMs));
        try {
            const response = await this.fetcher(url, {
                method,
                headers,
                body: options.body === undefined ? undefined : JSON.stringify(options.body),
                signal: scoped.signal,
                redirect: "error",
            });
            if (!response.ok) throw await deliveryError(response);
            return response;
        } finally {
            scoped.dispose();
        }
    }
}

function normalizeHost(value: string): string {
    if (!value) throw new TypeError("deliveryHost is required");
    const url = new URL(value);
    if (url.protocol !== "https:" || url.username || url.password || url.search || url.hash || url.pathname !== "/") {
        throw new TypeError("deliveryHost must be an origin-only HTTPS URL");
    }
    return url.origin;
}

function segment(value: string): string {
    if (!value) throw new TypeError("path identifiers must not be empty");
    return encodeURIComponent(value);
}

function listQuery(options: DeliveryListOptions): RequestOptions {
    if (options.limit !== undefined && (!Number.isInteger(options.limit) || options.limit < 1 || options.limit > 100)) {
        throw new RangeError("limit must be an integer from 1 through 100");
    }
    if (options.cursor !== undefined && options.cursor.length > 4096) throw new RangeError("cursor exceeds the contract limit");
    return { query: { limit: options.limit, cursor: options.cursor }, signal: options.signal };
}

function checkedEtag(value: string): string {
    if (!/^W\/"[0-9]+"$/.test(value)) throw new TypeError('ifMatch must match W/"<revision>"');
    return value;
}

function checkedIdempotencyKey(value: string): string {
    if (!value || value.length > 128) throw new RangeError("idempotencyKey must contain 1 through 128 characters");
    return value;
}

function positiveInteger(value: number, name: string): number {
    if (!Number.isInteger(value) || value < 1) throw new RangeError(`${name} must be a positive integer`);
    return value;
}

function boundedTimeout(value: number): number {
    if (!Number.isFinite(value) || value < 1 || value > MAX_TIMEOUT_MS) throw new RangeError(`timeoutMs must be between 1 and ${MAX_TIMEOUT_MS}`);
    return value;
}

function boundedPollInterval(value: number): number {
    if (!Number.isFinite(value) || value < 0 || value > MAX_POLL_INTERVAL_MS) throw new RangeError(`intervalMs must be between 0 and ${MAX_POLL_INTERVAL_MS}`);
    return value;
}

function parseRetryAfter(value: string | null): number | undefined {
    if (!value) return undefined;
    const seconds = Number(value);
    return Number.isFinite(seconds) && seconds >= 0 ? seconds : undefined;
}

async function deliveryError(response: Response): Promise<YogabaseDeliveryError> {
    let problem: ProblemDetails | undefined;
    try { problem = await response.json() as ProblemDetails; } catch { /* preserve status when body is not problem JSON */ }
    return new YogabaseDeliveryError(problem?.detail ?? problem?.title ?? `Yogabase delivery request failed with HTTP ${response.status}`, response.status, problem, problem?.correlationId ?? response.headers.get("X-Yogabase-Correlation-Id") ?? undefined);
}

function scopedSignal(parent: AbortSignal | undefined, timeoutMs: number | undefined) {
    if (timeoutMs === undefined && !parent) return { signal: undefined, dispose: () => undefined };
    const controller = new AbortController();
    const abort = () => controller.abort(parent?.reason);
    if (parent) {
        if (parent.aborted) abort();
        else parent.addEventListener("abort", abort, { once: true });
    }
    const timer = timeoutMs === undefined ? undefined : setTimeout(() => controller.abort(new Error("Yogabase delivery request timed out")), timeoutMs);
    return {
        signal: controller.signal,
        dispose: () => {
            if (timer !== undefined) clearTimeout(timer);
            parent?.removeEventListener("abort", abort);
        },
    };
}

function abortError(reason: unknown): Error {
    if (reason instanceof Error) return reason;
    const error = new Error("The operation was cancelled");
    error.name = "AbortError";
    return error;
}

async function delay(milliseconds: number, signal?: AbortSignal): Promise<void> {
    if (signal?.aborted) throw abortError(signal.reason);
    await new Promise<void>((resolve, reject) => {
        const timer = setTimeout(resolve, milliseconds);
        const abort = () => { clearTimeout(timer); reject(abortError(signal?.reason)); };
        signal?.addEventListener("abort", abort, { once: true });
    });
}

function parseSseEvent(block: string): DeliveryOperationStreamEvent | undefined {
    if (!block) return undefined;
    let id: string | undefined;
    let event: string | undefined;
    const data: string[] = [];
    for (const line of block.split(/\r?\n/)) {
        if (line.startsWith(":")) continue;
        if (line.startsWith("id:")) id = sseFieldValue(line.slice(3));
        else if (line.startsWith("event:")) event = sseFieldValue(line.slice(6));
        else if (line.startsWith("data:")) data.push(sseFieldValue(line.slice(5)));
    }
    return data.length > 0 ? { id, event, data: JSON.parse(data.join("\n")) as DeliveryOperationEvent } : undefined;
}

// The SSE grammar removes a single U+0020 after the field colon. Resume ids are opaque, so any
// further leading whitespace belongs to the value and must round-trip unchanged.
function sseFieldValue(value: string): string {
    return value.startsWith(" ") ? value.slice(1) : value;
}
