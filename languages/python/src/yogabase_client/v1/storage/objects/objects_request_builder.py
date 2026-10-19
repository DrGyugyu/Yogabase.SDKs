from __future__ import annotations
from collections.abc import Callable
from kiota_abstractions.base_request_builder import BaseRequestBuilder
from kiota_abstractions.get_path_parameters import get_path_parameters
from kiota_abstractions.request_adapter import RequestAdapter
from typing import Any, Optional, TYPE_CHECKING, Union

if TYPE_CHECKING:
    from .item.with_key_item_request_builder import WithKeyItemRequestBuilder

class ObjectsRequestBuilder(BaseRequestBuilder):
    """
    Builds and executes requests for operations under /v1/storage/objects
    """
    def __init__(self,request_adapter: RequestAdapter, path_parameters: Union[str, dict[str, Any]]) -> None:
        """
        Instantiates a new ObjectsRequestBuilder and sets the default values.
        param path_parameters: The raw url or the url-template parameters for the request.
        param request_adapter: The request adapter to use to execute the requests.
        Returns: None
        """
        super().__init__(request_adapter, "{+baseurl}/v1/storage/objects", path_parameters)
    
    def by_key(self,key: str) -> WithKeyItemRequestBuilder:
        """
        Gets an item from the yogabase.client.v1.storage.objects.item collection
        param key: Project-local logical key. The live route is a catch-all path tail; SDKs must validate the key and percent-encode each segment separately while preserving literal slash separators.
        Returns: WithKeyItemRequestBuilder
        """
        if key is None:
            raise TypeError("key cannot be null.")
        from .item.with_key_item_request_builder import WithKeyItemRequestBuilder

        url_tpl_params = get_path_parameters(self.path_parameters)
        url_tpl_params["key"] = key
        return WithKeyItemRequestBuilder(self.request_adapter, url_tpl_params)
    

