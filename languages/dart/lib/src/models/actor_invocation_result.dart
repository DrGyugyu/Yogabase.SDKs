// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';
import './actor_invocation_result_status.dart';

/// auto generated
class ActorInvocationResult extends ApiException implements Parsable {
    ///  The correlationId property
    String? correlationId;
    ///  The failureCode property
    String? failureCode;
    ///  The invocationId property
    UuidValue? invocationId;
    ///  The resultReference property
    String? resultReference;
    ///  The stateVersion property
    int? stateVersion;
    ///  The status property
    ActorInvocationResultStatus? status;
    /// Instantiates a new [ActorInvocationResult] and sets the default values.
    ActorInvocationResult( {
        super.message,
        super.statusCode,
        super.responseHeaders,
        super.innerExceptions,
        this.correlationId,
        this.failureCode,
        this.invocationId,
        this.resultReference,
        this.stateVersion,
        this.status,
    });
    /// Creates a copy of the object.
    @override
    ActorInvocationResult copyWith({int? statusCode, String? message, Map<String, List<String>>? responseHeaders, Iterable<Object?>? innerExceptions, String? correlationId, String? failureCode, UuidValue? invocationId, String? resultReference, int? stateVersion, ActorInvocationResultStatus? status }){
        return ActorInvocationResult(
        message : message ?? this.message, 
        statusCode : statusCode ?? this.statusCode, 
        responseHeaders : responseHeaders ?? this.responseHeaders, 
        innerExceptions : innerExceptions ?? this.innerExceptions, 
        correlationId : correlationId ?? this.correlationId, 
        failureCode : failureCode ?? this.failureCode, 
        invocationId : invocationId ?? this.invocationId, 
        resultReference : resultReference ?? this.resultReference, 
        stateVersion : stateVersion ?? this.stateVersion, 
        status : status ?? this.status, 
        );
    }
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static ActorInvocationResult createFromDiscriminatorValue(ParseNode parseNode) {
        return ActorInvocationResult();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['correlationId'] = (node) => correlationId = node.getStringValue();
        deserializerMap['failureCode'] = (node) => failureCode = node.getStringValue();
        deserializerMap['invocationId'] = (node) => invocationId = node.getGuidValue();
        deserializerMap['resultReference'] = (node) => resultReference = node.getStringValue();
        deserializerMap['stateVersion'] = (node) => stateVersion = node.getIntValue();
        deserializerMap['status'] = (node) => status = node.getEnumValue<ActorInvocationResultStatus>((stringValue) => ActorInvocationResultStatus.values.where((enumVal) => enumVal.value == stringValue).firstOrNull);
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeStringValue('correlationId', correlationId);
        writer.writeStringValue('failureCode', failureCode);
        writer.writeUuidValue('invocationId', invocationId);
        writer.writeStringValue('resultReference', resultReference);
        writer.writeIntValue('stateVersion', stateVersion);
        writer.writeEnumValue<ActorInvocationResultStatus>('status', status, (e) => e?.value);
    }
}
