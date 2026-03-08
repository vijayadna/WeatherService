using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using WeatherService.Application.DTO;
using WeatherService.Application.Services;

namespace WeatherService.API.Controllers
{
    /// <summary>
    /// Manage weather alert subscriptions and view triggered alert history.
    /// </summary>
    [ApiController]
    [Route("api/v1/alerts")]
    [Authorize]
    [Produces("application/json")]
    public class AlertController : ControllerBase
    {
        private readonly IAlertService _alerts;

        public AlertController(IAlertService alerts) => _alerts = alerts;

        // ─── POST /api/v1/alerts/subscriptions ───────────────────────────────────

        /// <summary>
        /// Create a new alert subscription. The system will evaluate thresholds every
        /// 15 minutes and record an alert event when conditions are met.
        /// </summary>
        [HttpPost("subscriptions")]
        [SwaggerOperation(Summary = "Create alert subscription", Tags = new[] { "Alerts" })]
        [SwaggerResponse(201, "Subscription created", typeof(AlertSubscriptionResponse))]
        [SwaggerResponse(400, "Validation error")]
        public async Task<IActionResult> Create(
            [FromBody] CreateAlertSubscriptionRequest request,
            CancellationToken ct)
        {
            var result = await _alerts.CreateSubscriptionAsync(request, ct);
            return CreatedAtAction(nameof(GetByEmail), new { email = result.SubscriberEmail }, result);
        }

        // ─── GET /api/v1/alerts/subscriptions ────────────────────────────────────

        /// <summary>
        /// List all alert subscriptions for a given email address.
        /// </summary>
        [HttpGet("subscriptions")]
        [SwaggerOperation(Summary = "List alert subscriptions by email", Tags = new[] { "Alerts" })]
        [SwaggerResponse(200, "List of subscriptions", typeof(IEnumerable<AlertSubscriptionResponse>))]
        public async Task<IActionResult> GetByEmail(
            [FromQuery] string email,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { error = "email is required." });

            var subs = await _alerts.GetSubscriptionsAsync(email, ct);
            return Ok(subs);
        }

        // ─── PATCH /api/v1/alerts/subscriptions/{id} ─────────────────────────────

        /// <summary>
        /// Update an existing subscription (pause/resume, change threshold).
        /// </summary>
        [HttpPatch("subscriptions/{id:int}")]
        [SwaggerOperation(Summary = "Update alert subscription", Tags = new[] { "Alerts" })]
        [SwaggerResponse(200, "Updated subscription", typeof(AlertSubscriptionResponse))]
        [SwaggerResponse(404, "Subscription not found")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateAlertSubscriptionRequest request,
            CancellationToken ct)
        {
            var result = await _alerts.UpdateSubscriptionAsync(id, request, ct);
            if (result is null) return NotFound(new { error = $"Subscription {id} not found." });
            return Ok(result);
        }

        // ─── DELETE /api/v1/alerts/subscriptions/{id} ────────────────────────────

        /// <summary>
        /// Delete an alert subscription.
        /// </summary>
        [HttpDelete("subscriptions/{id:int}")]
        [SwaggerOperation(Summary = "Delete alert subscription", Tags = new[] { "Alerts" })]
        [SwaggerResponse(204, "Deleted")]
        [SwaggerResponse(404, "Subscription not found")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var deleted = await _alerts.DeleteSubscriptionAsync(id, ct);
            if (!deleted) return NotFound(new { error = $"Subscription {id} not found." });
            return NoContent();
        }       
    }
}