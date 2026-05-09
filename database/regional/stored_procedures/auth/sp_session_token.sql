-- Session and token stored procedures

CREATE OR REPLACE FUNCTION sp_session_get_by_id(p_tenant_id UUID, p_session_id UUID)
RETURNS SETOF sessions AS $$
BEGIN RETURN QUERY SELECT * FROM sessions WHERE tenant_id = p_tenant_id AND id = p_session_id; END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE PROCEDURE sp_session_create(
    p_id UUID, p_user_id UUID, p_tenant_id UUID, p_device_fingerprint VARCHAR,
    p_ip_address VARCHAR, p_user_agent TEXT, p_created_at TIMESTAMPTZ, p_expires_at TIMESTAMPTZ)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO sessions (id, user_id, tenant_id, device_fingerprint, ip_address, user_agent, created_at, expires_at)
    VALUES (p_id, p_user_id, p_tenant_id, p_device_fingerprint, p_ip_address, p_user_agent, p_created_at, p_expires_at);
END; $$;

CREATE OR REPLACE PROCEDURE sp_session_revoke(p_tenant_id UUID, p_session_id UUID)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE sessions SET is_revoked = TRUE WHERE id = p_session_id AND tenant_id = p_tenant_id;
    UPDATE refresh_tokens SET is_revoked = TRUE WHERE session_id = p_session_id AND tenant_id = p_tenant_id;
END; $$;

CREATE OR REPLACE PROCEDURE sp_session_revoke_all_for_user(p_tenant_id UUID, p_user_id UUID)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE sessions SET is_revoked = TRUE WHERE user_id = p_user_id AND tenant_id = p_tenant_id AND is_revoked = FALSE;
    UPDATE refresh_tokens SET is_revoked = TRUE WHERE user_id = p_user_id AND tenant_id = p_tenant_id AND is_revoked = FALSE;
END; $$;

CREATE OR REPLACE FUNCTION sp_token_get_by_hash(p_tenant_id UUID, p_token_hash VARCHAR)
RETURNS SETOF refresh_tokens AS $$
BEGIN RETURN QUERY SELECT * FROM refresh_tokens WHERE tenant_id = p_tenant_id AND token_hash = p_token_hash; END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE PROCEDURE sp_token_create_refresh(
    p_id UUID, p_session_id UUID, p_user_id UUID, p_tenant_id UUID,
    p_token_hash VARCHAR, p_created_at TIMESTAMPTZ, p_expires_at TIMESTAMPTZ)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO refresh_tokens (id, session_id, user_id, tenant_id, token_hash, created_at, expires_at)
    VALUES (p_id, p_session_id, p_user_id, p_tenant_id, p_token_hash, p_created_at, p_expires_at);
END; $$;

CREATE OR REPLACE PROCEDURE sp_token_revoke(p_tenant_id UUID, p_token_id UUID, p_replaced_by UUID)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE refresh_tokens SET is_revoked = TRUE, replaced_by_token_id = p_replaced_by
    WHERE id = p_token_id AND tenant_id = p_tenant_id;
END; $$;
