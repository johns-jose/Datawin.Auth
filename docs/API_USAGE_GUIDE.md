# DataWin.Auth - API Usage Guide

## Overview

This guide documents every endpoint exposed by the DataWin.Auth API, including request/response
schemas, authentication requirements, error codes, and integration examples.

**Base URL:** https://auth.datawin.io (production) or https://localhost:5001 (development)

---

## Authentication

Most endpoints require a Bearer JWT access token in the Authorization header:

    Authorization: Bearer eyJhbGciOiJIUzI1NiIs...

Endpoints marked **Anonymous** do not require authentication.

### Tenant Identification

Every request must identify the tenant. Use one of these methods:

1. **Header** (preferred): X-Tenant-Id: {guid}
2. **Request body**: Include tenantId in the JSON payload
3. **Route slug** (planned): /api/{tenant-slug}/auth/login

---

## Endpoints

### POST /api/auth/register

Register a new user account within a tenant. Encrypts PII, hashes the password,
and creates both the user and credential records.

**Auth:** Anonymous

**Request:**

    POST /api/auth/register
    Content-Type: application/json

    {
      "tenantId": "019568b0-0002-7000-8000-000000000002",
      "email": "jane.doe@acme-corp.com",
      "password": "P@ssw0rd!2025",
      "displayName": "Jane Doe"
    }

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| tenantId | UUID | Yes | The tenant to register under |
| email | string | Yes | User email address (stored encrypted, hashed for lookup) |
| password | string | Yes | User password (hashed with PBKDF2-SHA256) |
| displayName | string | No | Display name (stored encrypted) |

**Response 201 - Created:**

    {
      "isSuccess": true,
      "userId": "019568d0-0015-7000-8000-000000000015",
      "errorCode": null,
      "errorMessage": null
    }

**Response 409 - Conflict (duplicate email):**

    {
      "isSuccess": false,
      "userId": null,
      "errorCode": "email_exists",
      "errorMessage": "A user with this email already exists."
    }

**Error Codes:**

| Code | Description |
|------|-------------|
| email_exists | A user with this email is already registered in this tenant |
| tenant_not_found | The specified tenantId does not exist |
| validation_error | Request body validation failed |

**Registration Flow:**

1. Email is normalized to lowercase
2. SHA-256 hash of normalized email is computed for duplicate detection
3. If duplicate found, returns email_exists error
4. Email (and display name if provided) are encrypted with AES-256-GCM using tenant DEK
5. Password is hashed with PBKDF2-SHA256 (100,000 iterations, 16-byte salt, 32-byte hash)
6. User and credential are created in the tenant's regional database
7. Returns the new user's UUID v7

---

### POST /api/auth/login

Authenticate a user with email and password. Returns tokens on success, or an MFA challenge
if the user has multi-factor authentication enabled.

**Auth:** Anonymous

**Request:**

    POST /api/auth/login
    Content-Type: application/json

    {
      "tenantId": "019568b0-0002-7000-8000-000000000002",
      "email": "john.doe@acme-corp.com",
      "password": "P@ssw0rd!2025",
      "deviceFingerprint": "fp-browser-abc123",
      "audience": "acme-web-app"
    }

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| tenantId | UUID | Yes | The tenant to authenticate against |
| email | string | Yes | User email address |
| password | string | Yes | User password |
| deviceFingerprint | string | Yes | Unique client device identifier |
| audience | string | No | OAuth client_id for audience claim |

**Response 200 - Success (no MFA):**

    {
      "isSuccess": true,
      "accessToken": "eyJhbGciOiJIUzI1NiIs...",
      "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4...",
      "expiresIn": 900,
      "tokenType": "Bearer",
      "requiresMfa": false,
      "mfaChallengeToken": null,
      "errorCode": null,
      "errorMessage": null
    }

**Response 200 - MFA Required:**

    {
      "isSuccess": false,
      "accessToken": null,
      "refreshToken": null,
      "expiresIn": 0,
      "tokenType": null,
      "requiresMfa": true,
      "mfaChallengeToken": "YTJkNTY3ZTgtMzQ1Ni00...",
      "errorCode": null,
      "errorMessage": null
    }

When requiresMfa is true, the client must complete the MFA flow by calling
POST /api/mfa/verify with the mfaChallengeToken before tokens are issued.

**Response 401 - Authentication Failed:**

    {
      "isSuccess": false,
      "accessToken": null,
      "refreshToken": null,
      "expiresIn": 0,
      "tokenType": null,
      "requiresMfa": false,
      "mfaChallengeToken": null,
      "errorCode": "invalid_credentials",
      "errorMessage": "Invalid email or password."
    }

**Error Codes:**

| Code | Description |
|------|-------------|
| invalid_credentials | Email not found or wrong password |
| account_locked | Too many failed attempts, account temporarily locked |
| account_disabled | Account has been deactivated |
| tenant_not_found | The specified tenantId does not exist |

---

### POST /api/auth/logout

Revoke an active session and all associated refresh tokens.

**Auth:** Bearer token required

**Request:**

    POST /api/auth/logout
    Content-Type: application/json
    Authorization: Bearer eyJhbGciOi...

    {
      "tenantId": "019568b0-0002-7000-8000-000000000002",
      "userId": "019568d0-0011-7000-8000-000000000011",
      "sessionId": "019568f0-0040-7000-8000-000000000040"
    }

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| tenantId | UUID | Yes | Tenant ID |
| userId | UUID | Yes | User ID to log out |
| sessionId | UUID | Yes | Session to revoke |

**Response 204 - No Content**

Session and all refresh tokens for that session are revoked.

---

### POST /api/auth/refresh

Exchange a valid refresh token for new access and refresh tokens. Implements
refresh token rotation: the old refresh token is immediately revoked.

**Auth:** Anonymous

**Request:**

    POST /api/auth/refresh
    Content-Type: application/json

    {
      "tenantId": "019568b0-0002-7000-8000-000000000002",
      "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4..."
    }

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| tenantId | UUID | Yes | Tenant ID |
| refreshToken | string | Yes | The current refresh token |

**Response 200 - Success:**

    {
      "accessToken": "eyJhbGciOiJIUzI1NiIs...",
      "refreshToken": "bmV3IHJlZnJlc2ggdG9rZW4...",
      "tokenType": "Bearer",
      "expiresIn": 900
    }

IMPORTANT: The old refresh token is now invalid. The client MUST store the new
refreshToken for the next refresh cycle. Attempting to reuse the old token will
cause the entire session to be revoked (replay detection).

---

### POST /api/auth/external

Authenticate via an external identity provider (Google, Azure AD, Okta).
The client must have already completed the OAuth authorization code flow with
the provider and obtained an authorization code.

**Auth:** Anonymous

**Request:**

    POST /api/auth/external
    Content-Type: application/json

    {
      "tenantId": "019568b0-0002-7000-8000-000000000002",
      "provider": "google",
      "code": "4/0AY0e-g7...",
      "redirectUri": "https://app.acme-corp.com/auth/callback",
      "state": "random-state-value",
      "deviceFingerprint": "fp-browser-abc123"
    }

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| tenantId | UUID | Yes | Tenant ID |
| provider | string | Yes | Provider name: "google", "azure_ad", or "okta" |
| code | string | Yes | Authorization code from the IdP |
| redirectUri | string | Yes | Must match the redirect URI used in the auth request |
| state | string | No | CSRF state parameter (recommended) |
| deviceFingerprint | string | Yes | Unique client device identifier |

**Response 200 - Success:**

Same format as POST /api/auth/login success response.

If the external identity has not been linked to a local user account, a new user
is automatically created and linked.

**Supported Providers:**

| Provider | Value | Protocol |
|----------|-------|----------|
| Google | google | OAuth 2.0 / OIDC |
| Microsoft Entra ID | azure_ad | OpenID Connect |
| Okta | okta | OpenID Connect |

---

### POST /api/mfa/enroll

Enroll a new multi-factor authentication method for the authenticated user.

**Auth:** Bearer token required

**Request:**

    POST /api/mfa/enroll
    Content-Type: application/json
    Authorization: Bearer eyJhbGciOi...

    {
      "tenantId": "019568b0-0002-7000-8000-000000000002",
      "userId": "019568d0-0010-7000-8000-000000000010",
      "method": "totp"
    }

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| tenantId | UUID | Yes | Tenant ID |
| userId | UUID | Yes | User enrolling MFA |
| method | string | Yes | MFA method: "totp", "sms", "email", or "webauthn" |

**Response 200 - TOTP Enrollment:**

    {
      "setupUri": "otpauth://totp/DataWin:john.doe@acme.com?secret=JBSWY3DPEHPK3PXP&issuer=DataWin&algorithm=SHA1&digits=6&period=30"
    }

The client should render the setupUri as a QR code for the user to scan with
an authenticator app (Google Authenticator, Authy, Microsoft Authenticator, etc.).

**MFA Methods:**

| Method | Value | Description |
|--------|-------|-------------|
| TOTP | totp | Time-based one-time password (RFC 6238) |
| SMS | sms | One-time code sent via SMS |
| Email | email | One-time code sent via email |
| WebAuthn | webauthn | FIDO2 / passkey hardware key |

---

### POST /api/mfa/verify

Verify an MFA code to complete authentication. Called after POST /api/auth/login
returns requiresMfa: true.

**Auth:** Anonymous

**Request:**

    POST /api/mfa/verify
    Content-Type: application/json

    {
      "tenantId": "019568b0-0002-7000-8000-000000000002",
      "userId": "019568d0-0010-7000-8000-000000000010",
      "code": "123456",
      "method": "totp",
      "challengeToken": "YTJkNTY3ZTgtMzQ1Ni00..."
    }

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| tenantId | UUID | Yes | Tenant ID |
| userId | UUID | Yes | User completing MFA |
| code | string | Yes | The 6-digit TOTP code or one-time code |
| method | string | Yes | Must match the enrolled method |
| challengeToken | string | Yes | The mfaChallengeToken from the login response |

**Response 200 - Success:**

Same format as POST /api/auth/login success response. The access token will
include the claim mfa_verified: true.

---

### GET /api/user/profile

Retrieve the authenticated user's profile information.

**Auth:** Bearer token required

**Request:**

    GET /api/user/profile?tenantId=019568b0-0002-7000-8000-000000000002&userId=019568d0-0011-7000-8000-000000000011
    Authorization: Bearer eyJhbGciOi...

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| tenantId | UUID | Yes | Tenant ID (query string) |
| userId | UUID | Yes | User ID (query string) |

**Response 200:**

    {
      "userId": "019568d0-0011-7000-8000-000000000011",
      "tenantId": "019568b0-0002-7000-8000-000000000002",
      "email": "john.doe@acme-corp.com",
      "displayName": "John Doe",
      "phoneNumber": null,
      "mfaEnabled": false,
      "emailConfirmed": true,
      "createdAt": "2025-01-15T10:30:00Z"
    }

NOTE: The email, displayName, and phoneNumber fields are decrypted from
AES-256-GCM storage. This access is logged in the PII audit log.

---

### GET /api/user/export

Export all personal data for the authenticated user (GDPR Art. 15).

**Auth:** Bearer token required

**Request:**

    GET /api/user/export?tenantId=019568b0-0002-7000-8000-000000000002&userId=019568d0-0011-7000-8000-000000000011
    Authorization: Bearer eyJhbGciOi...

**Response 200:**

Returns a JSON document containing all PII, consent records, sessions, and
audit log entries for the user. This access is logged in the PII audit log.

---

### POST /api/consent/grant

Record that a user has granted consent for a specific purpose.

**Auth:** Bearer token required

**Request:**

    POST /api/consent/grant
    Content-Type: application/json
    Authorization: Bearer eyJhbGciOi...

    {
      "tenantId": "019568b0-0002-7000-8000-000000000002",
      "userId": "019568d0-0011-7000-8000-000000000011",
      "purpose": "marketing"
    }

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| tenantId | UUID | Yes | Tenant ID |
| userId | UUID | Yes | User granting consent |
| purpose | string | Yes | Consent purpose (see table below) |

**Consent Purposes:**

| Purpose | Description |
|---------|-------------|
| authentication | Required for login (implicit) |
| profile_data | Store and display profile information |
| marketing | Marketing communications |
| analytics | Usage analytics collection |
| third_party_sharing | Share data with third parties |

**Response 204 - No Content**

---

### POST /api/consent/withdraw

Withdraw a previously granted consent.

**Auth:** Bearer token required

**Request:**

    POST /api/consent/withdraw
    Content-Type: application/json
    Authorization: Bearer eyJhbGciOi...

    {
      "tenantId": "019568b0-0002-7000-8000-000000000002",
      "userId": "019568d0-0011-7000-8000-000000000011",
      "purpose": "marketing"
    }

**Response 204 - No Content**

The consent record is marked as withdrawn with a timestamp. Historical consent
records are preserved for audit compliance.

---

### GET /api/consent/status

Get all consent records for a user, including granted and withdrawn statuses.

**Auth:** Bearer token required

**Request:**

    GET /api/consent/status?tenantId=019568b0-0002-7000-8000-000000000002&userId=019568d0-0011-7000-8000-000000000011
    Authorization: Bearer eyJhbGciOi...

**Response 200:**

    {
      "userId": "019568d0-0011-7000-8000-000000000011",
      "consents": [
        {
          "purpose": "authentication",
          "isGranted": true,
          "grantedAt": "2025-01-15T10:30:00Z",
          "withdrawnAt": null
        },
        {
          "purpose": "profile_data",
          "isGranted": true,
          "grantedAt": "2025-01-15T10:30:00Z",
          "withdrawnAt": null
        },
        {
          "purpose": "marketing",
          "isGranted": false,
          "grantedAt": "2025-01-15T10:30:00Z",
          "withdrawnAt": "2025-02-01T14:00:00Z"
        }
      ]
    }

---

### POST /api/consent/erasure

Request erasure of all personal data (GDPR Art. 17 - Right to be Forgotten).
This initiates an asynchronous crypto-shredding process that destroys the
tenant-scoped encryption keys, making all PII permanently unrecoverable.

**Auth:** Bearer token required

**Request:**

    POST /api/consent/erasure
    Content-Type: application/json
    Authorization: Bearer eyJhbGciOi...

    {
      "tenantId": "019568b0-0002-7000-8000-000000000002",
      "userId": "019568d0-0011-7000-8000-000000000011"
    }

**Response 202 - Accepted:**

    {
      "requestId": "019568f0-0080-7000-8000-000000000080"
    }

The erasure request is queued for processing. Use the requestId to track status.

**Erasure Process:**

1. All active sessions for the user are revoked
2. All refresh tokens are invalidated
3. The user encryption DEK is destroyed (crypto-shredding)
4. All encrypted PII becomes permanently unreadable
5. The user account is deactivated
6. The erasure request status is updated to Completed

---

### POST /api/tenant

Onboard a new tenant (organization) with region assignment.

**Auth:** Bearer token required (admin role)

**Request:**

    POST /api/tenant
    Content-Type: application/json
    Authorization: Bearer eyJhbGciOi...

    {
      "name": "New Company Inc.",
      "slug": "new-company",
      "domain": "newcompany.com",
      "primaryRegion": "us-east-1",
      "additionalRegions": ["eu-west-1"]
    }

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| name | string | Yes | Display name of the tenant |
| slug | string | Yes | URL-safe unique identifier (lowercase, hyphens) |
| domain | string | No | Custom domain for tenant resolution |
| primaryRegion | string | Yes | Primary data region code |
| additionalRegions | string[] | No | Additional replica regions |

**Available Regions:**

| Code | Location |
|------|----------|
| us-east-1 | US East (Virginia) |
| eu-west-1 | EU West (Ireland) |
| ap-southeast-1 | Asia Pacific (Singapore) |
| ap-south-1 | Asia Pacific (Mumbai) |
| sa-east-1 | South America (Sao Paulo) |

**Response 201 - Created:**

    {
      "id": "019568b0-0006-7000-8000-000000000006",
      "name": "New Company Inc.",
      "slug": "new-company",
      "domain": "newcompany.com",
      "status": "Active",
      "regions": [
        { "regionCode": "us-east-1", "isPrimary": true },
        { "regionCode": "eu-west-1", "isPrimary": false }
      ]
    }

---

## Error Response Format

All error responses follow this format:

    {
      "isSuccess": false,
      "errorCode": "error_code_here",
      "errorMessage": "Human-readable error description."
    }

### Common Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| invalid_credentials | 401 | Wrong email or password |
| email_exists | 409 | Email already registered in this tenant |
| account_locked | 401 | Account locked after failed attempts |
| account_disabled | 401 | Account has been deactivated |
| tenant_not_found | 400 | Unknown tenant ID |
| invalid_token | 401 | JWT is expired, malformed, or revoked |
| mfa_required | 200 | Login succeeded but MFA verification needed |
| mfa_invalid_code | 401 | Wrong MFA code |
| consent_required | 403 | Required consent not granted |
| duplicate_slug | 409 | Tenant slug already exists |
| validation_error | 400 | Request body validation failed |

---

## JWT Token Structure

### Access Token Claims

Access tokens are JWTs signed with HMAC-SHA256. Decode the payload to access claims:

    {
      "sub": "019568d0-0011-7000-8000-000000000011",
      "tid": "019568b0-0002-7000-8000-000000000002",
      "region": "us-east-1",
      "mfa_verified": false,
      "role": ["user"],
      "jti": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "iss": "https://auth.datawin.io",
      "exp": 1705320600,
      "iat": 1705319700
    }

| Claim | Type | Description |
|-------|------|-------------|
| sub | string (UUID) | User ID |
| tid | string (UUID) | Tenant ID |
| region | string | Primary data region |
| mfa_verified | boolean | True if MFA was completed this session |
| role | string[] | Assigned roles |
| jti | string (UUID) | Unique token identifier |
| iss | string | Token issuer |
| exp | number | Expiry time (Unix timestamp) |
| iat | number | Issued-at time (Unix timestamp) |

### Token Lifetimes

| Token | Default Lifetime | Configurable |
|-------|-----------------|-------------|
| Access Token | 15 minutes (900s) | Jwt:AccessTokenLifetimeSeconds |
| Refresh Token | 30 days | Per OAuth client (refresh_token_lifetime_seconds) |
| MFA Challenge | 5 minutes | Fixed |

---

## Rate Limiting (Recommended Client Behavior)

While rate limiting is enforced server-side, clients should implement these practices:

| Endpoint | Recommendation |
|----------|---------------|
| /api/auth/login | Max 5 attempts per 15 minutes per email per tenant |
| /api/auth/refresh | Refresh only when access token is about to expire (< 60s) |
| /api/mfa/verify | Max 3 attempts per challenge token |
| All endpoints | Implement exponential backoff on 429 responses |

---

## Webhook Events (Planned)

The following events will be available for webhook subscriptions:

| Event | Trigger |
|-------|---------|
| user.registered | New user account created |
| user.authenticated | Successful login (including MFA) |
| user.locked | Account locked after failed attempts |
| user.mfa_enrolled | New MFA method enrolled |
| consent.granted | Consent granted for a purpose |
| consent.withdrawn | Consent withdrawn |
| erasure.requested | Data erasure request created |
| erasure.completed | Crypto-shredding completed |
| tenant.created | New tenant onboarded |