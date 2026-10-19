from __future__ import annotations
from collections.abc import Callable
from kiota_abstractions.base_request_builder import BaseRequestBuilder
from kiota_abstractions.get_path_parameters import get_path_parameters
from kiota_abstractions.request_adapter import RequestAdapter
from typing import Any, Optional, TYPE_CHECKING, Union

if TYPE_CHECKING:
    from .client_config.client_config_request_builder import ClientConfigRequestBuilder
    from .data.data_request_builder import DataRequestBuilder
    from .projects.projects_request_builder import ProjectsRequestBuilder
    from .servers.servers_request_builder import ServersRequestBuilder
    from .storage.storage_request_builder import StorageRequestBuilder

class V1RequestBuilder(BaseRequestBuilder):
    """
    Builds and executes requests for operations under /v1
    """
    def __init__(self,request_adapter: RequestAdapter, path_parameters: Union[str, dict[str, Any]]) -> None:
        """
        Instantiates a new V1RequestBuilder and sets the default values.
        param path_parameters: The raw url or the url-template parameters for the request.
        param request_adapter: The request adapter to use to execute the requests.
        Returns: None
        """
        super().__init__(request_adapter, "{+baseurl}/v1", path_parameters)
    
    @property
    def client_config(self) -> ClientConfigRequestBuilder:
        """
        The clientConfig property
        """
        from .client_config.client_config_request_builder import ClientConfigRequestBuilder

        return ClientConfigRequestBuilder(self.request_adapter, self.path_parameters)
    
    @property
    def data(self) -> DataRequestBuilder:
        """
        The data property
        """
        from .data.data_request_builder import DataRequestBuilder

        return DataRequestBuilder(self.request_adapter, self.path_parameters)
    
    @property
    def projects(self) -> ProjectsRequestBuilder:
        """
        The projects property
        """
        from .projects.projects_request_builder import ProjectsRequestBuilder

        return ProjectsRequestBuilder(self.request_adapter, self.path_parameters)
    
    @property
    def servers(self) -> ServersRequestBuilder:
        """
        The servers property
        """
        from .servers.servers_request_builder import ServersRequestBuilder

        return ServersRequestBuilder(self.request_adapter, self.path_parameters)
    
    @property
    def storage(self) -> StorageRequestBuilder:
        """
        The storage property
        """
        from .storage.storage_request_builder import StorageRequestBuilder

        return StorageRequestBuilder(self.request_adapter, self.path_parameters)
    

