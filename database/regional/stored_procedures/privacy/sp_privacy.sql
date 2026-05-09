-- Privacy stored procedures

CREATE OR REPLACE PROCEDURE sp_pii_audit_write(
    p_id UUID, p_tenant_id UUID, p_user_id UUID, p_actor_id UUID,
    p_action VARCHAR, p_field_name VARCHAR, p_reason VARCHAR, p_ip_address VARCHAR, p_timestamp TIMESTAMPTZ)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO pii_audit_log (id, tenant_id, user_id, actor_id, action, field_name, reason, ip_address, timestamp)
    VALUES (p_id, p_tenant_id, p_user_id, p_actor_id, p_action, p_field_name, p_reason, p_ip_address, p_timestamp);
END; $$;

CREATE OR REPLACE FUNCTION sp_pii_audit_get_by_user(p_tenant_id UUID, p_user_id UUID)
RETURNS SETOF pii_audit_log AS $$
BEGIN RETURN QUERY SELECT * FROM pii_audit_log WHERE tenant_id = p_tenant_id AND user_id = p_user_id ORDER BY timestamp DESC; END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE PROCEDURE sp_erasure_request_create(
    p_id UUID, p_user_id UUID, p_tenant_id UUID, p_status INT, p_requested_by_ip VARCHAR, p_requested_at TIMESTAMPTZ)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO data_erasure_requests (id, user_id, tenant_id, status, requested_by_ip, requested_at)
    VALUES (p_id, p_user_id, p_tenant_id, p_status, p_requested_by_ip, p_requested_at);
END; $$;

CREATE OR REPLACE FUNCTION sp_erasure_get(p_tenant_id UUID, p_request_id UUID)
RETURNS SETOF data_erasure_requests AS $$
BEGIN RETURN QUERY SELECT * FROM data_erasure_requests WHERE tenant_id = p_tenant_id AND id = p_request_id; END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE PROCEDURE sp_erasure_request_update(
    p_id UUID, p_tenant_id UUID, p_status INT, p_completion_notes TEXT, p_completed_at TIMESTAMPTZ)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE data_erasure_requests SET status = p_status, completion_notes = p_completion_notes, completed_at = p_completed_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END; $$;

CREATE OR REPLACE PROCEDURE sp_encryption_key_destroy(p_tenant_id UUID)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE encryption_keys SET is_active = FALSE, destroyed_at = NOW()
    WHERE tenant_id = p_tenant_id AND is_active = TRUE;
END; $$;
