namespace Cirreum.Authentication.EntraClaims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;

/// <summary>
/// ASP.NET <see cref="IClaimsTransformation"/> that enriches Entra External ID access tokens
/// with a role claim sourced from the application's data store.
/// </summary>
/// <remarks>
/// Runs during <c>UseAuthentication()</c>, before <c>UseAuthorization()</c> evaluates
/// endpoint policies. The result is cached in <c>HttpContext.Items</c> — the resolver
/// is invoked at most once per request.
/// </remarks>
internal sealed partial class EntraClaimsEnricher(
	IEntraRoleResolver resolver,
	IHttpContextAccessor httpContextAccessor,
	IOptions<EntraClaimsOptions> options,
	ILogger<EntraClaimsEnricher> logger
) : IClaimsTransformation {

	private const string EnrichedKey = "__EntraClaims_Enriched";

	private readonly HashSet<string> _enrichmentIssuers = options.Value.GetEnrichmentIssuerSet();

	public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal) {
		var context = httpContextAccessor.HttpContext;
		if (context is null || context.Items.ContainsKey(EnrichedKey)) {
			return principal;
		}

		// Mark immediately — prevents re-entry if ASP.NET calls TransformAsync again
		// on the same request before the async work completes.
		context.Items[EnrichedKey] = true;

		// Skip: workforce or internally-assigned user already has roles in the token.
		if (principal.HasClaim(c => c.Type == ClaimTypes.Role)) {
			return principal;
		}

		// Skip: issuer is not in the configured enrichment set.
		var issuer = principal.FindFirstValue("iss");
		if (issuer is null || !_enrichmentIssuers.Contains(issuer)) {
			return principal;
		}

		var oid = principal.FindFirstValue("oid");
		if (oid is null) {
			return principal;
		}

		var roles = await resolver.ResolveRolesAsync(oid);
		if (roles is null or { Count: 0 }) {
			Log.NoRolesResolved(logger, oid);
			return principal;
		}

		if (principal.Identity is ClaimsIdentity identity) {
			foreach (var role in roles) {
				identity.AddClaim(new Claim(ClaimTypes.Role, role));
			}
			var roleString = string.Join(", ", roles);
			Log.RolesResolved(logger, roleString, oid);
		}

		return principal;
	}

	private static partial class Log {
		[LoggerMessage(Level = LogLevel.Warning,
			Message = "No roles resolved for Entra External ID user '{EntraUserId}'. Authorization policies requiring a role will deny this request.")]
		internal static partial void NoRolesResolved(ILogger logger, string entraUserId);

		[LoggerMessage(Level = LogLevel.Debug,
			Message = "Resolved roles '{Roles}' for Entra External ID user '{EntraUserId}'.")]
		internal static partial void RolesResolved(ILogger logger, string roles, string entraUserId);
	}

}
