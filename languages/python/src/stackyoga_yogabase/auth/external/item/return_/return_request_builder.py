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
    from .....models.problem import Problem
    from .return_post_request_body import ReturnPostRequestBody

class ReturnRequestBuilder(BaseRequestBuilder):
    """
    Builds and executes requests for operations under /auth/external/{provider}/return
    """
    def __init__(self,request_adapter: RequestAdapter, path_parameters: Union[str, dict[str, Any]]) -> None:
        """
        Instantiates a new ReturnRequestBuilder and sets the default values.
        param path_parameters: The raw url or the url-template parameters for the request.
        param request_adapter: The request adapter to use to execute the requests.
        Returns: None
        """
        super().__init__(request_adapter, "{+baseurl}/auth/external/{provider}/return{?code*,state*}", path_parameters)
    
    async def get(self,request_configuration: Optional[RequestConfiguration[ReturnRequestBuilderGetQueryParameters]] = None) -> None:
        """
        The one address registered with a provider (https://<gateway>/auth/external/<id>/return); no application calls it. A provider that answers by query lands here. It redirects to the redirect URI given to start, carrying the code and state, or error=access_denied and the state when the provider sent no code. Nothing else of the answer is read.
        param request_configuration: Configuration for the request such as headers, query parameters, and middleware options.
        Returns: None
        """
        request_info = self.to_get_request_information(
            request_configuration
        )
        from .....models.problem import Problem

        error_mapping: dict[str, type[ParsableFactory]] = {
            "401": Problem,
            "404": Problem,
            "503": Problem,
        }
        if not self.request_adapter:
            raise Exception("Http core is null") 
        return await self.request_adapter.send_no_response_content_async(request_info, error_mapping)
    
    async def post(self,body: ReturnPostRequestBody, request_configuration: Optional[RequestConfiguration[QueryParameters]] = None) -> None:
        """
        The same address for a provider that answers by form_post (Apple). Same redirect, same fields.
        param body: The request body
        param request_configuration: Configuration for the request such as headers, query parameters, and middleware options.
        Returns: None
        """
        if body is None:
            raise TypeError("body cannot be null.")
        request_info = self.to_post_request_information(
            body, request_configuration
        )
        from .....models.problem import Problem

        error_mapping: dict[str, type[ParsableFactory]] = {
            "401": Problem,
            "404": Problem,
            "503": Problem,
        }
        if not self.request_adapter:
            raise Exception("Http core is null") 
        return await self.request_adapter.send_no_response_content_async(request_info, error_mapping)
    
    def to_get_request_information(self,request_configuration: Optional[RequestConfiguration[ReturnRequestBuilderGetQueryParameters]] = None) -> RequestInformation:
        """
        The one address registered with a provider (https://<gateway>/auth/external/<id>/return); no application calls it. A provider that answers by query lands here. It redirects to the redirect URI given to start, carrying the code and state, or error=access_denied and the state when the provider sent no code. Nothing else of the answer is read.
        param request_configuration: Configuration for the request such as headers, query parameters, and middleware options.
        Returns: RequestInformation
        """
        request_info = RequestInformation(Method.GET, self.url_template, self.path_parameters)
        request_info.configure(request_configuration)
        request_info.headers.try_add("Accept", "application/problem+json")
        return request_info
    
    def to_post_request_information(self,body: ReturnPostRequestBody, request_configuration: Optional[RequestConfiguration[QueryParameters]] = None) -> RequestInformation:
        """
        The same address for a provider that answers by form_post (Apple). Same redirect, same fields.
        param body: The request body
        param request_configuration: Configuration for the request such as headers, query parameters, and middleware options.
        Returns: RequestInformation
        """
        if body is None:
            raise TypeError("body cannot be null.")
        request_info = RequestInformation(Method.POST, self.url_template, self.path_parameters)
        request_info.configure(request_configuration)
        request_info.headers.try_add("Accept", "application/problem+json")
        request_info.set_content_from_parsable(self.request_adapter, "application/x-www-form-urlencoded", body)
        return request_info
    
    def with_url(self,raw_url: str) -> ReturnRequestBuilder:
        """
        Returns a request builder with the provided arbitrary URL. Using this method means any other path or query parameters are ignored.
        param raw_url: The raw URL to use for the request builder.
        Returns: ReturnRequestBuilder
        """
        if raw_url is None:
            raise TypeError("raw_url cannot be null.")
        return ReturnRequestBuilder(self.request_adapter, raw_url)
    
    @dataclass
    class ReturnRequestBuilderGetQueryParameters():
        """
        The one address registered with a provider (https://<gateway>/auth/external/<id>/return); no application calls it. A provider that answers by query lands here. It redirects to the redirect URI given to start, carrying the code and state, or error=access_denied and the state when the provider sent no code. Nothing else of the answer is read.
        """
        code: Optional[str] = None

        state: Optional[str] = None

    
    @dataclass
    class ReturnRequestBuilderGetRequestConfiguration(RequestConfiguration[ReturnRequestBuilderGetQueryParameters]):
        """
        Configuration for the request such as headers, query parameters, and middleware options.
        """
        warn("This class is deprecated. Please use the generic RequestConfiguration class generated by the generator.", DeprecationWarning)
    
    @dataclass
    class ReturnRequestBuilderPostRequestConfiguration(RequestConfiguration[QueryParameters]):
        """
        Configuration for the request such as headers, query parameters, and middleware options.
        """
        warn("This class is deprecated. Please use the generic RequestConfiguration class generated by the generator.", DeprecationWarning)
    

