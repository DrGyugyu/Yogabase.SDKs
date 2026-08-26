// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import './invocation_request_object_reference_member1.dart';
import './object_reference.dart';

/// auto generated
/// Composed type wrapper for classes [InvocationRequestObjectReferenceMember1], [ObjectReference]
class InvocationRequestObjectReference implements Parsable {
    ///  Composed type representation for type [InvocationRequestObjectReferenceMember1]
    InvocationRequestObjectReferenceMember1? invocationRequestObjectReferenceMember1;
    ///  Composed type representation for type [ObjectReference]
    ObjectReference? objectReference;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static InvocationRequestObjectReference createFromDiscriminatorValue(ParseNode parseNode) {
        var result = InvocationRequestObjectReference();
        var mappingValue = parseNode.getChildNode('')?.getStringValue();
        if('ObjectReference' == mappingValue) {
            result.objectReference = ObjectReference();
        }
        return result;
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        if(invocationRequestObjectReferenceMember1 != null) {
            return invocationRequestObjectReferenceMember1!.getFieldDeserializers();
        }
        else if(objectReference != null) {
            return objectReference!.getFieldDeserializers();
        }
        return <String, void Function(ParseNode)>{};
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        if(invocationRequestObjectReferenceMember1 != null) {
            writer.writeObjectValue<InvocationRequestObjectReferenceMember1>(null, invocationRequestObjectReferenceMember1);
        }
        else if(objectReference != null) {
            writer.writeObjectValue<ObjectReference>(null, objectReference);
        }
    }
}
