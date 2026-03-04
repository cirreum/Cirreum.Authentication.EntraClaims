# Cirreum.Authentication.EntraClaims

[![NuGet Version](https://img.shields.io/nuget/v/Cirreum.Authentication.EntraClaims.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Authentication.EntraClaims/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cirreum.Authentication.EntraClaims.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Authentication.EntraClaims/)
[![GitHub Release](https://img.shields.io/github/v/release/cirreum/Cirreum.Authentication.EntraClaims?style=flat-square&labelColor=1F1F1F&color=FF3B2E)](https://github.com/cirreum/Cirreum.Authentication.EntraClaims/releases)
[![License](https://img.shields.io/github/license/cirreum/Cirreum.Authentication.EntraClaims?style=flat-square&labelColor=1F1F1F&color=F2F2F2)](https://github.com/cirreum/Cirreum.Authentication.EntraClaims/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-003D8F?style=flat-square&labelColor=1F1F1F)](https://dotnet.microsoft.com/)

**Drop-in Microsoft Entra External ID custom authentication extension handler for ASP.NET Core.**

## Overview

**Cirreum.Authentication.EntraClaims** handles `onTokenIssuanceStart` custom authentication extension callbacks from Microsoft Entra External ID. It registers a configurable anonymous minimal API endpoint that validates Entra's bearer token with full OIDC signature verification and returns custom role claims for new user sign-ups.

## Installation

```xml
<PackageReference Include="Cirreum.Authentication.EntraClaims" Version="1.0.0" />
```

## Usage

### Program.cs

```csharp
builder.Services.AddEntraClaims(builder.Configuration);

var app = builder.Build();

app.MapEntraClaims();
```

### appsettings.json

```json
{
  "EntraClaims": {
    "Route": "/auth/entra/claims",
    "ClientId": "<claims-provider-app-client-id>",
    "Issuer": "https://<tenant-id>.ciamlogin.com/<tenant-id>/v2.0",
    "EntraAppId": "99045fe1-7639-4a75-9d4a-577b6ca3810f",
    "MetadataEndpoint": "https://<tenant-id>.ciamlogin.com/<tenant-id>/v2.0/.well-known/openid-configuration",
    "DefaultRole": "app:user",
    "AllowedAppIds": "<client-app-id>"
  }
}
```

| Setting | Description |
|---|---|
| `Route` | The endpoint path. Defaults to `/auth/entra/claims`. Must match the Target URL on the custom extension. |
| `ClientId` | The Application (client) ID of your **custom claims provider** app registration (validated as `aud`). |
| `Issuer` | Your Entra External ID tenant issuer. Use the tenant ID subdomain format: `https://<tenant-id>.ciamlogin.com/<tenant-id>/v2.0`. |
| `EntraAppId` | The Microsoft authentication events API app ID (validated as `appid`/`azp`). Fixed value `99045fe1-7639-4a75-9d4a-577b6ca3810f` for all Entra External ID tenants. |
| `MetadataEndpoint` | OIDC discovery endpoint for fetching signing keys. |
| `DefaultRole` | The custom role assigned to new users. |
| `AllowedAppIds` | Comma or semicolon-separated client app IDs allowed to trigger this endpoint. |

> For full Azure Portal setup steps, ngrok local development guidance, and troubleshooting, see [SETUP.md](SETUP.md).

### Entra Portal Configuration

When registering the custom authentication extension in the Entra portal, set the **Target URL** to your API's public URL plus the configured route:

```
https://your-api.azurecontainerapps.io/auth/entra/claims
```

## Security

- **Full JWT signature validation** using Microsoft's published OIDC signing keys (cached via `ConfigurationManager`)
- **Issuer, audience, and lifetime validation** via `TokenValidationParameters`
- **App ID verification** ensures the token was issued by Microsoft's authentication events service
- **Allowed app list** restricts which client applications can trigger the claims endpoint
- **Anonymous endpoint** — authentication is handled entirely by the token validation logic, not by ASP.NET middleware

## Contribution Guidelines

1. **Be conservative with new abstractions**
   The API surface must remain stable and meaningful.

2. **Limit dependency expansion**
   Only add foundational, version-stable dependencies.

3. **Favor additive, non-breaking changes**
   Breaking changes ripple through the entire ecosystem.

4. **Include thorough unit tests**
   All primitives and patterns should be independently testable.

5. **Document architectural decisions**
   Context and reasoning should be clear for future maintainers.

6. **Follow .NET conventions**
   Use established patterns from Microsoft.Extensions.* libraries.

## Versioning

Cirreum.Authentication.EntraClaims follows [Semantic Versioning](https://semver.org/):

- **Major** - Breaking API changes
- **Minor** - New features, backward compatible
- **Patch** - Bug fixes, backward compatible

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Cirreum Foundation Framework**
*Layered simplicity for modern .NET*
