from __future__ import annotations
from collections.abc import Callable
from kiota_abstractions.base_request_builder import BaseRequestBuilder
from kiota_abstractions.get_path_parameters import get_path_parameters
from kiota_abstractions.request_adapter import RequestAdapter
from typing import Any, Optional, TYPE_CHECKING, Union

if TYPE_CHECKING:
    from .item.with_resource_item_request_builder import WithResourceItemRequestBuilder

class DataRequestBuilder(BaseRequestBuilder):
    """
    Builds and executes requests for operations under /v1/data
    """
    def __init__(self,request_adapter: RequestAdapter, path_parameters: Union[str, dict[str, Any]]) -> None:
        """
        Instantiates a new DataRequestBuilder and sets the default values.
        param path_parameters: The raw url or the url-template parameters for the request.
        param request_adapter: The request adapter to use to execute the requests.
        Returns: None
        """
        super().__init__(request_adapter, "{+baseurl}/v1/data", path_parameters)
    
    def by_resource(self,resource: str) -> WithResourceItemRequestBuilder:
        """
        Gets an item from the stackyoga.yogabase.v1.data.item collection
        param resource: Unique identifier of the item
        Returns: WithResourceItemRequestBuilder
        """
        if resource is None:
            raise TypeError("resource cannot be null.")
        from .item.with_resource_item_request_builder import WithResourceItemRequestBuilder

        url_tpl_params = get_path_parameters(self.path_parameters)
        url_tpl_params["resource"] = resource
        return WithResourceItemRequestBuilder(self.request_adapter, url_tpl_params)
    

