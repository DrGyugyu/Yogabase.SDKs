from __future__ import annotations
from collections.abc import Callable
from kiota_abstractions.base_request_builder import BaseRequestBuilder
from kiota_abstractions.get_path_parameters import get_path_parameters
from kiota_abstractions.request_adapter import RequestAdapter
from typing import Any, Optional, TYPE_CHECKING, Union

if TYPE_CHECKING:
    from .item.with_instance_key_item_request_builder import WithInstanceKeyItemRequestBuilder

class InstancesRequestBuilder(BaseRequestBuilder):
    """
    Builds and executes requests for operations under /v1/projects/{projectId}/actors/{functionId}/versions/{functionVersion}/instances
    """
    def __init__(self,request_adapter: RequestAdapter, path_parameters: Union[str, dict[str, Any]]) -> None:
        """
        Instantiates a new InstancesRequestBuilder and sets the default values.
        param path_parameters: The raw url or the url-template parameters for the request.
        param request_adapter: The request adapter to use to execute the requests.
        Returns: None
        """
        super().__init__(request_adapter, "{+baseurl}/v1/projects/{projectId}/actors/{functionId}/versions/{functionVersion}/instances", path_parameters)
    
    def by_instance_key(self,instance_key: str) -> WithInstanceKeyItemRequestBuilder:
        """
        Gets an item from the yogabase.client.v1.projects.item.actors.item.versions.item.instances.item collection
        param instance_key: Unique identifier of the item
        Returns: WithInstanceKeyItemRequestBuilder
        """
        if instance_key is None:
            raise TypeError("instance_key cannot be null.")
        from .item.with_instance_key_item_request_builder import WithInstanceKeyItemRequestBuilder

        url_tpl_params = get_path_parameters(self.path_parameters)
        url_tpl_params["instanceKey"] = instance_key
        return WithInstanceKeyItemRequestBuilder(self.request_adapter, url_tpl_params)
    

