from __future__ import annotations
from collections.abc import Callable
from kiota_abstractions.base_request_builder import BaseRequestBuilder
from kiota_abstractions.get_path_parameters import get_path_parameters
from kiota_abstractions.request_adapter import RequestAdapter
from typing import Any, Optional, TYPE_CHECKING, Union

if TYPE_CHECKING:
    from .item.with_provider_item_request_builder import WithProviderItemRequestBuilder

class ExternalRequestBuilder(BaseRequestBuilder):
    """
    Builds and executes requests for operations under /auth/external
    """
    def __init__(self,request_adapter: RequestAdapter, path_parameters: Union[str, dict[str, Any]]) -> None:
        """
        Instantiates a new ExternalRequestBuilder and sets the default values.
        param path_parameters: The raw url or the url-template parameters for the request.
        param request_adapter: The request adapter to use to execute the requests.
        Returns: None
        """
        super().__init__(request_adapter, "{+baseurl}/auth/external", path_parameters)
    
    def by_provider(self,provider: str) -> WithProviderItemRequestBuilder:
        """
        Gets an item from the stackyoga.yogabase.auth.external.item collection
        param provider: Unique identifier of the item
        Returns: WithProviderItemRequestBuilder
        """
        if provider is None:
            raise TypeError("provider cannot be null.")
        from .item.with_provider_item_request_builder import WithProviderItemRequestBuilder

        url_tpl_params = get_path_parameters(self.path_parameters)
        url_tpl_params["provider"] = provider
        return WithProviderItemRequestBuilder(self.request_adapter, url_tpl_params)
    

