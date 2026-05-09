-- =============================================================================
-- DataWin.Auth — Regional Database Seed (eu-west-1)
-- Target: datawin_auth_eu_west_1
-- Run AFTER all regional migrations and stored procedures.
--
-- This region is the primary for Europa GmbH (GDPR-primary tenant).
-- Encryption keys and BYTEA values are placeholders for development only.
-- =============================================================================

-- ─────────────────────────────────────────────────────────────────────────────
-- Encryption Keys
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO encryption_keys (id, tenant_id, key_id, wrapped_key, is_active, created_at) VALUES
  (gen_random_uuid(), '019568b0-0003-7000-8000-000000000003', 'dek_019568b0-0003-7000-8000-000000000003', E'\\xDEADBEEF00030003', TRUE, NOW())
ON CONFLICT (tenant_id, key_id) DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- Users (Europa GmbH — EU region)
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO users (
    id, tenant_id,
    email_cipher, email_nonce, email_tag, email_key_id, email_hash,
    display_name_cipher, display_name_nonce, display_name_tag, display_name_key_id,
    is_active, email_confirmed, mfa_enabled, failed_login_attempts, created_at, updated_at
) VALUES
  -- admin@europa-gmbh.de
  ('019568d0-0020-7000-8000-000000000020', '019568b0-0003-7000-8000-000000000003',
   E'\\x736565645F656D61696C5F6575726F70615F61646D696E', E'\\x000000000030', E'\\x00000000000000000000000000000030',
   'dek_019568b0-0003-7000-8000-000000000003',
   'sOp1QtU5wB7YzAcD0El6Fv9Gw2Hx4Iy8Jz3Ka5Lb7Mc=',
   E'\\x4575726F70612041646D696E', E'\\x000000000031', E'\\x00000000000000000000000000000031',
   'dek_019568b0-0003-7000-8000-000000000003',
   TRUE, TRUE, TRUE, 0, NOW(), NOW()),
  -- hans.mueller@europa-gmbh.de
  ('019568d0-0021-7000-8000-000000000021', '019568b0-0003-7000-8000-000000000003',
   E'\\x736565645F656D61696C5F68616E735F6D75656C6C6572', E'\\x000000000032', E'\\x00000000000000000000000000000032',
   'dek_019568b0-0003-7000-8000-000000000003',
   'tPq2RuV6xC8ZaBdE1Fm7Gw0Hy3Iz5Ja1Kb4Lc6Md8Ne=',
   E'\\x48616E73204DC3BC6C6C6572', E'\\x000000000033', E'\\x00000000000000000000000000000033',
   'dek_019568b0-0003-7000-8000-000000000003',
   TRUE, TRUE, FALSE, 0, NOW(), NOW()),
  -- maria.schmidt@europa-gmbh.de (unconfirmed email)
  ('019568d0-0022-7000-8000-000000000022', '019568b0-0003-7000-8000-000000000003',
   E'\\x736565645F656D61696C5F6D617269615F7363686D696474', E'\\x000000000034', E'\\x00000000000000000000000000000034',
   'dek_019568b0-0003-7000-8000-000000000003',
   'uQr3SvW7yD9AbCeF2Gn8Hx1Iz4Ja2Kb5Lc7Md9Ne0Of=',
   E'\\x4D6172696120536368656D696474', E'\\x000000000035', E'\\x00000000000000000000000000000035',
   'dek_019568b0-0003-7000-8000-000000000003',
   TRUE, FALSE, FALSE, 0, NOW(), NOW())
ON CONFLICT DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- User Credentials (password: "P@ssw0rd!2025")
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO user_credentials (id, user_id, tenant_id, password_hash, algorithm, recovery_codes, created_at, updated_at) VALUES
  ('019568e0-0020-7000-8000-000000000020', '019568d0-0020-7000-8000-000000000020', '019568b0-0003-7000-8000-000000000003',
   'c2VlZF9zYWx0XzAwMjA=.c2VlZF9oYXNoXzAwMjA=', 'argon2id', 'EU-RCVRY-AAAA-BBBB,EU-RCVRY-CCCC-DDDD', NOW(), NOW()),
  ('019568e0-0021-7000-8000-000000000021', '019568d0-0021-7000-8000-000000000021', '019568b0-0003-7000-8000-000000000003',
   'c2VlZF9zYWx0XzAwMjE=.c2VlZF9oYXNoXzAwMjE=', 'argon2id', NULL, NOW(), NOW()),
  ('019568e0-0022-7000-8000-000000000022', '019568d0-0022-7000-8000-000000000022', '019568b0-0003-7000-8000-000000000003',
   'c2VlZF9zYWx0XzAwMjI=.c2VlZF9oYXNoXzAwMjI=', 'argon2id', NULL, NOW(), NOW())
ON CONFLICT DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- MFA Enrollment (Europa admin has TOTP + WebAuthn)
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO user_mfa (id, user_id, tenant_id, method, secret_cipher, secret_nonce, secret_tag, secret_key_id, is_verified, created_at) VALUES
  ('019568f0-0090-7000-8000-000000000090',
   '019568d0-0020-7000-8000-000000000020', '019568b0-0003-7000-8000-000000000003',
   1,  -- TOTP
   E'\\x736565645F6575726F70615F746F7470', E'\\x000000000040', E'\\x00000000000000000000000000000040',
   'dek_019568b0-0003-7000-8000-000000000003',
   TRUE, NOW()),
  ('019568f0-0091-7000-8000-000000000091',
   '019568d0-0020-7000-8000-000000000020', '019568b0-0003-7000-8000-000000000003',
   4,  -- WebAuthn
   E'\\x736565645F6575726F70615F776562617574686E', E'\\x000000000041', E'\\x00000000000000000000000000000041',
   'dek_019568b0-0003-7000-8000-000000000003',
   TRUE, NOW())
ON CONFLICT DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- External Login (Hans linked to Azure AD)
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO user_external_logins (id, user_id, tenant_id, provider, provider_key, provider_display_name, created_at) VALUES
  ('019568f0-00A0-7000-8000-0000000000A0',
   '019568d0-0021-7000-8000-000000000021', '019568b0-0003-7000-8000-000000000003',
   11,  -- ExternalAzureAd
   'azure-ad-oid-europa-hans-001',
   'Hans Müller (Azure AD)',
   NOW())
ON CONFLICT (tenant_id, provider, provider_key) DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- Consent Records (GDPR — all Europa users have auth + profile consent)
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO consent_records (id, user_id, tenant_id, purpose, is_granted, ip_address, granted_at) VALUES
  ('019568f0-00B0-7000-8000-0000000000B0', '019568d0-0020-7000-8000-000000000020', '019568b0-0003-7000-8000-000000000003', 0, TRUE, '10.10.0.1', NOW()),
  ('019568f0-00B1-7000-8000-0000000000B1', '019568d0-0020-7000-8000-000000000020', '019568b0-0003-7000-8000-000000000003', 1, TRUE, '10.10.0.1', NOW()),
  ('019568f0-00B2-7000-8000-0000000000B2', '019568d0-0021-7000-8000-000000000021', '019568b0-0003-7000-8000-000000000003', 0, TRUE, '10.10.0.2', NOW()),
  ('019568f0-00B3-7000-8000-0000000000B3', '019568d0-0021-7000-8000-000000000021', '019568b0-0003-7000-8000-000000000003', 1, TRUE, '10.10.0.2', NOW()),
  ('019568f0-00B4-7000-8000-0000000000B4', '019568d0-0022-7000-8000-000000000022', '019568b0-0003-7000-8000-000000000003', 0, TRUE, '10.10.0.3', NOW())
ON CONFLICT DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- Active Session (Europa admin)
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO sessions (id, user_id, tenant_id, device_fingerprint, ip_address, user_agent, is_revoked, created_at, expires_at) VALUES
  ('019568f0-00C0-7000-8000-0000000000C0',
   '019568d0-0020-7000-8000-000000000020', '019568b0-0003-7000-8000-000000000003',
   'fp-seed-eu-desktop-001', '10.10.0.1', 'Mozilla/5.0 (X11; Linux x86_64)',
   FALSE, NOW(), NOW() + INTERVAL '30 days')
ON CONFLICT DO NOTHING;

INSERT INTO refresh_tokens (id, session_id, user_id, tenant_id, token_hash, is_revoked, created_at, expires_at) VALUES
  ('019568f0-00D0-7000-8000-0000000000D0',
   '019568f0-00C0-7000-8000-0000000000C0',
   '019568d0-0020-7000-8000-000000000020', '019568b0-0003-7000-8000-000000000003',
   'c2VlZF9ldV90b2tlbl9oYXNoXzAwMDE=',
   FALSE, NOW(), NOW() + INTERVAL '30 days', NULL)
ON CONFLICT DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- PII Audit Log
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO pii_audit_log (id, tenant_id, user_id, actor_id, action, field_name, reason, ip_address, timestamp) VALUES
  ('019568f0-00E0-7000-8000-0000000000E0',
   '019568b0-0003-7000-8000-000000000003',
   '019568d0-0020-7000-8000-000000000020',
   '019568d0-0020-7000-8000-000000000020',
   'ACCESS', 'email', 'Login flow — email lookup', '10.10.0.1', NOW()),
  ('019568f0-00E1-7000-8000-0000000000E1',
   '019568b0-0003-7000-8000-000000000003',
   '019568d0-0021-7000-8000-000000000021',
   NULL,
   'ACCESS', 'display_name', 'Profile page view', '10.10.0.2', NOW())
ON CONFLICT DO NOTHING;
