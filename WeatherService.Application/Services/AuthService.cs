using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WeatherService.Application.DTO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace WeatherService.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;

        // Hardcoded demo credentials — replace with a proper user store in production
        private static readonly Dictionary<string, string> Users = new(StringComparer.OrdinalIgnoreCase)
        {
            ["admin"] = "Admin@Weather1!",
            ["readonly"] = "ReadOnly@Weather1!"
        };

        public AuthService(IConfiguration config, ILogger<AuthService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public TokenResponse? Authenticate(LoginRequest request)
        {
            if (!Users.TryGetValue(request.Username, out var pwd) || pwd != request.Password)
            {
                _logger.LogWarning("Failed login attempt for user {Username}", request.Username);
                return null;
            }

            var jwtSettings = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddHours(int.Parse(jwtSettings["ExpiryHours"] ?? "8"));

            var claims = new[]
            {
            new Claim(ClaimTypes.Name, request.Username),
            new Claim(ClaimTypes.Role, request.Username == "admin" ? "admin" : "reader"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: expiry,
                signingCredentials: creds
            );

            return new TokenResponse(new JwtSecurityTokenHandler().WriteToken(token), expiry);
        }
    }
}