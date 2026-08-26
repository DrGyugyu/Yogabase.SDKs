// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './problem_extensions.dart';

/// auto generated
class Problem extends ApiException implements AdditionalDataHolder, Parsable {
    ///  Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.
    @override
    Map<String, Object?> additionalData;
    ///  The code property
    String? code;
    ///  The detail property
    String? detail;
    ///  The extensions property
    ProblemExtensions? extensions;
    ///  The status property
    int? status;
    ///  The title property
    String? title;
    ///  The traceId property
    String? traceId;
    ///  The type property
    String? type_;
    /// Instantiates a new [Problem] and sets the default values.
    Problem( {
        super.message,
        super.statusCode,
        super.responseHeaders,
        super.innerExceptions,
        required this.additionalData,
        this.code,
        this.detail,
        this.extensions,
        this.status,
        this.title,
        this.traceId,
        this.type_,
    });
    /// Creates a copy of the object.
    @override
    Problem copyWith({int? statusCode, String? message, Map<String, List<String>>? responseHeaders, Iterable<Object?>? innerExceptions, Map<String, Object?>? additionalData, String? code, String? detail, ProblemExtensions? extensions, int? status, String? title, String? traceId, String? type_ }){
        return Problem(
        message : message ?? this.message, 
        statusCode : statusCode ?? this.statusCode, 
        responseHeaders : responseHeaders ?? this.responseHeaders, 
        innerExceptions : innerExceptions ?? this.innerExceptions, 
        additionalData : additionalData ?? this.additionalData, 
        code : code ?? this.code, 
        detail : detail ?? this.detail, 
        extensions : extensions ?? this.extensions, 
        status : status ?? this.status, 
        title : title ?? this.title, 
        traceId : traceId ?? this.traceId, 
        type_ : type_ ?? this.type_, 
        );
    }
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static Problem createFromDiscriminatorValue(ParseNode parseNode) {
        return Problem(additionalData: {});
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['code'] = (node) => code = node.getStringValue();
        deserializerMap['detail'] = (node) => detail = node.getStringValue();
        deserializerMap['extensions'] = (node) => extensions = node.getObjectValue<ProblemExtensions>(ProblemExtensions.createFromDiscriminatorValue);
        deserializerMap['status'] = (node) => status = node.getIntValue();
        deserializerMap['title'] = (node) => title = node.getStringValue();
        deserializerMap['traceId'] = (node) => traceId = node.getStringValue();
        deserializerMap['type'] = (node) => type_ = node.getStringValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeStringValue('code', code);
        writer.writeStringValue('detail', detail);
        writer.writeObjectValue<ProblemExtensions>('extensions', extensions);
        writer.writeIntValue('status', status);
        writer.writeStringValue('title', title);
        writer.writeStringValue('traceId', traceId);
        writer.writeStringValue('type', type_);
        writer.writeAdditionalData(additionalData);
    }
}
