// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';

/// auto generated
class ReturnPostRequestBody implements AdditionalDataHolder, Parsable {
    ///  Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.
    @override
    Map<String, Object?> additionalData;
    ///  The code property
    String? code;
    ///  The state property
    String? state;
    /// Instantiates a new [ReturnPostRequestBody] and sets the default values.
    ReturnPostRequestBody() :  
        additionalData = {};
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static ReturnPostRequestBody createFromDiscriminatorValue(ParseNode parseNode) {
        return ReturnPostRequestBody();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['code'] = (node) => code = node.getStringValue();
        deserializerMap['state'] = (node) => state = node.getStringValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeStringValue('code', code);
        writer.writeStringValue('state', state);
        writer.writeAdditionalData(additionalData);
    }
}
