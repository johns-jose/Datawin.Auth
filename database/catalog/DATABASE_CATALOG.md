# DataWin.Auth — Database Catalog

## Overview

The DataWin.Auth framework uses a **split-database architecture**:

- **Global Database** (`datawin_auth_global`) — Tenant registry, region mapping, endpoint routing.
  One instance worldwide. Contains no PII.
- **Regional Databases** (`datawin_auth_{region}`) — User data, sessions, credentials, consent,
  audit logs. One instance per compliance region. All PII is AES-256-GCM encrypted at field level.

All primary keys are **UUID v7** (RFC 9562) — time-ordered, globally unique, no index fragmentation.

---

## Enum Reference

### TenantStatus
| Value | Name        | Description                      |
|-------|-------------|----------------------------------|
| 0     | Active      | Tenant is operational            |
| 1     | Suspended   | Temporarily disabled by admin    |
| 2     | Deactivated | Permanently decommissioned       |

### AuthSchemeType
| Value | Name            | Description                      |
|-------|-----------------|----------------------------------|
| 0     | Internal        | Email/password (local)           |
| 1     | OAuth2          | OAuth 2.0 Authorization Code     |
| 2     | OpenIdConnect   | OpenID Connect                   |
| 3     | Saml2           | SAML 2.0 SSO                     |
| 10    | ExternalGoogle  | Google OAuth                     |
| 11    | ExternalAzureAd | Microsoft Entra ID (Azure AD)    |
| 12    | ExternalOkta    | Okta OIDC/OAuth                  |

### MfaMethod
| Value | Name     | Description                        |
|-------|----------|------------------------------------|
| 0     | None     | No MFA                             |
| 1     | Totp     | Time-based OTP (RFC 6238)          |
| 2     | Sms      | SMS one-time code                  |
| 3     | Email    | Email one-time code                |
| 4     | WebAuthn | FIDO2 / passkey                    |

### ConsentPurpose
| Value | Name              | Description                       |
|-------|-------------------|-----------------------------------|
| 0     | Authentication    | Required for login                 |
| 1     | ProfileData       | Store/display profile information  |
| 2     | Marketing         | Marketing communications           |
| 3     | Analytics         | Usage analytics collection         |
| 4     | ThirdPartySharing | Share data with third parties      |

### ErasureStatus
| Value | Name       | Description                        |
|-------|------------|------------------------------------|
| 0     | Requested  | Awaiting processing                |
| 1     | InProgress | Crypto-shredding underway          |
| 2     | Completed  | DEK destroyed, PII unrecoverable   |
| 3     | Failed     | Processing error, needs retry      |

---

## Global Database: `datawin_auth_global`

### Table: `tenants`
Registered tenants (organizations) using DataWin products.

| Column     | Type          | Nullable | Default | Description                          |
|------------|---------------|----------|---------|--------------------------------------|
| id         | UUID          | NO       | —       | PK. UUID v7.                         |
| name       | VARCHAR(200)  | NO       | —       | Display name of the tenant.          |
| slug       | VARCHAR(100)  | NO       | —       | URL-safe unique identifier. UNIQUE.  |
| domain     | VARCHAR(256)  | YES      | NULL    | Custom domain for tenant resolution. |
| status     | INT           | NO       | 0       | TenantStatus enum.                   |
| created_at | TIMESTAMPTZ   | NO       | NOW()   | Row creation timestamp.              |
| updated_at | TIMESTAMPTZ   | NO       | NOW()   | Last modification timestamp.         |

**Indexes:**
- `idx_tenants_slug` — B-tree on `slug`
- `idx_tenants_domain` — Partial B-tree on `domain` WHERE domain IS NOT NULL

---

### Table: `tenant_regions`
Maps each tenant to one or more compliance regions. One region must be primary.

| Column      | Type         | Nullable | Default | Description                          |
|-------------|--------------|----------|---------|--------------------------------------|
| id          | UUID         | NO       | —       | PK. UUID v7.                         |
| tenant_id   | UUID         | NO       | —       | FK → tenants(id) ON DELETE CASCADE.  |
| region_code | VARCHAR(50)  | NO       | —       | Region identifier (e.g. "eu-west-1").|
| is_primary  | BOOLEAN      | NO       | FALSE   | Only one per tenant should be TRUE.  |
| created_at  | TIMESTAMPTZ  | NO       | NOW()   | Row creation timestamp.              |

**Constraints:** UNIQUE(tenant_id, region_code)
**Indexes:**
- `idx_tenant_regions_tenant` — B-tree on `tenant_id`
- `idx_tenant_regions_primary` — Partial on `tenant_id` WHERE is_primary = TRUE

---

### Table: `regional_endpoints`
Connection metadata for each regional database instance.

| Column            | Type         | Nullable | Default          | Description                              |
|-------------------|--------------|----------|------------------|------------------------------------------|
| id                | UUID         | NO       | gen_random_uuid()| PK. Auto-generated.                      |
| region_code       | VARCHAR(50)  | NO       | —                | Region identifier. UNIQUE.               |
| connection_string | TEXT         | NO       | —                | PostgreSQL connection string for region.  |
| is_active         | BOOLEAN      | NO       | TRUE             | FALSE to disable routing to this region. |
| created_at        | TIMESTAMPTZ  | NO       | NOW()            | Row creation timestamp.                  |

---

## Regional Database: `datawin_auth_{region}`

### Table: `users`
User accounts. PII fields (email, display_name, phone) are AES-256-GCM encrypted.
Lookup is via `email_hash` (SHA-256 of lowercase email).

| Column                | Type         | Nullable | Default | Description                          |
|-----------------------|--------------|----------|---------|--------------------------------------|
| id                    | UUID         | NO       | —       | PK. UUID v7.                         |
| tenant_id             | UUID         | NO       | —       | Owning tenant.                       |
| email_cipher          | BYTEA        | NO       | —       | AES-GCM ciphertext of email.        |
| email_nonce           | BYTEA        | NO       | —       | 12-byte AES-GCM nonce.              |
| email_tag             | BYTEA        | NO       | —       | 16-byte AES-GCM auth tag.           |
| email_key_id          | VARCHAR(100) | NO       | —       | DEK identifier used for encryption.  |
| email_hash            | VARCHAR(64)  | NO       | —       | SHA-256 of lowercase email (lookup). |
| display_name_cipher   | BYTEA        | YES      | NULL    | Encrypted display name.              |
| display_name_nonce    | BYTEA        | YES      | NULL    | Nonce for display name.              |
| display_name_tag      | BYTEA        | YES      | NULL    | Auth tag for display name.           |
| display_name_key_id   | VARCHAR(100) | YES      | NULL    | DEK id for display name.             |
| phone_cipher          | BYTEA        | YES      | NULL    | Encrypted phone number.              |
| phone_nonce           | BYTEA        | YES      | NULL    | Nonce for phone.                     |
| phone_tag             | BYTEA        | YES      | NULL    | Auth tag for phone.                  |
| phone_key_id          | VARCHAR(100) | YES      | NULL    | DEK id for phone.                    |
| is_active             | BOOLEAN      | NO       | TRUE    | Account enabled flag.                |
| email_confirmed       | BOOLEAN      | NO       | FALSE   | Email verification status.           |
| mfa_enabled           | BOOLEAN      | NO       | FALSE   | Whether any MFA method is enrolled.  |
| failed_login_attempts | INT          | NO       | 0       | Consecutive failures. Resets on success.|
| lockout_end           | TIMESTAMPTZ  | YES      | NULL    | Account locked until this time.      |
| created_at            | TIMESTAMPTZ  | NO       | NOW()   | Row creation timestamp.              |
| updated_at            | TIMESTAMPTZ  | NO       | NOW()   | Last modification timestamp.         |

**Indexes:**
- `idx_users_tenant` — B-tree on `tenant_id`
- `idx_users_email_hash` — UNIQUE on (tenant_id, email_hash)

---

### Table: `user_credentials`
Password hashes. One row per user. Algorithm is always Argon2id.

| Column         | Type         | Nullable | Default    | Description                       |
|----------------|--------------|----------|------------|-----------------------------------|
| id             | UUID         | NO       | —          | PK. UUID v7.                      |
| user_id        | UUID         | NO       | —          | FK → users(id) ON DELETE CASCADE. |
| tenant_id      | UUID         | NO       | —          | Owning tenant.                    |
| password_hash  | TEXT         | NO       | —          | Format: base64(salt).base64(hash).|
| algorithm      | VARCHAR(20)  | NO       | 'argon2id' | Hash algorithm identifier.        |
| recovery_codes | TEXT         | YES      | NULL       | Backup codes (encrypted or hashed).|
| created_at     | TIMESTAMPTZ  | NO       | NOW()      | Row creation timestamp.           |
| updated_at     | TIMESTAMPTZ  | NO       | NOW()      | Last modification timestamp.      |

**Indexes:** `idx_user_credentials_user` — UNIQUE on (tenant_id, user_id)

---

### Table: `user_mfa`
MFA enrollments. A user can have multiple methods enrolled simultaneously.

| Column        | Type         | Nullable | Default | Description                         |
|---------------|--------------|----------|---------|-------------------------------------|
| id            | UUID         | NO       | —       | PK. UUID v7.                        |
| user_id       | UUID         | NO       | —       | FK → users(id) ON DELETE CASCADE.   |
| tenant_id     | UUID         | NO       | —       | Owning tenant.                      |
| method        | INT          | NO       | —       | MfaMethod enum.                     |
| secret_cipher | BYTEA        | NO       | —       | AES-GCM encrypted TOTP/FIDO secret.|
| secret_nonce  | BYTEA        | NO       | —       | 12-byte nonce.                      |
| secret_tag    | BYTEA        | NO       | —       | 16-byte auth tag.                   |
| secret_key_id | VARCHAR(100) | NO       | —       | DEK identifier.                     |
| is_verified   | BOOLEAN      | NO       | FALSE   | Set TRUE after first successful use.|
| created_at    | TIMESTAMPTZ  | NO       | NOW()   | Row creation timestamp.             |

**Indexes:** `idx_user_mfa_user` — B-tree on (tenant_id, user_id)

---

### Table: `sessions`
Active login sessions. Bound to device and IP.

| Column             | Type         | Nullable | Default | Description                        |
|--------------------|--------------|----------|---------|------------------------------------|
| id                 | UUID         | NO       | —       | PK. UUID v7.                       |
| user_id            | UUID         | NO       | —       | FK → users(id) ON DELETE CASCADE.  |
| tenant_id          | UUID         | NO       | —       | Owning tenant.                     |
| device_fingerprint | VARCHAR(256) | NO       | —       | Client device identifier.          |
| ip_address         | VARCHAR(45)  | NO       | —       | IPv4 or IPv6 address.              |
| user_agent         | TEXT         | YES      | NULL    | Browser/client user agent string.  |
| is_revoked         | BOOLEAN      | NO       | FALSE   | TRUE after logout or forced revoke.|
| created_at         | TIMESTAMPTZ  | NO       | NOW()   | Session start time.                |
| expires_at         | TIMESTAMPTZ  | NO       | —       | Absolute session expiry.           |

**Indexes:**
- `idx_sessions_user` — B-tree on (tenant_id, user_id)
- `idx_sessions_active` — Partial on `tenant_id` WHERE is_revoked = FALSE

---

### Table: `refresh_tokens`
Rotating refresh tokens with chain tracking for replay detection.

| Column               | Type         | Nullable | Default | Description                           |
|----------------------|--------------|----------|---------|---------------------------------------|
| id                   | UUID         | NO       | —       | PK. UUID v7.                          |
| session_id           | UUID         | NO       | —       | FK → sessions(id) ON DELETE CASCADE.  |
| user_id              | UUID         | NO       | —       | Token owner.                          |
| tenant_id            | UUID         | NO       | —       | Owning tenant.                        |
| token_hash           | VARCHAR(64)  | NO       | —       | SHA-256 of the opaque refresh token.  |
| is_revoked           | BOOLEAN      | NO       | FALSE   | TRUE when rotated or force-revoked.   |
| created_at           | TIMESTAMPTZ  | NO       | NOW()   | Issuance time.                        |
| expires_at           | TIMESTAMPTZ  | NO       | —       | Absolute token expiry.                |
| replaced_by_token_id | UUID         | YES      | NULL    | Points to successor token in chain.   |

**Indexes:**
- `idx_refresh_tokens_hash` — B-tree on (tenant_id, token_hash)
- `idx_refresh_tokens_session` — B-tree on `session_id`

---

### Table: `oauth_clients`
Registered OAuth 2.0 / OIDC clients per tenant.

| Column                         | Type         | Nullable | Default | Description                          |
|--------------------------------|--------------|----------|---------|--------------------------------------|
| id                             | UUID         | NO       | —       | PK. UUID v7.                         |
| tenant_id                      | UUID         | NO       | —       | Owning tenant.                       |
| client_id                      | VARCHAR(100) | NO       | —       | OAuth client_id.                     |
| client_secret_hash             | TEXT         | NO       | —       | Hashed client_secret.                |
| display_name                   | VARCHAR(200) | NO       | —       | Human-readable name.                 |
| redirect_uris                  | TEXT[]       | NO       | —       | Allowed redirect URIs.               |
| allowed_scopes                 | TEXT[]       | NO       | —       | Permitted OAuth scopes.              |
| allowed_grant_types            | TEXT[]       | NO       | —       | Permitted grant types.               |
| access_token_lifetime_seconds  | INT          | NO       | 900     | Access token TTL (15 min default).   |
| refresh_token_lifetime_seconds | INT          | NO       | 86400   | Refresh token TTL (24 hr default).   |
| is_active                      | BOOLEAN      | NO       | TRUE    | Client enabled flag.                 |
| created_at                     | TIMESTAMPTZ  | NO       | NOW()   | Row creation timestamp.              |
| updated_at                     | TIMESTAMPTZ  | NO       | NOW()   | Last modification timestamp.         |

**Indexes:** `idx_oauth_clients_client_id` — UNIQUE on (tenant_id, client_id)

---

### Table: `saml_configurations`
SAML 2.0 IdP metadata per tenant.

| Column                          | Type         | Nullable | Default | Description                           |
|---------------------------------|--------------|----------|---------|---------------------------------------|
| id                              | UUID         | NO       | —       | PK. UUID v7.                          |
| tenant_id                       | UUID         | NO       | —       | Owning tenant.                        |
| entity_id                       | VARCHAR(500) | NO       | —       | SAML EntityID of the IdP.             |
| metadata_url                    | TEXT         | NO       | —       | IdP metadata endpoint URL.            |
| assertion_consumer_service_url  | TEXT         | NO       | —       | ACS URL (DataWin SP endpoint).        |
| single_logout_service_url       | TEXT         | NO       | —       | SLO endpoint.                         |
| certificate_base64              | TEXT         | NO       | —       | Base64-encoded IdP X.509 certificate. |
| sign_requests                   | BOOLEAN      | NO       | TRUE    | Whether to sign AuthnRequests.        |
| is_active                       | BOOLEAN      | NO       | TRUE    | Configuration enabled flag.           |
| created_at                      | TIMESTAMPTZ  | NO       | NOW()   | Row creation timestamp.               |
| updated_at                      | TIMESTAMPTZ  | NO       | NOW()   | Last modification timestamp.          |

**Indexes:** `idx_saml_entity` — UNIQUE on (tenant_id, entity_id)

---

### Table: `consent_records`
GDPR consent tracking per user per purpose.

| Column       | Type         | Nullable | Default | Description                          |
|--------------|--------------|----------|---------|--------------------------------------|
| id           | UUID         | NO       | —       | PK. UUID v7.                         |
| user_id      | UUID         | NO       | —       | FK → users(id) ON DELETE CASCADE.    |
| tenant_id    | UUID         | NO       | —       | Owning tenant.                       |
| purpose      | INT          | NO       | —       | ConsentPurpose enum.                 |
| is_granted   | BOOLEAN      | NO       | TRUE    | FALSE after withdrawal.              |
| ip_address   | VARCHAR(45)  | NO       | —       | IP at time of consent action.        |
| granted_at   | TIMESTAMPTZ  | NO       | NOW()   | When consent was granted.            |
| withdrawn_at | TIMESTAMPTZ  | YES      | NULL    | When consent was withdrawn.          |

**Indexes:**
- `idx_consent_user` — B-tree on (tenant_id, user_id)
- `idx_consent_unique` — Partial UNIQUE on (tenant_id, user_id, purpose) WHERE is_granted = TRUE

---

### Table: `pii_audit_log`
Immutable log of every PII access or mutation.

| Column     | Type         | Nullable | Default | Description                          |
|------------|--------------|----------|---------|--------------------------------------|
| id         | UUID         | NO       | —       | PK. UUID v7.                         |
| tenant_id  | UUID         | NO       | —       | Owning tenant.                       |
| user_id    | UUID         | NO       | —       | User whose PII was accessed.         |
| actor_id   | UUID         | YES      | NULL    | Who performed the action (admin/sys).|
| action     | VARCHAR(20)  | NO       | —       | "ACCESS" or "MUTATION".              |
| field_name | VARCHAR(100) | NO       | —       | Which field (email, phone, etc.).    |
| reason     | VARCHAR(500) | NO       | —       | Business justification.              |
| ip_address | VARCHAR(45)  | NO       | —       | IP of the actor.                     |
| timestamp  | TIMESTAMPTZ  | NO       | NOW()   | When the action occurred.            |

**Indexes:**
- `idx_pii_audit_user` — B-tree on (tenant_id, user_id)
- `idx_pii_audit_timestamp` — B-tree on `timestamp`

---

### Table: `data_erasure_requests`
GDPR Art. 17 right-to-erasure request queue.

| Column           | Type         | Nullable | Default | Description                       |
|------------------|--------------|----------|---------|-----------------------------------|
| id               | UUID         | NO       | —       | PK. UUID v7.                      |
| user_id          | UUID         | NO       | —       | User requesting erasure.          |
| tenant_id        | UUID         | NO       | —       | Owning tenant.                    |
| status           | INT          | NO       | 0       | ErasureStatus enum.               |
| requested_by_ip  | VARCHAR(45)  | NO       | —       | IP at time of request.            |
| completion_notes | TEXT         | YES      | NULL    | Processing details or error info. |
| requested_at     | TIMESTAMPTZ  | NO       | NOW()   | When erasure was requested.       |
| completed_at     | TIMESTAMPTZ  | YES      | NULL    | When processing finished.         |

**Indexes:**
- `idx_erasure_tenant` — B-tree on `tenant_id`
- `idx_erasure_status` — Partial on `status` WHERE status IN (0, 1)

---

### Table: `encryption_keys`
Tenant-scoped data encryption keys (DEKs). Destroying a key = crypto-shredding all PII for that tenant.

| Column       | Type         | Nullable | Default          | Description                         |
|--------------|--------------|----------|------------------|-------------------------------------|
| id           | UUID         | NO       | gen_random_uuid()| PK. Auto-generated.                 |
| tenant_id    | UUID         | NO       | —                | Owning tenant.                      |
| key_id       | VARCHAR(100) | NO       | —                | Logical key identifier.             |
| wrapped_key  | BYTEA        | NO       | —                | DEK encrypted by master KEK.        |
| is_active    | BOOLEAN      | NO       | TRUE             | FALSE after crypto-shredding.       |
| created_at   | TIMESTAMPTZ  | NO       | NOW()            | Key creation time.                  |
| destroyed_at | TIMESTAMPTZ  | YES      | NULL             | When the key was destroyed.         |

**Indexes:** `idx_encryption_keys_tenant` — UNIQUE on (tenant_id, key_id)

---

### Table: `user_external_logins`
Links between local user accounts and external identity provider identities.

| Column                | Type         | Nullable | Default | Description                         |
|-----------------------|--------------|----------|---------|-------------------------------------|
| id                    | UUID         | NO       | —       | PK. UUID v7.                        |
| user_id               | UUID         | NO       | —       | FK → users(id) ON DELETE CASCADE.   |
| tenant_id             | UUID         | NO       | —       | Owning tenant.                      |
| provider              | INT          | NO       | —       | AuthSchemeType enum.                |
| provider_key          | VARCHAR(256) | NO       | —       | User ID from the external provider. |
| provider_display_name | VARCHAR(200) | YES      | NULL    | Display name from the provider.     |
| created_at            | TIMESTAMPTZ  | NO       | NOW()   | When the link was created.          |

**Indexes:**
- `idx_external_login` — UNIQUE on (tenant_id, provider, provider_key)
- `idx_external_login_user` — B-tree on (tenant_id, user_id)

---

## Stored Procedure Reference

### Global Database

| Procedure/Function | Type | Description |
|---|---|---|
| `sp_tenant_get_by_id(UUID)` | FUNCTION | Get tenant by primary key |
| `sp_tenant_get_by_slug(VARCHAR)` | FUNCTION | Get tenant by URL slug |
| `sp_tenant_get_by_domain(VARCHAR)` | FUNCTION | Get tenant by custom domain |
| `sp_tenant_create(...)` | PROCEDURE | Insert new tenant |
| `sp_tenant_update(...)` | PROCEDURE | Update tenant details |
| `sp_tenant_get_regions(UUID)` | FUNCTION | List all regions for a tenant |
| `sp_tenant_add_region(...)` | PROCEDURE | Add region mapping |
| `sp_tenant_remove_region(UUID, VARCHAR)` | PROCEDURE | Remove region mapping |

### Regional Database — Users

| Procedure/Function | Type | Description |
|---|---|---|
| `sp_user_get_by_id(UUID, UUID)` | FUNCTION | Get user by tenant + user id |
| `sp_user_get_by_email_hash(UUID, VARCHAR)` | FUNCTION | Get user by tenant + email hash |
| `sp_user_create(...)` | PROCEDURE | Insert new user (encrypted PII) |
| `sp_user_update(...)` | PROCEDURE | Update user flags/lockout |
| `sp_user_get_credential(UUID, UUID)` | FUNCTION | Get password hash |
| `sp_user_create_credential(...)` | PROCEDURE | Insert password hash |
| `sp_user_update_credential(...)` | PROCEDURE | Update password hash |
| `sp_user_get_external_logins(UUID, UUID)` | FUNCTION | List external logins |
| `sp_user_link_external_login(...)` | PROCEDURE | Link external identity (upsert) |

### Regional Database — Auth / Sessions

| Procedure/Function | Type | Description |
|---|---|---|
| `sp_session_get_by_id(UUID, UUID)` | FUNCTION | Get session by id |
| `sp_session_create(...)` | PROCEDURE | Create new session |
| `sp_session_revoke(UUID, UUID)` | PROCEDURE | Revoke session + its tokens |
| `sp_session_revoke_all_for_user(UUID, UUID)` | PROCEDURE | Revoke all sessions for user |
| `sp_token_get_by_hash(UUID, VARCHAR)` | FUNCTION | Get refresh token by hash |
| `sp_token_create_refresh(...)` | PROCEDURE | Issue new refresh token |
| `sp_token_revoke(UUID, UUID, UUID)` | PROCEDURE | Revoke + set replacement pointer |

### Regional Database — MFA

| Procedure/Function | Type | Description |
|---|---|---|
| `sp_mfa_enroll(...)` | PROCEDURE | Enroll MFA, auto-enables mfa_enabled |
| `sp_mfa_verify(UUID, UUID, INT)` | PROCEDURE | Mark enrollment as verified |
| `sp_mfa_disable(UUID, UUID, INT)` | PROCEDURE | Remove method, auto-check remaining |

### Regional Database — Consent

| Procedure/Function | Type | Description |
|---|---|---|
| `sp_consent_get(UUID, UUID, INT)` | FUNCTION | Get active consent for purpose |
| `sp_consent_get_all_for_user(UUID, UUID)` | FUNCTION | List all consents for user |
| `sp_consent_grant(...)` | PROCEDURE | Grant consent (upsert) |
| `sp_consent_withdraw(UUID, UUID, INT)` | PROCEDURE | Withdraw consent |

### Regional Database — Privacy

| Procedure/Function | Type | Description |
|---|---|---|
| `sp_pii_audit_write(...)` | PROCEDURE | Write PII access/mutation audit entry |
| `sp_pii_audit_get_by_user(UUID, UUID)` | FUNCTION | Read audit log for user |
| `sp_erasure_request_create(...)` | PROCEDURE | Create erasure request |
| `sp_erasure_get(UUID, UUID)` | FUNCTION | Get erasure request by id |
| `sp_erasure_request_update(...)` | PROCEDURE | Update erasure status/notes |
| `sp_encryption_key_destroy(UUID)` | PROCEDURE | Crypto-shred: deactivate all DEKs |
