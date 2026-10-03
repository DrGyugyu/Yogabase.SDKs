"""Authored async facade for the Yogabase application gateway's schedule routes.

Create, change, read, delete and run once the signed-in user's own recurring or one-shot schedules of a deployed
stateless function.

The product's client OpenAPI document describes the schedule routes, so Kiota also generates request builders for
them (``client.v1.projects.by_project_id(id).schedules.by_schedule_id(id)``). This client sits beside those
builders, not on top of them: every write is conditional on the schedule's ETag, which a generated builder can
neither read from a response nor be told to send except by hand, and each call is validated before it is sent and
every refusal is named. It is deliberately separate from the generated application client: like
:class:`AsyncDeliveryClient` and :class:`AsyncJobsClient` it takes its own gateway host and access token.
``tests/test_schedules.py`` pins every shape and holds each request to the one the generated builder builds.

A schedule belongs to the user who created it, and only that user reads, changes, deletes or runs it: another
user's schedule is answered as one that does not exist (except that updating it is a 403 and creating a schedule
under its id is a 412). Every call is authenticated with the user's access token against
``/v1/projects/{projectId}/schedules/{scheduleId}``, behind the project token's ``lambda.invoke`` scope.

The write is conditional. ``create`` sends ``If-None-Match: *``; ``update``, ``delete`` and ``trigger`` send the
ETag last read as ``If-Match``. A stale ETag, creating an id that is already held and updating one that does not
exist are each a 412 that changes nothing, and the scheduler advances a recurring schedule's version as it runs,
so an ETag held for long is stale: read the schedule again with ``get``.
"""

from __future__ import annotations

import base64
import json
import re
from collections.abc import Mapping
from dataclasses import dataclass
from datetime import datetime, timezone
from typing import Any
from urllib.parse import quote

import httpx

from ._gateway import (
    CONTROL_CHARACTER,
    TIMESTAMP,
    GatewayAccessToken,
    GatewayChannel,
    is_non_zero_uuid,
    read_problem,
)

#: What a schedule does about occurrences it missed while the scheduler was unavailable.
YOGABASE_SCHEDULE_MISFIRE_POLICIES = ("RunImmediately", "SkipToNextOccurrence", "RunAllMissed")
#: What a schedule asks for when an occurrence is due while the one before it is still running. The gateway
#: checks and stores the choice, but its scheduler does not yet look for a running occurrence, so every
#: occurrence starts whichever is chosen.
YOGABASE_SCHEDULE_CONCURRENCY_POLICIES = ("Allow", "Forbid", "Replace")
#: Whether a manual run enqueued a new invocation (``Enqueued``) or found the one its key had already made (``Replayed``).
YOGABASE_SCHEDULE_TRIGGER_OUTCOMES = ("Enqueued", "Replayed")

#: The bounds of the schedule routes, which the client checks before it sends a request.
YOGABASE_SCHEDULES_MAXIMUM_SCHEDULE_ID_LENGTH = 128
YOGABASE_SCHEDULES_MAXIMUM_FUNCTION_ID_LENGTH = 256
YOGABASE_SCHEDULES_MAXIMUM_FUNCTION_VERSION_LENGTH = 64
YOGABASE_SCHEDULES_MAXIMUM_CRON_LENGTH = 128
YOGABASE_SCHEDULES_MAXIMUM_TIME_ZONE_ID_LENGTH = 128
YOGABASE_SCHEDULES_MAXIMUM_CATCH_UP = 128
YOGABASE_SCHEDULES_MAXIMUM_ATTEMPTS = 16
YOGABASE_SCHEDULES_MINIMUM_DEADLINE_SECONDS = 1
#: 24 hours.
YOGABASE_SCHEDULES_MAXIMUM_DEADLINE_SECONDS = 86_400
YOGABASE_SCHEDULES_MAXIMUM_IDEMPOTENCY_KEY_LENGTH = 256
#: The furthest a manual run's deadline may be from now: one hour.
YOGABASE_SCHEDULES_MAXIMUM_MANUAL_DEADLINE_SECONDS = 3_600
#: The most bytes a schedule's payload may have, before base64 encoding: the 1 MiB inline limit.
YOGABASE_SCHEDULES_MAXIMUM_PAYLOAD_BYTES = 1_048_576

#: The codes the schedule routes refuse with, as ``YogabaseSchedulesError.code`` carries them. Only one refusal of
#: the gateway's names a code (``SCHEDULE_PAYLOAD_TOO_LARGE``); every other is a status with a fixed title, or no
#: body at all, and its code is derived from the status (the ``STATUS_*`` constants). The last is this client's
#: own, for an answer that is not what the route declares.
#:
#: 413: the payload is over 1 MiB before base64 encoding, or the request body is over the largest the route reads.
SCHEDULE_PAYLOAD_TOO_LARGE = "schedule_payload_too_large"
SCHEDULE_RESPONSE_INVALID = "schedule_response_invalid"

_ETAG = re.compile(r'"schedule-v([1-9][0-9]*)-([0-9a-f]{32})"')
_VERSION = re.compile(r"[1-9][0-9]*")
_MAXIMUM_INT64 = 9_223_372_036_854_775_807

SchedulesAccessToken = GatewayAccessToken


@dataclass(frozen=True)
class YogabaseScheduleDefinition:
    """What a client asks the schedule write route to hold.

    A recurring schedule (``cron`` evaluated in ``time_zone``) or a one-shot (``one_shot_at_utc``) of one
    deployed stateless function version. The project and the owner are never fields: the project is the
    client's own and the owner is the signed-in user. Every member but ``payload`` and ``one_shot_at_utc`` is
    required; the two policies in particular are always sent, because the gateway reads a missing one as the
    first name, which its contract does not promise.
    """

    #: The schedule's id, chosen by the caller: 1 to 128 characters, trimmed, with no control character.
    schedule_id: str
    #: The id of a deployed Stateless Lambda, at most 256 characters.
    function_id: str
    #: The exact released version of that function which every occurrence runs, at most 64 characters. It must
    #: be admitted for the project.
    function_version: str
    #: Five fields, minute hour day-of-month month day-of-week, each a number, ``*``, a list (``1,15``), a range
    #: (``1-5``), a step (``*/5`` or ``8-18/2``) or, for the month and the day of the week, a name (``JAN``,
    #: ``MON``); 7 is Sunday. No seconds field and no ``@`` macros; at most 128 characters. A one-shot still
    #: sends a valid expression, which does not decide when it runs.
    cron: str
    #: The time zone the cron expression is evaluated in, by the id the gateway's system knows it by (``UTC``,
    #: ``Asia/Seoul``).
    time_zone: str
    #: One of :data:`YOGABASE_SCHEDULE_MISFIRE_POLICIES`.
    misfire_policy: str
    #: One of :data:`YOGABASE_SCHEDULE_CONCURRENCY_POLICIES`.
    concurrency_policy: str
    #: The most missed occurrences ``RunAllMissed`` runs after an outage, 1 through 128; the other policies ignore it.
    max_catch_up: int
    #: How many times an occurrence's invocation may be attempted, 1 through 16.
    max_attempts: int
    #: How long an occurrence's invocation has to finish, in seconds, 1 through 86400.
    deadline_seconds: int
    #: The request every occurrence's invocation carries, at most 1 MiB, or None for none.
    payload: bytes | None = None
    #: Makes the schedule a one-shot: it runs once at this timezone-aware instant, which is sent as UTC, and is
    #: then deleted. An instant already past runs at the scheduler's next poll. None is a recurring schedule; an
    #: update keeps a schedule's kind, so it cannot turn a one-shot into a recurring schedule or the reverse (a 409).
    one_shot_at_utc: datetime | None = None


@dataclass(frozen=True)
class YogabaseSchedule:
    """One schedule as the gateway holds it, with the ETag of this version of it."""

    schedule_id: str
    project_id: str
    #: The owner: the sub of the user who created the schedule.
    subject: str
    function_id: str
    function_version: str
    #: The schedule's version as decimal text: a 64-bit number a JSON number cannot carry exactly. It changes
    #: whenever the schedule does, including when the scheduler records an occurrence; the ``etag`` carries it
    #: with the instance.
    version: str
    #: Lasts until the schedule is deleted: a schedule deleted and created again has a new one.
    instance_id: str
    #: How far the scheduler has got, or None for a one-shot.
    last_occurrence: str | None
    #: The instant a one-shot runs, or None for a recurring schedule.
    one_shot_at_utc: str | None
    #: The ETag to send as ``If-Match`` to change, delete or run exactly this version: the quotes included,
    #: ``"schedule-v<version>-<instance>"`` with the instance as 32 lowercase hexadecimal digits. It is built
    #: from ``version`` and ``instance_id``, which the gateway's ETag header carries too (a strong header that
    #: disagrees is refused).
    etag: str

    @property
    def is_one_shot(self) -> bool:
        """True for a one-shot, which runs once and is then deleted."""
        return self.one_shot_at_utc is not None


@dataclass(frozen=True)
class YogabaseScheduleTriggerResult:
    """The invocation a manual run made, or the one the same key already made."""

    #: ``Enqueued`` when this request enqueued the invocation, ``Replayed`` when it found the one its key had made.
    outcome: str
    #: The invocation's id; a repeat of the same key against the same version has the same one.
    invocation_id: str


@dataclass(frozen=True)
class SchedulesClientOptions:
    #: The installation's application-gateway origin, such as ``https://apps.example.test``.
    gateway_host: str
    #: The project's id, a canonical UUID (the bootstrap document's ``projectId``).
    project_id: str
    #: The signed-in user's access token, or a (possibly async) function that returns the current one.
    access_token: SchedulesAccessToken
    timeout: float = 30.0
    headers: Mapping[str, str] | None = None
    #: Accept a plain ``http`` gateway origin when it is a loopback host.
    allow_insecure_loopback: bool = False


class YogabaseSchedulesError(Exception):
    """A non-successful schedule response, or an answer this client refuses, with the gateway's problem details."""

    def __init__(
        self,
        message: str,
        status: int,
        code: str,
        problem: dict[str, Any] | None = None,
        correlation_id: str | None = None,
        limit_bytes: int | None = None,
        max_body_bytes: int | None = None,
    ) -> None:
        super().__init__(message)
        self.status = status
        #: The gateway's stable code (``schedule_payload_too_large``), or one derived from the status when the
        #: body names none.
        self.code = code
        self.problem = problem
        self.correlation_id = correlation_id
        #: The most bytes the refused payload may have, before base64, when a 413 named it (``limitBytes``).
        self.limit_bytes = limit_bytes
        #: The most bytes the whole request body may have, when a 413 named it (``maxBodyBytes``).
        self.max_body_bytes = max_body_bytes


class AsyncSchedulesClient:
    """Async client for the application gateway's schedule routes.

    No capability gates it: the bootstrap document carries no capability identifier for schedules, so the client
    does not pretend to know whether an installation serves them. An installation without the Scheduler answers
    every route with a 404 that has no body, which reads as ``not_found``, as does a schedule that does not exist.
    """

    def __init__(
        self,
        options: SchedulesClientOptions | None = None,
        *,
        gateway_host: str | None = None,
        project_id: str | None = None,
        access_token: SchedulesAccessToken | None = None,
        timeout: float = 30.0,
        headers: Mapping[str, str] | None = None,
        allow_insecure_loopback: bool = False,
        http_client: httpx.AsyncClient | None = None,
    ) -> None:
        if options is not None and any(value is not None for value in (gateway_host, project_id, access_token, headers)):
            raise TypeError("pass either options or keyword client settings, not both")
        if options is None:
            if gateway_host is None or project_id is None or access_token is None:
                raise TypeError("gateway_host, project_id and access_token are required")
            options = SchedulesClientOptions(gateway_host, project_id, access_token, timeout, headers, allow_insecure_loopback)
        self._channel = GatewayChannel(
            "schedules",
            gateway_host=options.gateway_host,
            project_id=options.project_id,
            access_token=options.access_token,
            timeout=options.timeout,
            headers=options.headers,
            allow_insecure_loopback=options.allow_insecure_loopback,
            http_client=http_client,
            refuse=_schedules_error,
        )
        self.gateway_host = self._channel.gateway_host
        self.project_id = self._channel.project_id

    async def __aenter__(self) -> "AsyncSchedulesClient":
        return self

    async def __aexit__(self, *_: object) -> None:
        await self.aclose()

    async def aclose(self) -> None:
        await self._channel.aclose()

    async def create(self, definition: YogabaseScheduleDefinition) -> YogabaseSchedule:
        """Creates the schedule as the signed-in user, who then owns it (201), and answers it with its ETag."""
        return await self._write(definition, None)

    async def update(self, definition: YogabaseScheduleDefinition, etag: str) -> YogabaseSchedule:
        """Changes the schedule the ``etag`` names (200) and answers it with its new ETag.

        The definition names the same ``schedule_id`` and keeps the schedule's kind: recurring or one-shot.
        """
        return await self._write(definition, _require_etag(etag))

    async def get(self, schedule_id: str) -> YogabaseSchedule:
        """Reads the caller's own schedule, with the ETag to change, delete or run it."""
        identifier = _require_schedule_id(schedule_id)
        response = await self._channel.send("GET", self._path(identifier))
        return _read_schedule(response, identifier, self.project_id)

    async def delete(self, schedule_id: str, etag: str) -> None:
        """Deletes the schedule the ``etag`` names (204)."""
        identifier = _require_schedule_id(schedule_id)
        await self._channel.send("DELETE", self._path(identifier), headers={"If-Match": _require_etag(etag)})

    async def trigger(
        self,
        schedule_id: str,
        etag: str,
        *,
        idempotency_key: str,
        deadline: datetime,
    ) -> YogabaseScheduleTriggerResult:
        """Runs the schedule the ``etag`` names once now, outside its cadence.

        It enqueues one invocation of the schedule's function with the schedule's payload, as the schedule's
        owner (202). The same ``idempotency_key`` against the same version answers the same invocation, with
        outcome ``Replayed`` where the first said ``Enqueued``. ``idempotency_key`` is 1 to 256 characters with
        no leading or trailing white space; ``deadline`` is a timezone-aware instant later than now and at most
        3600 seconds from now, else the gateway refuses it.
        """
        identifier = _require_schedule_id(schedule_id)
        token = _require_etag(etag)
        if (
            not isinstance(idempotency_key, str)
            or not idempotency_key.strip()
            or len(idempotency_key) > YOGABASE_SCHEDULES_MAXIMUM_IDEMPOTENCY_KEY_LENGTH
            or idempotency_key != idempotency_key.strip()
        ):
            raise ValueError("an idempotency key is 1 to 256 characters with no leading or trailing white space")
        body = json.dumps(
            {"idempotencyKey": idempotency_key, "deadline": _to_instant(deadline, "deadline")},
            separators=(",", ":"),
        ).encode("utf-8")
        response = await self._channel.send("POST", self._path(identifier) + "/trigger", body, headers={"If-Match": token})
        return _read_trigger(response)

    async def _write(self, definition: YogabaseScheduleDefinition, etag: str | None) -> YogabaseSchedule:
        body = json.dumps(_definition_body(definition), separators=(",", ":")).encode("utf-8")
        # Exactly one of the two preconditions, never both and never neither: a create must not overwrite what
        # another request made first, and an update must not overwrite what it never read.
        precondition = {"If-None-Match": "*"} if etag is None else {"If-Match": etag}
        response = await self._channel.send("PUT", self._path(definition.schedule_id), body, headers=precondition)
        return _read_schedule(response, definition.schedule_id, self.project_id)

    def _path(self, schedule_id: str) -> str:
        # The id is one escaped path segment, here and nowhere else.
        return f"/v1/projects/{self.project_id}/schedules/{quote(schedule_id, safe='')}"


def _require_schedule_id(value: str) -> str:
    if (
        not isinstance(value, str)
        or not value.strip()
        or len(value) > YOGABASE_SCHEDULES_MAXIMUM_SCHEDULE_ID_LENGTH
        or value != value.strip()
        or CONTROL_CHARACTER.search(value)
    ):
        raise ValueError("a schedule id is 1 to 128 characters, trimmed, with no control character")
    # The id is a path segment, and a URL parser collapses a segment of dots (percent-encoded or not) into the
    # one before it, so these two would address another route altogether.
    if value in (".", ".."):
        raise ValueError("a schedule id of only dots cannot be addressed in a URL path; choose another id")
    return value


def _require_etag(value: str) -> str:
    match = _ETAG.fullmatch(value) if isinstance(value, str) else None
    if match is None or int(match.group(1)) > _MAXIMUM_INT64 or not match.group(2).strip("0"):
        raise TypeError('an ETag is the quoted token a schedule read or write answered, "schedule-v<version>-<instance>", exactly as sent')
    return value


def _to_instant(value: datetime, name: str) -> str:
    if not isinstance(value, datetime):
        raise TypeError(f"{name} must be a datetime")
    if value.tzinfo is None or value.utcoffset() is None:
        raise ValueError(f"{name} must be timezone-aware: the gateway needs an instant, not a local time")
    return value.astimezone(timezone.utc).isoformat()


def _bounded_text(value: object, name: str, maximum_length: int) -> str:
    if not isinstance(value, str) or not value.strip() or len(value) > maximum_length:
        raise ValueError(f"{name} is required and at most {maximum_length} characters")
    return value


def _bounded_integer(value: object, name: str, minimum: int, maximum: int) -> int:
    if isinstance(value, bool) or not isinstance(value, int) or not minimum <= value <= maximum:
        raise ValueError(f"{name} is between {minimum} and {maximum}")
    return value


# The member order and the names are the gateway's ScheduledLambdaDefinitionRequest, which refuses any other
# member. A schedule with no payload and one with an empty payload are the same schedule to the gateway.
def _definition_body(definition: YogabaseScheduleDefinition) -> dict[str, Any]:
    if not isinstance(definition, YogabaseScheduleDefinition):
        raise TypeError("a YogabaseScheduleDefinition is required")
    _require_schedule_id(definition.schedule_id)
    body: dict[str, Any] = {
        "scheduleId": definition.schedule_id,
        "functionId": _bounded_text(definition.function_id, "function_id", YOGABASE_SCHEDULES_MAXIMUM_FUNCTION_ID_LENGTH),
        "functionVersion": _bounded_text(definition.function_version, "function_version", YOGABASE_SCHEDULES_MAXIMUM_FUNCTION_VERSION_LENGTH),
        "cron": _bounded_text(definition.cron, "cron", YOGABASE_SCHEDULES_MAXIMUM_CRON_LENGTH),
        "timeZone": _bounded_text(definition.time_zone, "time_zone", YOGABASE_SCHEDULES_MAXIMUM_TIME_ZONE_ID_LENGTH),
    }
    if definition.misfire_policy not in YOGABASE_SCHEDULE_MISFIRE_POLICIES:
        raise ValueError("a misfire policy is required: the gateway reads a missing one as RunImmediately, which its contract does not promise")
    if definition.concurrency_policy not in YOGABASE_SCHEDULE_CONCURRENCY_POLICIES:
        raise ValueError("a concurrency policy is required: the gateway reads a missing one as Allow, which its contract does not promise")
    body["misfirePolicy"] = definition.misfire_policy
    body["concurrencyPolicy"] = definition.concurrency_policy
    body["maxCatchUp"] = _bounded_integer(definition.max_catch_up, "max_catch_up", 1, YOGABASE_SCHEDULES_MAXIMUM_CATCH_UP)
    body["maxAttempts"] = _bounded_integer(definition.max_attempts, "max_attempts", 1, YOGABASE_SCHEDULES_MAXIMUM_ATTEMPTS)
    body["deadlineSeconds"] = _bounded_integer(
        definition.deadline_seconds,
        "deadline_seconds",
        YOGABASE_SCHEDULES_MINIMUM_DEADLINE_SECONDS,
        YOGABASE_SCHEDULES_MAXIMUM_DEADLINE_SECONDS,
    )
    if definition.payload is not None:
        if not isinstance(definition.payload, (bytes, bytearray, memoryview)):
            raise TypeError("payload must be bytes")
        if len(definition.payload) > YOGABASE_SCHEDULES_MAXIMUM_PAYLOAD_BYTES:
            raise ValueError("a schedule payload is limited to one MiB before base64 encoding")
        if len(definition.payload) > 0:
            body["payloadBase64"] = base64.b64encode(bytes(definition.payload)).decode("ascii")
    if definition.one_shot_at_utc is not None:
        body["oneShotAtUtc"] = _to_instant(definition.one_shot_at_utc, "one_shot_at_utc")
    return body


def _invalid(message: str, response: httpx.Response | None = None) -> YogabaseSchedulesError:
    return YogabaseSchedulesError(
        message,
        502,
        SCHEDULE_RESPONSE_INVALID,
        None,
        response.headers.get("X-Correlation-ID") if response is not None else None,
    )


def _schedules_error(response: httpx.Response) -> YogabaseSchedulesError:
    refusal = read_problem(response)
    return YogabaseSchedulesError(
        refusal.message,
        refusal.status,
        refusal.code,
        refusal.problem,
        refusal.correlation_id,
        refusal.limit_bytes,
        refusal.max_body_bytes,
    )


def _read_document(response: httpx.Response, what: str) -> Any:
    try:
        return response.json()
    except (ValueError, json.JSONDecodeError):
        raise _invalid(f"Yogabase returned a {what} document that is not JSON.", response) from None


# The schedule the answer describes must be the one asked for, in this client's own project: a gateway that
# describes another is not trusted, and a caller that holds the wrong ETag for the wrong schedule would delete it.
# The ETag header, where the gateway sends a strong one, must say what the body says; a weak or repeated one is
# ignored, because a proxy that compresses a response may weaken its ETag and the body carries the same facts. A
# member this client does not know is ignored, as the generated clients keep unknown members aside.
def _read_schedule(response: httpx.Response, requested_schedule_id: str, project_id: str) -> YogabaseSchedule:
    document = _read_document(response, "schedule")
    if not isinstance(document, dict):
        raise _invalid("Yogabase returned an invalid schedule.", response)

    def text(name: str) -> str | None:
        value = document.get(name)
        return value if isinstance(value, str) else None

    def instant(name: str) -> str | None | bool:
        # False marks a member that is neither null nor a time.
        value = document.get(name)
        if value is None:
            return None
        return value if isinstance(value, str) and TIMESTAMP.fullmatch(value) else False

    schedule_id = text("scheduleId")
    project = text("projectId")
    subject = text("subject")
    function_id = text("functionId")
    function_version = text("functionVersion")
    version = text("version")
    instance_id = text("instanceId")
    last_occurrence = instant("lastOccurrence")
    one_shot = instant("oneShotAtUtc")
    if (
        schedule_id is None or schedule_id != requested_schedule_id
        or project is None or project.lower() != project_id.lower()
        or not is_non_zero_uuid(subject)
        or function_id is None or not function_id.strip() or len(function_id) > YOGABASE_SCHEDULES_MAXIMUM_FUNCTION_ID_LENGTH
        or function_version is None or not function_version.strip() or len(function_version) > YOGABASE_SCHEDULES_MAXIMUM_FUNCTION_VERSION_LENGTH
        or version is None or not _VERSION.fullmatch(version) or int(version) > _MAXIMUM_INT64
        or not is_non_zero_uuid(instance_id)
        or last_occurrence is False
        or one_shot is False
    ):
        raise _invalid("Yogabase returned an invalid schedule.", response)
    assert subject is not None and instance_id is not None
    etag = f'"schedule-v{version}-{instance_id.replace("-", "").lower()}"'
    declared = response.headers.get_list("ETag")
    if len(declared) == 1 and not declared[0].startswith("W/") and "," not in declared[0] and declared[0] != etag:
        raise _invalid("Yogabase returned a schedule whose ETag header does not match the schedule.", response)
    return YogabaseSchedule(
        schedule_id,
        project,
        subject,
        function_id,
        function_version,
        version,
        instance_id,
        last_occurrence,
        one_shot,
        etag,
    )


def _read_trigger(response: httpx.Response) -> YogabaseScheduleTriggerResult:
    document = _read_document(response, "schedule run")
    outcome = document.get("outcome") if isinstance(document, dict) else None
    invocation_id = document.get("invocationId") if isinstance(document, dict) else None
    if (
        isinstance(outcome, bool)
        or outcome not in (0, 1)
        or not isinstance(outcome, int)
        or not is_non_zero_uuid(invocation_id)
    ):
        raise _invalid("Yogabase returned an invalid schedule run.", response)
    return YogabaseScheduleTriggerResult("Enqueued" if outcome == 0 else "Replayed", invocation_id)
