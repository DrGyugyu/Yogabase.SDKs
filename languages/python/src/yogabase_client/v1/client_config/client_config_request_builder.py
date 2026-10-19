from __future__ import annotations
from collections.abc import Callable
from kiota_abstractions.base_request_builder import BaseRequestBuilder
from kiota_abstractions.get_path_parameters import get_path_parameters
from kiota_abstractions.request_adapter import RequestAdapter
from typing import Any, Optional, TYPE_CHECKING, Union

if TYPE_CHECKING:
    from .item.with_publishable_key_item_request_builder import WithPublishableKeyItemRequestBuilder

class ClientConfigRequestBuilder(BaseRequestBuilder):
    """
    Builds and executes requests for operations under /v1/client-config
    """
    def __init__(self,request_adapter: RequestAdapter, path_parameters: Union[str, dict[str, Any]]) -> None:
        """
        Instantiates a new ClientConfigRequestBuilder and sets the default values.
        param path_parameters: The raw url or the url-template parameters for the request.
        param request_adapter: The request adapter to use to execute the requests.
        Returns: None
        """
        super().__init__(request_adapter, "{+baseurl}/v1/client-config", path_parameters)
    
    def by_publishable_key(self,publishable_key: str) -> WithPublishableKeyItemRequestBuilder:
        """
        Gets an item from the yogabase.client.v1.clientConfig.item collection
        param publishable_key: Unique identifier of the item
        Returns: WithPublishableKeyItemRequestBuilder
        """
        if publishable_key is None:
            raise TypeError("publishable_key cannot be null.")
        from .item.with_publishable_key_item_request_builder import WithPublishableKeyItemRequestBuilder

        url_tpl_params = get_path_parameters(self.path_parameters)
        url_tpl_params["publishableKey"] = publishable_key
        return WithPublishableKeyItemRequestBuilder(self.request_adapter, url_tpl_params)
    

