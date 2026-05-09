-- Regional DB: Consent records (GDPR)
CREATE TABLE consent_records (
    id              UUID PRIMARY KEY,
    user_id         UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    tenant_id       UUID NOT NULL,
    purpose         INT NOT NULL,
    is_granted      BOOLEAN NOT NULL DEFAULT TRUE,
    ip_address      VARCHAR(45) NOT NULL,
    granted_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    withdrawn_at    TIMESTAMPTZ
);

CREATE INDEX idx_consent_user ON consent_records(tenant_id, user_id);
CREATE UNIQUE INDEX idx_consent_unique ON consent_records(tenant_id, user_id, purpose) WHERE is_granted = TRUE;
