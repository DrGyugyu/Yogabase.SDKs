// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';

/// auto generated
class ProblemDetails extends ApiException implements AdditionalDataHolder, Parsable {
    ///  Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.
    @override
    Map<String, Object?> additionalData;
    ///  The code property
    String? code;
    ///  The correlationId property
    String? correlationId;
    ///  The detail property
    String? detail;
    ///  The instance property
    String? instance;
    ///  The status property
    int? status;
    ///  The title property
    String? title;
    ///  The type property
    String? type_;
    /// Instantiates a new [ProblemDetails] and sets the default values.
    ProblemDetails( {
        super.message,
        super.statusCode,
        super.responseHeaders,
        super.innerExceptions,
        required this.additionalData,
        this.code,
        this.correlationId,
        this.detail,
        this.instance,
        this.status,
        this.title,
        this.type_,
    });
    /// Creates a copy of the object.
    @override
    ProblemDetails copyWith({int? statusCode, String? message, Map<String, List<String>>? responseHeaders, Iterable<Object?>? innerExceptions, Map<String, Object?>? additionalData, String? code, String? correlationId, String? detail, String? instance, int? status, String? title, String? type_ }){
        return ProblemDetails(
        message : message ?? this.message, 
        statusCode : statusCode ?? this.statusCode, 
        responseHeaders : responseHeaders ?? this.responseHeaders, 
        innerExceptions : innerExceptions ?? this.innerExceptions, 
        additionalData : additionalData ?? this.additionalData, 
        code : code ?? this.code, 
        correlationId : correlationId ?? this.correlationId, 
        detail : detail ?? this.detail, 
        instance : instance ?? this.instance, 
        status : status ?? this.status, 
        title : title ?? this.title, 
        type_ : type_ ?? this.type_, 
        );
    }
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static ProblemDetails createFromDiscriminatorValue(ParseNode parseNode) {
        return ProblemDetails(additionalData: {});
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['code'] = (node) => code = node.getStringValue();
        deserializerMap['correlationId'] = (node) => correlationId = node.getStringValue();
        deserializerMap['detail'] = (node) => detail = node.getStringValue();
        deserializerMap['instance'] = (node) => instance = node.getStringValue();
        deserializerMap['status'] = (node) => status = node.getIntValue();
        deserializerMap['title'] = (node) => title = node.getStringValue();
        deserializerMap['type'] = (node) => type_ = node.getStringValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeStringValue('code', code);
        writer.writeStringValue('correlationId', correlationId);
        writer.writeStringValue('detail', detail);
        writer.writeStringValue('instance', instance);
        writer.writeIntValue('status', status);
        writer.writeStringValue('title', title);
        writer.writeStringValue('type', type_);
        writer.writeAdditionalData(additionalData);
    }
}
