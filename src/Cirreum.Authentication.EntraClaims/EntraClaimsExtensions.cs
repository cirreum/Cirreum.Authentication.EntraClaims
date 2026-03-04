namespace Cirreum.Authentication.EntraClaims;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

/// <summary>
/// Extension methods for registering the Entra External ID custom claims endpoint.
/// </summary>
/// <remarks>
/// See SETUP.md for full Azure Portal configuration, appsettings.json reference,
/// and troubleshooting guidance.
/// </remarks>
public static class EntraClaimsExtensions {

	/// <summary>
	/// Registers the Entra custom claims services and configuration.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="configuration">The configuration root.</param>
	/// <param name="sectionName">
	/// The configuration section name. Defaults to <c>"EntraClaims"</c>.
	/// See SETUP.md Part 3 for the full configuration schema.
	/// </param>
	public static IServiceCollection AddEntraClaims(
	  this IServiceCollection services,
	  IConfiguration configuration,
	  string sectionName = "EntraClaims") {

		services.Configure<EntraClaimsOptions>(configuration.GetSection(sectionName));

		services.AddSingleton<EntraTokenValidator>();
		services.AddScoped<EntraClaimsHandler>();

		return services;
	}

	/// <summary>
	/// Maps the anonymous Entra custom claims endpoint.
	/// Route is configurable via <see cref="EntraClaimsOptions.Route"/>.
	/// </summary>
	/// <remarks>
	/// Register this after <c>UseAuthentication</c> / <c>UseAuthorization</c>.
	/// The endpoint is registered as <c>AllowAnonymous</c> — all authentication is
	/// performed internally by validating the Entra bearer token. See SETUP.md Part 5.
	/// </remarks>
	public static IEndpointRouteBuilder MapEntraClaims(this IEndpointRouteBuilder app) {

		var options = app.ServiceProvider.GetRequiredService<IOptions<EntraClaimsOptions>>().Value;

		app.MapPost(options.Route, async (HttpRequest request, EntraClaimsHandler handler) =>
			await handler.HandleAsync(request))
		  .AllowAnonymous()
		  .ExcludeFromDescription(); // Hide from OpenAPI/Swagger

		return app;
	}

}