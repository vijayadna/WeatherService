
using WeatherService.Application.DTO;

namespace WeatherService.Application.Services
{
    public interface IAuthService
    {
        TokenResponse? Authenticate(LoginRequest request);
    }
}
