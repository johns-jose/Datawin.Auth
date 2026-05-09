# DataWin.Auth - Multi-Tenant Authentication Framework

## Overview

A centralized authentication framework for all DataWin software products.

**Tech Stack:** .NET 10, PostgreSQL (stored procedures only), UUID v7

## Architecture

    DataWin Products --> DataWin.Auth.Sdk --> DataWin.Auth.Api
                                                   |
                             +---------+-----------+-----------+
                             v                     v           v
                       Global DB          Regional DB (EU)  Regional DB (US)
                     (tenant registry)    (users, PII)      (users, PII)

## Projects

| Project | Purpose |
|---------|---------|
| DataWin.Auth.Core | Domain entities, value objects, interfaces (zero dependencies) |
| DataWin.Auth.Contracts | Shared models/events for consuming products |
| DataWin.Auth.Application | CQRS commands, queries, handlers, validators |
| DataWin.Auth.Infrastructure | PostgreSQL stored proc repos, JWT, encryption, MFA, IdP engines |
| DataWin.Auth.Api | ASP.NET 10 Web API - controllers, middleware, pipeline |
| DataWin.Auth.Sdk | NuGet client SDK for DataWin products |

## Key Features

- **Multi-tenancy** with per-tenant region routing
- **OAuth 2.0 + PKCE**, **OpenID Connect**, **SAML 2.0**
- **External IdPs**: Google, Azure AD, Okta (pluggable)
- **MFA**: TOTP, SMS, Email, WebAuthn (FIDO2)
- **GDPR compliance**: consent tracking, PII export, right-to-erasure via crypto-shredding
- **Field-level PII encryption** (AES-256-GCM) with tenant-scoped DEKs
- **UUID v7** for all primary keys (time-sortable)
- **Stored procedures only** for data access (no ORM)

## Documentation

| Document | Description |
|----------|-------------|
| [Architecture and Design](docs/ARCHITECTURE.md) | System architecture, security model, GDPR compliance, deployment |
| [API Usage Guide](docs/API_USAGE_GUIDE.md) | Complete REST API reference with request/response schemas and error codes |
| [SDK Integration Guide](docs/SDK_INTEGRATION_GUIDE.md) | .NET SDK setup, methods, patterns, and troubleshooting |
| [Database Catalog](database/catalog/DATABASE_CATALOG.md) | Full schema reference: 15 tables, 37 stored procedures, all enums |

## Getting Started

### 1. Database Setup

    # Create global database
    psql -U postgres -c "CREATE DATABASE datawin_auth_global;"
    psql -U postgres -d datawin_auth_global -f database/global/migrations/001_create_tenants.sql
    psql -U postgres -d datawin_auth_global -f database/global/migrations/002_create_tenant_regions.sql
    psql -U postgres -d datawin_auth_global -f database/global/migrations/003_create_regional_endpoints.sql
    psql -U postgres -d datawin_auth_global -f database/global/stored_procedures/sp_tenant.sql
    psql -U postgres -d datawin_auth_global -f database/global/seeds/seed_global.sql

    # Create regional database (repeat per region)
    psql -U postgres -c "CREATE DATABASE datawin_auth_us_east_1;"
    psql -U postgres -d datawin_auth_us_east_1 -f database/regional/migrations/001_create_users.sql
    psql -U postgres -d datawin_auth_us_east_1 -f database/regional/migrations/002_create_user_credentials.sql
    psql -U postgres -d datawin_auth_us_east_1 -f database/regional/migrations/003_create_user_mfa.sql
    psql -U postgres -d datawin_auth_us_east_1 -f database/regional/migrations/004_create_sessions.sql
    psql -U postgres -d datawin_auth_us_east_1 -f database/regional/migrations/005_create_refresh_tokens.sql
    psql -U postgres -d datawin_auth_us_east_1 -f database/regional/migrations/006_create_oauth_clients.sql
    psql -U postgres -d datawin_auth_us_east_1 -f database/regional/migrations/007_create_saml_configurations.sql
    psql -U postgres -d datawin_auth_us_east_1 -f database/regional/migrations/008_create_consent_records.sql
    psql -U postgres -d datawin_auth_us_east_1 -f database/regional/migrations/009_create_pii_audit_log.sql
    psql -U postgres -d datawin_auth_us_east_1 -f database/regional/migrations/010_create_data_erasure_requests.sql
    psql -U postgres -d datawin_auth_us_east_1 -f database/regional/migrations/011_create_encryption_keys.sql
    psql -U postgres -d datawin_auth_us_east_1 -f database/regional/migrations/012_create_user_external_logins.sql
    psql -U postgres -d datawin_auth_us_east_1 -f database/regional/stored_procedures/users/sp_user.sql
    psql -U postgres -d datawin_auth_us_east_1 -f database/regional/stored_procedures/auth/sp_session_token.sql
    psql -U postgres -d datawin_auth_us_east_1 -f database/regional/stored_procedures/mfa/sp_mfa.sql
    psql -U postgres -d datawin_auth_us_east_1 -f database/regional/stored_procedures/consent/sp_consent.sql
    psql -U postgres -d datawin_auth_us_east_1 -f database/regional/stored_procedures/privacy/sp_privacy.sql
    psql -U postgres -d datawin_auth_us_east_1 -f database/regional/seeds/seed_us_east_1.sql

### 2. Run the API

    cd src/DataWin.Auth.Api
    dotnet run

### 3. Integrate via SDK

    builder.Services.AddDataWinAuthSdk("https://auth.datawin.io");

## API Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | /api/auth/register | Anonymous | Register new user |
| POST | /api/auth/login | Anonymous | Email/password login |
| POST | /api/auth/logout | Bearer | Revoke session |
| POST | /api/auth/refresh | Anonymous | Rotate refresh token |
| POST | /api/auth/external | Anonymous | External IdP login |
| POST | /api/mfa/enroll | Bearer | Enroll MFA method |
| POST | /api/mfa/verify | Anonymous | Verify MFA code |
| GET | /api/user/profile | Bearer | Get user profile |
| GET | /api/user/export | Bearer | GDPR data export |
| POST | /api/consent/grant | Bearer | Grant consent |
| POST | /api/consent/withdraw | Bearer | Withdraw consent |
| GET | /api/consent/status | Bearer | Get consent status |
| POST | /api/consent/erasure | Bearer | Request data erasure |
| POST | /api/tenant | Bearer | Onboard new tenant |

See the [API Usage Guide](docs/API_USAGE_GUIDE.md) for complete request/response schemas.

## Seed Data

| Script | Target Database | Contents |
|--------|----------------|----------|
| seed_global.sql | datawin_auth_global | 5 tenants, 5 regions, 7 tenant-region mappings |
| seed_us_east_1.sql | datawin_auth_us_east_1 | 5 users, credentials, MFA, OAuth clients, sessions, consent, audit |
| seed_eu_west_1.sql | datawin_auth_eu_west_1 | 3 users, credentials, MFA (TOTP+WebAuthn), Azure AD link, consent |

Default password for all seed users: P@ssw0rd!2025

## Testing

    dotnet test tests/DataWin.Auth.UnitTests

37 unit tests covering: UuidV7, value objects, login handler (5 scenarios), user registration
handler (5 scenarios), tenant onboarding, password hashing (12 tests incl. format validation,
DB round-trip simulation, edge cases), AES-GCM encryption, JWT token generation/validation.