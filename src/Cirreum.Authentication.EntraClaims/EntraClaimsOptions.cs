namespace Cirreum.Authentication.EntraClaims;

/// <summary>
/// Configuration options for the Entra External ID custom claims endpoint.
/// Bind from appsettings.json under the section name specified during registration.
/// </summary>
public sealed class EntraClaimsOptions {

	/// <summary>
	/// The endpoint route path. Defaults to "/auth/entra/claims".
	/// </summary>
	public string Route { get; set; } = "/auth/entra/claims";

	/// <summary>
	/// Your API's Entra App Registration Client ID (validated as the "aud" claim).
	/// </summary>
	public required string ClientId { get; set; }

	/// <summary>
	/// The expected token issuer URL for your Entra tenant.
	/// Example: "https://login.microsoftonline.com/{tenantId}/v2.0"
	/// </summary>
	public required string Issuer { get; set; }

	/// <summary>
	/// The Microsoft Entra application ID that issues the callback token.
	/// Validated against the "appid" (v1) or "azp" (v2) claim.
	/// Example: "99045fe1-7639-4a75-9d4a-577b6ca3810f" (Microsoft Authentication Events API)
	/// </summary>
	public required string EntraAppId { get; set; }

	/// <summary>
	/// The OIDC metadata endpoint for fetching signing keys.
	/// Example: "https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration"
	/// </summary>
	public required string MetadataEndpoint { get; set; }

	/// <summary>
	/// The default custom role assigned to new users during sign-up.
	/// </summary>
	public required string DefaultRole { get; set; }

	/// <summary>
	/// Comma or semicolon-separated list of allowed application IDs
	/// that can trigger this claims endpoint.
	/// </summary>
	public required string AllowedAppIds { get; set; }

	/// <summary>
	/// Parses AllowedAppIds into a set for fast lookup.
	/// </summary>
	internal HashSet<string> GetAllowedAppIdSet() =>
	  [.. this.AllowedAppIds.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
}
