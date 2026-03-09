namespace Cirreum.Authentication.EntraClaims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for registering the Entra External ID server-side claims enrichment.
/// </summary>
/// <remarks>
/// <para>
/// Call <c>AddEntraClaimsEnrichment&lt;TResolver&gt;()</c> on the API that validates
/// Entra External ID access tokens. This registers an <see cref="IClaimsTransformation"/>
/// that runs during <c>UseAuthentication()</c> and adds the user's application role
/// as a <c>ClaimTypes.Role</c> claim — enabling ASP.NET authorization policies and
/// Cirreum domain authorization to work identically for external and internal users.
/// </para>
/// <para>
/// Can be used alongside <c>AddEntraClaims&lt;TProvisioner&gt;()</c> on the same app,
/// or independently on an API that does not host the <c>onTokenIssuanceStart</c> endpoint.
/// Both methods share the same configuration section.
/// </para>
/// </remarks>
[Obsolete("Use AddRoleResolver<T>() from Cirreum.Runtime.Authorization instead.")]
public static class EntraClaimsEnrichmentExtensions {

	// -------------------------------------------------------------------------
	// IHostApplicationBuilder overloads (primary — WebApplicationBuilder etc.)
	// -------------------------------------------------------------------------

	/// <summary>
	/// Registers Entra External ID server-side claims enrichment using the
	/// <typeparamref name="TResolver"/> implementation to load roles from the application data store.
	/// </summary>
	/// <typeparam name="TResolver">
	/// The resolver implementation. Must implement <see cref="IEntraRoleResolver"/>.
	/// Registered as scoped to allow access to database contexts and other request-scoped services.
	/// </typeparam>
	/// <param name="builder">The application builder (<c>WebApplicationBuilder</c> etc.).</param>
	/// <param name="sectionName">
	/// The configuration section name. Defaults to <c>"Cirreum:Authentication:EntraClaims"</c>.
	/// Must contain at least <c>Issuer</c> to identify Entra External ID tokens.
	/// </param>
	public static IHostApplicationBuilder AddEntraClaimsEnrichment<TResolver>(
		this IHostApplicationBuilder builder,
		string sectionName = "Cirreum:Authentication:EntraClaims")
		where TResolver : class, IEntraRoleResolver {
		builder.Services.AddEntraClaimsEnrichment<TResolver>(builder.Configuration, sectionName);
		return builder;
	}

	/// <summary>
	/// Registers Entra External ID server-side claims enrichment using a factory function.
	/// </summary>
	/// <typeparam name="TResolver">
	/// The resolver implementation. Must implement <see cref="IEntraRoleResolver"/>.
	/// </typeparam>
	/// <param name="builder">The application builder (<c>WebApplicationBuilder</c> etc.).</param>
	/// <param name="factory">Factory function to create the resolver instance.</param>
	/// <param name="sectionName">
	/// The configuration section name. Defaults to <c>"Cirreum:Authentication:EntraClaims"</c>.
	/// </param>
	public static IHostApplicationBuilder AddEntraClaimsEnrichment<TResolver>(
		this IHostApplicationBuilder builder,
		Func<IServiceProvider, TResolver> factory,
		string sectionName = "Cirreum:Authentication:EntraClaims")
		where TResolver : class, IEntraRoleResolver {
		builder.Services.AddEntraClaimsEnrichment<TResolver>(builder.Configuration, factory, sectionName);
		return builder;
	}

	// -------------------------------------------------------------------------
	// IServiceCollection overloads (for testing and advanced scenarios)
	// -------------------------------------------------------------------------

	/// <summary>
	/// Registers Entra External ID server-side claims enrichment using the
	/// <typeparamref name="TResolver"/> implementation to load roles from the application data store.
	/// </summary>
	/// <typeparam name="TResolver">
	/// The resolver implementation. Must implement <see cref="IEntraRoleResolver"/>.
	/// </typeparam>
	/// <param name="services">The service collection.</param>
	/// <param name="configuration">The configuration root.</param>
	/// <param name="sectionName">
	/// The configuration section name. Defaults to <c>"Cirreum:Authentication:EntraClaims"</c>.
	/// </param>
	public static IServiceCollection AddEntraClaimsEnrichment<TResolver>(
		this IServiceCollection services,
		IConfiguration configuration,
		string sectionName = "Cirreum:Authentication:EntraClaims")
		where TResolver : class, IEntraRoleResolver {
		services.Configure<EntraClaimsOptions>(configuration.GetSection(sectionName));
		services.AddHttpContextAccessor();
		services.AddScoped<IEntraRoleResolver, TResolver>();
		services.AddScoped<IClaimsTransformation, EntraClaimsEnricher>();
		return services;
	}

	/// <summary>
	/// Registers Entra External ID server-side claims enrichment using a factory function.
	/// </summary>
	/// <typeparam name="TResolver">
	/// The resolver implementation. Must implement <see cref="IEntraRoleResolver"/>.
	/// </typeparam>
	/// <param name="services">The service collection.</param>
	/// <param name="configuration">The configuration root.</param>
	/// <param name="factory">Factory function to create the resolver instance.</param>
	/// <param name="sectionName">
	/// The configuration section name. Defaults to <c>"Cirreum:Authentication:EntraClaims"</c>.
	/// </param>
	public static IServiceCollection AddEntraClaimsEnrichment<TResolver>(
		this IServiceCollection services,
		IConfiguration configuration,
		Func<IServiceProvider, TResolver> factory,
		string sectionName = "Cirreum:Authentication:EntraClaims")
		where TResolver : class, IEntraRoleResolver {
		services.Configure<EntraClaimsOptions>(configuration.GetSection(sectionName));
		services.AddHttpContextAccessor();
		services.AddScoped<IEntraRoleResolver>(factory);
		services.AddScoped<IClaimsTransformation, EntraClaimsEnricher>();
		return services;
	}

}
