from __future__ import annotations
from collections.abc import Callable
from dataclasses import dataclass, field
from kiota_abstractions.base_request_builder import BaseRequestBuilder
from kiota_abstractions.base_request_configuration import RequestConfiguration
from kiota_abstractions.default_query_parameters import QueryParameters
from kiota_abstractions.get_path_parameters import get_path_parameters
from kiota_abstractions.method import Method
from kiota_abstractions.request_adapter import RequestAdapter
from kiota_abstractions.request_information import RequestInformation
from kiota_abstractions.request_option import RequestOption
from kiota_abstractions.serialization import Parsable, ParsableFactory
from typing import Any, Optional, TYPE_CHECKING, Union
from warnings import warn

if TYPE_CHECKING:
    from ...models.game_server import GameServer
    from ...models.problem import Problem
    from .item.with_server_item_request_builder import WithServerItemRequestBuilder
    from .start.start_request_builder import StartRequestBuilder

class ServersRequestBuilder(BaseRequestBuilder):
    """
    Builds and executes requests for operations under /v1/servers
    """
    def __init__(self,request_adapter: RequestAdapter, path_parameters: Union[str, dict[str, Any]]) -> None:
        """
        Instantiates a new ServersRequestBuilder and sets the default values.
        param path_parameters: The raw url or the url-template parameters for the request.
        param request_adapter: The request adapter to use to execute the requests.
        Returns: None
        """
        super().__init__(request_adapter, "{+baseurl}/v1/servers", path_parameters)
    
    def by_server_id(self,server_id: str) -> WithServerItemRequestBuilder:
        """
        Gets an item from the yogabase.client.v1.servers.item collection
        param server_id: Opaque game-server instance identifier, as returned by start or list.
        Returns: WithServerItemRequestBuilder
        """
        if server_id is None:
            raise TypeError("server_id cannot be null.")
        from .item.with_server_item_request_builder import WithServerItemRequestBuilder

        url_tpl_params = get_path_parameters(self.path_parameters)
        url_tpl_params["serverId"] = server_id
        return WithServerItemRequestBuilder(self.request_adapter, url_tpl_params)
    
    async def get(self,request_configuration: Optional[RequestConfiguration[QueryParameters]] = None) -> Optional[list[GameServer]]:
        """
        Every instance of the caller's environment as a bare array, ordered by server id. An instance whose address no member has observed is listed with an empty networkPorts rather than omitted or given a composed host.
        param request_configuration: Configuration for the request such as headers, query parameters, and middleware options.
        Returns: Optional[list[GameServer]]
        """
        request_info = self.to_get_request_information(
            request_configuration
        )
        from ...models.problem import Problem

        error_mapping: dict[str, type[ParsableFactory]] = {
            "401": Problem,
            "503": Problem,
        }
        if not self.request_adapter:
            raise Exception("Http core is null") 
        from ...models.game_server import GameServer

        return await self.request_adapter.send_collection_async(request_info, GameServer, error_mapping)
    
    def to_get_request_information(self,request_configuration: Optional[RequestConfiguration[QueryParameters]] = None) -> RequestInformation:
        """
        Every instance of the caller's environment as a bare array, ordered by server id. An instance whose address no member has observed is listed with an empty networkPorts rather than omitted or given a composed host.
        param request_configuration: Configuration for the request such as headers, query parameters, and middleware options.
        Returns: RequestInformation
        """
        request_info = RequestInformation(Method.GET, self.url_template, self.path_parameters)
        request_info.configure(request_configuration)
        request_info.headers.try_add("Accept", "application/json")
        return request_info
    
    def with_url(self,raw_url: str) -> ServersRequestBuilder:
        """
        Returns a request builder with the provided arbitrary URL. Using this method means any other path or query parameters are ignored.
        param raw_url: The raw URL to use for the request builder.
        Returns: ServersRequestBuilder
        """
        if raw_url is None:
            raise TypeError("raw_url cannot be null.")
        return ServersRequestBuilder(self.request_adapter, raw_url)
    
    @property
    def start(self) -> StartRequestBuilder:
        """
        The start property
        """
        from .start.start_request_builder import StartRequestBuilder

        return StartRequestBuilder(self.request_adapter, self.path_parameters)
    
    @dataclass
    class ServersRequestBuilderGetRequestConfiguration(RequestConfiguration[QueryParameters]):
        """
        Configuration for the request such as headers, query parameters, and middleware options.
        """
        warn("This class is deprecated. Please use the generic RequestConfiguration class generated by the generator.", DeprecationWarning)
    

