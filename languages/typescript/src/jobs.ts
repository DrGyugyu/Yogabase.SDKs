/**
 * Authored Yogabase durable Jobs facade for the application gateway.
 *
 * The product's client OpenAPI document describes the Jobs routes, so Kiota also generates request builders
 * for them (`client.v1.projects.byProjectId(id).jobs`). This client sits beside those builders, not on top
 * of them: a generated builder cannot hand back a response header, and the result route's integrity check is
 * one (the bytes are verified here against `X-Yogabase-Content-Sha256`), and each call is validated before it is
 * sent and every refusal is named by its code. It is deliberately separate from the generated application
 * client: it takes its own gateway host and access token, as the authored delivery client does.
 * `tests/jobs.test.mjs` pins every shape and holds each request to the one the generated builder builds.
 *
 * A job belongs to the signed-in user who submitted it. Every call is authenticated with that user's access token
 * against `/v1/projects/{projectId}/jobs`, behind the project token's `lambda.invoke` scope.
 */

import type { Uuid } from "./delivery.js";
import {
    CONTROL_CHARACTER,
    GatewayChannel,
    isNonZeroCanonicalUuid,
    readProblem,
    requireUuid,
    toBase64,
} from "./gateway.js";

/**
 * Where a job is in its lifecycle: `Queued`, then `Running`, then it settles as `Succeeded`, `Cancelled`
 * or `DeadLettered`. `Failed` is a checkpoint on the way to a retry (`Queued` again) or to
 * `DeadLettered`; a job never rests in it, so it is not terminal.
 */
export type YogabaseJobStatus = "Queued" | "Running" | "Succeeded" | "Failed" | "Cancelled" | "DeadLettered";

/** The statuses a job settles in: it will not change again. */
export const YOGABASE_JOB_TERMINAL_STATUSES = ["Succeeded", "Cancelled", "DeadLettered"] as const;

/** The bounds of the Jobs routes, which the client checks before it sends a request. */
export const YOGABASE_JOBS_LIMITS = {
    /** The most bytes a job's payload may have, before base64 encoding: the 1 MiB inline limit. */
    maximumPayloadBytes: 1_048_576,
    maximumAttempts: 10,
    maximumJobTypeLength: 128,
    maximumIdempotencyKeyLength: 256,
    /** The most bytes a job's result may have: 16 MiB. */
    maximumResultBytes: 16 * 1_048_576,
} as const;

/**
 * The codes the Jobs routes refuse with, as `YogabaseJobsError.code` carries them. The `job_*` codes up to
 * `job_result_reference_invalid` are the gateway's; the last two are this client's own, for an answer that
 * is not what the route declares.
 */
export const YOGABASE_JOB_ERROR_CODES = {
    /** 422: the job type is not the id of a deployed function. */
    typeNotRegistered: "job_type_not_registered",
    /** 409: the idempotency key already belongs to a different request by this user. */
    idempotencyConflict: "job_idempotency_conflict",
    /** 413: the payload is over 1 MiB before base64 encoding, or the body is over the largest the route reads. */
    payloadTooLarge: "job_payload_too_large",
    /** 400: a field is malformed, or the payload is not base64. */
    requestInvalid: "job_request_invalid",
    /** 404: no such job, which is also another user's job (the two are never told apart). */
    notFound: "job_not_found",
    /** 403: the token may not call the Jobs routes, or names another project. */
    accessDenied: "job_access_denied",
    /** 503: the job store could not be reached. */
    unavailable: "job_unavailable",
    /** 404: the job has no result to give (another user's, unknown, or finished without one). */
    resultNotFound: "job_result_not_found",
    /** 409: the job is the caller's own and has not settled. */
    resultNotReady: "job_result_not_ready",
    /** 502: the stored result is not what the job recorded; no byte of it was sent. */
    resultIntegrityFailed: "job_result_integrity_failed",
    /** 500: the job's stored result reference is not that job's own; no byte of it was sent. */
    resultReferenceInvalid: "job_result_reference_invalid",
    /** This client: the gateway's answer to a Jobs call is not what the route declares. */
    responseInvalid: "job_response_invalid",
    /** This client: the result's bytes do not hash to the digest the gateway sent with them. */
    resultDigestMismatch: "job_result_digest_mismatch",
} as const;

/** What a client asks the submit route to start: one durable run of a deployed stateless function. */
export interface YogabaseJobSubmitRequest {
    /**
     * The id of a deployed stateless function; its highest released version runs the job. 1 to 128 ASCII
     * letters, digits, `.`, `-`, `_` or `:`, with no surrounding whitespace.
     */
    jobType: string;
    /**
     * The caller's key for this request, 1 to 256 characters, trimmed, with no control character. The same
     * request under the same key is the job it already made (retrying a submit is safe); a different request
     * under that key is refused with `job_idempotency_conflict`. The key belongs to the signed-in user. Keep
     * one key across the retries of one submit and use a new one for a new job.
     */
    idempotencyKey: string;
    /** The function's request, at most 1 MiB, or absent for a job that carries none. */
    payload?: Uint8Array;
    /** How many times the job may be attempted, 1 through 10; absent is the gateway's default of 5. */
    maxAttempts?: number;
}

/** One durable job as the gateway reads it back. The payload never travels back. */
export interface YogabaseJob {
    jobId: Uuid;
    jobType: string;
    status: YogabaseJobStatus;
    /** How many attempts have started; 0 while the job has never run. */
    attempt: number;
    maxAttempts: number;
    /** How far the function says it has got, 0 through 100. */
    progressPercent: number;
    /** True once a cancel has been asked for; a running job is stopped by its worker, so this can precede `Cancelled`. */
    cancellationRequested: boolean;
    /** A name for the stored result, not a capability: read the bytes with `result()`. Null while there is none. */
    resultReference: string | null;
    /** Why the job last failed, as the function or the worker named it, or null. */
    failureCode: string | null;
    createdAt: string;
    updatedAt: string;
    completedAt: string | null;
}

/** The stored result of a job that succeeded with one: exactly the bytes the function returned. */
export interface YogabaseJobResult {
    content: Uint8Array;
    /** `sha256:` and sixty-four lowercase hexadecimal digits, as sent in `X-Yogabase-Content-Sha256` and as `content` hashes to. */
    digest: string;
}

export type YogabaseJobsAccessToken = string | (() => string | Promise<string>);
export type YogabaseJobsFetch = (input: RequestInfo | URL, init?: RequestInit) => Promise<Response>;

export interface YogabaseJobsClientOptions {
    /** The installation's application-gateway origin, such as `https://apps.example.test`. */
    gatewayHost: string;
    /** The project's id, a canonical UUID (the bootstrap document's `projectId`). */
    projectId: Uuid;
    /** The signed-in user's access token, or a function that returns the current one. */
    accessToken: YogabaseJobsAccessToken;
    fetch?: YogabaseJobsFetch;
    headers?: Record<string, string>;
    timeoutMs?: number | null;
    /** Accept a plain `http:` gateway origin when it is a loopback host (`localhost`, `127.0.0.1`, `[::1]`). */
    allowInsecureLoopback?: boolean;
}

export interface YogabaseJobsRequestOptions {
    signal?: AbortSignal;
}

/** A non-successful Jobs response, or an answer this client refuses, with the gateway's problem details. */
export class YogabaseJobsError extends Error {
    public readonly status: number;
    /** The gateway's stable code, or one derived from the status when the body names none. */
    public readonly code: string;
    public readonly problem?: Record<string, unknown>;
    public readonly correlationId?: string;
    /** The most bytes the refused payload may have, before base64, when a 413 named it (`limitBytes`). */
    public readonly limitBytes?: number;
    /** The most bytes the whole request body may have, when a 413 named it (`maxBodyBytes`). */
    public readonly maxBodyBytes?: number;

    public constructor(
        message: string,
        status: number,
        code: string,
        problem?: Record<string, unknown>,
        correlationId?: string,
        limitBytes?: number,
        maxBodyBytes?: number,
    ) {
        super(message);
        this.name = "YogabaseJobsError";
        this.status = status;
        this.code = code;
        this.problem = problem;
        this.correlationId = correlationId;
        this.limitBytes = limitBytes;
        this.maxBodyBytes = maxBodyBytes;
    }
}

const JOB_STATUSES: readonly string[] = ["Queued", "Running", "Succeeded", "Failed", "Cancelled", "DeadLettered"];
const RESULT_DIGEST_HEADER = "X-Yogabase-Content-Sha256";
const JOB_TYPE = /^[A-Za-z0-9._:-]{1,128}$/;
const FAILURE_CODE = /^[a-z0-9_]{1,128}$/;
const RESULT_DIGEST = /^sha256:[0-9a-f]{64}$/;

/**
 * Client for the application gateway's durable Jobs routes: submit a run of a deployed stateless function,
 * read it, cancel it and fetch what it returned.
 *
 * No capability gates it: the bootstrap document carries no capability identifier for Jobs, so the client
 * does not pretend to know whether an installation serves them. An installation without them answers a plain
 * 404 with code `not_found`; a missing job is `job_not_found`, a different code.
 */
export class YogabaseJobsClient {
    public readonly gatewayHost: string;
    public readonly projectId: Uuid;
    private readonly channel: GatewayChannel;

    public constructor(options: YogabaseJobsClientOptions) {
        this.channel = new GatewayChannel(options, "jobs", jobsError);
        this.gatewayHost = this.channel.gatewayHost;
        this.projectId = this.channel.projectId;
    }

    /**
     * Starts a job and returns it as the gateway accepted it (202), normally `Queued`. Idempotent per user and
     * `idempotencyKey`: the same request again returns the job it already made, whatever that job has become.
     */
    public async submit(request: YogabaseJobSubmitRequest, options: YogabaseJobsRequestOptions = {}): Promise<YogabaseJob> {
        validateSubmit(request);
        // The member order and the names are the gateway's submission, which refuses any other member. A job with
        // no payload and one with an empty payload are the same job to the gateway.
        const body: Record<string, unknown> = {
            jobType: request.jobType,
            idempotencyKey: request.idempotencyKey,
        };
        if (request.payload !== undefined && request.payload.length > 0) {
            body.payloadBase64 = toBase64(request.payload);
        }
        if (request.maxAttempts !== undefined) {
            body.maxAttempts = request.maxAttempts;
        }
        const { response, dispose } = await this.dispatch("POST", "", options, JSON.stringify(body));
        try {
            return readJob(await readJsonDocument(response));
        } finally {
            dispose();
        }
    }

    /** Reads the caller's own job. Poll this until its status is terminal. */
    public async get(jobId: Uuid, options: YogabaseJobsRequestOptions = {}): Promise<YogabaseJob> {
        const { response, dispose } = await this.dispatch("GET", `/${requireUuid(jobId, "jobId")}`, options);
        try {
            return readJob(await readJsonDocument(response));
        } finally {
            dispose();
        }
    }

    /**
     * Asks for the caller's own job to stop, idempotently, and returns it: a queued job is cancelled at once, a
     * running one is flagged (`cancellationRequested`) and stopped by its worker, and a settled one is returned as it is.
     */
    public async cancel(jobId: Uuid, options: YogabaseJobsRequestOptions = {}): Promise<YogabaseJob> {
        const { response, dispose } = await this.dispatch("POST", `/${requireUuid(jobId, "jobId")}/cancel`, options);
        try {
            return readJob(await readJsonDocument(response));
        } finally {
            dispose();
        }
    }

    /**
     * Reads the result of the caller's own job. The bytes are hashed here and compared with the
     * `X-Yogabase-Content-Sha256` the gateway sent; a result that does not match, or that arrives without exactly
     * one well-formed `sha256:` digest, is refused and never returned.
     */
    public async result(jobId: Uuid, options: YogabaseJobsRequestOptions = {}): Promise<YogabaseJobResult> {
        const { response, dispose } = await this.dispatch("GET", `/${requireUuid(jobId, "jobId")}/result`, options, undefined, "application/octet-stream, application/problem+json");
        // The call's timeout and abort signal stay in force until the whole body has been read and hashed.
        try {
            const declared = response.headers.get(RESULT_DIGEST_HEADER);
            // A Headers object joins a repeated header with ", ", so exactly one well-formed digest is the whole value.
            if (response.status !== 200 || declared === null || !RESULT_DIGEST.test(declared)) {
                await response.body?.cancel().catch(() => undefined);
                throw invalid("Yogabase returned an invalid job result.", response);
            }
            const content = await readBounded(response, YOGABASE_JOBS_LIMITS.maximumResultBytes);
            const computed = `sha256:${toHex(new Uint8Array(await sha256(content)))}`;
            if (computed !== declared) {
                throw new YogabaseJobsError(
                    "The job result does not match the digest Yogabase sent with it, so none of it was returned.",
                    502,
                    YOGABASE_JOB_ERROR_CODES.resultDigestMismatch,
                    undefined,
                    response.headers.get("X-Correlation-ID") ?? undefined,
                );
            }
            return { content, digest: declared };
        } finally {
            dispose();
        }
    }

    /**
     * Sends one request and hands back its successful response with the disposer of the call's own timeout and
     * abort scope, which the caller runs once it has read the body. A refusal is read and thrown inside the scope.
     */
    private dispatch(
        method: string,
        suffix: string,
        options: YogabaseJobsRequestOptions,
        body?: string,
        accept = "application/json, application/problem+json",
    ): Promise<{ response: Response; dispose: () => void }> {
        return this.channel.dispatch(method, `/v1/projects/${this.projectId}/jobs${suffix}`, options.signal, { body, accept });
    }
}

function validateSubmit(request: YogabaseJobSubmitRequest): void {
    if (!request) {
        throw new TypeError("a job submit request is required");
    }
    if (typeof request.jobType !== "string" || !JOB_TYPE.test(request.jobType)) {
        throw new RangeError("a job type is 1 to 128 ASCII letters, digits, '.', '-', '_' or ':', with no surrounding whitespace");
    }
    const key = request.idempotencyKey;
    if (typeof key !== "string" || key.trim() === "" || key.length > YOGABASE_JOBS_LIMITS.maximumIdempotencyKeyLength
        || key !== key.trim() || CONTROL_CHARACTER.test(key)) {
        throw new RangeError("an idempotency key is 1 to 256 characters, trimmed, with no control character");
    }
    if (request.payload !== undefined) {
        if (!(request.payload instanceof Uint8Array)) {
            throw new TypeError("payload must be a Uint8Array");
        }
        if (request.payload.length > YOGABASE_JOBS_LIMITS.maximumPayloadBytes) {
            throw new RangeError("a job payload is limited to one MiB before base64 encoding");
        }
    }
    if (request.maxAttempts !== undefined
        && (!Number.isInteger(request.maxAttempts) || request.maxAttempts < 1 || request.maxAttempts > YOGABASE_JOBS_LIMITS.maximumAttempts)) {
        throw new RangeError("a job allows between 1 and 10 attempts");
    }
}

function invalid(message: string, response: Response): YogabaseJobsError {
    return new YogabaseJobsError(
        message,
        502,
        YOGABASE_JOB_ERROR_CODES.responseInvalid,
        undefined,
        response.headers.get("X-Correlation-ID") ?? undefined,
    );
}

async function readJsonDocument(response: Response): Promise<unknown> {
    let text: string;
    try {
        text = await response.text();
        return JSON.parse(text) as unknown;
    } catch {
        throw invalid("Yogabase returned a job document that is not JSON.", response);
    }
}

// A job the gateway describes wrongly is worse than one it refuses: a caller that polls for a state it was
// never told about waits for ever. Reject the answer instead of returning a guess. A member this client does
// not know is kept out of the result and otherwise ignored, as the generated clients keep unknown members aside.
function readJob(value: unknown): YogabaseJob {
    const record = value as Record<string, unknown> | null;
    const bad = () => new YogabaseJobsError("Yogabase returned an invalid job.", 502, YOGABASE_JOB_ERROR_CODES.responseInvalid);
    if (record === null || typeof record !== "object" || Array.isArray(record)) {
        throw bad();
    }
    const text = (name: string) => (typeof record[name] === "string" ? (record[name] as string) : undefined);
    const whole = (name: string) => (Number.isInteger(record[name]) ? (record[name] as number) : undefined);
    const nullableText = (name: string) => (record[name] === null || record[name] === undefined ? null : text(name));
    const jobId = text("jobId");
    const jobType = text("jobType");
    const status = text("status");
    const attempt = whole("attempt");
    const maxAttempts = whole("maxAttempts");
    const progressPercent = whole("progressPercent");
    const resultReference = nullableText("resultReference");
    const failureCode = nullableText("failureCode");
    const createdAt = text("createdAt");
    const updatedAt = text("updatedAt");
    const completedAt = nullableText("completedAt");
    if (!isNonZeroCanonicalUuid(jobId)
        || jobType === undefined || !JOB_TYPE.test(jobType)
        || status === undefined || !JOB_STATUSES.includes(status)
        || attempt === undefined || attempt < 0
        || maxAttempts === undefined || maxAttempts < 1 || maxAttempts > YOGABASE_JOBS_LIMITS.maximumAttempts
        || progressPercent === undefined || progressPercent < 0 || progressPercent > 100
        || typeof record.cancellationRequested !== "boolean"
        || resultReference === undefined || (resultReference !== null && !isResultReference(resultReference))
        || failureCode === undefined || (failureCode !== null && !FAILURE_CODE.test(failureCode))
        || createdAt === undefined || Number.isNaN(Date.parse(createdAt))
        || updatedAt === undefined || Number.isNaN(Date.parse(updatedAt))
        || completedAt === undefined || (completedAt !== null && Number.isNaN(Date.parse(completedAt)))) {
        throw bad();
    }
    return {
        jobId,
        jobType,
        status: status as YogabaseJobStatus,
        attempt,
        maxAttempts,
        progressPercent,
        cancellationRequested: record.cancellationRequested,
        resultReference,
        failureCode,
        createdAt,
        updatedAt,
        completedAt,
    };
}

// The reference is opaque to a client: bounded, an object:// name, and nothing that could be read as more
// than text. Its grammar belongs to the gateway and is not restated here.
function isResultReference(value: string): boolean {
    return value.length > 9 && value.length <= 2048 && value.startsWith("object://") && !/[\s\u0000-\u001f\u007f-\u009f]/.test(value);
}

async function jobsError(response: Response): Promise<YogabaseJobsError> {
    const refusal = await readProblem(response);
    return new YogabaseJobsError(
        refusal.message,
        refusal.status,
        refusal.code,
        refusal.problem,
        refusal.correlationId,
        refusal.limitBytes,
        refusal.maxBodyBytes,
    );
}

async function readBounded(response: Response, maximumBytes: number): Promise<Uint8Array<ArrayBuffer>> {
    const declared = response.headers.get("Content-Length");
    if (declared !== null && Number(declared) > maximumBytes) {
        await response.body?.cancel().catch(() => undefined);
        throw invalid("Yogabase returned an oversized job result.", response);
    }
    if (!response.body) {
        const whole = new Uint8Array(await response.arrayBuffer());
        if (whole.length > maximumBytes) {
            throw invalid("Yogabase returned an oversized job result.", response);
        }
        return whole;
    }
    const reader = response.body.getReader();
    const chunks: Uint8Array[] = [];
    let total = 0;
    try {
        while (true) {
            const chunk = await reader.read();
            if (chunk.done) {
                break;
            }
            total += chunk.value.length;
            if (total > maximumBytes) {
                throw invalid("Yogabase returned an oversized job result.", response);
            }
            chunks.push(chunk.value);
        }
    } finally {
        await reader.cancel().catch(() => undefined);
    }
    const content = new Uint8Array(total);
    let offset = 0;
    for (const chunk of chunks) {
        content.set(chunk, offset);
        offset += chunk.length;
    }
    return content;
}

async function sha256(content: Uint8Array<ArrayBuffer>): Promise<ArrayBuffer> {
    const subtle = globalThis.crypto?.subtle;
    if (!subtle) {
        throw new Error("The platform has no Web Crypto SHA-256, which a job result is verified with.");
    }
    return subtle.digest("SHA-256", content);
}

function toHex(bytes: Uint8Array): string {
    let text = "";
    for (const byte of bytes) {
        text += byte.toString(16).padStart(2, "0");
    }
    return text;
}
