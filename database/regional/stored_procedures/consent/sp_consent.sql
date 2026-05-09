-- Consent stored procedures

CREATE OR REPLACE FUNCTION sp_consent_get(p_tenant_id UUID, p_user_id UUID, p_purpose INT)
RETURNS SETOF consent_records AS $$
BEGIN RETURN QUERY SELECT * FROM consent_records WHERE tenant_id = p_tenant_id AND user_id = p_user_id AND purpose = p_purpose AND is_granted = TRUE; END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION sp_consent_get_all_for_user(p_tenant_id UUID, p_user_id UUID)
RETURNS SETOF consent_records AS $$
BEGIN RETURN QUERY SELECT * FROM consent_records WHERE tenant_id = p_tenant_id AND user_id = p_user_id ORDER BY granted_at DESC; END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE PROCEDURE sp_consent_grant(
    p_id UUID, p_user_id UUID, p_tenant_id UUID, p_purpose INT, p_ip_address VARCHAR, p_granted_at TIMESTAMPTZ)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO consent_records (id, user_id, tenant_id, purpose, is_granted, ip_address, granted_at)
    VALUES (p_id, p_user_id, p_tenant_id, p_purpose, TRUE, p_ip_address, p_granted_at)
    ON CONFLICT (tenant_id, user_id, purpose) WHERE is_granted = TRUE
    DO UPDATE SET granted_at = p_granted_at, ip_address = p_ip_address;
END; $$;

CREATE OR REPLACE PROCEDURE sp_consent_withdraw(p_tenant_id UUID, p_user_id UUID, p_purpose INT)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE consent_records SET is_granted = FALSE, withdrawn_at = NOW()
    WHERE tenant_id = p_tenant_id AND user_id = p_user_id AND purpose = p_purpose AND is_granted = TRUE;
END; $$;
