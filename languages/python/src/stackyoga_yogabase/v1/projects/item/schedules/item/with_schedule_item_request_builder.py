from __future__ import annotations
from collections.abc import Callable
from dataclasses import dataclass, field
from kiota_abstractions.base_request_builder import BaseRequestBuilder
from kiota_abstractions.base_request_configuration import RequestConfiguration
from kiota_abstractions.default_query_parameters import QueryParameters
from kiota_abstractions.get_path_parameters import get_path_parameters
from kiota_abstractions.method import Method
from kiota_abstractions.request_adapter import RequestAdapter
from kiota_abstractions.request_information import RequestInformation
from kiota_abstractions.request_option import RequestOption
from kiota_abstractions.serialization import Parsable, ParsableFactory
from typing import Any, Optional, TYPE_CHECKING, Union
from warnings import warn

if TYPE_CHECKING:
    from ......models.problem import Problem
    from ......models.schedule import Schedule
    from ......models.schedule_definition_request import ScheduleDefinitionRequest
    from .trigger.trigger_request_builder import TriggerRequestBuilder

class WithScheduleItemRequestBuilder(BaseRequestBuilder):
    """
    Builds and executes requests for operations under /v1/projects/{projectId}/schedules/{scheduleId}
    """
    def __init__(self,request_adapter: RequestAdapter, path_parameters: Union[str, dict[str, Any]]) -> None:
        """
        Instantiates a new WithScheduleItemRequestBuilder and sets the default values.
        param path_parameters: The raw url or the url-template parameters for the request.
        param request_adapter: The request adapter to use to execute the requests.
        Returns: None
        """
        super().__init__(request_adapter, "{+baseurl}/v1/projects/{projectId}/schedules/{scheduleId}", path_parameters)
    
    async def delete(self,request_configuration: Optional[RequestConfiguration[QueryParameters]] = None) -> None:
        """
        Deletes the caller's own schedule, 204. The ETag last read is required in If-Match: 428 without it, 412 when it is stale. A schedule that does not exist or is another user's is a bare 404, to a request whose ETag is well formed whatever version it names.
        param request_configuration: Configuration for the request such as headers, query parameters, and middleware options.
        Returns: None
        """
        request_info = self.to_delete_request_information(
            request_configuration
        )
        from ......models.problem import Problem

        error_mapping: dict[str, type[ParsableFactory]] = {
            "400": Problem,
            "401": Problem,
            "403": Problem,
            "412": Problem,
            "428": Problem,
            "503": Problem,
        }
        if not self.request_adapter:
            raise Exception("Http core is null") 
        return await self.request_adapter.send_no_response_content_async(request_info, error_mapping)
    
    async def get(self,request_configuration: Optional[RequestConfiguration[QueryParameters]] = None) -> Optional[Schedule]:
        """
        Reads the caller's own schedule, with the ETag to send as If-Match to change, delete or run it. A schedule that does not exist and one that is another user's are the same bare 404.
        param request_configuration: Configuration for the request such as headers, query parameters, and middleware options.
        Returns: Optional[Schedule]
        """
        request_info = self.to_get_request_information(
            request_configuration
        )
        from ......models.problem import Problem

        error_mapping: dict[str, type[ParsableFactory]] = {
            "400": Problem,
            "401": Problem,
            "403": Problem,
            "503": Problem,
        }
        if not self.request_adapter:
            raise Exception("Http core is null") 
        from ......models.schedule import Schedule

        return await self.request_adapter.send_async(request_info, Schedule, error_mapping)
    
    async def put(self,body: ScheduleDefinitionRequest, request_configuration: Optional[RequestConfiguration[QueryParameters]] = None) -> Optional[Schedule]:
        """
        Creates or updates the schedule scheduleId as the signed-in user, who then owns it: only the owner reads, changes, deletes or runs it. Create with If-None-Match: * (201); update with the ETag last read as If-Match (200); exactly one of the two, and 428 when neither is sent. The answer is the schedule with its new ETag. The id in the path must be the id in the body (else a 400 whose problem says so), and must follow the pattern of the definition's scheduleId member (else a 400 whose problem names the member and the rule). A schedule is recurring (cron, in timeZone) or a one-shot (oneShotAtUtc: it runs once and is then deleted), and an update cannot turn one into the other (409). Its function version must be an admitted Stateless Lambda version of the project, else 409. A request body that cannot be read as one JSON object of exactly the documented members is a bare 400, and one that is not JSON a bare 415; a member that is missing, out of range or invalid (a cron expression that does not parse, an unknown time zone, a payload that is not base64) is a 400 with a problem body; a missing misfirePolicy or concurrencyPolicy is refused like any other, and its problem names the member and the names it may take in its detail. A payload over the limit is 413 schedule_payload_too_large.
        param body: The request body
        param request_configuration: Configuration for the request such as headers, query parameters, and middleware options.
        Returns: Optional[Schedule]
        """
        if body is None:
            raise TypeError("body cannot be null.")
        request_info = self.to_put_request_information(
            body, request_configuration
        )
        from ......models.problem import Problem

        error_mapping: dict[str, type[ParsableFactory]] = {
            "400": Problem,
            "401": Problem,
            "403": Problem,
            "412": Problem,
            "413": Problem,
            "428": Problem,
            "503": Problem,
        }
        if not self.request_adapter:
            raise Exception("Http core is null") 
        from ......models.schedule import Schedule

        return await self.request_adapter.send_async(request_info, Schedule, error_mapping)
    
    def to_delete_request_information(self,request_configuration: Optional[RequestConfiguration[QueryParameters]] = None) -> RequestInformation:
        """
        Deletes the caller's own schedule, 204. The ETag last read is required in If-Match: 428 without it, 412 when it is stale. A schedule that does not exist or is another user's is a bare 404, to a request whose ETag is well formed whatever version it names.
        param request_configuration: Configuration for the request such as headers, query parameters, and middleware options.
        Returns: RequestInformation
        """
        request_info = RequestInformation(Method.DELETE, self.url_template, self.path_parameters)
        request_info.configure(request_configuration)
        request_info.headers.try_add("Accept", "application/problem+json")
        return request_info
    
    def to_get_request_information(self,request_configuration: Optional[RequestConfiguration[QueryParameters]] = None) -> RequestInformation:
        """
        Reads the caller's own schedule, with the ETag to send as If-Match to change, delete or run it. A schedule that does not exist and one that is another user's are the same bare 404.
        param request_configuration: Configuration for the request such as headers, query parameters, and middleware options.
        Returns: RequestInformation
        """
        request_info = RequestInformation(Method.GET, self.url_template, self.path_parameters)
        request_info.configure(request_configuration)
        request_info.headers.try_add("Accept", "application/json")
        return request_info
    
    def to_put_request_information(self,body: ScheduleDefinitionRequest, request_configuration: Optional[RequestConfiguration[QueryParameters]] = None) -> RequestInformation:
        """
        Creates or updates the schedule scheduleId as the signed-in user, who then owns it: only the owner reads, changes, deletes or runs it. Create with If-None-Match: * (201); update with the ETag last read as If-Match (200); exactly one of the two, and 428 when neither is sent. The answer is the schedule with its new ETag. The id in the path must be the id in the body (else a 400 whose problem says so), and must follow the pattern of the definition's scheduleId member (else a 400 whose problem names the member and the rule). A schedule is recurring (cron, in timeZone) or a one-shot (oneShotAtUtc: it runs once and is then deleted), and an update cannot turn one into the other (409). Its function version must be an admitted Stateless Lambda version of the project, else 409. A request body that cannot be read as one JSON object of exactly the documented members is a bare 400, and one that is not JSON a bare 415; a member that is missing, out of range or invalid (a cron expression that does not parse, an unknown time zone, a payload that is not base64) is a 400 with a problem body; a missing misfirePolicy or concurrencyPolicy is refused like any other, and its problem names the member and the names it may take in its detail. A payload over the limit is 413 schedule_payload_too_large.
        param body: The request body
        param request_configuration: Configuration for the request such as headers, query parameters, and middleware options.
        Returns: RequestInformation
        """
        if body is None:
            raise TypeError("body cannot be null.")
        request_info = RequestInformation(Method.PUT, self.url_template, self.path_parameters)
        request_info.configure(request_configuration)
        request_info.headers.try_add("Accept", "application/json")
        request_info.set_content_from_parsable(self.request_adapter, "application/json", body)
        return request_info
    
    def with_url(self,raw_url: str) -> WithScheduleItemRequestBuilder:
        """
        Returns a request builder with the provided arbitrary URL. Using this method means any other path or query parameters are ignored.
        param raw_url: The raw URL to use for the request builder.
        Returns: WithScheduleItemRequestBuilder
        """
        if raw_url is None:
            raise TypeError("raw_url cannot be null.")
        return WithScheduleItemRequestBuilder(self.request_adapter, raw_url)
    
    @property
    def trigger(self) -> TriggerRequestBuilder:
        """
        The trigger property
        """
        from .trigger.trigger_request_builder import TriggerRequestBuilder

        return TriggerRequestBuilder(self.request_adapter, self.path_parameters)
    
    @dataclass
    class WithScheduleItemRequestBuilderDeleteRequestConfiguration(RequestConfiguration[QueryParameters]):
        """
        Configuration for the request such as headers, query parameters, and middleware options.
        """
        warn("This class is deprecated. Please use the generic RequestConfiguration class generated by the generator.", DeprecationWarning)
    
    @dataclass
    class WithScheduleItemRequestBuilderGetRequestConfiguration(RequestConfiguration[QueryParameters]):
        """
        Configuration for the request such as headers, query parameters, and middleware options.
        """
        warn("This class is deprecated. Please use the generic RequestConfiguration class generated by the generator.", DeprecationWarning)
    
    @dataclass
    class WithScheduleItemRequestBuilderPutRequestConfiguration(RequestConfiguration[QueryParameters]):
        """
        Configuration for the request such as headers, query parameters, and middleware options.
        """
        warn("This class is deprecated. Please use the generic RequestConfiguration class generated by the generator.", DeprecationWarning)
    

