// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';

/// auto generated
class EnvironmentsRequestBuilderGetQueryParameters implements AbstractQueryParameters {
    String? cursor;
    int? limit;
    /// Extracts the query parameters into a map for the URI template parsing.
    @override
    Map<String, dynamic> toMap() {
        return {
            'cursor' : cursor,
            'limit' : limit,
        };
    }
}
