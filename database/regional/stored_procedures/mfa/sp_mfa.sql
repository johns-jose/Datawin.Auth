-- MFA stored procedures

CREATE OR REPLACE PROCEDURE sp_mfa_enroll(
    p_id UUID, p_user_id UUID, p_tenant_id UUID, p_method INT,
    p_secret_cipher BYTEA, p_secret_nonce BYTEA, p_secret_tag BYTEA, p_secret_key_id VARCHAR,
    p_created_at TIMESTAMPTZ)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO user_mfa (id, user_id, tenant_id, method, secret_cipher, secret_nonce, secret_tag, secret_key_id, created_at)
    VALUES (p_id, p_user_id, p_tenant_id, p_method, p_secret_cipher, p_secret_nonce, p_secret_tag, p_secret_key_id, p_created_at);
    UPDATE users SET mfa_enabled = TRUE, updated_at = NOW() WHERE id = p_user_id AND tenant_id = p_tenant_id;
END; $$;

CREATE OR REPLACE PROCEDURE sp_mfa_verify(p_tenant_id UUID, p_user_id UUID, p_method INT)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE user_mfa SET is_verified = TRUE WHERE user_id = p_user_id AND tenant_id = p_tenant_id AND method = p_method;
END; $$;

CREATE OR REPLACE PROCEDURE sp_mfa_disable(p_tenant_id UUID, p_user_id UUID, p_method INT)
LANGUAGE plpgsql AS $$
BEGIN
    DELETE FROM user_mfa WHERE user_id = p_user_id AND tenant_id = p_tenant_id AND method = p_method;
    -- Disable MFA flag if no methods remain
    UPDATE users SET mfa_enabled = EXISTS(SELECT 1 FROM user_mfa WHERE user_id = p_user_id AND tenant_id = p_tenant_id),
        updated_at = NOW()
    WHERE id = p_user_id AND tenant_id = p_tenant_id;
END; $$;
