from __future__ import annotations
from collections.abc import Callable
from kiota_abstractions.base_request_builder import BaseRequestBuilder
from kiota_abstractions.get_path_parameters import get_path_parameters
from kiota_abstractions.request_adapter import RequestAdapter
from typing import Any, Optional, TYPE_CHECKING, Union

if TYPE_CHECKING:
    from .item.with_function_item_request_builder import WithFunctionItemRequestBuilder

class FunctionsRequestBuilder(BaseRequestBuilder):
    """
    Builds and executes requests for operations under /v1/projects/{projectId}/functions
    """
    def __init__(self,request_adapter: RequestAdapter, path_parameters: Union[str, dict[str, Any]]) -> None:
        """
        Instantiates a new FunctionsRequestBuilder and sets the default values.
        param path_parameters: The raw url or the url-template parameters for the request.
        param request_adapter: The request adapter to use to execute the requests.
        Returns: None
        """
        super().__init__(request_adapter, "{+baseurl}/v1/projects/{projectId}/functions", path_parameters)
    
    def by_function_id(self,function_id: str) -> WithFunctionItemRequestBuilder:
        """
        Gets an item from the yogabase.client.v1.projects.item.functions.item collection
        param function_id: Unique identifier of the item
        Returns: WithFunctionItemRequestBuilder
        """
        if function_id is None:
            raise TypeError("function_id cannot be null.")
        from .item.with_function_item_request_builder import WithFunctionItemRequestBuilder

        url_tpl_params = get_path_parameters(self.path_parameters)
        url_tpl_params["functionId"] = function_id
        return WithFunctionItemRequestBuilder(self.request_adapter, url_tpl_params)
    

