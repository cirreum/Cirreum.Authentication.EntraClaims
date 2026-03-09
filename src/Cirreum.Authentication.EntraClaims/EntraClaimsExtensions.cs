namespace Cirreum.Authentication.EntraClaims;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

/// <summary>
/// Extension methods for registering the Entra External ID custom claims endpoint.
/// </summary>
/// <remarks>
/// See SETUP.md for full Azure Portal configuration, appsettings.json reference,
/// and troubleshooting guidance.
/// </remarks>
[Obsolete("Use EntraExternalIdExtensions from Cirreum.Identity.EntraExternalId instead.")]
public static class EntraClaimsExtensions {

	// Sentinel registered alongside the provisioner so MapEntraClaims can validate
	// at startup that a provisioner has been registered.
	private sealed class EntraProvisionerMarker { }

	// -------------------------------------------------------------------------
	// IHostApplicationBuilder overloads (primary — WebApplicationBuilder etc.)
	// -------------------------------------------------------------------------

	/// <summary>
	/// Registers Entra custom claims services, configuration, and the
	/// <see cref="IEntraUserProvisioner"/> implementation that controls user access.
	/// </summary>
	/// <typeparam name="TProvisioner">
	/// The provisioner implementation. Must implement <see cref="IEntraUserProvisioner"/>.
	/// Register as scoped to allow access to database contexts and other request-scoped services.
	/// </typeparam>
	/// <param name="builder">The application builder (<c>WebApplicationBuilder</c> etc.).</param>
	/// <param name="sectionName">
	/// The configuration section name. Defaults to
	/// <c>"Cirreum:Authentication:EntraClaims"</c>.
	/// See SETUP.md Part 3 for the full configuration schema.
	/// </param>
	public static IHostApplicationBuilder AddEntraClaims<TProvisioner>(
		this IHostApplicationBuilder builder,
		string sectionName = "Cirreum:Authentication:EntraClaims")
		where TProvisioner : class, IEntraUserProvisioner {
		builder.Services.AddEntraClaims<TProvisioner>(builder.Configuration, sectionName);
		return builder;
	}

	/// <summary>
	/// Registers Entra custom claims services, configuration, and the
	/// <see cref="IEntraUserProvisioner"/> implementation using a factory function.
	/// </summary>
	/// <typeparam name="TProvisioner">
	/// The provisioner implementation. Must implement <see cref="IEntraUserProvisioner"/>.
	/// </typeparam>
	/// <param name="builder">The application builder (<c>WebApplicationBuilder</c> etc.).</param>
	/// <param name="factory">Factory function to create the provisioner instance.</param>
	/// <param name="sectionName">
	/// The configuration section name. Defaults to
	/// <c>"Cirreum:Authentication:EntraClaims"</c>.
	/// See SETUP.md Part 3 for the full configuration schema.
	/// </param>
	public static IHostApplicationBuilder AddEntraClaims<TProvisioner>(
		this IHostApplicationBuilder builder,
		Func<IServiceProvider, TProvisioner> factory,
		string sectionName = "Cirreum:Authentication:EntraClaims")
		where TProvisioner : class, IEntraUserProvisioner {
		builder.Services.AddEntraClaims<TProvisioner>(builder.Configuration, factory, sectionName);
		return builder;
	}

	// -------------------------------------------------------------------------
	// IServiceCollection overloads (for testing and advanced scenarios)
	// -------------------------------------------------------------------------

	/// <summary>
	/// Registers Entra custom claims services, configuration, and the
	/// <see cref="IEntraUserProvisioner"/> implementation that controls user access.
	/// </summary>
	/// <typeparam name="TProvisioner">
	/// The provisioner implementation. Must implement <see cref="IEntraUserProvisioner"/>.
	/// Register as scoped to allow access to database contexts and other request-scoped services.
	/// </typeparam>
	/// <param name="services">The service collection.</param>
	/// <param name="configuration">The configuration root.</param>
	/// <param name="sectionName">
	/// The configuration section name. Defaults to
	/// <c>"Cirreum:Authentication:EntraClaims"</c>.
	/// See SETUP.md Part 3 for the full configuration schema.
	/// </param>
	public static IServiceCollection AddEntraClaims<TProvisioner>(
		this IServiceCollection services,
		IConfiguration configuration,
		string sectionName = "Cirreum:Authentication:EntraClaims")
		where TProvisioner : class, IEntraUserProvisioner {
		services.Configure<EntraClaimsOptions>(configuration.GetSection(sectionName));
		services.AddSingleton<EntraTokenValidator>();
		services.AddScoped<EntraClaimsHandler>();
		services.AddScoped<IEntraUserProvisioner, TProvisioner>();
		services.AddSingleton<EntraProvisionerMarker>();
		return services;
	}

	/// <summary>
	/// Registers Entra custom claims services, configuration, and the
	/// <see cref="IEntraUserProvisioner"/> implementation using a factory function.
	/// </summary>
	/// <typeparam name="TProvisioner">
	/// The provisioner implementation. Must implement <see cref="IEntraUserProvisioner"/>.
	/// </typeparam>
	/// <param name="services">The service collection.</param>
	/// <param name="configuration">The configuration root.</param>
	/// <param name="factory">Factory function to create the provisioner instance.</param>
	/// <param name="sectionName">
	/// The configuration section name. Defaults to
	/// <c>"Cirreum:Authentication:EntraClaims"</c>.
	/// See SETUP.md Part 3 for the full configuration schema.
	/// </param>
	public static IServiceCollection AddEntraClaims<TProvisioner>(
		this IServiceCollection services,
		IConfiguration configuration,
		Func<IServiceProvider, TProvisioner> factory,
		string sectionName = "Cirreum:Authentication:EntraClaims")
		where TProvisioner : class, IEntraUserProvisioner {
		services.Configure<EntraClaimsOptions>(configuration.GetSection(sectionName));
		services.AddSingleton<EntraTokenValidator>();
		services.AddScoped<EntraClaimsHandler>();
		services.AddScoped<IEntraUserProvisioner>(factory);
		services.AddSingleton<EntraProvisionerMarker>();
		return services;
	}

	// -------------------------------------------------------------------------
	// Endpoint mapping
	// -------------------------------------------------------------------------

	/// <summary>
	/// Maps the anonymous Entra custom claims endpoint.
	/// Route is configurable via <see cref="EntraClaimsOptions.Route"/>.
	/// </summary>
	/// <remarks>
	/// Register this after <c>UseAuthentication</c> / <c>UseAuthorization</c>.
	/// The endpoint is registered as <c>AllowAnonymous</c> — all authentication is
	/// performed internally by validating the Entra bearer token. See SETUP.md Part 5.
	/// </remarks>
	/// <exception cref="InvalidOperationException">
	/// Thrown if <c>AddEntraClaims&lt;TProvisioner&gt;</c> has not been called.
	/// </exception>
	public static IEndpointRouteBuilder MapEntraClaims(this IEndpointRouteBuilder app) {
		if (app.ServiceProvider.GetService<EntraProvisionerMarker>() is null) {
			throw new InvalidOperationException(
				"No IEntraUserProvisioner has been registered. " +
				"Call builder.AddEntraClaims<TProvisioner>() before calling app.MapEntraClaims().");
		}

		var options = app.ServiceProvider.GetRequiredService<IOptions<EntraClaimsOptions>>().Value;
		app.MapPost(options.Route, async (HttpRequest request, EntraClaimsHandler handler, CancellationToken cancellationToken) =>
			await handler.HandleAsync(request, cancellationToken))
			.AllowAnonymous()
			.ExcludeFromDescription(); // Hide from OpenAPI/Swagger
		return app;
	}

}
