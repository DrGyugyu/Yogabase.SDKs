// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';

/// auto generated
class EventsRequestBuilderGetQueryParameters implements AbstractQueryParameters {
    UuidValue? environmentId;
    UuidValue? operationId;
    /// Extracts the query parameters into a map for the URI template parsing.
    @override
    Map<String, dynamic> toMap() {
        return {
            'environmentId' : environmentId,
            'operationId' : operationId,
        };
    }
}
