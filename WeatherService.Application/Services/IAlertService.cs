using WeatherService.Application.DTO;

namespace WeatherService.Application.Services
{
    public interface IAlertService
    {
        Task<AlertSubscriptionResponse> CreateSubscriptionAsync(CreateAlertSubscriptionRequest req, CancellationToken ct = default);
        Task<IEnumerable<AlertSubscriptionResponse>> GetSubscriptionsAsync(string email, CancellationToken ct = default);
        Task<AlertSubscriptionResponse?> UpdateSubscriptionAsync(int id, UpdateAlertSubscriptionRequest req, CancellationToken ct = default);
        Task<bool> DeleteSubscriptionAsync(int id, CancellationToken ct = default);       
    }
}
