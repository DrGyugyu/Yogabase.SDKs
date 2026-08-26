from __future__ import annotations
from collections.abc import Callable
from kiota_abstractions.base_request_builder import BaseRequestBuilder
from kiota_abstractions.get_path_parameters import get_path_parameters
from kiota_abstractions.request_adapter import RequestAdapter
from typing import Any, Optional, TYPE_CHECKING, Union

if TYPE_CHECKING:
    from .delete.delete_request_builder import DeleteRequestBuilder
    from .insert.insert_request_builder import InsertRequestBuilder
    from .select.select_request_builder import SelectRequestBuilder
    from .update.update_request_builder import UpdateRequestBuilder

class WithResourceItemRequestBuilder(BaseRequestBuilder):
    """
    Builds and executes requests for operations under /v1/data/{resource}
    """
    def __init__(self,request_adapter: RequestAdapter, path_parameters: Union[str, dict[str, Any]]) -> None:
        """
        Instantiates a new WithResourceItemRequestBuilder and sets the default values.
        param path_parameters: The raw url or the url-template parameters for the request.
        param request_adapter: The request adapter to use to execute the requests.
        Returns: None
        """
        super().__init__(request_adapter, "{+baseurl}/v1/data/{resource}", path_parameters)
    
    @property
    def delete_path(self) -> DeleteRequestBuilder:
        """
        The deletePath property
        """
        from .delete.delete_request_builder import DeleteRequestBuilder

        return DeleteRequestBuilder(self.request_adapter, self.path_parameters)
    
    @property
    def insert(self) -> InsertRequestBuilder:
        """
        The insert property
        """
        from .insert.insert_request_builder import InsertRequestBuilder

        return InsertRequestBuilder(self.request_adapter, self.path_parameters)
    
    @property
    def select(self) -> SelectRequestBuilder:
        """
        The select property
        """
        from .select.select_request_builder import SelectRequestBuilder

        return SelectRequestBuilder(self.request_adapter, self.path_parameters)
    
    @property
    def update(self) -> UpdateRequestBuilder:
        """
        The update property
        """
        from .update.update_request_builder import UpdateRequestBuilder

        return UpdateRequestBuilder(self.request_adapter, self.path_parameters)
    

