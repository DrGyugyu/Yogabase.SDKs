/**
 * Authored Yogabase schedule facade for the application gateway: create, change, read, delete and run once the
 * signed-in user's own recurring or one-shot schedules of a deployed stateless function.
 *
 * The product's client OpenAPI document describes the schedule routes, so Kiota also generates request builders
 * for them (`client.v1.projects.byProjectId(id).schedules.byScheduleId(id)`). This client sits beside those
 * builders, not on top of them: every write is conditional on the schedule's ETag, which a generated builder can
 * neither read from a response nor be told to send except by hand, and each call is validated before it is sent
 * and every refusal is named. It is deliberately separate from the generated application client: it takes its
 * own gateway host and access token, as the authored delivery and Jobs clients do. `tests/schedules.test.mjs`
 * pins every shape and holds each request to the one the generated builder builds.
 *
 * A schedule belongs to the user who created it, and only that user reads, changes, deletes or runs it: another
 * user's schedule is answered as one that does not exist (except that updating it is a 403 and creating a
 * schedule under its id is a 412). Every call is authenticated with the user's access token against
 * `/v1/projects/{projectId}/schedules/{scheduleId}`, behind the project token's `lambda.invoke` scope.
 *
 * The write is conditional. `create` sends `If-None-Match: *`; `update`, `delete` and `trigger` send the ETag last
 * read as `If-Match`. A stale ETag, creating an id that is already held and updating one that does not exist are
 * each a 412 that changes nothing, and the scheduler advances a recurring schedule's version as it runs, so an
 * ETag held for long is stale: read the schedule again with `get`.
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

/** What a schedule does about occurrences it missed while the scheduler was unavailable. */
export type YogabaseScheduleMisfirePolicy = "RunImmediately" | "SkipToNextOccurrence" | "RunAllMissed";

/**
 * What a schedule asks for when an occurrence is due while the one before it is still running. The gateway checks
 * and stores the choice, but its scheduler does not yet look for a running occurrence, so every occurrence starts
 * whichever is chosen.
 */
export type YogabaseScheduleConcurrencyPolicy = "Allow" | "Forbid" | "Replace";

/** Whether a manual run enqueued a new invocation or found the one its key had already made. */
export type YogabaseScheduleTriggerOutcome = "Enqueued" | "Replayed";

/** The bounds of the schedule routes, which the client checks before it sends a request. */
export const YOGABASE_SCHEDULES_LIMITS = {
    maximumScheduleIdLength: 128,
    maximumFunctionIdLength: 256,
    maximumFunctionVersionLength: 64,
    maximumCronLength: 128,
    maximumTimeZoneIdLength: 128,
    maximumCatchUp: 128,
    maximumAttempts: 16,
    minimumDeadlineSeconds: 1,
    /** 24 hours. */
    maximumDeadlineSeconds: 86_400,
    maximumIdempotencyKeyLength: 256,
    /** The furthest a manual run's deadline may be from now: one hour. */
    maximumManualDeadlineSeconds: 3_600,
    /** The most bytes a schedule's payload may have, before base64 encoding: the 1 MiB inline limit. */
    maximumPayloadBytes: 1_048_576,
} as const;

/**
 * The codes the schedule routes refuse with, as `YogabaseSchedulesError.code` carries them. Only one refusal of
 * the gateway's names a code (`schedule_payload_too_large`); every other is a status with a fixed title, or no
 * body at all, and its code is derived from the status (`YOGABASE_STATUS_ERROR_CODES`). The last is this
 * client's own, for an answer that is not what the route declares.
 */
export const YOGABASE_SCHEDULE_ERROR_CODES = {
    /** 413: the payload is over 1 MiB before base64 encoding, or the request body is over the largest the route reads. */
    payloadTooLarge: "schedule_payload_too_large",
    /** This client: the gateway's answer to a schedule call is not what the route declares. */
    responseInvalid: "schedule_response_invalid",
} as const;

/**
 * What a client asks the schedule write route to hold: a recurring schedule (`cron` evaluated in `timeZone`) or a
 * one-shot (`oneShotAtUtc`) of one deployed stateless function version. The project and the owner are never
 * fields: the project is the client's own and the owner is the signed-in user. Every member but `payload` and
 * `oneShotAtUtc` is required; the two policies in particular are always sent, because the gateway reads a missing
 * one as the first name, which its contract does not promise.
 */
export interface YogabaseScheduleDefinition {
    /** The schedule's id, chosen by the caller: 1 to 128 characters, trimmed, with no control character. */
    scheduleId: string;
    /** The id of a deployed Stateless Lambda, at most 256 characters. */
    functionId: string;
    /** The exact released version of that function which every occurrence runs, at most 64 characters. It must be admitted for the project. */
    functionVersion: string;
    /**
     * Five fields, minute hour day-of-month month day-of-week, each a number, `*`, a list (`1,15`), a range (`1-5`),
     * a step (`*` + `/5` or `8-18/2`) or, for the month and the day of the week, a name (`JAN`, `MON`); 7 is Sunday. No
     * seconds field and no `@` macros; at most 128 characters. A one-shot still sends a valid expression, which does not
     * decide when it runs.
     */
    cron: string;
    /** The time zone the cron expression is evaluated in, by the id the gateway's system knows it by (`UTC`, `Asia/Seoul`). */
    timeZone: string;
    misfirePolicy: YogabaseScheduleMisfirePolicy;
    concurrencyPolicy: YogabaseScheduleConcurrencyPolicy;
    /** The most missed occurrences `RunAllMissed` runs after an outage, 1 through 128; the other policies ignore it. */
    maxCatchUp: number;
    /** How many times an occurrence's invocation may be attempted, 1 through 16. */
    maxAttempts: number;
    /** How long an occurrence's invocation has to finish, in seconds, 1 through 86400. */
    deadlineSeconds: number;
    /** The request every occurrence's invocation carries, at most 1 MiB, or absent for none. */
    payload?: Uint8Array;
    /**
     * Makes the schedule a one-shot: it runs once at this instant and is then deleted. Sent as a UTC instant. An
     * instant already past runs at the scheduler's next poll. Absent is a recurring schedule; an update keeps a
     * schedule's kind, so it cannot turn a one-shot into a recurring schedule or the reverse (a 409).
     */
    oneShotAtUtc?: Date | string;
}

/** One schedule as the gateway holds it, with the ETag of this version of it. */
export interface YogabaseSchedule {
    scheduleId: string;
    projectId: Uuid;
    /** The owner: the sub of the user who created the schedule. */
    subject: Uuid;
    functionId: string;
    functionVersion: string;
    /**
     * The schedule's version as decimal text: a 64-bit number a JSON number cannot carry exactly. It changes whenever
     * the schedule does, including when the scheduler records an occurrence; the `eTag` carries it with the instance.
     */
    version: string;
    /** Lasts until the schedule is deleted: a schedule deleted and created again has a new one. */
    instanceId: Uuid;
    /** How far the scheduler has got, or null for a one-shot. */
    lastOccurrence: string | null;
    /** The instant a one-shot runs, or null for a recurring schedule. */
    oneShotAtUtc: string | null;
    /**
     * The ETag to send as `If-Match` to change, delete or run exactly this version: the quotes included,
     * `"schedule-v<version>-<instance>"` with the instance as 32 lowercase hexadecimal digits. It is built from
     * `version` and `instanceId`, which the gateway's ETag header carries too (a strong header that disagrees is refused).
     */
    eTag: string;
}

/** What a client asks the trigger route to do: run the schedule once now. */
export interface YogabaseScheduleTriggerRequest {
    /**
     * The caller's key for this run: 1 to 256 characters, with no leading or trailing white space. The same key
     * against the same version of the schedule is the same run, so retrying a trigger is safe.
     */
    idempotencyKey: string;
    /** When the invocation must finish: later than now and at most 3600 seconds from now, else the gateway refuses it. Sent as a UTC instant. */
    deadline: Date | string;
}

/** The invocation a manual run made, or the one the same key already made. */
export interface YogabaseScheduleTriggerResult {
    outcome: YogabaseScheduleTriggerOutcome;
    /** The invocation's id; a repeat of the same key against the same version has the same one. */
    invocationId: Uuid;
}

export type YogabaseSchedulesAccessToken = string | (() => string | Promise<string>);
export type YogabaseSchedulesFetch = (input: RequestInfo | URL, init?: RequestInit) => Promise<Response>;

export interface YogabaseSchedulesClientOptions {
    /** The installation's application-gateway origin, such as `https://apps.example.test`. */
    gatewayHost: string;
    /** The project's id, a canonical UUID (the bootstrap document's `projectId`). */
    projectId: Uuid;
    /** The signed-in user's access token, or a function that returns the current one. */
    accessToken: YogabaseSchedulesAccessToken;
    fetch?: YogabaseSchedulesFetch;
    headers?: Record<string, string>;
    timeoutMs?: number | null;
    /** Accept a plain `http:` gateway origin when it is a loopback host (`localhost`, `127.0.0.1`, `[::1]`). */
    allowInsecureLoopback?: boolean;
}

export interface YogabaseSchedulesRequestOptions {
    signal?: AbortSignal;
}

/** A non-successful schedule response, or an answer this client refuses, with the gateway's problem details. */
export class YogabaseSchedulesError extends Error {
    public readonly status: number;
    /** The gateway's stable code (`schedule_payload_too_large`), or one derived from the status when the body names none. */
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
        this.name = "YogabaseSchedulesError";
        this.status = status;
        this.code = code;
        this.problem = problem;
        this.correlationId = correlationId;
        this.limitBytes = limitBytes;
        this.maxBodyBytes = maxBodyBytes;
    }
}

const MISFIRE_POLICIES: readonly string[] = ["RunImmediately", "SkipToNextOccurrence", "RunAllMissed"];
const CONCURRENCY_POLICIES: readonly string[] = ["Allow", "Forbid", "Replace"];
const ETAG = /^"schedule-v([1-9][0-9]*)-([0-9a-f]{32})"$/;
const VERSION = /^[1-9][0-9]*$/;
const MAXIMUM_INT64 = 9_223_372_036_854_775_807n;

/**
 * Client for the application gateway's schedule routes.
 *
 * No capability gates it: the bootstrap document carries no capability identifier for schedules, so the client does
 * not pretend to know whether an installation serves them. An installation without the Scheduler answers every route
 * with a 404 that has no body, which reads as `not_found`, as does a schedule that does not exist.
 */
export class YogabaseSchedulesClient {
    public readonly gatewayHost: string;
    public readonly projectId: Uuid;
    private readonly channel: GatewayChannel;

    public constructor(options: YogabaseSchedulesClientOptions) {
        this.channel = new GatewayChannel(options, "schedules", schedulesError);
        this.gatewayHost = this.channel.gatewayHost;
        this.projectId = this.channel.projectId;
    }

    /** Creates the schedule as the signed-in user, who then owns it (201), and answers it with its ETag. */
    public async create(definition: YogabaseScheduleDefinition, options: YogabaseSchedulesRequestOptions = {}): Promise<YogabaseSchedule> {
        return this.write(definition, undefined, options);
    }

    /**
     * Changes the schedule the `eTag` names (200) and answers it with its new ETag. The definition names the same
     * `scheduleId` and keeps the schedule's kind: recurring or one-shot.
     */
    public async update(definition: YogabaseScheduleDefinition, eTag: string, options: YogabaseSchedulesRequestOptions = {}): Promise<YogabaseSchedule> {
        return this.write(definition, requireETag(eTag), options);
    }

    /** Reads the caller's own schedule, with the ETag to change, delete or run it. */
    public async get(scheduleId: string, options: YogabaseSchedulesRequestOptions = {}): Promise<YogabaseSchedule> {
        const id = requireScheduleId(scheduleId);
        const { response, dispose } = await this.dispatch("GET", id, "", options);
        try {
            return readSchedule(await readJsonDocument(response), id, this.projectId, response);
        } finally {
            dispose();
        }
    }

    /** Deletes the schedule the `eTag` names (204). */
    public async delete(scheduleId: string, eTag: string, options: YogabaseSchedulesRequestOptions = {}): Promise<void> {
        const id = requireScheduleId(scheduleId);
        const { response, dispose } = await this.dispatch("DELETE", id, "", options, { headers: { "If-Match": requireETag(eTag) } });
        try {
            await response.body?.cancel().catch(() => undefined);
        } finally {
            dispose();
        }
    }

    /**
     * Runs the schedule the `eTag` names once now, outside its cadence: it enqueues one invocation of the schedule's
     * function with the schedule's payload, as the schedule's owner (202). The same idempotency key against the same
     * version answers the same invocation, with outcome `Replayed` where the first said `Enqueued`.
     */
    public async trigger(
        scheduleId: string,
        eTag: string,
        request: YogabaseScheduleTriggerRequest,
        options: YogabaseSchedulesRequestOptions = {},
    ): Promise<YogabaseScheduleTriggerResult> {
        const id = requireScheduleId(scheduleId);
        const token = requireETag(eTag);
        if (!request) {
            throw new TypeError("a schedule trigger request is required");
        }
        const key = request.idempotencyKey;
        if (typeof key !== "string" || key.trim() === "" || key.length > YOGABASE_SCHEDULES_LIMITS.maximumIdempotencyKeyLength || key !== key.trim()) {
            throw new RangeError("an idempotency key is 1 to 256 characters with no leading or trailing white space");
        }
        const body = JSON.stringify({ idempotencyKey: key, deadline: toInstant(request.deadline, "deadline") });
        const { response, dispose } = await this.dispatch("POST", id, "/trigger", options, { body, headers: { "If-Match": token } });
        try {
            return readTrigger(await readJsonDocument(response));
        } finally {
            dispose();
        }
    }

    private async write(
        definition: YogabaseScheduleDefinition,
        eTag: string | undefined,
        options: YogabaseSchedulesRequestOptions,
    ): Promise<YogabaseSchedule> {
        const body = JSON.stringify(definitionBody(definition));
        // Exactly one of the two preconditions, never both and never neither: a create must not overwrite what another
        // request made first, and an update must not overwrite what it never read.
        const headers: Record<string, string> = eTag === undefined ? { "If-None-Match": "*" } : { "If-Match": eTag };
        const { response, dispose } = await this.dispatch("PUT", definition.scheduleId, "", options, { body, headers });
        try {
            return readSchedule(await readJsonDocument(response), definition.scheduleId, this.projectId, response);
        } finally {
            dispose();
        }
    }

    private dispatch(
        method: string,
        scheduleId: string,
        suffix: "" | "/trigger",
        options: YogabaseSchedulesRequestOptions,
        init: { body?: string; headers?: Record<string, string> } = {},
    ): Promise<{ response: Response; dispose: () => void }> {
        // The id is one escaped path segment, here and nowhere else.
        const path = `/v1/projects/${this.projectId}/schedules/${encodeURIComponent(scheduleId)}${suffix}`;
        return this.channel.dispatch(method, path, options.signal, init);
    }
}

function requireScheduleId(value: string): string {
    if (typeof value !== "string" || value.trim() === "" || value.length > YOGABASE_SCHEDULES_LIMITS.maximumScheduleIdLength
        || value !== value.trim() || CONTROL_CHARACTER.test(value)) {
        throw new RangeError("a schedule id is 1 to 128 characters, trimmed, with no control character");
    }
    // The id is a path segment, and a URL parser collapses a segment of dots (percent-encoded or not) into the one
    // before it, so these two would address another route altogether.
    if (value === "." || value === "..") {
        throw new RangeError("a schedule id of only dots cannot be addressed in a URL path; choose another id");
    }
    return value;
}

function requireETag(value: string): string {
    const match = typeof value === "string" ? ETAG.exec(value) : null;
    if (match === null || BigInt(match[1]) > MAXIMUM_INT64 || /^0+$/.test(match[2])) {
        throw new TypeError("an ETag is the quoted token a schedule read or write answered, \"schedule-v<version>-<instance>\", exactly as sent");
    }
    return value;
}

function toInstant(value: Date | string, name: string): string {
    const date = value instanceof Date ? value : typeof value === "string" ? new Date(value) : undefined;
    if (date === undefined || Number.isNaN(date.getTime())) {
        throw new TypeError(`${name} must be a Date or an ISO 8601 instant`);
    }
    return date.toISOString();
}

function boundedText(value: unknown, name: string, maximumLength: number): string {
    if (typeof value !== "string" || value.trim() === "" || value.length > maximumLength) {
        throw new RangeError(`${name} is required and at most ${maximumLength} characters`);
    }
    return value;
}

function boundedInteger(value: unknown, name: string, minimum: number, maximum: number): number {
    if (typeof value !== "number" || !Number.isInteger(value) || value < minimum || value > maximum) {
        throw new RangeError(`${name} is between ${minimum} and ${maximum}`);
    }
    return value;
}

// The member order and the names are the gateway's ScheduledLambdaDefinitionRequest, which refuses any other
// member. A schedule with no payload and one with an empty payload are the same schedule to the gateway.
function definitionBody(definition: YogabaseScheduleDefinition): Record<string, unknown> {
    if (!definition) {
        throw new TypeError("a schedule definition is required");
    }
    const limits = YOGABASE_SCHEDULES_LIMITS;
    requireScheduleId(definition.scheduleId);
    const body: Record<string, unknown> = {
        scheduleId: definition.scheduleId,
        functionId: boundedText(definition.functionId, "functionId", limits.maximumFunctionIdLength),
        functionVersion: boundedText(definition.functionVersion, "functionVersion", limits.maximumFunctionVersionLength),
        cron: boundedText(definition.cron, "cron", limits.maximumCronLength),
        timeZone: boundedText(definition.timeZone, "timeZone", limits.maximumTimeZoneIdLength),
    };
    if (!MISFIRE_POLICIES.includes(definition.misfirePolicy)) {
        throw new RangeError("a misfire policy is required: the gateway reads a missing one as RunImmediately, which its contract does not promise");
    }
    if (!CONCURRENCY_POLICIES.includes(definition.concurrencyPolicy)) {
        throw new RangeError("a concurrency policy is required: the gateway reads a missing one as Allow, which its contract does not promise");
    }
    body.misfirePolicy = definition.misfirePolicy;
    body.concurrencyPolicy = definition.concurrencyPolicy;
    body.maxCatchUp = boundedInteger(definition.maxCatchUp, "maxCatchUp", 1, limits.maximumCatchUp);
    body.maxAttempts = boundedInteger(definition.maxAttempts, "maxAttempts", 1, limits.maximumAttempts);
    body.deadlineSeconds = boundedInteger(definition.deadlineSeconds, "deadlineSeconds", limits.minimumDeadlineSeconds, limits.maximumDeadlineSeconds);
    if (definition.payload !== undefined) {
        if (!(definition.payload instanceof Uint8Array)) {
            throw new TypeError("payload must be a Uint8Array");
        }
        if (definition.payload.length > limits.maximumPayloadBytes) {
            throw new RangeError("a schedule payload is limited to one MiB before base64 encoding");
        }
        if (definition.payload.length > 0) {
            body.payloadBase64 = toBase64(definition.payload);
        }
    }
    if (definition.oneShotAtUtc !== undefined) {
        body.oneShotAtUtc = toInstant(definition.oneShotAtUtc, "oneShotAtUtc");
    }
    return body;
}

function invalid(message: string, response?: Response): YogabaseSchedulesError {
    return new YogabaseSchedulesError(
        message,
        502,
        YOGABASE_SCHEDULE_ERROR_CODES.responseInvalid,
        undefined,
        response?.headers.get("X-Correlation-ID") ?? undefined,
    );
}

async function readJsonDocument(response: Response): Promise<unknown> {
    try {
        return JSON.parse(await response.text()) as unknown;
    } catch {
        throw invalid("Yogabase returned a schedule document that is not JSON.", response);
    }
}

// The schedule the answer describes must be the one asked for, in this client's own project: a gateway that
// describes another is not trusted, and a caller that holds the wrong ETag for the wrong schedule would delete it.
// The ETag header, where the gateway sends a strong one, must say what the body says; a weak or repeated one is
// ignored, because a proxy that compresses a response may weaken its ETag and the body carries the same facts. A
// member this client does not know is kept out of the result and otherwise ignored, as the generated clients keep
// unknown members aside.
function readSchedule(value: unknown, requestedScheduleId: string, projectId: Uuid, response: Response): YogabaseSchedule {
    const record = value as Record<string, unknown> | null;
    const bad = () => invalid("Yogabase returned an invalid schedule.", response);
    if (record === null || typeof record !== "object" || Array.isArray(record)) {
        throw bad();
    }
    const text = (name: string) => (typeof record[name] === "string" ? (record[name] as string) : undefined);
    const nullableInstant = (name: string): string | null | undefined => {
        const member = record[name];
        if (member === null || member === undefined) {
            return null;
        }
        const instant = text(name);
        return instant !== undefined && !Number.isNaN(Date.parse(instant)) ? instant : undefined;
    };
    const scheduleId = text("scheduleId");
    const projectIdText = text("projectId");
    const subject = text("subject");
    const functionId = text("functionId");
    const functionVersion = text("functionVersion");
    const version = text("version");
    const instanceId = text("instanceId");
    const lastOccurrence = nullableInstant("lastOccurrence");
    const oneShotAtUtc = nullableInstant("oneShotAtUtc");
    if (scheduleId === undefined || scheduleId !== requestedScheduleId
        || projectIdText === undefined || projectIdText.toLowerCase() !== projectId.toLowerCase()
        || !isNonZeroCanonicalUuid(subject)
        || functionId === undefined || functionId.trim() === "" || functionId.length > YOGABASE_SCHEDULES_LIMITS.maximumFunctionIdLength
        || functionVersion === undefined || functionVersion.trim() === "" || functionVersion.length > YOGABASE_SCHEDULES_LIMITS.maximumFunctionVersionLength
        || version === undefined || !VERSION.test(version) || BigInt(version) > MAXIMUM_INT64
        || !isNonZeroCanonicalUuid(instanceId)
        || lastOccurrence === undefined
        || oneShotAtUtc === undefined) {
        throw bad();
    }
    const eTag = `"schedule-v${version}-${instanceId.replaceAll("-", "").toLowerCase()}"`;
    const header = response.headers.get("ETag");
    if (header !== null && !header.startsWith("W/") && !header.includes(",") && header !== eTag) {
        throw invalid("Yogabase returned a schedule whose ETag header does not match the schedule.", response);
    }
    return {
        scheduleId,
        projectId: projectIdText,
        subject,
        functionId,
        functionVersion,
        version,
        instanceId,
        lastOccurrence,
        oneShotAtUtc,
        eTag,
    };
}

function readTrigger(value: unknown): YogabaseScheduleTriggerResult {
    const record = value as Record<string, unknown> | null;
    if (record === null || typeof record !== "object" || Array.isArray(record)
        || (record.outcome !== 0 && record.outcome !== 1)
        || !isNonZeroCanonicalUuid(record.invocationId)) {
        throw invalid("Yogabase returned an invalid schedule run.");
    }
    return { outcome: record.outcome === 0 ? "Enqueued" : "Replayed", invocationId: record.invocationId };
}

async function schedulesError(response: Response): Promise<YogabaseSchedulesError> {
    const refusal = await readProblem(response);
    return new YogabaseSchedulesError(
        refusal.message,
        refusal.status,
        refusal.code,
        refusal.problem,
        refusal.correlationId,
        refusal.limitBytes,
        refusal.maxBodyBytes,
    );
}
