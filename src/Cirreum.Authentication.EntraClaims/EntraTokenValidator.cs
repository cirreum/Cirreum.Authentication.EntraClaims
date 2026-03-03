namespace Cirreum.Authentication.EntraClaims;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

/// <summary>
/// Validates the bearer token sent by Entra External ID during the
/// onTokenIssuanceStart custom authentication extension callback.
/// Uses OIDC discovery to fetch and cache Microsoft's signing keys.
/// </summary>
internal sealed class EntraTokenValidator {

  private readonly ConfigurationManager<OpenIdConnectConfiguration> _configManager;
  private readonly EntraClaimsOptions _options;
  private readonly ILogger<EntraTokenValidator> _logger;

  public EntraTokenValidator(
    IOptions<EntraClaimsOptions> options,
    ILogger<EntraTokenValidator> logger) {

    _options = options.Value;
    _logger = logger;

    _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
      _options.MetadataEndpoint,
      new OpenIdConnectConfigurationRetriever(),
      new HttpDocumentRetriever());
  }

  public async Task<bool> ValidateAsync(string token) {
    try {
      var config = await _configManager.GetConfigurationAsync(CancellationToken.None);

      var validationParameters = new TokenValidationParameters {
        ValidateIssuer = true,
        ValidIssuer = _options.Issuer,
        ValidateAudience = true,
        ValidAudience = _options.ClientId,
        ValidateIssuerSigningKey = true,
        IssuerSigningKeys = config.SigningKeys,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(5)
      };

      var handler = new JwtSecurityTokenHandler();
      var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);

      if (validatedToken is not JwtSecurityToken jwt) {
        _logger.LogWarning("Token is not a valid JWT");
        return false;
      }

      // Validate appid (v1) or azp (v2) matches the Entra service app ID
      var appId = jwt.Claims.FirstOrDefault(c => c.Type is "appid")?.Value
        ?? jwt.Claims.FirstOrDefault(c => c.Type is "azp")?.Value;

      if (appId != _options.EntraAppId) {
        _logger.LogWarning("Token appid/azp '{AppId}' does not match expected '{Expected}'", appId, _options.EntraAppId);
        return false;
      }

      return true;

    } catch (SecurityTokenValidationException ex) {
      _logger.LogWarning(ex, "Token validation failed: {Message}", ex.Message);
      return false;
    } catch (Exception ex) {
      _logger.LogError(ex, "Unexpected error during token validation: {Message}", ex.Message);
      return false;
    }
  }
}
