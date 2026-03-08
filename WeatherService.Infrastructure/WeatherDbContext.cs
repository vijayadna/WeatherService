using Microsoft.EntityFrameworkCore;
using WeatherService.Domain.Models;

namespace WeatherService.Infrastructure
{
    public class WeatherDbContext : DbContext
    {
        public WeatherDbContext(DbContextOptions<WeatherDbContext> options) : base(options) { }

        public DbSet<WeatherReading> WeatherReadings => Set<WeatherReading>();
        public DbSet<WeatherForecast> WeatherForecasts => Set<WeatherForecast>();
        public DbSet<AlertSubscription> AlertSubscriptions => Set<AlertSubscription>();
        public DbSet<AlertEvent> AlertEvents => Set<AlertEvent>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // WeatherReading
            modelBuilder.Entity<WeatherReading>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.Location, x.ObservedAtUtc });
                e.HasIndex(x => x.ObservedAtUtc);
                e.Property(x => x.Location).HasMaxLength(200).IsRequired();
                e.Property(x => x.Source).HasMaxLength(50);
                e.Property(x => x.Condition).HasMaxLength(100);
                e.Property(x => x.AqiCategory).HasMaxLength(50);
            });

            // WeatherForecast
            modelBuilder.Entity<WeatherForecast>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.Location, x.ForecastForUtc, x.Period });
                e.Property(x => x.Location).HasMaxLength(200).IsRequired();
                e.Property(x => x.Period).HasMaxLength(20);
                e.Property(x => x.Source).HasMaxLength(50);
            });

            // AlertSubscription
            modelBuilder.Entity<AlertSubscription>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.SubscriberEmail, x.Location, x.AlertType });
                e.Property(x => x.SubscriberEmail).HasMaxLength(256).IsRequired();
                e.Property(x => x.Location).HasMaxLength(200).IsRequired();
                e.Property(x => x.AlertType).HasMaxLength(50).IsRequired();
                e.Property(x => x.Operator).HasMaxLength(10).IsRequired();
            });

            // AlertEvent
            modelBuilder.Entity<AlertEvent>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.AlertSubscriptionId);
                e.HasOne(x => x.AlertSubscription)
                 .WithMany(x => x.AlertEvents)
                 .HasForeignKey(x => x.AlertSubscriptionId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.Property(x => x.Message).HasMaxLength(500);
            });
        }

    }
}
