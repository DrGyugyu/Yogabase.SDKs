// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';
import './job_status.dart';

/// auto generated
class Job implements Parsable {
    ///  How many attempts have started: 0 until a worker first claims the job, and never more than maxAttempts.
    int? attempt;
    ///  True once the job's owner has asked for it to stop, and so on a cancelled job.
    bool? cancellationRequested;
    ///  When the job became Succeeded, Cancelled or DeadLettered. Null before.
    DateTime? completedAt;
    ///  The createdAt property
    DateTime? createdAt;
    ///  Why the last failed attempt failed, as a stable code. Null until an attempt fails, and again once the job succeeds or is cancelled.
    String? failureCode;
    ///  The jobId property
    UuidValue? jobId;
    ///  The jobType property
    String? jobType;
    ///  The maxAttempts property
    int? maxAttempts;
    ///  How far the job reports it has got; 100 once it has succeeded.
    int? progressPercent;
    ///  A name for the stored result, not a capability: the bytes are read from the result route. Null while there is none.
    String? resultReference;
    ///  Queued and Running are in progress. Succeeded, Cancelled and DeadLettered are final: nothing about the job changes again. A failed attempt sends the job back to Queued for its next attempt, or on to DeadLettered once maxAttempts attempts have started. Failed names a failed attempt between those two, is never final, and is not a state the ledger records: a client that reads it treats the job as in progress.
    JobStatus? status;
    ///  The updatedAt property
    DateTime? updatedAt;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static Job createFromDiscriminatorValue(ParseNode parseNode) {
        return Job();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['attempt'] = (node) => attempt = node.getIntValue();
        deserializerMap['cancellationRequested'] = (node) => cancellationRequested = node.getBoolValue();
        deserializerMap['completedAt'] = (node) => completedAt = node.getDateTimeValue();
        deserializerMap['createdAt'] = (node) => createdAt = node.getDateTimeValue();
        deserializerMap['failureCode'] = (node) => failureCode = node.getStringValue();
        deserializerMap['jobId'] = (node) => jobId = node.getGuidValue();
        deserializerMap['jobType'] = (node) => jobType = node.getStringValue();
        deserializerMap['maxAttempts'] = (node) => maxAttempts = node.getIntValue();
        deserializerMap['progressPercent'] = (node) => progressPercent = node.getIntValue();
        deserializerMap['resultReference'] = (node) => resultReference = node.getStringValue();
        deserializerMap['status'] = (node) => status = node.getEnumValue<JobStatus>((stringValue) => JobStatus.values.where((enumVal) => enumVal.value == stringValue).firstOrNull);
        deserializerMap['updatedAt'] = (node) => updatedAt = node.getDateTimeValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeIntValue('attempt', attempt);
        writer.writeBoolValue('cancellationRequested', value:cancellationRequested);
        writer.writeDateTimeValue('completedAt', completedAt);
        writer.writeDateTimeValue('createdAt', createdAt);
        writer.writeStringValue('failureCode', failureCode);
        writer.writeUuidValue('jobId', jobId);
        writer.writeStringValue('jobType', jobType);
        writer.writeIntValue('maxAttempts', maxAttempts);
        writer.writeIntValue('progressPercent', progressPercent);
        writer.writeStringValue('resultReference', resultReference);
        writer.writeEnumValue<JobStatus>('status', status, (e) => e?.value);
        writer.writeDateTimeValue('updatedAt', updatedAt);
    }
}
