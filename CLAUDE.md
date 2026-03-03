# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Library Does

`Cirreum.Authentication.EntraClaims` is a drop-in .NET 10 library that handles Microsoft Entra External ID `onTokenIssuanceStart` custom authentication extension callbacks. It exposes a configurable anonymous minimal API endpoint that validates Entra's bearer token (full OIDC signature verification) and returns custom role claims for new user sign-ups.

## Common Commands

```powershell
# Build
dotnet build

# Pack NuGet package
dotnet pack src\Cirreum.Authentication.EntraClaims\Cirreum.Authentication.EntraClaims.csproj

# Restore
dotnet restore
```

There are no test projects in this repository.

## Project Structure

```
src\Cirreum.Authentication.EntraClaims\   # The library project
build\                                      # MSBuild props (versioning, packaging, SourceLink)
```

The `build\` directory contains imported `.props` files for package metadata, versioning, author info, icon, and SourceLink — not build scripts to run directly.

`src\Directory.Build.props` applies shared settings (net10.0, latest C#, nullable, XML docs) to all projects under `src\`.

## Architecture

The library follows a minimal, self-contained pattern. Consuming apps call two extension methods in `Program.cs`:

```csharp
builder.Services.AddEntraClaims(builder.Configuration);
app.MapEntraClaims();
```

### Request Flow

```
Entra Portal POST → anonymous minimal API endpoint
  → EntraTokenValidator  (singleton) — JWT signature + issuer/audience/appid validation via OIDC discovery
  → EntraClaimsHandler   (scoped)    — correlation ID, user ID, allowed app ID checks
  → EntraClaimsResponse              — returns custom role claims to Entra
```

### Key Types

| File | Type | Role |
|---|---|---|
| `EntraClaimsExtensions.cs` | static class | DI registration (`AddEntraClaims`) and endpoint mapping (`MapEntraClaims`) |
| `EntraClaimsOptions.cs` | `sealed class` | Configuration bound from `"EntraClaims"` config section |
| `EntraTokenValidator.cs` | `internal sealed class` | Singleton; validates JWT using `ConfigurationManager<OpenIdConnectConfiguration>` for signing key caching |
| `EntraClaimsHandler.cs` | `internal sealed class` | Scoped; orchestrates validation and builds the response |
| `EntraClaimsRequest.cs` | `internal sealed record`s | Incoming payload DTOs |
| `EntraClaimsResponse.cs` | `internal sealed record`s | Outgoing response DTOs |

### Configuration (`appsettings.json`)

```json
{
  "EntraClaims": {
    "Route": "/auth/entra/claims",
    "ClientId": "<api-app-registration-client-id>",
    "Issuer": "https://login.microsoftonline.com/<tenant-id>/v2.0",
    "EntraAppId": "99045fe1-7639-4a75-9d4a-577b6ca3810f",
    "MetadataEndpoint": "https://login.microsoftonline.com/<tenant-id>/v2.0/.well-known/openid-configuration",
    "DefaultRole": "Member",
    "AllowedAppIds": "<spa-client-id>,<other-app-id>"
  }
}
```

`EntraAppId` is Microsoft's fixed authentication events API app ID for Entra External ID (`99045fe1-7639-4a75-9d4a-577b6ca3810f`). `AllowedAppIds` is comma- or semicolon-separated.

### Security Design

The endpoint is intentionally anonymous — ASP.NET authentication middleware is bypassed. All authentication is performed inside `EntraTokenValidator` by validating the bearer token against Microsoft's OIDC metadata.
