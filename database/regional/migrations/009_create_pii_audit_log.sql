-- Regional DB: PII audit log
CREATE TABLE pii_audit_log (
    id              UUID PRIMARY KEY,
    tenant_id       UUID NOT NULL,
    user_id         UUID NOT NULL,
    actor_id        UUID,
    action          VARCHAR(20) NOT NULL,
    field_name      VARCHAR(100) NOT NULL,
    reason          VARCHAR(500) NOT NULL,
    ip_address      VARCHAR(45) NOT NULL,
    timestamp       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_pii_audit_user ON pii_audit_log(tenant_id, user_id);
CREATE INDEX idx_pii_audit_timestamp ON pii_audit_log(timestamp);
