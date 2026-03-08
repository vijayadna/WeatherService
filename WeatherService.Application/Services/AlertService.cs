using Microsoft.Extensions.Logging;
using WeatherService.Application.DTO;
using WeatherService.Domain.Models;
using WeatherService.Infrastructure.Repositories;

namespace WeatherService.Application.Services
{
    public class AlertService : IAlertService
    {
        private readonly IWeatherRepository _repo;
        private readonly IExternalWeatherService _external;
        private readonly ILogger<AlertService> _logger;

        public AlertService(
            IWeatherRepository repo,
            IExternalWeatherService external,
            ILogger<AlertService> logger)
        {
            _repo = repo;
            _external = external;
            _logger = logger;
        }

        public async Task<AlertSubscriptionResponse> CreateSubscriptionAsync(
            CreateAlertSubscriptionRequest req, CancellationToken ct)
        {
            if (!IsValidAlertType(req.AlertType))
                throw new ArgumentException($"Invalid AlertType. Allowed: Temperature, Rain, AQI, UV, Wind");
            if (!IsValidOperator(req.Operator))
                throw new ArgumentException($"Invalid Operator. Allowed: gt, lt, eq");

            var sub = new AlertSubscription
            {
                SubscriberEmail = req.SubscriberEmail,
                Location = req.Location,
                AlertType = req.AlertType,
                Operator = req.Operator,
                Threshold = req.Threshold,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var created = await _repo.CreateSubscriptionAsync(sub, ct);
            return ToResponse(created);
        }

        public async Task<IEnumerable<AlertSubscriptionResponse>> GetSubscriptionsAsync(
            string email, CancellationToken ct)
        {
            var subs = await _repo.GetSubscriptionsByEmailAsync(email, ct);
            return subs.Select(ToResponse);
        }

        public async Task<AlertSubscriptionResponse?> UpdateSubscriptionAsync(
            int id, UpdateAlertSubscriptionRequest req, CancellationToken ct)
        {
            var sub = await _repo.GetSubscriptionByIdAsync(id, ct);
            if (sub is null) return null;

            if (req.IsActive.HasValue) sub.IsActive = req.IsActive.Value;
            if (req.Threshold.HasValue) sub.Threshold = req.Threshold.Value;
            if (req.Operator is not null)
            {
                if (!IsValidOperator(req.Operator))
                    throw new ArgumentException("Invalid operator");
                sub.Operator = req.Operator;
            }

            await _repo.UpdateSubscriptionAsync(sub, ct);
            return ToResponse(sub);
        }

        public async Task<bool> DeleteSubscriptionAsync(int id, CancellationToken ct)
        {
            var sub = await _repo.GetSubscriptionByIdAsync(id, ct);
            if (sub is null) return false;
            await _repo.DeleteSubscriptionAsync(id, ct);
            return true;
        }      
       
        // ─── Private helpers ──────────────────────────────────────────────────────

        private static bool IsValidAlertType(string t) =>
            new[] { "temperature", "rain", "aqi", "uv", "wind" }
            .Contains(t.ToLower());

        private static bool IsValidOperator(string o) =>
            new[] { "gt", "lt", "eq" }.Contains(o.ToLower());

        private static AlertSubscriptionResponse ToResponse(AlertSubscription s) =>
            new(s.Id, s.SubscriberEmail, s.Location, s.AlertType,
                s.Operator, s.Threshold, s.IsActive, s.CreatedAtUtc, s.LastTriggeredUtc);
    }
}