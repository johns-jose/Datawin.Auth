-- Regional DB: Data erasure requests
CREATE TABLE data_erasure_requests (
    id                  UUID PRIMARY KEY,
    user_id             UUID NOT NULL,
    tenant_id           UUID NOT NULL,
    status              INT NOT NULL DEFAULT 0,
    requested_by_ip     VARCHAR(45) NOT NULL,
    completion_notes    TEXT,
    requested_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    completed_at        TIMESTAMPTZ
);

CREATE INDEX idx_erasure_tenant ON data_erasure_requests(tenant_id);
CREATE INDEX idx_erasure_status ON data_erasure_requests(status) WHERE status IN (0, 1);
