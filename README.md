# Cirreum.Authentication.EntraClaims

[![NuGet Version](https://img.shields.io/nuget/v/Cirreum.Authentication.EntraClaims.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Authentication.EntraClaims/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cirreum.Authentication.EntraClaims.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Authentication.EntraClaims/)
[![GitHub Release](https://img.shields.io/github/v/release/cirreum/Cirreum.Authentication.EntraClaims?style=flat-square&labelColor=1F1F1F&color=FF3B2E)](https://github.com/cirreum/Cirreum.Authentication.EntraClaims/releases)
[![License](https://img.shields.io/github/license/cirreum/Cirreum.Authentication.EntraClaims?style=flat-square&labelColor=1F1F1F&color=F2F2F2)](https://github.com/cirreum/Cirreum.Authentication.EntraClaims/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-003D8F?style=flat-square&labelColor=1F1F1F)](https://dotnet.microsoft.com/)

**Drop-in Microsoft Entra External ID custom authentication extension handler for ASP.NET Core.**

## Overview

`Cirreum.Authentication.EntraClaims` handles `onTokenIssuanceStart` custom authentication extension callbacks from Microsoft Entra External ID. It validates Entra's bearer token with full OIDC signature verification and delegates access control to your application via a simple provisioner interface — returning custom role claims for the issued token.

Authenticating successfully against Entra does **not** grant access to your application. Your provisioner decides.

## Installation

```xml
<PackageReference Include="Cirreum.Authentication.EntraClaims" Version="1.0.0" />
```

## Quick Start

### 1. Implement a provisioner

```csharp
public sealed class AppUserProvisioner(
    IUserRepository users,
    IInvitationService invitations
) : IEntraUserProvisioner {

    public async Task<ProvisionResult> ProvisionAsync(
        EntraClaimsContext context,
        CancellationToken cancellationToken = default) {

        var existing = await users.FindByEntraIdAsync(context.EntraUserId, cancellationToken);
        if (existing is not null) {
            return ProvisionResult.Allow(existing.Role);
        }

        var invitation = await invitations.RedeemAsync(context.Email, context.EntraUserId, cancellationToken);
        if (invitation is not null) {
            return ProvisionResult.Allow(invitation.Role);
        }

        return ProvisionResult.Deny();
    }
}
```

### 2. Register and map in Program.cs

```csharp
builder.AddEntraClaims<AppUserProvisioner>();

var app = builder.Build();

// after UseAuthentication / UseAuthorization
app.MapEntraClaims();
```

### 3. Configure appsettings.json

```json
{
  "Cirreum": {
    "Authentication": {
      "EntraClaims": {
        "Route": "/auth/entra/claims",
        "ClientId": "<claims-provider-app-client-id>",
        "Issuer": "https://<tenant-id>.ciamlogin.com/<tenant-id>/v2.0",
        "EntraAppId": "99045fe1-7639-4a75-9d4a-577b6ca3810f",
        "MetadataEndpoint": "https://<tenant-id>.ciamlogin.com/<tenant-id>/v2.0/.well-known/openid-configuration",
        "AllowedAppIds": "<client-app-id>"
      }
    }
  }
}
```

| Setting | Description |
|---|---|
| `Route` | Endpoint path. Defaults to `/auth/entra/claims`. Must match the Target URL on the Custom Authentication Extension in the Azure Portal. |
| `ClientId` | Application (client) ID of your **custom claims provider** app registration. Validated as the `aud` claim on the incoming bearer token. |
| `Issuer` | Entra External ID tenant issuer. **Must** use the tenant ID subdomain format — do not use a domain name. |
| `EntraAppId` | Microsoft's authentication events API app ID, validated as `appid`/`azp`. Fixed value `99045fe1-7639-4a75-9d4a-577b6ca3810f` for all Entra External ID tenants. |
| `MetadataEndpoint` | OIDC discovery endpoint for fetching and caching signing keys. Use the tenant ID subdomain format. |
| `AllowedAppIds` | Comma- or semicolon-separated list of client application IDs permitted to trigger this endpoint. |

> For full Azure Portal setup, ngrok local development guidance, and troubleshooting see [SETUP.md](SETUP.md).

---

## Provisioner Design

### IEntraUserProvisioner

Implement `IEntraUserProvisioner` directly for full control:

```csharp
public interface IEntraUserProvisioner {
    Task<ProvisionResult> ProvisionAsync(
        EntraClaimsContext context,
        CancellationToken cancellationToken = default);
}
```

`EntraClaimsContext` provides:

| Property | Description |
|---|---|
| `EntraUserId` | Entra object ID of the authenticating user. Store as `ExternalId` on your user record. |
| `Email` | Email address from the Entra payload. May be empty for social identity providers with email sharing disabled. |
| `CorrelationId` | Request correlation ID for end-to-end tracing. |
| `ClientAppId` | App ID of the client application the user is signing into. |

Return `ProvisionResult.Allow("role:name")` to permit the login, or `ProvisionResult.Deny()` to block it.

### EntraUserProvisionerBase&lt;TUser&gt;

For the standard invitation-redemption pattern, inherit from `EntraUserProvisionerBase<TUser>` instead of implementing `IEntraUserProvisioner` directly. The base class handles the two-path provisioning flow:

1. **Returning user** — look up by Entra object ID → if found, issue the stored role
2. **New user via invitation** — find, validate, and claim a pending invitation by email → create user record → issue the invitation's role
3. **Everything else** → deny

Implement two abstract methods:

```csharp
public sealed class AppUserProvisioner(AppDbContext db) : EntraUserProvisionerBase<AppUser> {

    protected override Task<AppUser?> FindUserAsync(
        string entraUserId,
        CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(u => u.EntraUserId == entraUserId, cancellationToken);

    protected override async Task<AppUser?> RedeemInvitationAsync(
        string email,
        string entraUserId,
        CancellationToken cancellationToken) {
        // Find, validate, claim, and create user atomically in one transaction
        var invitation = await db.Invitations
            .FirstOrDefaultAsync(
                i => i.Email == email.ToLowerInvariant()
                  && i.ClaimedAt == null
                  && i.ExpiresAt > DateTimeOffset.UtcNow,
                cancellationToken);
        if (invitation is null) {
            return null;
        }
        invitation.ClaimedAt = DateTimeOffset.UtcNow;
        invitation.ClaimedByEntraUserId = entraUserId;
        var user = new AppUser { EntraUserId = entraUserId, Role = invitation.Role };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }
}
```

`RedeemInvitationAsync` returns `null` to deny. Returning a user causes `ProvisionResult.Allow(user.Role)` to be issued.

> **Atomicity:** Perform the invitation find, expiry check, and claim in a single database transaction to prevent concurrent logins from redeeming the same invitation twice.

### IEntraProvisionedUser

Implement on your user entity so `EntraUserProvisionerBase<TUser>` can read the role:

```csharp
public record AppUser : IEntraProvisionedUser {
    public string EntraUserId { get; init; } = "";  // IEntraProvisionedUser
    public string Role { get; init; } = "";          // IEntraProvisionedUser
    // ... your other fields
}
```

### IEntraPendingInvitation

A modeling guide for your invitation entity. Not used as a generic constraint — documents the fields expected by the invitation pattern:

```csharp
public record Invitation : IEntraPendingInvitation {
    public string Email { get; init; } = "";
    public string Role { get; init; } = "";
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset? ClaimedAt { get; set; }
    public string? ClaimedByEntraUserId { get; set; }
    public DateTimeOffset? RedeemedAt { get; set; }
    // ... your other fields (e.g. CompanyId, InvitedBy)
}
```

`IsExpired` is provided as a default interface method (`ExpiresAt < DateTimeOffset.UtcNow`). Override it if you need additional expiry conditions (e.g. already claimed, already redeemed).

---

## Two-Phase Onboarding

The provisioner handles **Phase 1** only — gating access and issuing the initial role claim. A complete onboarding flow looks like:

```
Phase 1 — onTokenIssuanceStart (this library)
  ├── RedeemInvitationAsync: mark invitation as Claimed, create user record
  └── Return role → embedded in ID token

Phase 2 — in-app onboarding endpoint (your application)
  ├── Collect remaining profile data
  ├── Call Microsoft Graph API to assign user to the Entra app role group
  │   (required for the role to appear in the access token)
  └── Mark invitation as Redeemed
```

**Why two phases?** The Entra user object may not exist yet when Phase 1 fires, and group membership changes do not propagate to access tokens instantly. Phase 2 completes after the user is inside the application.

During the gap between Phase 1 and Phase 2 completion, onboarding endpoints should use a minimal auth policy (authenticated user, no role requirement) to allow the user through before Graph API assignment is complete.

---

## Request Flow

```
Entra POST → anonymous endpoint
  1. Validate bearer token (OIDC signature, issuer, audience, lifetime, appid/azp)
  2. Deserialize request payload
  3. Validate CorrelationId and User.Id presence
  4. Verify calling app is in AllowedAppIds
  5. Call IEntraUserProvisioner.ProvisionAsync
     ├── Allowed (with roles) → embed roles in token response → 200
     ├── Denied              → 403
     └── Exception / no roles → 500
```

---

## Advanced Registration

### Factory overload

```csharp
builder.AddEntraClaims<AppUserProvisioner>(
    sp => new AppUserProvisioner(sp.GetRequiredService<AppDbContext>()));
```

### IServiceCollection overload (for testing)

```csharp
services.AddEntraClaims<AppUserProvisioner>(configuration);
```

### Custom configuration section

```csharp
builder.AddEntraClaims<AppUserProvisioner>("MyApp:EntraExtension");
```

---

## Security

- Full JWT signature validation using Microsoft's published OIDC signing keys, fetched and cached via `ConfigurationManager<OpenIdConnectConfiguration>`
- Issuer, audience, and token lifetime validated on every request
- `appid`/`azp` claim verified against Microsoft's fixed authentication events service app ID
- Allowed app list restricts which client applications can trigger the endpoint
- The endpoint is registered as `AllowAnonymous` — all authentication is performed internally, not by ASP.NET middleware

---

## Contribution Guidelines

1. **Be conservative with new abstractions** — the API surface must remain stable and meaningful
2. **Limit dependency expansion** — only add foundational, version-stable dependencies
3. **Favor additive, non-breaking changes** — breaking changes ripple through the ecosystem
4. **Include thorough unit tests** — all primitives and patterns should be independently testable
5. **Document architectural decisions** — context and reasoning should be clear for future maintainers
6. **Follow .NET conventions** — use established patterns from `Microsoft.Extensions.*` libraries

## Versioning

Follows [Semantic Versioning](https://semver.org/): **Major** for breaking API changes, **Minor** for backward-compatible new features, **Patch** for bug fixes.

## License

MIT License — see the [LICENSE](LICENSE) file for details.

---

**Cirreum Foundation Framework**
*Layered simplicity for modern .NET*
