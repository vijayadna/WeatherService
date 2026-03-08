using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherService.Application.Services;
using WeatherService.Domain.Models;
using WeatherService.Infrastructure.Repositories;
using Xunit;

namespace WeatherService.Application.Tests
{
    public class WeatherServiceImplTests
    {
        private readonly Mock<IWeatherRepository> _repoMock;
        private readonly Mock<IExternalWeatherService> _externalMock;
        private readonly Mock<ILogger<WeatherServiceImpl>> _loggerMock;
        private readonly WeatherServiceImpl _svc;

        public WeatherServiceImplTests()
        {
            _repoMock = new Mock<IWeatherRepository>();
            _externalMock = new Mock<IExternalWeatherService>();
            _loggerMock = new Mock<ILogger<WeatherServiceImpl>>();

            _svc = new WeatherServiceImpl(
                _repoMock.Object,
                _externalMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task GetCurrentWeatherAsync_ReturnsCached_WhenRecentReadingExists()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var cached = new WeatherReading
            {
                Id = 1,
                Location = "TestCity",
                Latitude = 1.23,
                Longitude = 4.56,
                TemperatureCelsius = 20.0,
                ObservedAtUtc = now
            };

            _repoMock.Setup(r => r.GetLatestReadingAsync("TestCity", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(cached);

            // Act
            var res = await _svc.GetCurrentWeatherAsync("TestCity", forceRefresh: false, ct: CancellationToken.None);

            // Assert
            Assert.NotNull(res);
            Assert.Equal(cached.Location, res!.Location);
            Assert.Equal(cached.TemperatureCelsius, res.TemperatureCelsius);
            _externalMock.Verify(e => e.GetCurrentWeatherAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _repoMock.Verify(r => r.AddReadingAsync(It.IsAny<WeatherReading>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetCurrentWeatherAsync_FetchesExternal_WhenNoRecentCache()
        {
            // Arrange
            _repoMock.Setup(r => r.GetLatestReadingAsync("X", It.IsAny<CancellationToken>()))
                     .ReturnsAsync((WeatherReading?)null);

            var fresh = new WeatherReading
            {
                Id = 2,
                Location = "X",
                Latitude = 10,
                Longitude = 20,
                TemperatureCelsius = 15.5,
                ObservedAtUtc = DateTime.UtcNow
            };

            _externalMock.Setup(e => e.GetCurrentWeatherAsync("X", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(fresh);

            _repoMock.Setup(r => r.AddReadingAsync(fresh, It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask)
                     .Verifiable();

            // Act
            var res = await _svc.GetCurrentWeatherAsync("X", forceRefresh: false, ct: CancellationToken.None);

            // Assert
            Assert.NotNull(res);
            Assert.Equal(fresh.Location, res!.Location);
            Assert.Equal(fresh.TemperatureCelsius, res.TemperatureCelsius);
            _repoMock.Verify();
        }

        [Fact]
        public async Task GetCurrentWeatherAsync_ReturnsNull_WhenExternalReturnsNull()
        {
            _repoMock.Setup(r => r.GetLatestReadingAsync("Nope", It.IsAny<CancellationToken>()))
                     .ReturnsAsync((WeatherReading?)null);

            _externalMock.Setup(e => e.GetCurrentWeatherAsync("Nope", It.IsAny<CancellationToken>()))
                         .ReturnsAsync((WeatherReading?)null);

            var res = await _svc.GetCurrentWeatherAsync("Nope", forceRefresh: false, ct: CancellationToken.None);
            Assert.Null(res);
            _repoMock.Verify(r => r.AddReadingAsync(It.IsAny<WeatherReading>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetForecastAsync_ReturnsCached_WhenRecentForecastsExist()
        {
            var freshForecast = new WeatherForecast
            {
                Id = 1,
                Location = "CityA",
                ForecastForUtc = DateTime.UtcNow,
                Period = "daily",
                TempMinCelsius = 5,
                TempMaxCelsius = 10,
                CreatedAtUtc = DateTime.UtcNow
            };

            _repoMock.Setup(r => r.GetForecastAsync("CityA", "daily", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new[] { freshForecast });

            var res = await _svc.GetForecastAsync("CityA", "daily", CancellationToken.None);

            Assert.NotNull(res);
            Assert.Equal("CityA", res!.Location);
            _externalMock.Verify(e => e.GetDailyForecastAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _repoMock.Verify(r => r.UpsertForecastsAsync(It.IsAny<IEnumerable<WeatherForecast>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetForecastAsync_FetchesExternal_WhenNoFreshForecasts()
        {
            // existing stale
            var stale = new WeatherForecast
            {
                Id = 2,
                Location = "CityB",
                ForecastForUtc = DateTime.UtcNow.AddHours(-2),
                Period = "hourly",
                CreatedAtUtc = DateTime.UtcNow.AddHours(-2)
            };

            _repoMock.Setup(r => r.GetForecastAsync("CityB", "hourly", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new[] { stale });

            var externalList = new[]
            {
                new WeatherForecast { Id = 3, Location = "CityB", ForecastForUtc = DateTime.UtcNow, Period = "hourly", TempMinCelsius = 1, TempMaxCelsius = 2 }
            }.AsEnumerable();

            _externalMock.Setup(e => e.GetHourlyForecastAsync("CityB", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(externalList);

            _repoMock.Setup(r => r.UpsertForecastsAsync(It.Is<IEnumerable<WeatherForecast>>(l => l.Any()), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask)
                     .Verifiable();

            var res = await _svc.GetForecastAsync("CityB", "hourly", CancellationToken.None);

            Assert.NotNull(res);
            Assert.Equal("CityB", res!.Location);
            _repoMock.Verify();
        }

        [Fact]
        public async Task ExportToCsvAsync_IncludesReadings()
        {
            var readings = new[]
            {
                new WeatherReading
                {
                    Id = 10,
                    Location = "CSVCity",
                    Latitude = 1.1,
                    Longitude = 2.2,
                    ObservedAtUtc = DateTime.UtcNow,
                    TemperatureCelsius = 12.34,
                    FeelsLikeCelsius = 11.0,
                    HumidityPercent = 50,
                    WindSpeedKph = 5.5,
                    WindDirection = "N",
                    PrecipitationMm = 0,
                    AirQualityIndex = 10,
                    AqiCategory = "Good",
                    VisibilityKm = 10,
                    Condition = "Clear",
                    UvIndex = 3,
                    Source = "test"
                }
            }.AsEnumerable();

            _repoMock.Setup(r => r.GetHistoricalAsync("CSVCity", It.IsAny<DateTime>(), It.IsAny<DateTime>(), 1, int.MaxValue, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(readings);

            var bytes = await _svc.ExportToCsvAsync("CSVCity", null, null, CancellationToken.None);

            Assert.NotNull(bytes);
            var csv = Encoding.UTF8.GetString(bytes);
            Assert.Contains("CSVCity", csv);
            Assert.Contains("TemperatureCelsius", csv); // header present
        }
    }
}