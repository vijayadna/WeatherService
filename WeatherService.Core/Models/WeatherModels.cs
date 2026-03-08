namespace WeatherService.Domain.Models
{
    /// <summary>
    /// Persisted weather observation record.
    /// </summary>
    public class WeatherReading
    {
        public int Id { get; set; }
        public string Location { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double TemperatureCelsius { get; set; }
        public double FeelsLikeCelsius { get; set; }
        public int HumidityPercent { get; set; }
        public double WindSpeedKph { get; set; }
        public string WindDirection { get; set; } = string.Empty;
        public double PrecipitationMm { get; set; }
        public int AirQualityIndex { get; set; }
        public string AqiCategory { get; set; } = string.Empty;   // Good / Moderate / Unhealthy …
        public double VisibilityKm { get; set; }
        public string Condition { get; set; } = string.Empty;      // Sunny, Cloudy, Rain …
        public string ConditionIcon { get; set; } = string.Empty;
        public int UvIndex { get; set; }
        public string Source { get; set; } = string.Empty;         // openweathermap | data.gov.sg
        public DateTime ObservedAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Weather forecast entry (hourly or daily).
    /// </summary>
    public class WeatherForecast
    {
        public int Id { get; set; }
        public string Location { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime ForecastForUtc { get; set; }
        public string Period { get; set; } = string.Empty;         // hourly | daily
        public double TempMinCelsius { get; set; }
        public double TempMaxCelsius { get; set; }
        public int HumidityPercent { get; set; }
        public double PrecipitationMm { get; set; }
        public int PrecipitationChancePercent { get; set; }
        public double WindSpeedKph { get; set; }
        public string Condition { get; set; } = string.Empty;
        public string ConditionIcon { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Alert subscription registered by a user.
    /// </summary>
    public class AlertSubscription
    {
        public int Id { get; set; }
        public string SubscriberEmail { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;      // Temperature | Rain | AQI | UV | Wind
        public string Operator { get; set; } = string.Empty;       // gt | lt | eq
        public double Threshold { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? LastTriggeredUtc { get; set; }
        public ICollection<AlertEvent> AlertEvents { get; set; } = new List<AlertEvent>();
    }

    /// <summary>
    /// Record of a triggered alert.
    /// </summary>
    public class AlertEvent
    {
        public int Id { get; set; }
        public int AlertSubscriptionId { get; set; }
        public AlertSubscription AlertSubscription { get; set; } = null!;
        public string Message { get; set; } = string.Empty;
        public double ActualValue { get; set; }
        public DateTime TriggeredAtUtc { get; set; } = DateTime.UtcNow;
        public bool NotificationSent { get; set; }
    }

}
