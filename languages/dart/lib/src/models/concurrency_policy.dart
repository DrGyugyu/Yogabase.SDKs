// ignore_for_file: type=lint
/// auto generated
///  What a schedule does when an occurrence is due while an earlier occurrence of the same schedule has not finished. An occurrence has not finished while its invocation is queued (waiting to start, or waiting for a retry) or running, and its deadline has not passed. Allow starts the new occurrence alongside. Forbid skips it. Replace ends an earlier occurrence that is still queued, with the failure code schedule_occurrence_replaced, and starts the new one; an occurrence that is already running cannot be stopped, because nothing can interrupt a Lambda that is executing, so while one is running Replace skips the new occurrence, as Forbid does. A skipped occurrence is consumed, not deferred: the schedule moves on to its next occurrence and the skipped one never runs. Forbid never starts an occurrence while an earlier one is in flight, and Replace never while one is running; when several are due at once (RunAllMissed after an outage) Forbid starts only the oldest and Replace only the newest. A manual run is not an occurrence: the policy neither holds it back nor is held back by it, and a one-shot has no earlier occurrence, so the policy does not apply to it. The decision is made in the transaction that enqueues the occurrence, so two schedulers cannot both decide. An unknown name is a bare 400. Required: a request that leaves it out is a 400 whose problem names the member.
enum ConcurrencyPolicy {
    allow('Allow'),
    forbid('Forbid'),
    replace('Replace');
    const ConcurrencyPolicy(this.value);
    final String value;
}
