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
    from .......models.problem import Problem
    from .......models.schedule_trigger_request import ScheduleTriggerRequest
    from .......models.schedule_trigger_result import ScheduleTriggerResult

class TriggerRequestBuilder(BaseRequestBuilder):
    """
    Builds and executes requests for operations under /v1/projects/{projectId}/schedules/{scheduleId}/trigger
    """
    def __init__(self,request_adapter: RequestAdapter, path_parameters: Union[str, dict[str, Any]]) -> None:
        """
        Instantiates a new TriggerRequestBuilder and sets the default values.
        param path_parameters: The raw url or the url-template parameters for the request.
        param request_adapter: The request adapter to use to execute the requests.
        Returns: None
        """
        super().__init__(request_adapter, "{+baseurl}/v1/projects/{projectId}/schedules/{scheduleId}/trigger", path_parameters)
    
    async def post(self,body: ScheduleTriggerRequest, request_configuration: Optional[RequestConfiguration[QueryParameters]] = None) -> Optional[ScheduleTriggerResult]:
        """
        Runs the caller's own schedule once now, outside its cadence: it enqueues one invocation of the schedule's function with the schedule's payload, as the schedule's owner. The ETag last read is required in If-Match (428 without it, 412 when it is stale), and the idempotency key makes the request repeatable: the same key against the same version answers the same invocation, 202 again with outcome 1 where the first said 0. The deadline is when the invocation must finish: after now and at most an hour from now, else 400. A schedule that does not exist or is another user's is a bare 404. A schedule whose function version is no longer an admitted Stateless one of the project is a bare 409, as a write of it is. A run is not an occurrence of the schedule: its concurrencyPolicy neither holds it back nor is held back by it.
        param body: The request body
        param request_configuration: Configuration for the request such as headers, query parameters, and middleware options.
        Returns: Optional[ScheduleTriggerResult]
        """
        if body is None:
            raise TypeError("body cannot be null.")
        request_info = self.to_post_request_information(
            body, request_configuration
        )
        from .......models.problem import Problem

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
        from .......models.schedule_trigger_result import ScheduleTriggerResult

        return await self.request_adapter.send_async(request_info, ScheduleTriggerResult, error_mapping)
    
    def to_post_request_information(self,body: ScheduleTriggerRequest, request_configuration: Optional[RequestConfiguration[QueryParameters]] = None) -> RequestInformation:
        """
        Runs the caller's own schedule once now, outside its cadence: it enqueues one invocation of the schedule's function with the schedule's payload, as the schedule's owner. The ETag last read is required in If-Match (428 without it, 412 when it is stale), and the idempotency key makes the request repeatable: the same key against the same version answers the same invocation, 202 again with outcome 1 where the first said 0. The deadline is when the invocation must finish: after now and at most an hour from now, else 400. A schedule that does not exist or is another user's is a bare 404. A schedule whose function version is no longer an admitted Stateless one of the project is a bare 409, as a write of it is. A run is not an occurrence of the schedule: its concurrencyPolicy neither holds it back nor is held back by it.
        param body: The request body
        param request_configuration: Configuration for the request such as headers, query parameters, and middleware options.
        Returns: RequestInformation
        """
        if body is None:
            raise TypeError("body cannot be null.")
        request_info = RequestInformation(Method.POST, self.url_template, self.path_parameters)
        request_info.configure(request_configuration)
        request_info.headers.try_add("Accept", "application/json")
        request_info.set_content_from_parsable(self.request_adapter, "application/json", body)
        return request_info
    
    def with_url(self,raw_url: str) -> TriggerRequestBuilder:
        """
        Returns a request builder with the provided arbitrary URL. Using this method means any other path or query parameters are ignored.
        param raw_url: The raw URL to use for the request builder.
        Returns: TriggerRequestBuilder
        """
        if raw_url is None:
            raise TypeError("raw_url cannot be null.")
        return TriggerRequestBuilder(self.request_adapter, raw_url)
    
    @dataclass
    class TriggerRequestBuilderPostRequestConfiguration(RequestConfiguration[QueryParameters]):
        """
        Configuration for the request such as headers, query parameters, and middleware options.
        """
        warn("This class is deprecated. Please use the generic RequestConfiguration class generated by the generator.", DeprecationWarning)
    

