namespace Cirreum.Authentication.EntraClaims;

using Cirreum.Authentication.EntraClaims.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Handles the Entra External ID onTokenIssuanceStart callback.
/// Validates the bearer token, verifies the calling app is allowed,
/// and returns a default custom role claim for new user sign-ups.
/// </summary>
internal sealed class EntraClaimsHandler(
  EntraTokenValidator tokenValidator,
  IOptions<EntraClaimsOptions> options,
  ILogger<EntraClaimsHandler> logger
) {

	public async Task<IResult> HandleAsync(HttpRequest request) {

		// Extract and validate bearer token
		if (!request.Headers.TryGetValue("Authorization", out var authHeader)
		  || string.IsNullOrWhiteSpace(authHeader)) {
			logger.LogWarning("Missing Authorization header");
			return Results.Unauthorized();
		}

		var token = authHeader.ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
		if (!await tokenValidator.ValidateAsync(token)) {
			logger.LogWarning("Invalid Authorization token");
			return Results.Unauthorized();
		}

		// Deserialize the Entra callback payload
		var payload = await request.ReadFromJsonAsync<EntraClaimsRequest>();
		if (payload is null) {
			logger.LogWarning("Failed to deserialize request body");
			return Results.BadRequest("Invalid request body");
		}

		var context = payload.Data.AuthenticationContext;

		// Validate correlation ID
		if (string.IsNullOrWhiteSpace(context.CorrelationId)) {
			logger.LogWarning("Missing CorrelationId in request");
			return Results.BadRequest("Missing CorrelationId");
		}

		// Validate user ID
		if (string.IsNullOrWhiteSpace(context.User.Id)) {
			logger.LogWarning("Missing User Id in request");
			return Results.BadRequest("Missing User Id");
		}

		// Validate calling app is allowed
		var config = options.Value;
		var allowedApps = config.GetAllowedAppIdSet();
		if (!allowedApps.Contains(context.ClientServicePrincipal.AppId)) {
			logger.LogWarning("App '{AppId}' is not in the allowed list", context.ClientServicePrincipal.AppId);
			return Results.Forbid();
		}

		if (logger.IsEnabled(LogLevel.Information)) {
			logger.LogInformation(
		  "Issuing default role '{Role}' for user '{UserId}' (correlation: {CorrelationId})",
		  config.DefaultRole, context.User.Id, context.CorrelationId);
		}

		// Return the custom claims response
		var response = new EntraClaimsResponse {
			Data = new EntraResponseData {
				Actions = [
			  new EntraTokenAction {
			Claims = new EntraCustomClaims {
			  CorrelationId = context.CorrelationId,
			  CustomRoles = [config.DefaultRole]
			}
		  }
			]
			}
		};

		return Results.Ok(response);
	}

}