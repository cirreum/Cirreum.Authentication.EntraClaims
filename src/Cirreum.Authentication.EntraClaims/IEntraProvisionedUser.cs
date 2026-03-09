namespace Cirreum.Authentication.EntraClaims;

/// <summary>
/// Constrains the user entity returned by <see cref="EntraUserProvisionerBase{TUser}"/>
/// to expose the fields required for token issuance.
/// </summary>
/// <remarks>
/// Implement this interface on whatever user entity your application stores in its database.
/// The base provisioner only reads <see cref="EntraUserId"/> for lookup and <see cref="Roles"/>
/// for the issued token — all other fields on your type are invisible to this library.
/// </remarks>
/// <example>
/// <code>
/// public sealed class AppUser : IEntraProvisionedUser {
///     public Guid Id { get; init; }
///     public string EntraUserId { get; init; } = "";  // stored as ExternalId
///     public string Email { get; init; } = "";
///     public IReadOnlyList&lt;string&gt; Roles { get; init; } = [AppRoles.User];
///     // ... other application fields
/// }
/// </code>
/// </example>
[Obsolete("Use IProvisionedUser from Cirreum.Identity.EntraExternalId instead.")]
public interface IEntraProvisionedUser {

	/// <summary>
	/// Gets the Entra object ID of the user, as stored in the application database.
	/// </summary>
	/// <remarks>
	/// This must match the value of <see cref="EntraClaimsContext.EntraUserId"/> from the
	/// token issuance callback. Consuming apps typically store this as <c>ExternalId</c>
	/// or <c>EntraUserId</c> on their user record.
	/// </remarks>
	string EntraUserId { get; }

	/// <summary>
	/// Gets the roles to embed in the issued token for this user.
	/// </summary>
	/// <remarks>
	/// Each value must be a non-empty string matching a role defined in your application.
	/// Use application-level constants (e.g. <c>AppRoles.Admin</c>) rather than magic strings.
	/// Most users have a single role; return a list with one entry for the common case.
	/// </remarks>
	IReadOnlyList<string> Roles { get; }

}
