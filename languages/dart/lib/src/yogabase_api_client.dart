// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:microsoft_kiota_serialization_form/microsoft_kiota_serialization_form.dart';
import 'package:microsoft_kiota_serialization_json/microsoft_kiota_serialization_json.dart';
import 'package:microsoft_kiota_serialization_multipart/microsoft_kiota_serialization_multipart.dart';
import 'package:microsoft_kiota_serialization_text/microsoft_kiota_serialization_text.dart';
import './auth/auth_request_builder.dart';
import './v1/v1_request_builder.dart';

/// auto generated
/// The main entry point of the SDK, exposes the configuration and the fluent API.
class YogabaseApiClient extends BaseRequestBuilder<YogabaseApiClient> {
    ///  The auth property
    AuthRequestBuilder get auth {
        return AuthRequestBuilder(pathParameters, requestAdapter);
    }
    ///  The v1 property
    V1RequestBuilder get v1 {
        return V1RequestBuilder(pathParameters, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    YogabaseApiClient clone() {
        return YogabaseApiClient(requestAdapter);
    }
    /// Instantiates a new [YogabaseApiClient] and sets the default values.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    YogabaseApiClient(RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}", {}) {
        ApiClientBuilder.registerDefaultSerializer(JsonSerializationWriterFactory.new);
        ApiClientBuilder.registerDefaultSerializer(TextSerializationWriterFactory.new);
        ApiClientBuilder.registerDefaultSerializer(FormSerializationWriterFactory.new);
        ApiClientBuilder.registerDefaultSerializer(MultipartSerializationWriterFactory.new);
        ApiClientBuilder.registerDefaultDeserializer(JsonParseNodeFactory.new);
        ApiClientBuilder.registerDefaultDeserializer(FormParseNodeFactory.new);
        ApiClientBuilder.registerDefaultDeserializer(TextParseNodeFactory.new);
        if (requestAdapter.baseUrl == null || requestAdapter.baseUrl!.isEmpty) {
            requestAdapter.baseUrl = 'https://{gatewayHost}';
        }
        pathParameters['baseurl'] = requestAdapter.baseUrl;
    }
}
