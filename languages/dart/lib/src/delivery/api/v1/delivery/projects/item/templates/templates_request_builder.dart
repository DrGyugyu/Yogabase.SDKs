// ignore_for_file: type=lint
import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:uuid/uuid.dart';
import '../../../../../../models/delivery_template_list.dart';
import '../../../../../../models/problem_details.dart';
import './item/with_template_item_request_builder.dart';
import './templates_request_builder_get_query_parameters.dart';

/// auto generated
/// Builds and executes requests for operations under \api\v1\delivery\projects\{projectId}\templates
class TemplatesRequestBuilder extends BaseRequestBuilder<TemplatesRequestBuilder> {
    /// Gets an item from the yogabase_client_delivery.api.v1.delivery.projects.item.templates.item collection
    ///  [templateId] Unique identifier of the item
    WithTemplateItemRequestBuilder byTemplateId(UuidValue templateId) {
        var urlTplParams = Map.of(pathParameters);
        urlTplParams.putIfAbsent('templateId', () => templateId);
        return WithTemplateItemRequestBuilder(urlTplParams, requestAdapter);
    }
    /// Clones the requestbuilder.
    @override
    TemplatesRequestBuilder clone() {
        return TemplatesRequestBuilder(pathParameters, requestAdapter);
    }
    /// Instantiates a new [TemplatesRequestBuilder] and sets the default values.
    ///  [pathParameters] Path parameters for the request
    ///  [requestAdapter] The request adapter to use to execute the requests.
    TemplatesRequestBuilder(Map<String, dynamic> pathParameters, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/api/v1/delivery/projects/{projectId}/templates{?cursor*,limit*}", pathParameters) ;
    /// Instantiates a new [TemplatesRequestBuilder] and sets the default values.
    ///  [rawUrl] The raw URL to use for the request builder.
    ///  [requestAdapter] The request adapter to use to execute the requests.
    TemplatesRequestBuilder.withUrl(String rawUrl, RequestAdapter requestAdapter) : super(requestAdapter, "{+baseurl}/api/v1/delivery/projects/{projectId}/templates{?cursor*,limit*}", {RequestInformation.rawUrlKey : rawUrl}) ;
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    Future<DeliveryTemplateList?> getAsync([void Function(RequestConfiguration<TemplatesRequestBuilderGetQueryParameters>)? requestConfiguration]) async {
        var requestInfo = toGetRequestInformation(requestConfiguration);
        final errorMapping = <String, ParsableFactory<Parsable>>{
            '401' :  ProblemDetails.createFromDiscriminatorValue,
            '404' :  ProblemDetails.createFromDiscriminatorValue,
            '429' :  ProblemDetails.createFromDiscriminatorValue,
        };
        return await requestAdapter.send<DeliveryTemplateList>(requestInfo, DeliveryTemplateList.createFromDiscriminatorValue, errorMapping);
    }
    ///  [requestConfiguration] Configuration for the request such as headers, query parameters, and middleware options.
    RequestInformation toGetRequestInformation([void Function(RequestConfiguration<TemplatesRequestBuilderGetQueryParameters>)? requestConfiguration]) {
        var requestInfo = RequestInformation(httpMethod : HttpMethod.get, urlTemplate : urlTemplate, pathParameters :  pathParameters);
        requestInfo.configure<TemplatesRequestBuilderGetQueryParameters>(requestConfiguration, () => TemplatesRequestBuilderGetQueryParameters());
        requestInfo.headers.put('Accept', 'application/json');
        return requestInfo;
    }
}
