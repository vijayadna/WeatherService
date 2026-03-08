using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using WeatherService.Application.Services;
using WeatherService.Application.DTO;

namespace WeatherService.API.Controllers
{
    [ApiController]
    [Route("api/v1/weather")]
    [Authorize]
    [Produces("application/json")]
    public class WeatherController:ControllerBase
    {
        private readonly IWeatherService _weatherService;
        private readonly ILogger<WeatherController> _logger;
        public WeatherController(IWeatherService weatherService, ILogger<WeatherController> logger)
        {
            _weatherService = weatherService;
            _logger = logger;
        }

        /// <summary>
        /// Get the current weather conditions for a given location.
        /// Results are cached for 10 minutes; pass forceRefresh=true to bypass.
        /// </summary>
        [HttpGet("current")]
        [SwaggerOperation(Summary = "Get current weather", Tags = new[] { "Weather" })]
        [SwaggerResponse(200, "Current weather data", typeof(CurrentWeatherResponse))]
        [SwaggerResponse(404, "Location not found or external API unavailable")]
        public async Task<IActionResult> GetCurrentWeather(
            [FromQuery] string location,
            [FromQuery] bool forceRefresh = false,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(location))
                return BadRequest(new { error = "Location is required." });

            var result = await _weatherService.GetCurrentWeatherAsync(location, forceRefresh, ct);
            if (result is null)
                return NotFound(new { error = $"Could not retrieve weather for '{location}'." });

            return Ok(result);
        }

        /// <summary>
        /// Get weather forecast for a location.
        /// Use period=hourly (48 h) or period=daily (5-day).
        /// </summary>
        [HttpGet("forecast")]
        [SwaggerOperation(Summary = "Get weather forecast", Tags = new[] { "Weather" })]
        [SwaggerResponse(200, "Forecast data", typeof(ForecastResponse))]
        public async Task<IActionResult> GetForecast(
            [FromQuery] string location,
            [FromQuery] string period = "daily",
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(location))
                return BadRequest(new { error = "Location is required." });

            if (period != "hourly" && period != "daily")
                return BadRequest(new { error = "period must be 'hourly' or 'daily'." });

            var result = await _weatherService.GetForecastAsync(location, period, ct);
            if (result is null)
                return NotFound(new { error = $"Could not retrieve forecast for '{location}'." });

            return Ok(result);
        }

        // ─── GET /api/v1/weather/export ───────────────────────────────────────────

        /// <summary>
        /// Export historical weather data for a location as a CSV file.
        /// </summary>
        [HttpGet("export")]
        [SwaggerOperation(Summary = "Export weather data as CSV", Tags = new[] { "Weather" })]
        [SwaggerResponse(200, "CSV file download")]
        [Produces("text/csv", "application/json")]
        public async Task<IActionResult> ExportCsv(
            [FromQuery] string location,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(location))
                return BadRequest(new { error = "Location is required." });

            var csv = await _weatherService.ExportToCsvAsync(location, from?.ToUniversalTime(), to?.ToUniversalTime(), ct);

            var filename = $"weather_{location.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd}.csv";
            return File(csv, "text/csv", filename);
        }
    }
}
