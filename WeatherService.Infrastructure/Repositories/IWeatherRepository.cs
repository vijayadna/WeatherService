using WeatherService.Domain.Models;

namespace WeatherService.Infrastructure.Repositories
{
    public interface IWeatherRepository
    {
        Task<WeatherReading?> GetLatestReadingAsync(string location, CancellationToken ct = default);
        Task<IEnumerable<WeatherReading>> GetHistoricalAsync(string location, DateTime from, DateTime to, int page, int pageSize, CancellationToken ct = default);
        Task AddReadingAsync(WeatherReading reading, CancellationToken ct = default);
        Task<IEnumerable<WeatherForecast>> GetForecastAsync(string location, string period, CancellationToken ct = default);
        Task UpsertForecastsAsync(IEnumerable<WeatherForecast> forecasts, CancellationToken ct = default);
        Task<AlertSubscription?> GetSubscriptionByIdAsync(int id, CancellationToken ct);
        Task<IEnumerable<AlertSubscription>> GetActiveSubscriptionsAsync(CancellationToken ct);
        Task<IEnumerable<AlertSubscription>> GetSubscriptionsByEmailAsync(string email, CancellationToken ct = default);
        Task<AlertSubscription> CreateSubscriptionAsync(AlertSubscription sub, CancellationToken ct = default);
        Task UpdateSubscriptionAsync(AlertSubscription sub, CancellationToken ct = default);
        Task DeleteSubscriptionAsync(int id, CancellationToken ct = default);
        Task AddAlertEventAsync(AlertEvent evt, CancellationToken ct = default);
        Task<IEnumerable<AlertEvent>> GetAlertEventsAsync(int subscriptionId, int page, int pageSize, CancellationToken ct = default);
    }
}
