from __future__ import annotations
from collections.abc import Callable
from kiota_abstractions.base_request_builder import BaseRequestBuilder
from kiota_abstractions.get_path_parameters import get_path_parameters
from kiota_abstractions.request_adapter import RequestAdapter
from typing import Any, Optional, TYPE_CHECKING, Union

if TYPE_CHECKING:
    from .callback.callback_request_builder import CallbackRequestBuilder
    from .return_.return_request_builder import ReturnRequestBuilder
    from .start.start_request_builder import StartRequestBuilder

class WithProviderItemRequestBuilder(BaseRequestBuilder):
    """
    Builds and executes requests for operations under /auth/external/{provider}
    """
    def __init__(self,request_adapter: RequestAdapter, path_parameters: Union[str, dict[str, Any]]) -> None:
        """
        Instantiates a new WithProviderItemRequestBuilder and sets the default values.
        param path_parameters: The raw url or the url-template parameters for the request.
        param request_adapter: The request adapter to use to execute the requests.
        Returns: None
        """
        super().__init__(request_adapter, "{+baseurl}/auth/external/{provider}", path_parameters)
    
    @property
    def callback(self) -> CallbackRequestBuilder:
        """
        The callback property
        """
        from .callback.callback_request_builder import CallbackRequestBuilder

        return CallbackRequestBuilder(self.request_adapter, self.path_parameters)
    
    @property
    def return_(self) -> ReturnRequestBuilder:
        """
        The return property
        """
        from .return_.return_request_builder import ReturnRequestBuilder

        return ReturnRequestBuilder(self.request_adapter, self.path_parameters)
    
    @property
    def start(self) -> StartRequestBuilder:
        """
        The start property
        """
        from .start.start_request_builder import StartRequestBuilder

        return StartRequestBuilder(self.request_adapter, self.path_parameters)
    

