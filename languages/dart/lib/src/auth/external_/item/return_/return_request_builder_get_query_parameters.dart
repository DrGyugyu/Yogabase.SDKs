// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';

/// auto generated
/// The one address registered with a provider (https://<gateway>/auth/external/<id>/return); no application calls it. A provider that answers by query lands here. It redirects to the redirect URI given to start, carrying the code and state, or error=access_denied and the state when the provider sent no code. Nothing else of the answer is read.
class ReturnRequestBuilderGetQueryParameters implements AbstractQueryParameters {
    String? code;
    String? state;
    /// Extracts the query parameters into a map for the URI template parsing.
    @override
    Map<String, dynamic> toMap() {
        return {
            'code' : code,
            'state' : state,
        };
    }
}
