namespace Cirreum.Authentication.EntraClaims;

/// <summary>
/// Resolves the application roles for an authenticated Entra External ID user.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to load the user's roles from your application's data store.
/// The resolved roles are added to the <see cref="System.Security.Claims.ClaimsPrincipal"/>
/// as <see cref="System.Security.Claims.ClaimTypes.Role"/> claims before ASP.NET authorization
/// policies and Cirreum domain authorization evaluate the request.
/// </para>
/// <para>
/// The result is cached per-request via <c>HttpContext.Items</c> — this method is called
/// at most once per HTTP request regardless of how many times the ASP.NET authentication
/// pipeline re-evaluates claims.
/// </para>
/// <para>
/// Register via <c>builder.AddEntraClaimsEnrichment&lt;TResolver&gt;()</c>.
/// </para>
/// </remarks>
public interface IEntraRoleResolver {

	/// <summary>
	/// Resolves the roles for the given Entra External ID user.
	/// </summary>
	/// <param name="entraUserId">
	/// The user's Entra object ID, sourced from the <c>oid</c> claim in the access token.
	/// This matches <see cref="IEntraProvisionedUser.EntraUserId"/> written during Phase 1.
	/// </param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>
	/// One or more role strings (e.g. <c>["app:user", "app:subscriber"]</c>), or <c>null</c>
	/// if the user does not exist in the application data store. An empty list is treated
	/// the same as <c>null</c> — authorization policies that require a role will deny the request.
	/// </returns>
	Task<IReadOnlyList<string>?> ResolveRolesAsync(string entraUserId, CancellationToken cancellationToken = default);

}
