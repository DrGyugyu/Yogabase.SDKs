package io.yogabase.client;

import java.time.OffsetDateTime;
import java.util.UUID;

/**
 * One durable job as the gateway reads it back: its state, how far it has got and how it ended. The
 * payload never travels back, and a result is read separately ({@link YogabaseJobsClient#result}).
 *
 * @param jobId the job's id; every other call names the job by it
 * @param jobType the id of the function the job runs
 * @param status the job's state
 * @param attempt how many attempts have started; 0 while the job has never run
 * @param maxAttempts how many attempts the job may have in all, from 1 to 10
 * @param progressPercent how far the function says it has got, from 0 to 100
 * @param cancellationRequested true once a cancel has been asked for; a running job is stopped by its
 *     worker, so this can be true while the status is still {@link YogabaseJobStatus#RUNNING}
 * @param resultReference a name for the stored result, not a capability (read the bytes with
 *     {@link YogabaseJobsClient#result}); {@code null} while the job has none
 * @param failureCode why the job last failed, as the function or the worker named it, or {@code null}
 * @param createdAt when the job was accepted
 * @param updatedAt when the job last changed
 * @param completedAt when the job settled, or {@code null} while it has not
 */
public record YogabaseJob(
    UUID jobId,
    String jobType,
    YogabaseJobStatus status,
    int attempt,
    int maxAttempts,
    int progressPercent,
    boolean cancellationRequested,
    String resultReference,
    String failureCode,
    OffsetDateTime createdAt,
    OffsetDateTime updatedAt,
    OffsetDateTime completedAt) {

    /** True once the job has settled: succeeded, cancelled or dead-lettered. */
    public boolean isTerminal() {
        return status.isTerminal();
    }
}
