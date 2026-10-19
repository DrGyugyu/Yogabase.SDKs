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
        Gets an item from the yogabase.client.v1.projects.item.schedules.item collection
        param schedule_id: The schedule's id: the one it was written under, which a write holds to the pattern of the scheduleId member of the definition (one segment of this path, so there is nothing to escape). The id is the project's, so one that another user holds cannot be created (412). A schedule written before the pattern was enforced under another id is still found by that id, percent-encoded as any path segment is, by a read, a run and a delete; a write of it is refused, so to change it delete it and create it again under an id that follows the pattern.
        Returns: WithScheduleItemRequestBuilder
        """
        if schedule_id is None:
            raise TypeError("schedule_id cannot be null.")
        from .item.with_schedule_item_request_builder import WithScheduleItemRequestBuilder

        url_tpl_params = get_path_parameters(self.path_parameters)
        url_tpl_params["scheduleId"] = schedule_id
        return WithScheduleItemRequestBuilder(self.request_adapter, url_tpl_params)
    

