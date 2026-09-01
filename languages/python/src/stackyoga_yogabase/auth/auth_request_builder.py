from __future__ import annotations
from collections.abc import Callable
from kiota_abstractions.base_request_builder import BaseRequestBuilder
from kiota_abstractions.get_path_parameters import get_path_parameters
from kiota_abstractions.request_adapter import RequestAdapter
from typing import Any, Optional, TYPE_CHECKING, Union

if TYPE_CHECKING:
    from .email.email_request_builder import EmailRequestBuilder
    from .external.external_request_builder import ExternalRequestBuilder
    from .login.login_request_builder import LoginRequestBuilder
    from .logout.logout_request_builder import LogoutRequestBuilder
    from .mfa.mfa_request_builder import MfaRequestBuilder
    from .password.password_request_builder import PasswordRequestBuilder
    from .signup.signup_request_builder import SignupRequestBuilder

class AuthRequestBuilder(BaseRequestBuilder):
    """
    Builds and executes requests for operations under /auth
    """
    def __init__(self,request_adapter: RequestAdapter, path_parameters: Union[str, dict[str, Any]]) -> None:
        """
        Instantiates a new AuthRequestBuilder and sets the default values.
        param path_parameters: The raw url or the url-template parameters for the request.
        param request_adapter: The request adapter to use to execute the requests.
        Returns: None
        """
        super().__init__(request_adapter, "{+baseurl}/auth", path_parameters)
    
    @property
    def email(self) -> EmailRequestBuilder:
        """
        The email property
        """
        from .email.email_request_builder import EmailRequestBuilder

        return EmailRequestBuilder(self.request_adapter, self.path_parameters)
    
    @property
    def external(self) -> ExternalRequestBuilder:
        """
        The external property
        """
        from .external.external_request_builder import ExternalRequestBuilder

        return ExternalRequestBuilder(self.request_adapter, self.path_parameters)
    
    @property
    def login(self) -> LoginRequestBuilder:
        """
        The login property
        """
        from .login.login_request_builder import LoginRequestBuilder

        return LoginRequestBuilder(self.request_adapter, self.path_parameters)
    
    @property
    def logout(self) -> LogoutRequestBuilder:
        """
        The logout property
        """
        from .logout.logout_request_builder import LogoutRequestBuilder

        return LogoutRequestBuilder(self.request_adapter, self.path_parameters)
    
    @property
    def mfa(self) -> MfaRequestBuilder:
        """
        The mfa property
        """
        from .mfa.mfa_request_builder import MfaRequestBuilder

        return MfaRequestBuilder(self.request_adapter, self.path_parameters)
    
    @property
    def password(self) -> PasswordRequestBuilder:
        """
        The password property
        """
        from .password.password_request_builder import PasswordRequestBuilder

        return PasswordRequestBuilder(self.request_adapter, self.path_parameters)
    
    @property
    def signup(self) -> SignupRequestBuilder:
        """
        The signup property
        """
        from .signup.signup_request_builder import SignupRequestBuilder

        return SignupRequestBuilder(self.request_adapter, self.path_parameters)
    

