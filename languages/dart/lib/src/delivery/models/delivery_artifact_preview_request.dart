// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';

/// auto generated
class DeliveryArtifactPreviewRequest implements Parsable {
    ///  The artifactVersion property
    int? artifactVersion;
    ///  The bundleDigest property
    String? bundleDigest;
    ///  The criticalFindings property
    int? criticalFindings;
    ///  The highFindings property
    int? highFindings;
    ///  The manifestDigest property
    String? manifestDigest;
    ///  The manifestUri property
    String? manifestUri;
    ///  The sbomDigest property
    String? sbomDigest;
    ///  The sbomFormat property
    String? sbomFormat;
    ///  The sbomUri property
    String? sbomUri;
    ///  The scanEvidenceDigest property
    String? scanEvidenceDigest;
    ///  The scannedAt property
    DateTime? scannedAt;
    ///  The scanner property
    String? scanner;
    ///  The scanUri property
    String? scanUri;
    ///  Credential-free native provider URI; percent escapes are rejected.
    String? sourceLocator;
    ///  The sourceMediaType property
    String? sourceMediaType;
    ///  The sourceSizeBytes property
    int? sourceSizeBytes;
    /// Creates a new instance of the appropriate class based on discriminator value
    ///  [parseNode] The parse node to use to read the discriminator value and create the object
    static DeliveryArtifactPreviewRequest createFromDiscriminatorValue(ParseNode parseNode) {
        return DeliveryArtifactPreviewRequest();
    }
    /// The deserialization information for the current model
    @override
    Map<String, void Function(ParseNode)> getFieldDeserializers() {
        var deserializerMap = <String, void Function(ParseNode)>{};
        deserializerMap['artifactVersion'] = (node) => artifactVersion = node.getIntValue();
        deserializerMap['bundleDigest'] = (node) => bundleDigest = node.getStringValue();
        deserializerMap['criticalFindings'] = (node) => criticalFindings = node.getIntValue();
        deserializerMap['highFindings'] = (node) => highFindings = node.getIntValue();
        deserializerMap['manifestDigest'] = (node) => manifestDigest = node.getStringValue();
        deserializerMap['manifestUri'] = (node) => manifestUri = node.getStringValue();
        deserializerMap['sbomDigest'] = (node) => sbomDigest = node.getStringValue();
        deserializerMap['sbomFormat'] = (node) => sbomFormat = node.getStringValue();
        deserializerMap['sbomUri'] = (node) => sbomUri = node.getStringValue();
        deserializerMap['scanEvidenceDigest'] = (node) => scanEvidenceDigest = node.getStringValue();
        deserializerMap['scannedAt'] = (node) => scannedAt = node.getDateTimeValue();
        deserializerMap['scanner'] = (node) => scanner = node.getStringValue();
        deserializerMap['scanUri'] = (node) => scanUri = node.getStringValue();
        deserializerMap['sourceLocator'] = (node) => sourceLocator = node.getStringValue();
        deserializerMap['sourceMediaType'] = (node) => sourceMediaType = node.getStringValue();
        deserializerMap['sourceSizeBytes'] = (node) => sourceSizeBytes = node.getIntValue();
        return deserializerMap;
    }
    /// Serializes information the current object
    ///  [writer] Serialization writer to use to serialize this model
    @override
    void serialize(SerializationWriter writer) {
        writer.writeIntValue('artifactVersion', artifactVersion);
        writer.writeStringValue('bundleDigest', bundleDigest);
        writer.writeIntValue('criticalFindings', criticalFindings);
        writer.writeIntValue('highFindings', highFindings);
        writer.writeStringValue('manifestDigest', manifestDigest);
        writer.writeStringValue('manifestUri', manifestUri);
        writer.writeStringValue('sbomDigest', sbomDigest);
        writer.writeStringValue('sbomFormat', sbomFormat);
        writer.writeStringValue('sbomUri', sbomUri);
        writer.writeStringValue('scanEvidenceDigest', scanEvidenceDigest);
        writer.writeDateTimeValue('scannedAt', scannedAt);
        writer.writeStringValue('scanner', scanner);
        writer.writeStringValue('scanUri', scanUri);
        writer.writeStringValue('sourceLocator', sourceLocator);
        writer.writeStringValue('sourceMediaType', sourceMediaType);
        writer.writeIntValue('sourceSizeBytes', sourceSizeBytes);
    }
}
