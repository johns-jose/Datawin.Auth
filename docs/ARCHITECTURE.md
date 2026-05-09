    # DataWin.Auth - Architecture and Design Document

## 1. Introduction

DataWin.Auth is the centralized authentication and identity framework for all DataWin software
products. It provides multi-tenant authentication, session management, multi-factor authentication,
federated identity (OAuth 2.0, OpenID Connect, SAML 2.0), GDPR-compliant privacy controls, and
field-level PII encryption - all behind a single REST API consumed by every DataWin product
through a shared .NET SDK.

### 1.1 Design Goals

| Goal | Approach |
|------|----------|
| **Multi-tenancy** | Every entity is scoped to a tenant_id. No cross-tenant data leakage. |
| **Data residency** | Tenant data lives in a regional PostgreSQL instance chosen at onboarding. |
| **Privacy by design** | PII fields are AES-256-GCM encrypted at the application layer. Crypto-shredding for erasure. |
| **Zero ORM** | All database access uses stored procedures via raw NpgsqlCommand. No Entity Framework. |
| **Pluggable identity** | Authentication schemes (OAuth, OIDC, SAML, external IdPs) are registered via DI. |
| **SDK-first** | DataWin products never call the API directly - they use DataWin.Auth.Sdk. |

### 1.2 Technology Stack

| Component | Technology |
|-----------|-----------|
| Runtime | .NET 10 (C# 13) |
| API Framework | ASP.NET Core 10 Controllers |
| Database | PostgreSQL 16+ |
| Data Access | Npgsql (ADO.NET) + stored procedures |
| Authentication | JWT Bearer (HMAC-SHA256) |
| Encryption | AES-256-GCM (per-tenant DEK), HKDF for key derivation |
| Password Hashing | PBKDF2-SHA256 (Argon2id-ready) |
| Primary Keys | UUID v7 (RFC 9562) - time-ordered |
| Validation | FluentValidation |
| Testing | xUnit, NSubstitute |

---

## 2. Layered Architecture

Each layer depends only on the layer directly below it. Core has zero NuGet dependencies.
Infrastructure references Core. Application references Core. Api references Application and Infrastructure.

### Layer Responsibilities

**DataWin.Auth.Core** - Domain layer with zero external dependencies.
Contains entities (User, Session, Tenant, RefreshToken), value objects (UuidV7, EncryptedField,
RegionCode, TenantId, HashedPassword), enums, interfaces for repositories and services, and domain events.

**DataWin.Auth.Contracts** - Shared models consumed by DataWin products.
Contains TokenResponse, AuthenticatedUser, TenantContext.

**DataWin.Auth.Application** - CQRS application layer.
Contains command/query definitions, handlers, validators, and DTOs. No infrastructure knowledge.

**DataWin.Auth.Infrastructure** - Implementation layer.
Contains PostgreSQL connection factory, regional DB router, 6 repository implementations, JWT token
service, AES-GCM field encryptor, key management, password hasher, 4 MFA providers, 3 auth scheme
engines, 3 external IdP providers, data erasure service, PII audit logger.

**DataWin.Auth.Api** - HTTP layer.
Contains 5 controllers, 3 middleware components (TenantResolution, RegionalRouting, PiiAudit),
MFA filter, and the application entry point.

**DataWin.Auth.Sdk** - Client SDK (NuGet package).
Contains typed HTTP client, JWT validation handler, and DI extension method.

---

## 3. Multi-Tenancy and Regional Routing

### 3.1 Database Topology

The system uses a split-database architecture:

- **Global Database** (datawin_auth_global) - Tenant registry, region mapping, endpoint routing.
  One instance worldwide. Contains no PII.
- **Regional Databases** (datawin_auth_{region}) - User data, sessions, credentials, consent,
  audit logs. One instance per compliance region. All PII is AES-256-GCM encrypted at field level.

### 3.2 Request Flow

1. TenantResolutionMiddleware reads X-Tenant-Id header or route slug
2. Queries tenant_regions in global DB to find the primary region
3. RegionalDbRouter resolves the PostgreSQL connection string for that region
4. All data access goes through IDbConnectionFactory.CreateRegionalConnectionAsync()

All region lookups are cached in memory (5-10 minute TTL) to avoid per-request global DB queries.

### 3.3 Tenant Isolation

- Every database table includes a tenant_id column
- Every stored procedure accepts tenant_id as the first parameter
- Every repository method requires UuidV7 tenantId
- There is no way to query across tenants at the data layer

---

## 4. Authentication Flows

### 4.1 User Registration

1. Client sends POST /api/auth/register with email, password, tenantId, optional displayName
2. Handler normalizes email to lowercase, computes SHA-256 email hash for duplicate check
3. Queries user_credentials by email_hash — if found, returns email_exists error
4. Encrypts email (and displayName if provided) with AES-256-GCM using tenant-scoped DEK
5. Creates User entity with encrypted PII + email_hash
6. Hashes password with PBKDF2-SHA256 (100,000 iterations, 16-byte salt, 32-byte hash)
7. Creates UserCredential entity with hashed password
8. Returns userId

### 4.2 Email/Password Login

1. Client sends POST /api/auth/login with email, password, tenantId, deviceFingerprint
2. Handler hashes email for lookup (SHA-256), queries user by email_hash
3. If user not found or account locked, returns error
4. Verifies password against stored Argon2id hash
5. If MFA is enabled, returns mfaChallengeToken (requires POST /api/mfa/verify next)
6. If no MFA, creates session + refresh token + signs JWT access token
7. Returns accessToken, refreshToken, expiresIn

### 4.3 Token Refresh (Rotation)

1. Client sends POST /api/auth/refresh with tenantId, refreshToken
2. Handler hashes the refresh token (SHA-256), looks up by hash
3. Validates token is not revoked and not expired
4. Revokes old token, creates new token, issues new access token
5. Returns new accessToken + new refreshToken

### 4.4 MFA Verification

1. Client sends POST /api/mfa/verify with code, challengeToken, tenantId, userId
2. Handler resolves the MFA method (TOTP/SMS/Email/WebAuthn)
3. Delegates to the appropriate MfaProvider.VerifyAsync()
4. If verified, creates session + issues tokens with mfa_verified=true claim

### 4.5 External IdP Login (OAuth/OIDC)

1. Client sends POST /api/auth/external with provider name, authorization code, redirectUri
2. Handler resolves the scheme (GoogleProvider, AzureAdProvider, OktaProvider)
3. Scheme exchanges the code for tokens at the IdP
4. Extracts user identity (email, sub, name) from ID token or userinfo
5. Finds or creates local user, links external login
6. Creates session + issues tokens

---

## 5. Security Model

### 5.1 JWT Access Tokens

Claims included in every access token:
- sub: User ID (UUID)
- tid: Tenant ID (UUID)
- region: Primary data region
- mfa_verified: Whether MFA was completed
- role: User roles (array)
- jti: Unique token ID
- iss: Issuer (https://auth.datawin.io)
- exp: Expiry (default 15 minutes)
- aud: Audience (optional, per-client)

### 5.2 Refresh Token Rotation

- Refresh tokens are opaque (64-byte random, base64-encoded)
- Only the SHA-256 hash is stored in the database
- On each refresh, the old token is revoked and a new one is issued
- The revoked token stores a replaced_by_token_id pointer for replay detection
- If a revoked token is reused, the entire session is invalidated

### 5.3 Password Security

- Passwords are hashed using PBKDF2-SHA256 with 100,000 iterations (Argon2id-ready)
- A unique 16-byte random salt per password
- Hash format: base64(salt).base64(hash) stored in user_credentials.password_hash
- Verification uses constant-time comparison (CryptographicOperations.FixedTimeEquals)
- Verify includes defensive validation: null/empty check, base64 format, salt/hash length
- Invalid hash formats are logged at Warning level for diagnostics
- After 5 consecutive failures, the account is locked for 15 minutes

### 5.4 PII Encryption

Each PII field is encrypted with AES-256-GCM using a tenant-scoped DEK.
Each encrypted field stores: cipher_text + nonce (12 bytes) + auth_tag (16 bytes) + key_id.

Lookup is via email_hash (SHA-256 of lowercase email) - no decryption needed for queries.

Crypto-shredding: destroying a tenant DEK makes all their PII permanently unrecoverable.

---

## 6. GDPR Compliance

| GDPR Article | Feature | Implementation |
|-------------|---------|----------------|
| Art. 6 - Lawful basis | Consent tracking | consent_records table with purpose-based consent |
| Art. 7 - Conditions for consent | Granular consent | Per-purpose grant/withdraw with IP + timestamp |
| Art. 15 - Right of access | Data export | GET /api/user/export returns all PII as JSON |
| Art. 17 - Right to erasure | Crypto-shredding | POST /api/consent/erasure destroys tenant DEK |
| Art. 25 - Data protection by design | Field-level encryption | AES-256-GCM on all PII columns |
| Art. 30 - Records of processing | PII audit log | pii_audit_log table + PiiAuditMiddleware |
| Art. 44 - Cross-border transfers | Regional databases | Tenant data stays in the assigned region |

---

## 7. Middleware Pipeline

Requests flow through these middleware components in order:

1. CorrelationIdMiddleware - Generates/reads X-Correlation-Id, echoes on response
2. GlobalExceptionMiddleware - Catches all unhandled exceptions, logs with correlation
3. TenantResolutionMiddleware - Reads X-Tenant-Id header or route slug, sets MutableTenantContext
4. RegionalRoutingMiddleware - Sets RegionCode in HttpContext.Items
5. RequestLoggingMiddleware - Structured scope: CorrelationId, TenantId, UserId, ClientIp, timing
6. PiiAuditMiddleware - Logs access to PII-sensitive endpoints
7. JWT Authentication - Validates Bearer token
8. Authorization - Enforces [Authorize] / [AllowAnonymous]
9. Controller - Handles the request

---

## 8. Testing Strategy

| Layer | Framework | What is tested |
|-------|-----------|----------------|
| Unit | xUnit + NSubstitute | Handlers, value objects, encryption, JWT, password hashing |
| Integration | xUnit + Testcontainers (planned) | Repository to PostgreSQL stored procedures |
| API | xUnit + WebApplicationFactory (planned) | Full HTTP pipeline end-to-end |

Current test count: 37 unit tests, all passing.

---

## 9. Deployment Considerations

### 9.1 Configuration

| Setting | Source | Default |
|---------|--------|---------|
| ConnectionStrings:GlobalDb | appsettings / env var | Host=localhost;... |
| Jwt:SigningKey | Azure Key Vault / env var | (must be >= 256 bits) |
| Jwt:Issuer | appsettings | https://auth.datawin.io |
| Jwt:AccessTokenLifetimeSeconds | appsettings | 900 |

### 9.2 Database Migration Order

1. Create global database, apply 3 global migrations, apply sp_tenant.sql, run seed_global.sql
2. Per region: create regional database, apply 12 regional migrations, apply 5 SP scripts, run seed

### 9.3 Scaling

- The API is stateless - scale horizontally behind a load balancer
- The global database is read-heavy (tenant lookups) - add read replicas
- Regional databases are independent - scale each region separately
- Memory cache TTLs (5-10 min) reduce global DB load

---

## 10. Design Patterns

This section catalogs every design pattern used in the DataWin.Auth codebase, grouped by
category, with the concrete classes and interfaces that implement each pattern.

---

### 10.1 Architectural Patterns

#### Clean Architecture (Onion Architecture)

The solution follows Clean Architecture with four concentric layers. Dependencies point
inward only - outer layers depend on inner layers, never the reverse.

    +-------------------------------------------------------------+
    |                    DataWin.Auth.Api                          |  Outer ring
    |  Controllers, Middleware, Filters, Program.cs                |
    +-------------------------------------------------------------+
    |               DataWin.Auth.Infrastructure                    |
    |  Repositories, JWT, Encryption, MFA, IdP, Privacy           |
    +-------------------------------------------------------------+
    |               DataWin.Auth.Application                       |
    |  Commands, Queries, Handlers, Validators, DTOs               |
    +-------------------------------------------------------------+
    |                  DataWin.Auth.Core                            |  Inner ring
    |  Entities, Value Objects, Interfaces, Enums, Events          |  (zero dependencies)
    +-------------------------------------------------------------+

**Evidence:**
- DataWin.Auth.Core.csproj has zero NuGet package references
- All repository and service interfaces are defined in Core (IUserRepository, ITokenService, etc.)
- Implementations live in Infrastructure (UserRepository, JwtTokenService, etc.)
- Application layer depends only on Core interfaces, never on Infrastructure types

#### CQRS (Command Query Responsibility Segregation)

Commands and queries are separated into distinct object models with dedicated handler interfaces.
Commands mutate state and return a result. Queries read state without side effects.

**Interfaces:**

    ICommand<TResult>                 - Marker for commands (carries TenantId)
    IQuery<TResult>                   - Marker for queries (carries TenantId)
    ICommandHandler<TCommand, TResult> - Processes a command, returns result
    IQueryHandler<TQuery, TResult>     - Processes a query, returns result

**Commands:**
- RegisterUserCommand -> RegisterUserResultDto
- LoginCommand -> AuthResultDto
- LogoutCommand -> bool
- RefreshTokenCommand -> TokenResponseDto
- ExternalLoginCommand -> AuthResultDto
- EnrollMfaCommand -> string
- VerifyMfaCommand -> AuthResultDto
- GrantConsentCommand -> bool
- WithdrawConsentCommand -> bool
- RequestErasureCommand -> Guid
- OnboardTenantCommand -> TenantDto

**Queries:**
- GetUserProfileQuery -> UserProfileDto
- GetConsentStatusQuery -> ConsentStatusDto

**Location:** Application/Abstractions/Messaging.cs, Application/Commands/**, Application/Queries/**

#### Multi-Tenant Architecture

Every data entity is scoped to a tenant. The system uses a discriminator column (tenant_id)
on every table and a split-database topology (global + per-region) for data residency.

**Evidence:**
- Every entity has a TenantId property (UuidV7)
- Every stored procedure accepts tenant_id as a parameter
- Every repository method requires a tenantId argument
- ITenantContext/MutableTenantContext carry resolved tenant info through the request
- TenantResolutionMiddleware resolves the tenant at the start of every request

---

### 10.2 Creational Patterns

#### Factory Method

The IDbConnectionFactory abstraction provides factory methods for creating database connections
without exposing the concrete Npgsql connection creation logic to consumers.

    interface IDbConnectionFactory {
        Task<IDbConnection> CreateGlobalConnectionAsync(ct);
        Task<IDbConnection> CreateRegionalConnectionAsync(regionCode, ct);
    }

**Implementation:** PostgresConnectionFactory
- CreateGlobalConnectionAsync() creates and opens an NpgsqlConnection to the global DB
- CreateRegionalConnectionAsync() resolves the region's connection string (with caching),
  creates and opens an NpgsqlConnection to the regional DB

**Location:** Core/Interfaces/IDbConnectionFactory.cs, Infrastructure/Database/PostgresConnectionFactory.cs

#### Static Factory Method

Several value objects and DTOs use named static factory methods instead of public constructors
to provide clear intent and enforce invariants.

**UuidV7:**
- UuidV7.New() - generates a new time-ordered UUID v7
- UuidV7.From(Guid) - wraps an existing Guid
- UuidV7.Parse(string) - parses from string
- UuidV7.Empty - null-object sentinel

**EncryptedField:**
- EncryptedField.FromComponents(cipherText, nonce, tag, keyId)

**AuthResultDto:**
- AuthResultDto.Success(accessToken, refreshToken, expiresIn)
- AuthResultDto.MfaRequired(challengeToken)
- AuthResultDto.Failure(errorCode, message)

**RegisterUserResultDto:**
- RegisterUserResultDto.Success(userId)
- RegisterUserResultDto.Failure(errorCode, message)

**AuthenticationResult:**
- AuthenticationResult.Success(externalUserId, email, displayName, claims)
- AuthenticationResult.Failure(error)

**TenantId:**
- TenantId.New() / TenantId.From(Guid) / TenantId.Empty

---

### 10.3 Structural Patterns

#### Repository Pattern

All data access is encapsulated behind repository interfaces defined in Core. Infrastructure
provides concrete implementations that call PostgreSQL stored procedures via ADO.NET.

    interface ITenantRepository {
        Task<Tenant?> GetByIdAsync(UuidV7 id, ct);
        Task<Tenant?> GetBySlugAsync(string slug, ct);
        Task CreateAsync(Tenant tenant, ct);
        Task UpdateAsync(Tenant tenant, ct);
        ...
    }

**Repositories (6):**

| Interface | Implementation | Database | Key Stored Procedures |
|-----------|---------------|----------|----------------------|
| ITenantRepository | TenantRepository | Global | sp_tenant_get_by_id, sp_tenant_create, sp_tenant_add_region |
| IUserRepository | UserRepository | Regional | sp_user_get_by_email_hash, sp_user_create, sp_user_get_credential |
| ISessionRepository | SessionRepository | Regional | sp_session_create, sp_session_revoke, sp_token_create_refresh |
| IConsentRepository | ConsentRepository | Regional | sp_consent_grant, sp_consent_withdraw, sp_erasure_request_create |
| IOAuthClientRepository | OAuthClientRepository | Regional | Direct SQL (oauth_clients, saml_configurations) |
| IPiiAuditRepository | PiiAuditRepository | Regional | sp_pii_audit_write, sp_pii_audit_get_by_user |

**Location:** Core/Interfaces/Repositories/**, Infrastructure/Database/Repositories/**

#### Adapter Pattern

The external identity provider implementations adapt third-party IdP protocols (Google OAuth,
Azure AD OIDC, Okta OIDC) into the unified IAuthenticationScheme interface defined in Core.

    interface IAuthenticationScheme {
        AuthSchemeType SchemeType { get; }
        Task<AuthenticationResult> AuthenticateAsync(AuthenticationRequest request, ct);
    }

Each provider adapts a different external API into this common interface:

| Adapter Class | External Protocol | SchemeType |
|--------------|-------------------|------------|
| GoogleProvider | Google OAuth 2.0 token exchange | ExternalGoogle |
| AzureAdProvider | Microsoft Entra ID OIDC | ExternalAzureAd |
| OktaProvider | Okta OIDC | ExternalOkta |
| OAuthScheme | Generic OAuth 2.0 | OAuth2 |
| OidcScheme | OpenID Connect | OpenIdConnect |
| SamlScheme | SAML 2.0 SSO | Saml2 |

**Location:** Core/Interfaces/Services/IAuthenticationScheme.cs, Infrastructure/Identity/**

#### Facade Pattern

DependencyInjection.AddDataWinAuthInfrastructure() acts as a facade that registers all
infrastructure services (25+ registrations) behind a single method call.

    builder.Services.AddDataWinAuthInfrastructure(globalConnectionString, jwtSettings);

This hides the complexity of:
- Database connection factory and regional router
- 6 repository registrations
- Token service, encryption service, password hasher
- Key management service
- Data erasure service, PII audit logger
- 6 authentication scheme registrations
- 4 MFA provider registrations

Similarly, the SDK provides a facade:

    builder.Services.AddDataWinAuthSdk("https://auth.datawin.io");

**Location:** Infrastructure/DependencyInjection.cs, Sdk/ServiceCollectionExtensions.cs

#### Proxy Pattern (Delegating Handler)

TokenValidationHandler is a DelegatingHandler that intercepts outgoing HTTP requests in the
SDK pipeline to validate JWT tokens before they are sent, acting as a validation proxy.

    class TokenValidationHandler : DelegatingHandler {
        protected override Task<HttpResponseMessage> SendAsync(request, ct) {
            // Validate JWT from Authorization header
            // Return 401 if invalid, otherwise delegate to inner handler
        }
    }

**Location:** Sdk/TokenValidationHandler.cs

---

### 10.4 Behavioral Patterns

#### Strategy Pattern

Multiple interchangeable algorithms are encapsulated behind a common interface, with the
concrete strategy selected at runtime based on context.

**MFA Providers** - IMfaProvider selects the verification strategy based on MfaMethod:

    interface IMfaProvider {
        MfaMethod Method { get; }       // Discriminator
        Task<string> EnrollAsync(tenantId, userId, ct);
        Task<bool> VerifyAsync(tenantId, userId, code, ct);
        Task DisableAsync(tenantId, userId, ct);
    }

| Strategy | Method | Algorithm |
|----------|--------|-----------|
| TotpProvider | Totp | RFC 6238 HMAC-SHA1 30-second window |
| SmsMfaProvider | Sms | Random 6-digit code via SMS gateway |
| EmailMfaProvider | Email | Random 6-digit code via email |
| WebAuthnProvider | WebAuthn | FIDO2 challenge/response |

**Selection in VerifyMfaHandler:**
    var provider = _mfaProviders.FirstOrDefault(p => p.Method == parsedMethod);

**Authentication Schemes** - IAuthenticationScheme selects the IdP strategy based on AuthSchemeType:

    var scheme = _schemes.FirstOrDefault(s => s.SchemeType == schemeType);
    var result = await scheme.AuthenticateAsync(request, ct);

**Location:** Core/Interfaces/Services/IMfaProvider.cs, Infrastructure/Identity/Mfa/**,
Core/Interfaces/Services/IAuthenticationScheme.cs, Infrastructure/Identity/**

#### Template Method Pattern

All CQRS handlers follow the same processing template defined by ICommandHandler/IQueryHandler:

    1. Validate the command/query (via FluentValidation)
    2. Resolve the tenant and region context
    3. Execute domain logic (database calls, encryption, token generation)
    4. Return a typed result DTO

Each handler implements HandleAsync() with the same signature but different domain logic.
The middleware pipeline (TenantResolution -> RegionalRouting -> PiiAudit -> Auth) defines
the invariant request processing template that all endpoints share.

**Location:** Application/Abstractions/Messaging.cs, Application/Handlers/**

#### Chain of Responsibility Pattern

The ASP.NET Core middleware pipeline implements Chain of Responsibility. Each middleware
component decides whether to handle, modify, or pass the request to the next handler.

    Request -> CorrelationIdMiddleware
                    -> GlobalExceptionMiddleware
                        -> TenantResolutionMiddleware
                            -> RegionalRoutingMiddleware
                                -> RequestLoggingMiddleware
                                    -> PiiAuditMiddleware
                                        -> Authentication
                                            -> Authorization
                                                -> Controller

Each middleware:
1. Performs its specific concern (resolve tenant, set region, log PII access)
2. Calls await _next(context) to delegate to the next handler in the chain
3. Can short-circuit the pipeline by not calling _next

**Location:** Api/Middleware/CorrelationIdMiddleware.cs, GlobalExceptionMiddleware.cs,
TenantResolutionMiddleware.cs, RegionalRoutingMiddleware.cs, RequestLoggingMiddleware.cs,
PiiAuditMiddleware.cs

#### Observer Pattern (Domain Events)

Domain events are defined as immutable records implementing IDomainEvent. They capture
significant state changes for downstream consumers to observe.

    interface IDomainEvent {
        UuidV7 EventId { get; }
        DateTimeOffset OccurredAt { get; }
        UuidV7 TenantId { get; }
    }

**Events:**

| Event | Trigger |
|-------|---------|
| UserAuthenticatedEvent | Successful login (with MFA flag) |
| UserLockedOutEvent | Account locked after max failed attempts |
| MfaEnrolledEvent | New MFA method enrolled |
| ConsentGrantedEvent | User grants consent for a purpose |
| ConsentWithdrawnEvent | User withdraws consent |
| ErasureRequestedEvent | Data erasure request created |

**Location:** Core/Events/DomainEvents.cs

---

### 10.5 Data Patterns

#### Value Object Pattern

Immutable types with equality defined by their values rather than identity. Used throughout
the domain model for type safety and self-validation.

| Value Object | Type | Invariants |
|-------------|------|------------|
| UuidV7 | readonly struct | RFC 9562 v7 format, time-ordered, IEquatable, IComparable |
| RegionCode | readonly record struct | Non-empty, lowercase-normalized |
| TenantId | readonly record struct | Wraps UuidV7, Empty sentinel |
| EncryptedField | sealed record | Requires CipherText + Nonce + Tag + KeyId |
| HashedPassword | sealed record | Requires Hash + Algorithm |

**Key characteristics:**
- UuidV7 uses implicit/explicit conversion operators to/from Guid
- RegionCode normalizes to lowercase in the constructor
- All use guard clauses (ArgumentException.ThrowIfNullOrWhiteSpace)

**Location:** Core/ValueObjects/**

#### Null Object Pattern

UuidV7.Empty and TenantId.Empty provide safe null-object sentinels that avoid null checks.

    var id = TenantId.Empty;     // id.IsEmpty == true
    var uuid = UuidV7.Empty;     // uuid.IsEmpty == true

This eliminates null reference risks when a tenant or ID has not been resolved yet.

**Location:** Core/ValueObjects/UuidV7.cs, Core/ValueObjects/TenantId.cs

#### Result Object Pattern

AuthResultDto and RegisterUserResultDto encapsulate success/failure outcomes without throwing
exceptions for expected business failures (wrong password, account locked, MFA required,
duplicate email).

    AuthResultDto.Success(accessToken, refreshToken, expiresIn)   // IsSuccess = true
    AuthResultDto.MfaRequired(challengeToken)                     // IsSuccess = false, RequiresMfa = true
    AuthResultDto.Failure("invalid_credentials", "...")            // IsSuccess = false, ErrorCode set

    RegisterUserResultDto.Success(userId)                          // IsSuccess = true, UserId set
    RegisterUserResultDto.Failure("email_exists", "...")           // IsSuccess = false, ErrorCode set

Similarly, AuthenticationResult for external IdP auth:

    AuthenticationResult.Success(externalUserId, email, displayName)
    AuthenticationResult.Failure("token exchange failed")

This pattern keeps the handler flow linear (no try/catch for business rules) and gives
controllers a single return type to inspect.

**Location:** Application/DTOs/AuthResultDto.cs, Application/DTOs/RegisterUserResultDto.cs,
Core/Interfaces/Services/IAuthenticationScheme.cs

#### Data Transfer Object (DTO) Pattern

Dedicated response shapes decouple the API contract from internal domain entities:

| DTO | Maps From | Purpose |
|-----|-----------|---------|
| AuthResultDto | Login/MFA flow result | Authentication response (tokens + errors) |
| RegisterUserResultDto | Registration flow result | Registration response (userId + errors) |
| TokenResponseDto | Refresh flow result | New token pair |
| UserProfileDto | User entity (decrypted) | Profile endpoint response |
| ConsentStatusDto | Consent records | Consent status endpoint response |
| TenantDto | Tenant + TenantRegion entities | Tenant onboarding response |

**Location:** Application/DTOs/**

---

### 10.6 Security Patterns

#### Envelope Encryption (DEK/KEK)

Two-tier key hierarchy where data encryption keys (DEKs) are themselves encrypted by a
master key encryption key (KEK).

    Master KEK (environment-wide, stored in vault/HSM)
        |
        +-- wraps --> Tenant A DEK (stored in encryption_keys table as wrapped_key)
        +-- wraps --> Tenant B DEK
        +-- wraps --> Tenant C DEK

**Implementation:**
- KeyManagementService.GetCurrentKey() derives tenant-scoped DEKs using HKDF-SHA256
- AesGcmFieldEncryptor.Encrypt() uses the DEK for AES-256-GCM field encryption
- KeyManagementService.DestroyKeyAsync() calls sp_encryption_key_destroy for crypto-shredding

**Location:** Infrastructure/Encryption/KeyManagementService.cs, AesGcmFieldEncryptor.cs

#### Crypto-Shredding Pattern

Data is rendered unrecoverable by destroying the encryption key rather than deleting the
ciphertext. All encrypted PII for a tenant becomes permanently unreadable.

    DataErasureService.ProcessErasureAsync()
        -> IPiiEncryptionService.DestroyKeyAsync(tenantId)
            -> KeyManagementService.DestroyKeyAsync(tenantId)
                -> CALL sp_encryption_key_destroy(tenant_id)
                -> Cache invalidation

**Location:** Infrastructure/Privacy/DataErasureService.cs

#### Token Rotation Pattern

Refresh tokens are single-use. Each usage revokes the current token and issues a new one.
A chain pointer (replaced_by_token_id) enables replay detection - if a revoked token is
presented, the entire session is invalidated.

**Location:** Application/Handlers/Auth/RefreshTokenHandler.cs

#### Guard Clause Pattern

Early returns with validation at method entry points prevent processing of invalid data.

LoginHandler.HandleAsync:
    if (user is null) return AuthResultDto.Failure("invalid_credentials", ...);
    if (user.LockoutEnd > DateTimeOffset.UtcNow) return AuthResultDto.Failure("account_locked", ...);
    if (credential is null) return AuthResultDto.Failure("invalid_credentials", ...);
    if (!_passwordHasher.Verify(...)) { ... return AuthResultDto.Failure(...); }

RegisterUserHandler.HandleAsync:
    if (existingUser is not null) return RegisterUserResultDto.Failure("email_exists", ...);

RegionCode constructor:
    ArgumentException.ThrowIfNullOrWhiteSpace(value);

ExternalLoginHandler.HandleAsync:
    if (!Enum.TryParse(...)) return AuthResultDto.Failure("invalid_provider", ...);
    if (scheme is null) return AuthResultDto.Failure("unsupported_provider", ...);

**Location:** Used throughout all handlers, value object constructors, and middleware

---

### 10.7 Infrastructure Patterns

#### Unit of Work (Implicit)

Each repository method opens its own connection and executes a single stored procedure call.
The stored procedures themselves contain transactional logic (e.g., sp_session_revoke revokes
both the session and all its refresh tokens atomically, sp_mfa_enroll inserts the MFA record
and updates the user mfa_enabled flag in one transaction).

**Location:** Infrastructure/Database/Repositories/**, database/regional/stored_procedures/**

#### Cache-Aside Pattern

Frequently accessed, rarely changing data is cached in IMemoryCache with expiration TTLs.
On cache miss, the data is fetched from the database and stored in cache.

**Cached data:**

| Cache Key Pattern | TTL | Data |
|-------------------|-----|------|
| tenant_region_{tenantId} | 5 min | Primary region code for a tenant |
| conn_{regionCode} | 10 min | Regional database connection string |
| regional_conn_{regionCode} | 10 min | Connection string (PostgresConnectionFactory) |
| enc_key_{tenantId} | 30 min | Tenant encryption DEK |

**Implementation:**
    if (!_cache.TryGetValue(cacheKey, out var cached)) {
        cached = await FetchFromDatabase();
        _cache.Set(cacheKey, cached, TimeSpan.FromMinutes(ttl));
    }
    return cached;

**Location:** Infrastructure/Database/RegionalDbRouter.cs, PostgresConnectionFactory.cs,
Infrastructure/Encryption/KeyManagementService.cs

#### Composition Root Pattern

All dependency injection registrations are centralized in two composition roots:

1. Infrastructure/DependencyInjection.cs - Registers all infrastructure services
2. Api/Program.cs - Registers application handlers and builds the middleware pipeline

No service is resolved manually outside these two locations. All dependencies flow through
constructor injection.

**Location:** Infrastructure/DependencyInjection.cs, Api/Program.cs

---

### 10.8 Pattern Summary Matrix

| Pattern | Category | Primary Location | Count |
|---------|----------|-----------------|-------|
| Clean Architecture | Architectural | Solution structure | 1 |
| CQRS | Architectural | Application layer | 11 commands, 2 queries |
| Multi-Tenant | Architectural | All layers | Pervasive |
| Factory Method | Creational | IDbConnectionFactory | 1 |
| Static Factory Method | Creational | UuidV7, AuthResultDto, RegisterUserResultDto, EncryptedField | 5 types |
| Repository | Structural | Core/Infrastructure | 6 repositories |
| Adapter | Structural | Identity providers | 6 adapters |
| Facade | Structural | DI registration | 2 facades |
| Proxy (Delegating Handler) | Structural | SDK | 1 |
| Strategy | Behavioral | MFA + Auth schemes | 4 MFA + 6 IdP strategies |
| Template Method | Behavioral | Handler pipeline | 13 handlers |
| Chain of Responsibility | Behavioral | Middleware pipeline | 6 middleware + auth + authz |
| Observer (Domain Events) | Behavioral | Core events | 6 event types |
| Value Object | Data | Core value objects | 5 types |
| Null Object | Data | UuidV7.Empty, TenantId.Empty | 2 types |
| Result Object | Data | AuthResultDto, RegisterUserResultDto, AuthenticationResult | 3 types |
| DTO | Data | Application DTOs | 6 DTOs |
| Envelope Encryption | Security | Encryption layer | 1 |
| Crypto-Shredding | Security | Privacy layer | 1 |
| Token Rotation | Security | Auth handlers | 1 |
| Guard Clause | Security | All handlers | Pervasive |
| Unit of Work (Implicit) | Infrastructure | Stored procedures | Pervasive |
| Cache-Aside | Infrastructure | Router, factory, KMS | 4 cache sites |
| Composition Root | Infrastructure | DI + Program.cs | 2 roots |

**Total: 24 distinct design patterns identified across the codebase.**