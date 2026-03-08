using Microsoft.EntityFrameworkCore;
using WeatherService.Domain.Models;


namespace WeatherService.Infrastructure.Repositories
{
    public class WeatherRepository : IWeatherRepository
    {
        private readonly WeatherDbContext _db;
        public WeatherRepository(WeatherDbContext dbcontext)
        {
            _db = dbcontext;
        }
        public Task<WeatherReading?> GetLatestReadingAsync(string location, CancellationToken ct = default)
        {
            return _db.WeatherReadings.Where(r => r.Location.ToLower() == location.ToLower())
                .OrderByDescending(r => r.ObservedAtUtc)
                .FirstOrDefaultAsync(ct);
        }

        public Task<IEnumerable<WeatherReading>> GetHistoricalAsync(
       string location, DateTime from, DateTime to, int page, int pageSize, CancellationToken ct) =>
       _db.WeatherReadings
          .Where(r => r.Location.ToLower() == location.ToLower()
                   && r.ObservedAtUtc >= from && r.ObservedAtUtc <= to)
          .OrderByDescending(r => r.ObservedAtUtc)
          .Skip((page - 1) * pageSize)
          .Take(pageSize)
          .ToListAsync(ct)
          .ContinueWith(t => (IEnumerable<WeatherReading>)t.Result, ct);

        public Task<int> GetHistoricalCountAsync(string location, DateTime from, DateTime to, CancellationToken ct) =>
            _db.WeatherReadings
               .Where(r => r.Location.ToLower() == location.ToLower()
                        && r.ObservedAtUtc >= from && r.ObservedAtUtc <= to)
               .CountAsync(ct);


        public async Task AddReadingAsync(WeatherReading reading, CancellationToken ct)
        {
            _db.WeatherReadings.Add(reading);
            await _db.SaveChangesAsync(ct);
        }

        // ─── Forecasts ───────────────────────────────────────────────────────────

        public Task<IEnumerable<WeatherForecast>> GetForecastAsync(string location, string period, CancellationToken ct)
        {
            return _db.WeatherForecasts
               .Where(f => f.Location.ToLower() == location.ToLower()
                        && f.Period == period
                        && f.ForecastForUtc >= DateTime.UtcNow)
               .OrderBy(f => f.ForecastForUtc)
               .ToListAsync(ct)
               .ContinueWith(t => (IEnumerable<WeatherForecast>)t.Result, ct);
        }

        public async Task UpsertForecastsAsync(IEnumerable<WeatherForecast> forecasts, CancellationToken ct)
        {
            var list = forecasts.ToList();
            if (!list.Any()) return;

            var location = list[0].Location;
            var period = list[0].Period;
            var minDate = list.Min(f => f.ForecastForUtc);

            // Remove stale forecasts for same location+period from this point forward
            var stale = await _db.WeatherForecasts
                .Where(f => f.Location.ToLower() == location.ToLower()
                         && f.Period == period && f.ForecastForUtc >= minDate)
                .ToListAsync(ct);

            _db.WeatherForecasts.RemoveRange(stale);
            _db.WeatherForecasts.AddRange(list);
            await _db.SaveChangesAsync(ct);
        }

        // ─── Alert Subscriptions ──────────────────────────────────────────────────

        public Task<IEnumerable<AlertSubscription>> GetActiveSubscriptionsAsync(CancellationToken ct) =>
            _db.AlertSubscriptions
               .Where(s => s.IsActive)
               .ToListAsync(ct)
               .ContinueWith(t => (IEnumerable<AlertSubscription>)t.Result, ct);

        public Task<AlertSubscription?> GetSubscriptionByIdAsync(int id, CancellationToken ct) =>
            _db.AlertSubscriptions.FindAsync(new object[] { id }, ct).AsTask();

        public Task<IEnumerable<AlertSubscription>> GetSubscriptionsByEmailAsync(string email, CancellationToken ct) =>
            _db.AlertSubscriptions
               .Where(s => s.SubscriberEmail.ToLower() == email.ToLower())
               .OrderByDescending(s => s.CreatedAtUtc)
               .ToListAsync(ct)
               .ContinueWith(t => (IEnumerable<AlertSubscription>)t.Result, ct);

        public async Task<AlertSubscription> CreateSubscriptionAsync(AlertSubscription sub, CancellationToken ct)
        {
            _db.AlertSubscriptions.Add(sub);
            await _db.SaveChangesAsync(ct);
            return sub;
        }

        public async Task UpdateSubscriptionAsync(AlertSubscription sub, CancellationToken ct)
        {
            _db.AlertSubscriptions.Update(sub);
            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteSubscriptionAsync(int id, CancellationToken ct)
        {
            var sub = await _db.AlertSubscriptions.FindAsync(new object[] { id }, ct);
            if (sub is not null)
            {
                _db.AlertSubscriptions.Remove(sub);
                await _db.SaveChangesAsync(ct);
            }
        }

        public async Task AddAlertEventAsync(AlertEvent evt, CancellationToken ct)
        {
            _db.AlertEvents.Add(evt);
            await _db.SaveChangesAsync(ct);
        }

        public Task<IEnumerable<AlertEvent>> GetAlertEventsAsync(int subscriptionId, int page, int pageSize, CancellationToken ct) =>
            _db.AlertEvents
               .Where(e => e.AlertSubscriptionId == subscriptionId)
               .OrderByDescending(e => e.TriggeredAtUtc)
               .Skip((page - 1) * pageSize)
               .Take(pageSize)
               .ToListAsync(ct)
               .ContinueWith(t => (IEnumerable<AlertEvent>)t.Result, ct);

    }
}
