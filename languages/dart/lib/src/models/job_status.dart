// ignore_for_file: type=lint
/// auto generated
///  Queued and Running are in progress. Succeeded, Cancelled and DeadLettered are final: nothing about the job changes again. A failed attempt sends the job back to Queued for its next attempt, or on to DeadLettered once maxAttempts attempts have started. Failed names a failed attempt between those two, is never final, and is not a state the ledger records: a client that reads it treats the job as in progress.
enum JobStatus {
    queued('Queued'),
    running('Running'),
    succeeded('Succeeded'),
    failed('Failed'),
    cancelled('Cancelled'),
    deadLettered('DeadLettered');
    const JobStatus(this.value);
    final String value;
}
