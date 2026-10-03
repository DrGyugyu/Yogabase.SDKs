// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './concurrency_policy.dart';
import './misfire_policy.dart';

/// auto generated
class ScheduleDefinitionRequest implements Parsable {
    ///  What a schedule does when an occurrence is due while an earlier occurrence of the same schedule has not finished. An occurrence has not finished while its invocation is queued (waiting to start, or waiting for a retry) or running, and its deadline has not passed. Allow starts the new occurrence alongside. Forbid skips it. Replace ends an earlier occurrence that is still queued, with the failure code schedule_occurrence_replaced, and starts the new one; an occurrence that is already running cannot be stopped, because nothing can interrupt a Lambda that is executing, so while one is running Replace skips the new occurrence, as Forbid does. A skipped occurrence is consumed, not deferred: the schedule moves on to its next occurrence and the skipped one never runs. Forbid never starts an occurrence while an earlier one is in flight, and Replace never while one is running; when several are due at once (RunAllMissed after an outage) Forbid starts only the oldest and Replace only the newest. A manual run is not an occurrence: the policy neither holds it back nor is held back by it, and a one-shot has no earlier occurrence, so the policy does not apply to it. The decision is made in the transaction that enqueues the occurrence, so two schedulers cannot both decide. An unknown name is a bare 400. Required: a request that leaves it out is a 400 whose problem names the member.
    ConcurrencyPolicy? concurrencyPolicy;
    ///  Five fields, minute hour day-of-month month day-of-week, each a number, *, a list (1,15), a range (1-5), a step (*/5 or 8-18/2) or, for the month and the day of the week, a name (JAN, MON); 7 is Sunday. No seconds field and no @ macros. It is evaluated in timeZone. A one-shot still sends a valid expression, which does not decide when it runs.
    String? cron;
    ///  How long an occurrence's invocation has to finish, counted from when the scheduler starts it: the poll that finds the occurrence due, which is a little after its time.
    int? deadlineSeconds;
    ///  The id of a deployed Stateless Lambda.
    String? functionId;
    ///  The exact released version of that function which every occurrence runs. It must be admitted for the project, and an update checks it again.
    String? functionVersion;
    ///  How many times an occurrence's invocation may be attempted.
    int? maxAttempts;
    ///  The most missed occurrences RunAllMissed runs after an outage. The other policies ignore it.
    int? maxCatchUp;
    ///  What the scheduler does about occurrences it missed while it was unavailable. RunImmediately runs once, now, however many were missed, and resumes the cadence. SkipToNextOccurrence abandons what it missed and resumes at the next occurrence. RunAllMissed runs each missed occurrence, oldest first, up to maxCatchUp. An unknown name is a bare 400. Required: a request that leaves it out is a 400 whose problem names the member.
    MisfirePolicy? misfirePolicy;
    ///  Makes the schedule a one-shot: it runs once at this instant, which must be a UTC timestamp (Z or +00:00), and is then deleted. An instant already past runs at the scheduler's next poll. Absent or null is a recurring schedule. An update keeps a schedule's kind: it cannot turn a one-shot into a recurring schedule or the reverse (409).
    DateTime? oneShotAtUtc;
    ///  The request every occurrence's invocation carries, at most 1,048,576 bytes before base64 encoding. Absent, null or empty is none.
    String? payloadBase64;
    ///  The schedule's id, chosen by the caller and the same as the path's: 1 to 128 characters, a letter or a digit first and then letters, digits and . _ ~ : - only. The id is one segment of the schedule's URL path, so it is made of what a path segment carries without being escaped (a slash would end the segment, a space or a character outside ASCII would have to be escaped, and no id may be . or .., which URL clients resolve away), and the console addresses the schedule by the same id. An id that does not follow the rule is a 400 whose problem names the member and the rule in its detail.
    String? scheduleId;
    ///  The time zone the cron expression is evaluated in, by the id the gateway's system knows it by (UTC, Asia/Seoul). An unknown one is 400.
    String? timeZone;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static ScheduleDefinitionRequest createFromDiscriminatorValue(ParseNode parseNode) {
        return ScheduleDefinitionRequest();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['concurrencyPolicy'] = (node) => concurrencyPolicy = node.getEnumValue<ConcurrencyPolicy>((stringValue) => ConcurrencyPolicy.values.where((enumVal) => enumVal.value == stringValue).firstOrNull);
        deserializerMap['cron'] = (node) => cron = node.getStringValue();
        deserializerMap['deadlineSeconds'] = (node) => deadlineSeconds = node.getIntValue();
        deserializerMap['functionId'] = (node) => functionId = node.getStringValue();
        deserializerMap['functionVersion'] = (node) => functionVersion = node.getStringValue();
        deserializerMap['maxAttempts'] = (node) => maxAttempts = node.getIntValue();
        deserializerMap['maxCatchUp'] = (node) => maxCatchUp = node.getIntValue();
        deserializerMap['misfirePolicy'] = (node) => misfirePolicy = node.getEnumValue<MisfirePolicy>((stringValue) => MisfirePolicy.values.where((enumVal) => enumVal.value == stringValue).firstOrNull);
        deserializerMap['oneShotAtUtc'] = (node) => oneShotAtUtc = node.getDateTimeValue();
        deserializerMap['payloadBase64'] = (node) => payloadBase64 = node.getStringValue();
        deserializerMap['scheduleId'] = (node) => scheduleId = node.getStringValue();
        deserializerMap['timeZone'] = (node) => timeZone = node.getStringValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeEnumValue<ConcurrencyPolicy>('concurrencyPolicy', concurrencyPolicy, (e) => e?.value);
        writer.writeStringValue('cron', cron);
        writer.writeIntValue('deadlineSeconds', deadlineSeconds);
        writer.writeStringValue('functionId', functionId);
        writer.writeStringValue('functionVersion', functionVersion);
        writer.writeIntValue('maxAttempts', maxAttempts);
        writer.writeIntValue('maxCatchUp', maxCatchUp);
        writer.writeEnumValue<MisfirePolicy>('misfirePolicy', misfirePolicy, (e) => e?.value);
        writer.writeDateTimeValue('oneShotAtUtc', oneShotAtUtc);
        writer.writeStringValue('payloadBase64', payloadBase64);
        writer.writeStringValue('scheduleId', scheduleId);
        writer.writeStringValue('timeZone', timeZone);
    }
}
