from __future__ import annotations
from collections.abc import Callable
from kiota_abstractions.base_request_builder import BaseRequestBuilder
from kiota_abstractions.get_path_parameters import get_path_parameters
from kiota_abstractions.request_adapter import RequestAdapter
from typing import Any, Optional, TYPE_CHECKING, Union

if TYPE_CHECKING:
    from .regenerate.regenerate_request_builder import RegenerateRequestBuilder
    from .use.use_request_builder import UseRequestBuilder

class RecoveryRequestBuilder(BaseRequestBuilder):
    """
    Builds and executes requests for operations under /auth/mfa/recovery
    """
    def __init__(self,request_adapter: RequestAdapter, path_parameters: Union[str, dict[str, Any]]) -> None:
        """
        Instantiates a new RecoveryRequestBuilder and sets the default values.
        param path_parameters: The raw url or the url-template parameters for the request.
        param request_adapter: The request adapter to use to execute the requests.
        Returns: None
        """
        super().__init__(request_adapter, "{+baseurl}/auth/mfa/recovery", path_parameters)
    
    @property
    def regenerate(self) -> RegenerateRequestBuilder:
        """
        The regenerate property
        """
        from .regenerate.regenerate_request_builder import RegenerateRequestBuilder

        return RegenerateRequestBuilder(self.request_adapter, self.path_parameters)
    
    @property
    def use(self) -> UseRequestBuilder:
        """
        The use property
        """
        from .use.use_request_builder import UseRequestBuilder

        return UseRequestBuilder(self.request_adapter, self.path_parameters)
    

