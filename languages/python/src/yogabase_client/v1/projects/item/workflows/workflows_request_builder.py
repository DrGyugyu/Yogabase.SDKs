from __future__ import annotations
from collections.abc import Callable
from kiota_abstractions.base_request_builder import BaseRequestBuilder
from kiota_abstractions.get_path_parameters import get_path_parameters
from kiota_abstractions.request_adapter import RequestAdapter
from typing import Any, Optional, TYPE_CHECKING, Union

if TYPE_CHECKING:
    from .instances.instances_request_builder import InstancesRequestBuilder
    from .item.with_workflow_item_request_builder import WithWorkflowItemRequestBuilder

class WorkflowsRequestBuilder(BaseRequestBuilder):
    """
    Builds and executes requests for operations under /v1/projects/{projectId}/workflows
    """
    def __init__(self,request_adapter: RequestAdapter, path_parameters: Union[str, dict[str, Any]]) -> None:
        """
        Instantiates a new WorkflowsRequestBuilder and sets the default values.
        param path_parameters: The raw url or the url-template parameters for the request.
        param request_adapter: The request adapter to use to execute the requests.
        Returns: None
        """
        super().__init__(request_adapter, "{+baseurl}/v1/projects/{projectId}/workflows", path_parameters)
    
    def by_workflow_id(self,workflow_id: str) -> WithWorkflowItemRequestBuilder:
        """
        Gets an item from the yogabase.client.v1.projects.item.workflows.item collection
        param workflow_id: Unique identifier of the item
        Returns: WithWorkflowItemRequestBuilder
        """
        if workflow_id is None:
            raise TypeError("workflow_id cannot be null.")
        from .item.with_workflow_item_request_builder import WithWorkflowItemRequestBuilder

        url_tpl_params = get_path_parameters(self.path_parameters)
        url_tpl_params["workflowId"] = workflow_id
        return WithWorkflowItemRequestBuilder(self.request_adapter, url_tpl_params)
    
    @property
    def instances(self) -> InstancesRequestBuilder:
        """
        The instances property
        """
        from .instances.instances_request_builder import InstancesRequestBuilder

        return InstancesRequestBuilder(self.request_adapter, self.path_parameters)
    

