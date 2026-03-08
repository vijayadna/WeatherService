using System.ComponentModel.DataAnnotations;

namespace WeatherService.Application.DTO
{
    public record CurrentWeatherResponse(
    string Location,
    double Latitude,
    double Longitude,
    double TemperatureCelsius,
    double FeelsLikeCelsius,
    int HumidityPercent,
    double WindSpeedKph,
    string WindDirection,
    double PrecipitationMm,
    int AirQualityIndex,
    string AqiCategory,
    double VisibilityKm,
    string Condition,
    string ConditionIcon,
    int UvIndex,
    DateTime ObservedAtUtc,
    string Source
);

    public record ForecastResponse(
        string Location,
        double Latitude,
        double Longitude,
        string Period,
        IEnumerable<ForecastEntry> Entries
    );

    public record ForecastEntry(
        DateTime ForecastForUtc,
        double TempMinCelsius,
        double TempMaxCelsius,
        int HumidityPercent,
        double PrecipitationMm,
        int PrecipitationChancePercent,
        double WindSpeedKph,
        string Condition,
        string ConditionIcon
    );

    public record HistoricalWeatherResponse(
        string Location,
        DateTime From,
        DateTime To,
        IEnumerable<CurrentWeatherResponse> Readings
    );

    public record AlertSubscriptionResponse(
        int Id,
        string SubscriberEmail,
        string Location,
        string AlertType,
        string Operator,
        double Threshold,
        bool IsActive,
        DateTime CreatedAtUtc,
        DateTime? LastTriggeredUtc
    );

    public record AlertEventResponse(
        int Id,
        string Message,
        double ActualValue,
        DateTime TriggeredAtUtc,
        bool NotificationSent
    );

    public record PagedResponse<T>(
        IEnumerable<T> Data,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages
    );

    // ─── Requests ─────────────────────────────────────────────────────────────────

    public record CreateAlertSubscriptionRequest(
        [Required][EmailAddress] string SubscriberEmail,
        [Required][StringLength(200, MinimumLength = 1)] string Location,
        [Required] string AlertType,      // Temperature | Rain | AQI | UV | Wind
        [Required] string Operator,       // gt | lt | eq
        double Threshold
    );

    public record UpdateAlertSubscriptionRequest(
        bool? IsActive,
        double? Threshold,
        string? Operator
    );

    public record ExportRequest(
        [Required] string Location,
        DateTime? From,
        DateTime? To
    );

    // ─── Auth ─────────────────────────────────────────────────────────────────────

    public record LoginRequest(
        [Required] string Username,
        [Required] string Password
    );

    public record TokenResponse(
        string AccessToken,
        DateTime ExpiresAt,
        string TokenType = "Bearer"
    );
}