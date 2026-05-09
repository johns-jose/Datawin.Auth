-- Regional DB: Sessions
CREATE TABLE sessions (
    id                  UUID PRIMARY KEY,
    user_id             UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    tenant_id           UUID NOT NULL,
    device_fingerprint  VARCHAR(256) NOT NULL,
    ip_address          VARCHAR(45) NOT NULL,
    user_agent          TEXT,
    is_revoked          BOOLEAN NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at          TIMESTAMPTZ NOT NULL
);

CREATE INDEX idx_sessions_user ON sessions(tenant_id, user_id);
CREATE INDEX idx_sessions_active ON sessions(tenant_id) WHERE is_revoked = FALSE;
