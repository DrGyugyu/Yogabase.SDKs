from __future__ import annotations
from collections.abc import Callable
from kiota_abstractions.base_request_builder import BaseRequestBuilder
from kiota_abstractions.get_path_parameters import get_path_parameters
from kiota_abstractions.request_adapter import RequestAdapter
from typing import Any, Optional, TYPE_CHECKING, Union

if TYPE_CHECKING:
    from .item.with_schedule_item_request_builder import WithScheduleItemRequestBuilder

class SchedulesRequestBuilder(BaseRequestBuilder):
    """
    Builds and executes requests for operations under /v1/projects/{projectId}/schedules
    """
    def __init__(self,request_adapter: RequestAdapter, path_parameters: Union[str, dict[str, Any]]) -> None:
        """
        Instantiates a new SchedulesRequestBuilder and sets the default values.
        param path_parameters: The raw url or the url-template parameters for the request.
        param request_adapter: The request adapter to use to execute the requests.
        Returns: None
        """
        super().__init__(request_adapter, "{+baseurl}/v1/projects/{projectId}/schedules", path_parameters)
    
    def by_schedule_id(self,schedule_id: str) -> WithScheduleItemRequestBuilder:
        """
        Gets an item from the stackyoga.yogabase.v1.projects.item.schedules.item collection
        param schedule_id: The schedule's id, chosen by the caller: 1 to 128 characters, trimmed, with no control character. The id is the project's, so one that another user holds cannot be created (412).
        Returns: WithScheduleItemRequestBuilder
        """
        if schedule_id is None:
            raise TypeError("schedule_id cannot be null.")
        from .item.with_schedule_item_request_builder import WithScheduleItemRequestBuilder

        url_tpl_params = get_path_parameters(self.path_parameters)
        url_tpl_params["scheduleId"] = schedule_id
        return WithScheduleItemRequestBuilder(self.request_adapter, url_tpl_params)
    

