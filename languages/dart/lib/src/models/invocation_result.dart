// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';

/// auto generated
class InvocationResult extends ApiException implements Parsable {
    ///  The correlationId property
    String? correlationId;
    ///  The failureCode property
    String? failureCode;
    ///  The inlineResultBase64 property
    String? inlineResultBase64;
    ///  The invocationId property
    UuidValue? invocationId;
    ///  The outcome property
    int? outcome;
    ///  The resultReference property
    String? resultReference;
    /// Instantiates a new [InvocationResult] and sets the default values.
    InvocationResult( {
        super.message,
        super.statusCode,
        super.responseHeaders,
        super.innerExceptions,
        this.correlationId,
        this.failureCode,
        this.inlineResultBase64,
        this.invocationId,
        this.outcome,
        this.resultReference,
    });
    /// Creates a copy of the object.
    @override
    InvocationResult copyWith({int? statusCode, String? message, Map<String, List<String>>? responseHeaders, Iterable<Object?>? innerExceptions, String? correlationId, String? failureCode, String? inlineResultBase64, UuidValue? invocationId, int? outcome, String? resultReference }){
        return InvocationResult(
        message : message ?? this.message, 
        statusCode : statusCode ?? this.statusCode, 
        responseHeaders : responseHeaders ?? this.responseHeaders, 
        innerExceptions : innerExceptions ?? this.innerExceptions, 
        correlationId : correlationId ?? this.correlationId, 
        failureCode : failureCode ?? this.failureCode, 
        inlineResultBase64 : inlineResultBase64 ?? this.inlineResultBase64, 
        invocationId : invocationId ?? this.invocationId, 
        outcome : outcome ?? this.outcome, 
        resultReference : resultReference ?? this.resultReference, 
        );
    }
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static InvocationResult createFromDiscriminatorValue(ParseNode parseNode) {
        return InvocationResult();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['correlationId'] = (node) => correlationId = node.getStringValue();
        deserializerMap['failureCode'] = (node) => failureCode = node.getStringValue();
        deserializerMap['inlineResultBase64'] = (node) => inlineResultBase64 = node.getStringValue();
        deserializerMap['invocationId'] = (node) => invocationId = node.getGuidValue();
        deserializerMap['outcome'] = (node) => outcome = node.getIntValue();
        deserializerMap['resultReference'] = (node) => resultReference = node.getStringValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeStringValue('correlationId', correlationId);
        writer.writeStringValue('failureCode', failureCode);
        writer.writeStringValue('inlineResultBase64', inlineResultBase64);
        writer.writeUuidValue('invocationId', invocationId);
        writer.writeIntValue('outcome', outcome);
        writer.writeStringValue('resultReference', resultReference);
    }
}
