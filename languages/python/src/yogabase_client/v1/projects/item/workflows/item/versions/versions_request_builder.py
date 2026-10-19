from __future__ import annotations
from collections.abc import Callable
from kiota_abstractions.base_request_builder import BaseRequestBuilder
from kiota_abstractions.get_path_parameters import get_path_parameters
from kiota_abstractions.request_adapter import RequestAdapter
from typing import Any, Optional, TYPE_CHECKING, Union

if TYPE_CHECKING:
    from .item.with_definition_version_item_request_builder import WithDefinitionVersionItemRequestBuilder

class VersionsRequestBuilder(BaseRequestBuilder):
    """
    Builds and executes requests for operations under /v1/projects/{projectId}/workflows/{workflowId}/versions
    """
    def __init__(self,request_adapter: RequestAdapter, path_parameters: Union[str, dict[str, Any]]) -> None:
        """
        Instantiates a new VersionsRequestBuilder and sets the default values.
        param path_parameters: The raw url or the url-template parameters for the request.
        param request_adapter: The request adapter to use to execute the requests.
        Returns: None
        """
        super().__init__(request_adapter, "{+baseurl}/v1/projects/{projectId}/workflows/{workflowId}/versions", path_parameters)
    
    def by_definition_version(self,definition_version: str) -> WithDefinitionVersionItemRequestBuilder:
        """
        Gets an item from the yogabase.client.v1.projects.item.workflows.item.versions.item collection
        param definition_version: Unique identifier of the item
        Returns: WithDefinitionVersionItemRequestBuilder
        """
        if definition_version is None:
            raise TypeError("definition_version cannot be null.")
        from .item.with_definition_version_item_request_builder import WithDefinitionVersionItemRequestBuilder

        url_tpl_params = get_path_parameters(self.path_parameters)
        url_tpl_params["definitionVersion"] = definition_version
        return WithDefinitionVersionItemRequestBuilder(self.request_adapter, url_tpl_params)
    

