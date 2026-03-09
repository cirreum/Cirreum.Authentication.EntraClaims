# Cirreum.Authentication.EntraClaims

> **This package is deprecated.** All types are marked `[Obsolete]` and will not receive further updates. See [Migration](#migration) below.

[![NuGet Version](https://img.shields.io/nuget/v/Cirreum.Authentication.EntraClaims.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Authentication.EntraClaims/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cirreum.Authentication.EntraClaims.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Authentication.EntraClaims/)
[![License](https://img.shields.io/github/license/cirreum/Cirreum.Authentication.EntraClaims?style=flat-square&labelColor=1F1F1F&color=F2F2F2)](https://github.com/cirreum/Cirreum.Authentication.EntraClaims/blob/main/LICENSE)

## Migration

This package has been split into purpose-built replacements with cleaner naming, broader provider support, and better separation of concerns.

### Provisioning (onTokenIssuanceStart)

**Before:**
```csharp
builder.AddEntraClaims<AppUserProvisioner>();
app.MapEntraClaims();
```

**After:** Install [`Cirreum.Identity.EntraExternalId`](https://www.nuget.org/packages/Cirreum.Identity.EntraExternalId/)
```csharp
builder.AddEntraExternalId<AppUserProvisioner>();
app.MapEntraExternalId();
```

### Role Enrichment (API-side IClaimsTransformation)

**Before:**
```csharp
builder.AddEntraClaimsEnrichment<AppUserRoleResolver>();
```

**After:** Install [`Cirreum.Runtime.Authorization`](https://www.nuget.org/packages/Cirreum.Runtime.Authorization/)
```csharp
builder.AddAuthorization(auth => auth
    .AddRoleResolver<AppRoleResolver>());
```

The new `IRoleResolver` (in `Cirreum.AuthorizationProvider`) is provider-agnostic — it works with Entra, OIDC, External (BYOID), and any audience-based provider. No Entra-specific configuration needed.

### Type Mapping

| Old Type | Replacement | Package |
|----------|-------------|---------|
| `IEntraUserProvisioner` | `IUserProvisioner` | `Cirreum.Identity.EntraExternalId` |
| `IEntraProvisionedUser` | `IProvisionedUser` | `Cirreum.Identity.EntraExternalId` |
| `IEntraPendingInvitation` | `IPendingInvitation` | `Cirreum.Identity.EntraExternalId` |
| `EntraUserProvisionerBase<T>` | `UserProvisionerBase<T>` | `Cirreum.Identity.EntraExternalId` |
| `EntraClaimsContext` | `ProvisionContext` | `Cirreum.Identity.EntraExternalId` |
| `ProvisionResult` | `ProvisionResult` | `Cirreum.Identity.EntraExternalId` |
| `EntraClaimsOptions` | `EntraExternalIdOptions` | `Cirreum.Identity.EntraExternalId` |
| `EntraClaimsExtensions` | `EntraExternalIdExtensions` | `Cirreum.Identity.EntraExternalId` |
| `IEntraRoleResolver` | `IRoleResolver` | `Cirreum.AuthorizationProvider` |
| `EntraClaimsEnrichmentExtensions` | `AddRoleResolver<T>()` | `Cirreum.Runtime.Authorization` |

### Key Changes

- **`EntraUserId` → `ExternalUserId`** on all generic interfaces (`IProvisionedUser`, `ProvisionContext`, `UserProvisionerBase<T>`)
- **Config section** changed from `Cirreum:Authentication:EntraClaims` to `Cirreum:Identity:EntraExternalId`
- **Role enrichment** is no longer Entra-specific — `IRoleResolver` works across all audience-based providers

## License

MIT License — see the [LICENSE](LICENSE) file for details.

---

**Cirreum Foundation Framework**
*Layered simplicity for modern .NET*
