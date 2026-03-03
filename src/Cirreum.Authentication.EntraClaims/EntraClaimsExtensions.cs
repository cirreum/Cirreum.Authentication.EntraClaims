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
public static class EntraClaimsExtensions {

	/// <summary>
	/// Registers the Entra custom claims services and configuration.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="configuration">The configuration root.</param>
	/// <param name="sectionName">
	/// The configuration section name. Defaults to "EntraClaims".
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
	public static IEndpointRouteBuilder MapEntraClaims(this IEndpointRouteBuilder app) {

		var options = app.ServiceProvider.GetRequiredService<IOptions<EntraClaimsOptions>>().Value;

		app.MapPost(options.Route, async (HttpRequest request, EntraClaimsHandler handler) =>
			await handler.HandleAsync(request))
		  .AllowAnonymous()
		  .ExcludeFromDescription(); // Hide from OpenAPI/Swagger

		return app;
	}

}