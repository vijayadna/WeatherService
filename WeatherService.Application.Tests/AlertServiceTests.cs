using Microsoft.Extensions.Logging;
using Moq;
using WeatherService.Application.DTO;
using WeatherService.Application.Services;
using WeatherService.Domain.Models;
using WeatherService.Infrastructure.Repositories;
using Xunit;

namespace WeatherService.Application.Tests
{
    public class AlertServiceTests
    {
        private readonly Mock<IWeatherRepository> _repoMock;
        private readonly Mock<IExternalWeatherService> _externalMock;
        private readonly Mock<ILogger<AlertService>> _loggerMock;
        private readonly AlertService _svc;

        public AlertServiceTests()
        {
            _repoMock = new Mock<IWeatherRepository>();
            _externalMock = new Mock<IExternalWeatherService>();
            _loggerMock = new Mock<ILogger<AlertService>>();

            _svc = new AlertService(
                _repoMock.Object,
                _externalMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task CreateSubscriptionAsync_ValidRequest_CallsRepositoryAndReturnsResponse()
        {
            // Arrange
            var req = new CreateAlertSubscriptionRequest(
                "me@example.com",
                "NYC",
                "temperature",
                "gt",
                30.5
            );

            _repoMock
                .Setup(r => r.CreateSubscriptionAsync(It.IsAny<AlertSubscription>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AlertSubscription s, CancellationToken ct) =>
                {
                    s.Id = 42;
                    return s;
                });

            // Act
            var res = await _svc.CreateSubscriptionAsync(req, CancellationToken.None);

            // Assert
            Assert.NotNull(res);
            Assert.Equal(42, res.Id);
            Assert.Equal(req.SubscriberEmail, res.SubscriberEmail);
            Assert.Equal(req.Location, res.Location);
            Assert.Equal(req.AlertType, res.AlertType);
            Assert.Equal(req.Operator, res.Operator);
            Assert.Equal(req.Threshold, res.Threshold);

            _repoMock.Verify(r => r.CreateSubscriptionAsync(It.IsAny<AlertSubscription>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("badtype")]
        [InlineData("")]
        public async Task CreateSubscriptionAsync_InvalidAlertType_ThrowsArgumentException(string alertType)
        {
            var req = new CreateAlertSubscriptionRequest(
                "me@example.com",
                "L",
                alertType,
                "gt",
                10
            );

            await Assert.ThrowsAsync<ArgumentException>(() => _svc.CreateSubscriptionAsync(req, CancellationToken.None));
            _repoMock.Verify(r => r.CreateSubscriptionAsync(It.IsAny<AlertSubscription>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData("badop")]
        [InlineData("")]
        public async Task CreateSubscriptionAsync_InvalidOperator_ThrowsArgumentException(string op)
        {
            var req = new CreateAlertSubscriptionRequest(
                "me@example.com",
                "L",
                "temperature",
                op,
                10
            );

            await Assert.ThrowsAsync<ArgumentException>(() => _svc.CreateSubscriptionAsync(req, CancellationToken.None));
            _repoMock.Verify(r => r.CreateSubscriptionAsync(It.IsAny<AlertSubscription>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetSubscriptionsAsync_ReturnsMappedResponses()
        {
            var subs = new[]
            {
                new AlertSubscription { Id = 1, SubscriberEmail = "a@a.com", Location = "X", AlertType = "temperature", Operator = "gt", Threshold = 10, IsActive = true, CreatedAtUtc = DateTime.UtcNow },
                new AlertSubscription { Id = 2, SubscriberEmail = "a@a.com", Location = "Y", AlertType = "rain", Operator = "eq", Threshold = 1, IsActive = false, CreatedAtUtc = DateTime.UtcNow }
            };

            _repoMock.Setup(r => r.GetSubscriptionsByEmailAsync("a@a.com", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(subs);

            var results = await _svc.GetSubscriptionsAsync("a@a.com", CancellationToken.None);

            Assert.Equal(2, results.Count());
            Assert.Contains(results, r => r.Id == 1 && r.Location == "X");
            Assert.Contains(results, r => r.Id == 2 && r.Location == "Y");
        }

        [Fact]
        public async Task UpdateSubscriptionAsync_NotFound_ReturnsNull()
        {
            _repoMock.Setup(r => r.GetSubscriptionByIdAsync(10, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((AlertSubscription?)null);

            var updated = await _svc.UpdateSubscriptionAsync(10, new UpdateAlertSubscriptionRequest(null, null, null), CancellationToken.None);
            Assert.Null(updated);
        }

        [Fact]
        public async Task UpdateSubscriptionAsync_ValidUpdate_UpdatesRepoAndReturnsResponse()
        {
            var sub = new AlertSubscription
            {
                Id = 5,
                SubscriberEmail = "x@y.com",
                Location = "Z",
                AlertType = "temperature",
                Operator = "gt",
                Threshold = 1.0, // Changed from 1.0m (decimal) to 1.0 (double)
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            _repoMock.Setup(r => r.GetSubscriptionByIdAsync(5, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(sub);

            _repoMock.Setup(r => r.UpdateSubscriptionAsync(It.IsAny<AlertSubscription>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask)
                     .Verifiable();

            var req = new UpdateAlertSubscriptionRequest(
                false,
                2.5,
                "lt"
            );

            var res = await _svc.UpdateSubscriptionAsync(5, req, CancellationToken.None);

            Assert.NotNull(res);
            Assert.Equal(5, res!.Id);
            Assert.False(res.IsActive);
            Assert.Equal(2.5, res.Threshold);
            Assert.Equal("lt", res.Operator);

            _repoMock.Verify();
        }

        [Fact]
        public async Task DeleteSubscriptionAsync_NotFound_ReturnsFalse()
        {
            _repoMock.Setup(r => r.GetSubscriptionByIdAsync(77, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((AlertSubscription?)null);

            var result = await _svc.DeleteSubscriptionAsync(77, CancellationToken.None);
            Assert.False(result);
            _repoMock.Verify(r => r.DeleteSubscriptionAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteSubscriptionAsync_Found_DeletesAndReturnsTrue()
        {
            var sub = new AlertSubscription { Id = 9 };
            _repoMock.Setup(r => r.GetSubscriptionByIdAsync(9, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(sub);
            _repoMock.Setup(r => r.DeleteSubscriptionAsync(9, It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask)
                     .Verifiable();

            var result = await _svc.DeleteSubscriptionAsync(9, CancellationToken.None);
            Assert.True(result);
            _repoMock.Verify();
        }
    }
}