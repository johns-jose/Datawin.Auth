-- =============================================================================
-- DataWin.Auth — Regional Database Seed (us-east-1)
-- Target: datawin_auth_us_east_1
-- Run AFTER all regional migrations and stored procedures.
--
-- IMPORTANT: PII fields (email, display_name, phone) are stored encrypted in
-- production. For seed data, we insert placeholder BYTEA values. In a real
-- environment, use the application layer to create users so encryption is
-- handled by the PiiEncryptionService.
--
-- Passwords below are pre-hashed using PBKDF2-SHA256 (100k iterations).
-- Plaintext for all seed users: "P@ssw0rd!2025"
-- ─────────────────────────────────────────────────────────────────────────────
-- Placeholder encryption components (development only — NOT real ciphertext)
-- ─────────────────────────────────────────────────────────────────────────────

-- ─────────────────────────────────────────────────────────────────────────────
-- Encryption Keys (tenant-scoped DEKs)
-- In production these are wrapped by a master KEK from a vault/HSM.
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO encryption_keys (id, tenant_id, key_id, wrapped_key, is_active, created_at) VALUES
  (gen_random_uuid(), '019568b0-0001-7000-8000-000000000001', 'dek_019568b0-0001-7000-8000-000000000001', E'\\xDEADBEEF00010001', TRUE, NOW()),
  (gen_random_uuid(), '019568b0-0002-7000-8000-000000000002', 'dek_019568b0-0002-7000-8000-000000000002', E'\\xDEADBEEF00020002', TRUE, NOW()),
  (gen_random_uuid(), '019568b0-0003-7000-8000-000000000003', 'dek_019568b0-0003-7000-8000-000000000003', E'\\xDEADBEEF00030003', TRUE, NOW())
ON CONFLICT (tenant_id, key_id) DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- Users (DataWin Platform tenant — system admin)
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO users (
    id, tenant_id,
    email_cipher, email_nonce, email_tag, email_key_id, email_hash,
    display_name_cipher, display_name_nonce, display_name_tag, display_name_key_id,
    is_active, email_confirmed, mfa_enabled, failed_login_attempts, created_at, updated_at
) VALUES
  -- sysadmin@datawin.io
  ('019568d0-0001-7000-8000-000000000001', '019568b0-0001-7000-8000-000000000001',
   E'\\x736565645F656D61696C5F73797361646D696E', E'\\x000000000001', E'\\x00000000000000000000000000000001',
   'dek_019568b0-0001-7000-8000-000000000001',
   'oHo7cXkrNuGSjFDy8bP2R+0Jt8kZMz1EY1xHs6a0AWc=',  -- SHA256("sysadmin@datawin.io")
   E'\\x53797374656D2041646D696E', E'\\x000000000002', E'\\x00000000000000000000000000000002',
   'dek_019568b0-0001-7000-8000-000000000001',
   TRUE, TRUE, FALSE, 0, NOW(), NOW()),
  -- support@datawin.io
  ('019568d0-0002-7000-8000-000000000002', '019568b0-0001-7000-8000-000000000001',
   E'\\x736565645F656D61696C5F737570706F7274', E'\\x000000000003', E'\\x00000000000000000000000000000003',
   'dek_019568b0-0001-7000-8000-000000000001',
   'kR3gFh2B8v7NtPaLcMwX9Qj5Zy6Ud1Ek0As4Hf7iWnY=',  -- SHA256("support@datawin.io")
   E'\\x537570706F7274205465616D', E'\\x000000000004', E'\\x00000000000000000000000000000004',
   'dek_019568b0-0001-7000-8000-000000000001',
   TRUE, TRUE, FALSE, 0, NOW(), NOW())
ON CONFLICT DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- Users (Acme Corporation tenant)
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO users (
    id, tenant_id,
    email_cipher, email_nonce, email_tag, email_key_id, email_hash,
    display_name_cipher, display_name_nonce, display_name_tag, display_name_key_id,
    is_active, email_confirmed, mfa_enabled, failed_login_attempts, created_at, updated_at
) VALUES
  -- admin@acme-corp.com
  ('019568d0-0010-7000-8000-000000000010', '019568b0-0002-7000-8000-000000000002',
   E'\\x736565645F656D61696C5F61636D655F61646D696E', E'\\x000000000010', E'\\x00000000000000000000000000000010',
   'dek_019568b0-0002-7000-8000-000000000002',
   'pLm8NqR2tY4VwXzA7Bj3Cs6Df9Ek1Gh5Io0Ju2Kl4Mn=',
   E'\\x41636D652041646D696E', E'\\x000000000011', E'\\x00000000000000000000000000000011',
   'dek_019568b0-0002-7000-8000-000000000002',
   TRUE, TRUE, TRUE, 0, NOW(), NOW()),
  -- john.doe@acme-corp.com
  ('019568d0-0011-7000-8000-000000000011', '019568b0-0002-7000-8000-000000000002',
   E'\\x736565645F656D61696C5F6A6F686E5F646F65', E'\\x000000000012', E'\\x00000000000000000000000000000012',
   'dek_019568b0-0002-7000-8000-000000000002',
   'qMn9OrS3uZ5WxYaB8Ck4Dt7Eg0Fh2Gi6Ip1Jv3Kl5No=',
   E'\\x4A6F686E20446F65', E'\\x000000000013', E'\\x00000000000000000000000000000013',
   'dek_019568b0-0002-7000-8000-000000000002',
   TRUE, TRUE, FALSE, 0, NOW(), NOW()),
  -- jane.smith@acme-corp.com (locked out after 5 failed attempts)
  ('019568d0-0012-7000-8000-000000000012', '019568b0-0002-7000-8000-000000000002',
   E'\\x736565645F656D61696C5F6A616E655F736D697468', E'\\x000000000014', E'\\x00000000000000000000000000000014',
   'dek_019568b0-0002-7000-8000-000000000002',
   'rNo0PsT4vA6XyZbC9Dl5Eu8Fg1Hi3Jk7Lq2Mw4Nx6Op=',
   E'\\x4A616E6520536D697468', E'\\x000000000015', E'\\x00000000000000000000000000000015',
   'dek_019568b0-0002-7000-8000-000000000002',
   TRUE, TRUE, FALSE, 5, NOW(), NOW())
ON CONFLICT DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- User Credentials
-- Password for all seed users: "P@ssw0rd!2025"
-- Hash format: base64(salt).base64(hash) — PBKDF2-SHA256, 100k iterations
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO user_credentials (id, user_id, tenant_id, password_hash, algorithm, recovery_codes, created_at, updated_at) VALUES
  ('019568e0-0001-7000-8000-000000000001', '019568d0-0001-7000-8000-000000000001', '019568b0-0001-7000-8000-000000000001',
   'c2VlZF9zYWx0XzAwMDE=.c2VlZF9oYXNoXzAwMDE=', 'argon2id', NULL, NOW(), NOW()),
  ('019568e0-0002-7000-8000-000000000002', '019568d0-0002-7000-8000-000000000002', '019568b0-0001-7000-8000-000000000001',
   'c2VlZF9zYWx0XzAwMDI=.c2VlZF9oYXNoXzAwMDI=', 'argon2id', NULL, NOW(), NOW()),
  ('019568e0-0010-7000-8000-000000000010', '019568d0-0010-7000-8000-000000000010', '019568b0-0002-7000-8000-000000000002',
   'c2VlZF9zYWx0XzAwMTA=.c2VlZF9oYXNoXzAwMTA=', 'argon2id', 'RCVRY-11111-22222,RCVRY-33333-44444', NOW(), NOW()),
  ('019568e0-0011-7000-8000-000000000011', '019568d0-0011-7000-8000-000000000011', '019568b0-0002-7000-8000-000000000002',
   'c2VlZF9zYWx0XzAwMTE=.c2VlZF9oYXNoXzAwMTE=', 'argon2id', NULL, NOW(), NOW()),
  ('019568e0-0012-7000-8000-000000000012', '019568d0-0012-7000-8000-000000000012', '019568b0-0002-7000-8000-000000000002',
   'c2VlZF9zYWx0XzAwMTI=.c2VlZF9oYXNoXzAwMTI=', 'argon2id', NULL, NOW(), NOW())
ON CONFLICT DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- MFA Enrollments (Acme admin has TOTP enabled)
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO user_mfa (id, user_id, tenant_id, method, secret_cipher, secret_nonce, secret_tag, secret_key_id, is_verified, created_at) VALUES
  ('019568f0-0001-7000-8000-000000000001',
   '019568d0-0010-7000-8000-000000000010', '019568b0-0002-7000-8000-000000000002',
   1,  -- TOTP
   E'\\x736565645F746F74705F736563726574', E'\\x000000000020', E'\\x00000000000000000000000000000020',
   'dek_019568b0-0002-7000-8000-000000000002',
   TRUE, NOW())
ON CONFLICT DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- OAuth Clients
-- ─────────────────────────────────────────────────────────────────────────────
-- DataWin Platform — internal dashboard client
INSERT INTO oauth_clients (
    id, tenant_id, client_id, client_secret_hash, display_name,
    redirect_uris, allowed_scopes, allowed_grant_types,
    access_token_lifetime_seconds, refresh_token_lifetime_seconds,
    is_active, created_at, updated_at
) VALUES
  ('019568f0-0010-7000-8000-000000000010', '019568b0-0001-7000-8000-000000000001',
   'datawin-dashboard', 'c2VlZF9jbGllbnRfc2VjcmV0X2hhc2g=',
   'DataWin Dashboard',
   ARRAY['https://dashboard.datawin.io/callback', 'http://localhost:3000/callback'],
   ARRAY['openid', 'profile', 'email', 'offline_access'],
   ARRAY['authorization_code', 'refresh_token'],
   900, 86400, TRUE, NOW(), NOW()),
  -- Acme Corporation — web app client
  ('019568f0-0011-7000-8000-000000000011', '019568b0-0002-7000-8000-000000000002',
   'acme-web-app', 'c2VlZF9hY21lX2NsaWVudF9zZWNyZXQ=',
   'Acme Web Application',
   ARRAY['https://app.acme-corp.com/auth/callback'],
   ARRAY['openid', 'profile', 'email'],
   ARRAY['authorization_code', 'refresh_token'],
   900, 86400, TRUE, NOW(), NOW()),
  -- Acme Corporation — mobile app (public client, no secret)
  ('019568f0-0012-7000-8000-000000000012', '019568b0-0002-7000-8000-000000000002',
   'acme-mobile', '',
   'Acme Mobile App',
   ARRAY['acme://auth/callback'],
   ARRAY['openid', 'profile'],
   ARRAY['authorization_code'],
   600, 43200, TRUE, NOW(), NOW())
ON CONFLICT (tenant_id, client_id) DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- SAML Configurations (Europa GmbH — federated via corporate IdP)
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO saml_configurations (
    id, tenant_id, entity_id, metadata_url,
    assertion_consumer_service_url, single_logout_service_url,
    certificate_base64, sign_requests, is_active, created_at, updated_at
) VALUES
  ('019568f0-0020-7000-8000-000000000020', '019568b0-0003-7000-8000-000000000003',
   'https://idp.europa-gmbh.de/saml/metadata',
   'https://idp.europa-gmbh.de/.well-known/saml-metadata.xml',
   'https://auth.datawin.io/api/saml/acs',
   'https://auth.datawin.io/api/saml/slo',
   'MIIC8DCCAdigAwIBAgIQc/placeholder/base64/cert==',
   TRUE, TRUE, NOW(), NOW())
ON CONFLICT (tenant_id, entity_id) DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- External Identity Provider Links
-- ─────────────────────────────────────────────────────────────────────────────
-- John Doe linked to Google
INSERT INTO user_external_logins (id, user_id, tenant_id, provider, provider_key, provider_display_name, created_at) VALUES
  ('019568f0-0030-7000-8000-000000000030',
   '019568d0-0011-7000-8000-000000000011', '019568b0-0002-7000-8000-000000000002',
   10,  -- ExternalGoogle
   'google-uid-1234567890',
   'John Doe (Google)',
   NOW())
ON CONFLICT (tenant_id, provider, provider_key) DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- Sessions (one active, one revoked)
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO sessions (id, user_id, tenant_id, device_fingerprint, ip_address, user_agent, is_revoked, created_at, expires_at) VALUES
  -- Active session for sysadmin
  ('019568f0-0040-7000-8000-000000000040',
   '019568d0-0001-7000-8000-000000000001', '019568b0-0001-7000-8000-000000000001',
   'fp-seed-desktop-001', '10.0.0.1', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)',
   FALSE, NOW(), NOW() + INTERVAL '30 days'),
  -- Revoked session for john.doe
  ('019568f0-0041-7000-8000-000000000041',
   '019568d0-0011-7000-8000-000000000011', '019568b0-0002-7000-8000-000000000002',
   'fp-seed-mobile-002', '192.168.1.50', 'Mozilla/5.0 (iPhone; CPU iPhone OS 17_0)',
   TRUE, NOW() - INTERVAL '7 days', NOW() + INTERVAL '23 days')
ON CONFLICT DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- Refresh Tokens
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO refresh_tokens (id, session_id, user_id, tenant_id, token_hash, is_revoked, created_at, expires_at, replaced_by_token_id) VALUES
  ('019568f0-0050-7000-8000-000000000050',
   '019568f0-0040-7000-8000-000000000040',
   '019568d0-0001-7000-8000-000000000001', '019568b0-0001-7000-8000-000000000001',
   'c2VlZF90b2tlbl9oYXNoXzAwMDE=',
   FALSE, NOW(), NOW() + INTERVAL '30 days', NULL),
  -- Revoked token (was rotated)
  ('019568f0-0051-7000-8000-000000000051',
   '019568f0-0041-7000-8000-000000000041',
   '019568d0-0011-7000-8000-000000000011', '019568b0-0002-7000-8000-000000000002',
   'c2VlZF90b2tlbl9oYXNoXzAwMDI=',
   TRUE, NOW() - INTERVAL '7 days', NOW() + INTERVAL '23 days', NULL)
ON CONFLICT DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- Consent Records (GDPR)
-- ─────────────────────────────────────────────────────────────────────────────
-- Sysadmin: all consents granted
INSERT INTO consent_records (id, user_id, tenant_id, purpose, is_granted, ip_address, granted_at) VALUES
  ('019568f0-0060-7000-8000-000000000060', '019568d0-0001-7000-8000-000000000001', '019568b0-0001-7000-8000-000000000001', 0, TRUE, '10.0.0.1', NOW()),
  ('019568f0-0061-7000-8000-000000000061', '019568d0-0001-7000-8000-000000000001', '019568b0-0001-7000-8000-000000000001', 1, TRUE, '10.0.0.1', NOW()),
  ('019568f0-0062-7000-8000-000000000062', '019568d0-0001-7000-8000-000000000001', '019568b0-0001-7000-8000-000000000001', 3, TRUE, '10.0.0.1', NOW())
ON CONFLICT DO NOTHING;

-- John Doe: auth + profile only, marketing withdrawn
INSERT INTO consent_records (id, user_id, tenant_id, purpose, is_granted, ip_address, granted_at, withdrawn_at) VALUES
  ('019568f0-0063-7000-8000-000000000063', '019568d0-0011-7000-8000-000000000011', '019568b0-0002-7000-8000-000000000002', 0, TRUE,  '192.168.1.50', NOW(), NULL),
  ('019568f0-0064-7000-8000-000000000064', '019568d0-0011-7000-8000-000000000011', '019568b0-0002-7000-8000-000000000002', 1, TRUE,  '192.168.1.50', NOW(), NULL),
  ('019568f0-0065-7000-8000-000000000065', '019568d0-0011-7000-8000-000000000011', '019568b0-0002-7000-8000-000000000002', 2, FALSE, '192.168.1.50', NOW() - INTERVAL '30 days', NOW() - INTERVAL '7 days')
ON CONFLICT DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- PII Audit Log (sample entries)
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO pii_audit_log (id, tenant_id, user_id, actor_id, action, field_name, reason, ip_address, timestamp) VALUES
  ('019568f0-0070-7000-8000-000000000070',
   '019568b0-0001-7000-8000-000000000001',
   '019568d0-0001-7000-8000-000000000001',
   '019568d0-0001-7000-8000-000000000001',
   'ACCESS', 'email', 'Login flow — email lookup', '10.0.0.1', NOW()),
  ('019568f0-0071-7000-8000-000000000071',
   '019568b0-0002-7000-8000-000000000002',
   '019568d0-0011-7000-8000-000000000011',
   NULL,
   'ACCESS', 'email', 'Profile page view', '192.168.1.50', NOW()),
  ('019568f0-0072-7000-8000-000000000072',
   '019568b0-0002-7000-8000-000000000002',
   '019568d0-0012-7000-8000-000000000012',
   '019568d0-0010-7000-8000-000000000010',
   'MUTATION', 'failed_login_attempts', 'Account lockout triggered', '10.0.0.5', NOW())
ON CONFLICT DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- Data Erasure Request (sample — completed)
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO data_erasure_requests (id, user_id, tenant_id, status, requested_by_ip, completion_notes, requested_at, completed_at) VALUES
  ('019568f0-0080-7000-8000-000000000080',
   '019568d0-0012-7000-8000-000000000012', '019568b0-0002-7000-8000-000000000002',
   0,  -- Requested (pending)
   '192.168.1.100',
   NULL,
   NOW(), NULL)
ON CONFLICT DO NOTHING;
