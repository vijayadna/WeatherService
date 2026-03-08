using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using WeatherService.Application.DTO;
using WeatherService.Application.Services;

namespace WeatherService.API.Controllers
{
    /// <summary>
    /// Authentication — obtain a JWT bearer token to access protected endpoints.
    /// </summary>
    [ApiController]
    [Route("api/v1/auth")]
    [AllowAnonymous]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;

        public AuthController(IAuthService auth) => _auth = auth;

        /// <summary>
        /// Exchange credentials for a JWT access token.
        /// Demo credentials: admin / Admin@Weather1!  or  readonly / ReadOnly@Weather1!
        /// </summary>
        [HttpPost("login")]
        [SwaggerOperation(Summary = "Obtain JWT token", Tags = new[] { "Auth" })]
        [SwaggerResponse(200, "JWT token", typeof(TokenResponse))]
        [SwaggerResponse(401, "Invalid credentials")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var token = _auth.Authenticate(request);
            if (token is null)
                return Unauthorized(new { error = "Invalid username or password." });
            return Ok(token);
        }
    }

}
