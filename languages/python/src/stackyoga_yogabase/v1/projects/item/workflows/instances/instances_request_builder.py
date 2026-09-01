from __future__ import annotations
from collections.abc import Callable
from kiota_abstractions.base_request_builder import BaseRequestBuilder
from kiota_abstractions.get_path_parameters import get_path_parameters
from kiota_abstractions.request_adapter import RequestAdapter
from typing import Any, Optional, TYPE_CHECKING, Union
from uuid import UUID

if TYPE_CHECKING:
    from .item.with_workflow_instance_item_request_builder import WithWorkflowInstanceItemRequestBuilder

class InstancesRequestBuilder(BaseRequestBuilder):
    """
    Builds and executes requests for operations under /v1/projects/{projectId}/workflows/instances
    """
    def __init__(self,request_adapter: RequestAdapter, path_parameters: Union[str, dict[str, Any]]) -> None:
        """
        Instantiates a new InstancesRequestBuilder and sets the default values.
        param path_parameters: The raw url or the url-template parameters for the request.
        param request_adapter: The request adapter to use to execute the requests.
        Returns: None
        """
        super().__init__(request_adapter, "{+baseurl}/v1/projects/{projectId}/workflows/instances", path_parameters)
    
    def by_workflow_instance_id(self,workflow_instance_id: UUID) -> WithWorkflowInstanceItemRequestBuilder:
        """
        Gets an item from the stackyoga.yogabase.v1.projects.item.workflows.instances.item collection
        param workflow_instance_id: Unique identifier of the item
        Returns: WithWorkflowInstanceItemRequestBuilder
        """
        if workflow_instance_id is None:
            raise TypeError("workflow_instance_id cannot be null.")
        from .item.with_workflow_instance_item_request_builder import WithWorkflowInstanceItemRequestBuilder

        url_tpl_params = get_path_parameters(self.path_parameters)
        url_tpl_params["workflowInstanceId"] = workflow_instance_id
        return WithWorkflowInstanceItemRequestBuilder(self.request_adapter, url_tpl_params)
    

