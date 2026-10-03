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
    from .......models.problem import Problem

class ResultRequestBuilder(BaseRequestBuilder):
    """
    Builds and executes requests for operations under /v1/projects/{projectId}/jobs/{jobId}/result
    """
    def __init__(self,request_adapter: RequestAdapter, path_parameters: Union[str, dict[str, Any]]) -> None:
        """
        Instantiates a new ResultRequestBuilder and sets the default values.
        param path_parameters: The raw url or the url-template parameters for the request.
        param request_adapter: The request adapter to use to execute the requests.
        Returns: None
        """
        super().__init__(request_adapter, "{+baseurl}/v1/projects/{projectId}/jobs/{jobId}/result", path_parameters)
    
    async def get(self,request_configuration: Optional[RequestConfiguration[QueryParameters]] = None) -> Optional[bytes]:
        """
        The result of the caller's own succeeded job: the bytes its function returned, exactly as it returned them, from one byte to 16 MiB. The gateway reads the stored object twice, once to check it against the digest the job recorded and once to send it, so no byte leaves before all of them have been checked; that digest is in X-Yogabase-Content-Sha256. 404 job_result_not_found is the one answer for another user's job, an id that does not exist, and a job that finished without a result (cancelled, dead-lettered, or succeeded with none), so it never says which. 409 job_result_not_ready is the caller's own job that has not settled. 500 job_result_reference_invalid and 502 job_result_integrity_failed mean the stored result is not the one the job recorded, and send no byte of it.
        param request_configuration: Configuration for the request such as headers, query parameters, and middleware options.
        Returns: bytes
        """
        request_info = self.to_get_request_information(
            request_configuration
        )
        from .......models.problem import Problem

        error_mapping: dict[str, type[ParsableFactory]] = {
            "400": Problem,
            "401": Problem,
            "403": Problem,
            "404": Problem,
            "409": Problem,
            "500": Problem,
            "502": Problem,
            "503": Problem,
        }
        if not self.request_adapter:
            raise Exception("Http core is null") 
        return await self.request_adapter.send_primitive_async(request_info, "bytes", error_mapping)
    
    def to_get_request_information(self,request_configuration: Optional[RequestConfiguration[QueryParameters]] = None) -> RequestInformation:
        """
        The result of the caller's own succeeded job: the bytes its function returned, exactly as it returned them, from one byte to 16 MiB. The gateway reads the stored object twice, once to check it against the digest the job recorded and once to send it, so no byte leaves before all of them have been checked; that digest is in X-Yogabase-Content-Sha256. 404 job_result_not_found is the one answer for another user's job, an id that does not exist, and a job that finished without a result (cancelled, dead-lettered, or succeeded with none), so it never says which. 409 job_result_not_ready is the caller's own job that has not settled. 500 job_result_reference_invalid and 502 job_result_integrity_failed mean the stored result is not the one the job recorded, and send no byte of it.
        param request_configuration: Configuration for the request such as headers, query parameters, and middleware options.
        Returns: RequestInformation
        """
        request_info = RequestInformation(Method.GET, self.url_template, self.path_parameters)
        request_info.configure(request_configuration)
        request_info.headers.try_add("Accept", "application/octet-stream, application/problem+json")
        return request_info
    
    def with_url(self,raw_url: str) -> ResultRequestBuilder:
        """
        Returns a request builder with the provided arbitrary URL. Using this method means any other path or query parameters are ignored.
        param raw_url: The raw URL to use for the request builder.
        Returns: ResultRequestBuilder
        """
        if raw_url is None:
            raise TypeError("raw_url cannot be null.")
        return ResultRequestBuilder(self.request_adapter, raw_url)
    
    @dataclass
    class ResultRequestBuilderGetRequestConfiguration(RequestConfiguration[QueryParameters]):
        """
        Configuration for the request such as headers, query parameters, and middleware options.
        """
        warn("This class is deprecated. Please use the generic RequestConfiguration class generated by the generator.", DeprecationWarning)
    

