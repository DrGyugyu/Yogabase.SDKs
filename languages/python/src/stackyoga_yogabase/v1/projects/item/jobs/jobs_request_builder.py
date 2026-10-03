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
from uuid import UUID
from warnings import warn

if TYPE_CHECKING:
    from .....models.job import Job
    from .....models.job_submit_request import JobSubmitRequest
    from .....models.problem import Problem
    from .item.with_job_item_request_builder import WithJobItemRequestBuilder

class JobsRequestBuilder(BaseRequestBuilder):
    """
    Builds and executes requests for operations under /v1/projects/{projectId}/jobs
    """
    def __init__(self,request_adapter: RequestAdapter, path_parameters: Union[str, dict[str, Any]]) -> None:
        """
        Instantiates a new JobsRequestBuilder and sets the default values.
        param path_parameters: The raw url or the url-template parameters for the request.
        param request_adapter: The request adapter to use to execute the requests.
        Returns: None
        """
        super().__init__(request_adapter, "{+baseurl}/v1/projects/{projectId}/jobs", path_parameters)
    
    def by_job_id(self,job_id: UUID) -> WithJobItemRequestBuilder:
        """
        Gets an item from the stackyoga.yogabase.v1.projects.item.jobs.item collection
        param job_id: A job's id, as returned by submit. An id that is not a uuid in its hyphenated form, or is the all-zero uuid, is 400 job_request_invalid.
        Returns: WithJobItemRequestBuilder
        """
        if job_id is None:
            raise TypeError("job_id cannot be null.")
        from .item.with_job_item_request_builder import WithJobItemRequestBuilder

        url_tpl_params = get_path_parameters(self.path_parameters)
        url_tpl_params["jobId"] = job_id
        return WithJobItemRequestBuilder(self.request_adapter, url_tpl_params)
    
    async def post(self,body: JobSubmitRequest, request_configuration: Optional[RequestConfiguration[QueryParameters]] = None) -> Optional[Job]:
        """
        Starts one durable run of a deployed stateless function as the signed-in user. The job type is the id of a deployed stateless function, run by its highest released version; an id no released function carries is 422 job_type_not_registered and records nothing. The idempotency key is a member of the body, belongs to the signed-in user, and binds the whole request (job type, payload and maxAttempts): the same request under it answers the job it already made, 202 again and in whatever state that job has reached, and a different request under it is 409 job_idempotency_conflict. The 202 carries the job's own route in Location. A request body that cannot be read as one JSON object of exactly the documented members is a bare 400, and one that is not JSON a bare 415; a member that is missing or out of range (an empty or over-long job type or key, maxAttempts outside 1 to 10, a payload that is not base64) is 400 job_request_invalid.
        param body: The request body
        param request_configuration: Configuration for the request such as headers, query parameters, and middleware options.
        Returns: Optional[Job]
        """
        if body is None:
            raise TypeError("body cannot be null.")
        request_info = self.to_post_request_information(
            body, request_configuration
        )
        from .....models.problem import Problem

        error_mapping: dict[str, type[ParsableFactory]] = {
            "400": Problem,
            "401": Problem,
            "403": Problem,
            "409": Problem,
            "413": Problem,
            "422": Problem,
            "503": Problem,
        }
        if not self.request_adapter:
            raise Exception("Http core is null") 
        from .....models.job import Job

        return await self.request_adapter.send_async(request_info, Job, error_mapping)
    
    def to_post_request_information(self,body: JobSubmitRequest, request_configuration: Optional[RequestConfiguration[QueryParameters]] = None) -> RequestInformation:
        """
        Starts one durable run of a deployed stateless function as the signed-in user. The job type is the id of a deployed stateless function, run by its highest released version; an id no released function carries is 422 job_type_not_registered and records nothing. The idempotency key is a member of the body, belongs to the signed-in user, and binds the whole request (job type, payload and maxAttempts): the same request under it answers the job it already made, 202 again and in whatever state that job has reached, and a different request under it is 409 job_idempotency_conflict. The 202 carries the job's own route in Location. A request body that cannot be read as one JSON object of exactly the documented members is a bare 400, and one that is not JSON a bare 415; a member that is missing or out of range (an empty or over-long job type or key, maxAttempts outside 1 to 10, a payload that is not base64) is 400 job_request_invalid.
        param body: The request body
        param request_configuration: Configuration for the request such as headers, query parameters, and middleware options.
        Returns: RequestInformation
        """
        if body is None:
            raise TypeError("body cannot be null.")
        request_info = RequestInformation(Method.POST, self.url_template, self.path_parameters)
        request_info.configure(request_configuration)
        request_info.headers.try_add("Accept", "application/json")
        request_info.set_content_from_parsable(self.request_adapter, "application/json", body)
        return request_info
    
    def with_url(self,raw_url: str) -> JobsRequestBuilder:
        """
        Returns a request builder with the provided arbitrary URL. Using this method means any other path or query parameters are ignored.
        param raw_url: The raw URL to use for the request builder.
        Returns: JobsRequestBuilder
        """
        if raw_url is None:
            raise TypeError("raw_url cannot be null.")
        return JobsRequestBuilder(self.request_adapter, raw_url)
    
    @dataclass
    class JobsRequestBuilderPostRequestConfiguration(RequestConfiguration[QueryParameters]):
        """
        Configuration for the request such as headers, query parameters, and middleware options.
        """
        warn("This class is deprecated. Please use the generic RequestConfiguration class generated by the generator.", DeprecationWarning)
    

