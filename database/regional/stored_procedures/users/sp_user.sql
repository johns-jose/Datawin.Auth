-- User stored procedures

CREATE OR REPLACE FUNCTION sp_user_get_by_id(p_tenant_id UUID, p_user_id UUID)
RETURNS SETOF users AS $$
BEGIN RETURN QUERY SELECT * FROM users WHERE tenant_id = p_tenant_id AND id = p_user_id; END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION sp_user_get_by_email_hash(p_tenant_id UUID, p_email_hash VARCHAR)
RETURNS SETOF users AS $$
BEGIN RETURN QUERY SELECT * FROM users WHERE tenant_id = p_tenant_id AND email_hash = p_email_hash; END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE PROCEDURE sp_user_create(
    p_id UUID, p_tenant_id UUID,
    p_email_cipher BYTEA, p_email_nonce BYTEA, p_email_tag BYTEA, p_email_key_id VARCHAR,
    p_email_hash VARCHAR,
    p_display_name_cipher BYTEA, p_display_name_nonce BYTEA, p_display_name_tag BYTEA, p_display_name_key_id VARCHAR,
    p_is_active BOOLEAN, p_email_confirmed BOOLEAN, p_mfa_enabled BOOLEAN,
    p_created_at TIMESTAMPTZ, p_updated_at TIMESTAMPTZ)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO users (id, tenant_id, email_cipher, email_nonce, email_tag, email_key_id, email_hash,
        display_name_cipher, display_name_nonce, display_name_tag, display_name_key_id,
        is_active, email_confirmed, mfa_enabled, created_at, updated_at)
    VALUES (p_id, p_tenant_id, p_email_cipher, p_email_nonce, p_email_tag, p_email_key_id, p_email_hash,
        p_display_name_cipher, p_display_name_nonce, p_display_name_tag, p_display_name_key_id,
        p_is_active, p_email_confirmed, p_mfa_enabled, p_created_at, p_updated_at);
END; $$;

CREATE OR REPLACE PROCEDURE sp_user_update(
    p_id UUID, p_tenant_id UUID, p_is_active BOOLEAN, p_email_confirmed BOOLEAN,
    p_mfa_enabled BOOLEAN, p_failed_login_attempts INT, p_lockout_end TIMESTAMPTZ, p_updated_at TIMESTAMPTZ)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE users SET is_active = p_is_active, email_confirmed = p_email_confirmed,
        mfa_enabled = p_mfa_enabled, failed_login_attempts = p_failed_login_attempts,
        lockout_end = p_lockout_end, updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END; $$;

CREATE OR REPLACE FUNCTION sp_user_get_credential(p_tenant_id UUID, p_user_id UUID)
RETURNS SETOF user_credentials AS $$
BEGIN RETURN QUERY SELECT * FROM user_credentials WHERE tenant_id = p_tenant_id AND user_id = p_user_id; END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE PROCEDURE sp_user_create_credential(
    p_id UUID, p_user_id UUID, p_tenant_id UUID, p_password_hash TEXT, p_algorithm VARCHAR,
    p_recovery_codes TEXT, p_created_at TIMESTAMPTZ, p_updated_at TIMESTAMPTZ)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO user_credentials (id, user_id, tenant_id, password_hash, algorithm, recovery_codes, created_at, updated_at)
    VALUES (p_id, p_user_id, p_tenant_id, p_password_hash, p_algorithm, p_recovery_codes, p_created_at, p_updated_at);
END; $$;

CREATE OR REPLACE PROCEDURE sp_user_update_credential(
    p_id UUID, p_tenant_id UUID, p_password_hash TEXT, p_algorithm VARCHAR,
    p_recovery_codes TEXT, p_updated_at TIMESTAMPTZ)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE user_credentials SET password_hash = p_password_hash, algorithm = p_algorithm,
        recovery_codes = p_recovery_codes, updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END; $$;

CREATE OR REPLACE FUNCTION sp_user_get_external_logins(p_tenant_id UUID, p_user_id UUID)
RETURNS SETOF user_external_logins AS $$
BEGIN RETURN QUERY SELECT * FROM user_external_logins WHERE tenant_id = p_tenant_id AND user_id = p_user_id; END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE PROCEDURE sp_user_link_external_login(
    p_id UUID, p_user_id UUID, p_tenant_id UUID, p_provider INT,
    p_provider_key VARCHAR, p_provider_display_name VARCHAR, p_created_at TIMESTAMPTZ)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO user_external_logins (id, user_id, tenant_id, provider, provider_key, provider_display_name, created_at)
    VALUES (p_id, p_user_id, p_tenant_id, p_provider, p_provider_key, p_provider_display_name, p_created_at)
    ON CONFLICT (tenant_id, provider, provider_key) DO NOTHING;
END; $$;
