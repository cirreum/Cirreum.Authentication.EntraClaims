# Cirreum.Authentication.EntraClaims

Setup guide for integrating Entra External ID custom claims into an ASP.NET Core Minimal API using the `OnTokenIssuanceStart` custom authentication extension.

---

## Overview

This package provides a pre-built endpoint that Entra External ID calls at token issuance to inject custom claims (e.g. application roles) into the user's token. It handles bearer token validation, payload deserialization, and response formatting per the Microsoft Graph `onTokenIssuanceStart` contract.

**Flow:**

```
User signs in → Entra External ID → POST /auth/entra/claims → Your API → Custom claims returned → Token issued to user
```

---

## Prerequisites

- An **Entra External ID** tenant (CIAM)
- An existing **app registration** for the client application (SPA, API, etc.)
- An **ngrok** tunnel or publicly accessible HTTPS endpoint for local development

---

## Part 1 — Azure Portal Configuration

### 1.1 App Registration — Client App

In your client app registration manifest, ensure the following is set:

```json
"api": {
    "acceptMappedClaims": true
}
```

> ⚠️ Without `acceptMappedClaims: true`, Entra will return `AADSTS50146` and the token will not be issued even if your claims endpoint returns 200.

### 1.2 Create the Custom Claims Provider App Registration

This is a **separate** app registration used solely to authenticate the incoming request from Entra to your API endpoint.

1. Go to **App registrations → New registration**
2. Name it something like `MyApp-CustomClaims-Provider`
3. Under **Expose an API**, add the scope used by Entra to call your endpoint
4. Note the **Application (client) ID** — this is your `ClientId` config value

### 1.3 Create the Custom Authentication Extension

1. Go to **Enterprise Applications → Custom authentication extensions → New**
2. Select **TokenIssuanceStart** as the event type
3. Configure:
   - **Target URL**: your HTTPS endpoint (e.g. `https://your-api.com/auth/entra/claims`)
   - **API Authentication App ID**: the app registration from step 1.2
   - **Timeout**: 2000ms recommended
   - **Max Retries**: 1
4. Under **Attributes**, add `customRoles` (or your claim name)
5. Note the **Custom extension ID**

### 1.4 Grant Admin Consent

On the custom claims provider app registration:

1. Go to **API permissions**
2. Verify `CustomAuthenticationExt` (Microsoft Graph Application permission) is present
3. Click **Grant admin consent for {tenant}**

### 1.5 User Flow Configuration

1. Go to **External Identities → User flows → {your flow} → Identity providers**
2. Set **Email Accounts** to **Email one-time passcode**

> ⚠️ Using **Email with password** causes Entra to send a `username` parameter to Google OAuth, resulting in `Error 400: invalid_request / flowName=GeneralOAuthFlow`.

### 1.6 Associate the Extension with the User Flow

The `TokenIssuanceStart` event is **not** configured in the user flow's "Custom authentication extensions" page (that page only covers attribute collection events).

The binding is made when you create the Custom Authentication Extension — Entra links it to the target application at creation time. Verify by checking that the extension's target application matches your client app registration's client ID.

### 1.7 SSO Claims Mapping (Optional)

If you need the custom claim to appear in the SSO claims mapping UI:

1. Go to the **Enterprise Application** for your client app
2. **Single sign-on → Attributes & Claims → Add a claim**
3. Set **Source** to `Attribute` and select `customclaimsprovider.customRoles` as the source attribute

> The source attribute only appears once the custom extension has its **Attributes** schema populated and is correctly bound to the app.

---

## Part 2 — Package Installation

```shell
dotnet add package Cirreum.Authentication.EntraClaims
```

---

## Part 3 — Configuration

Add the following to `appsettings.json`:

```json
"Cirreum": {
    "Authentication": {
        "EntraClaims": {
            "Route": "/auth/entra/claims",
            "ClientId": "<your-claims-provider-app-client-id>",
            "Issuer": "https://<tenant-id>.ciamlogin.com/<tenant-id>/v2.0",
            "MetadataEndpoint": "https://<tenant-id>.ciamlogin.com/<tenant-id>/v2.0/.well-known/openid-configuration",
            "EntraAppId": "99045fe1-7639-4a75-9d4a-577b6ca3810f",
            "DefaultRole": "app:user",
            "AllowedAppIds": "<client-app-id>"
        }
    }
}
```

| Field | Description |
|---|---|
| `Route` | The endpoint path Entra will call. Must match the Target URL on the extension. |
| `ClientId` | The Application (client) ID of your **custom claims provider** app registration. Validated as the `aud` claim on the incoming bearer token. |
| `Issuer` | The token issuer. Use the **tenant ID subdomain** format (`<tenant-id>.ciamlogin.com`), not the domain name format. |
| `MetadataEndpoint` | OIDC discovery endpoint used to fetch signing keys. |
| `EntraAppId` | The Microsoft-owned app ID that Entra uses when calling your endpoint. This is `99045fe1-7639-4a75-9d4a-577b6ca3810f` for all Entra External ID tenants. |
| `DefaultRole` | The role assigned to all users on first sign-in. |
| `AllowedAppIds` | Comma-separated list of client app IDs allowed to trigger this endpoint. |

> ⚠️ **Issuer format matters.** The issuer in the incoming token from Entra uses the tenant ID subdomain format (`<tenant-id>.ciamlogin.com`), not the domain name format (`correxternaldev.ciamlogin.com`). Using the wrong format causes silent token validation failure and a hung request.

---

## Part 4 — Service Registration

In your `Program.cs` or host startup:

```csharp
builder.Services.AddEntraClaims(builder.Configuration);
```

---

## Part 5 — Endpoint Registration

After middleware is configured:

```csharp
app.UseAuthentication();
app.UseAuthorization();

// Register after auth middleware, before RunAsync
app.MapEntraClaims();
```

The endpoint is registered as `AllowAnonymous` and excluded from OpenAPI documentation automatically.

---

## Part 6 — Local Development with ngrok

Entra requires a publicly accessible HTTPS endpoint. Use ngrok for local development.

**Important:** Always forward to HTTPS, not HTTP:

```shell
ngrok http --url=<your-static-domain>.ngrok-free.app https://localhost:<port>
```

> ⚠️ Forwarding to `http://localhost` instead of `https://localhost` causes the request to hang indefinitely with no response, as the OIDC metadata fetch inside the token validator will time out.

Update your Custom Authentication Extension's **Target URL** to your ngrok URL.

---

## Troubleshooting

### Request hangs with no response
- Verify ngrok is forwarding to `https://` not `http://`
- Verify the `MetadataEndpoint` is reachable from the API server process

### `AADSTS50146` — application-specific signing key required
- Set `"acceptMappedClaims": true` in the client app registration manifest under the `api` section

### `Error 400: invalid_request / flowName=GeneralOAuthFlow` from Google
- Set the user flow's Email Accounts to **Email one-time passcode** instead of **Email with password**

### Token validation fails silently (no warnings logged)
- Verify the `Issuer` uses the tenant ID subdomain format: `https://<tenant-id>.ciamlogin.com/<tenant-id>/v2.0`
- Do not use the domain name format (`correxternaldev.ciamlogin.com`)

### Claims endpoint returns 200 but token still has no custom claims
- Verify `"acceptMappedClaims": true` in the client app manifest
- Verify the custom extension's **Attributes** schema has the claim name defined
- Verify the custom extension is targeting the correct client app

### `customclaimsprovider.*` attributes not visible in SSO claims mapping UI
- The extension's **Attributes** schema must be populated
- The extension must be bound to the correct target application
- Each extension exposes its own namespace — verify you're selecting from the correct extension

### Admin consent error on sign-in
- Verify the Blazor app's `ClientId` config matches the **client app** registration, not the claims provider app registration

---

## Architecture Notes

- Entra calls the claims endpoint **twice** per sign-in (retry behavior). This is expected and both calls will succeed.
- The endpoint validates the bearer token using OIDC discovery (signing keys fetched at startup and cached).
- Response model types use `[JsonSerializable]` source generation for AOT/trimming compatibility. Do not use `Results.Ok()` with `internal` types — use `Results.Content(JsonSerializer.Serialize(...), "application/json")` instead.
- The `EntraAppId` (`99045fe1-...`) is a Microsoft-owned constant across all Entra External ID tenants and does not change per-application.
