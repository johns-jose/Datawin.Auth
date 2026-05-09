-- Regional DB: Refresh tokens
CREATE TABLE refresh_tokens (
    id                  UUID PRIMARY KEY,
    session_id          UUID NOT NULL REFERENCES sessions(id) ON DELETE CASCADE,
    user_id             UUID NOT NULL,
    tenant_id           UUID NOT NULL,
    token_hash          VARCHAR(64) NOT NULL,
    is_revoked          BOOLEAN NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at          TIMESTAMPTZ NOT NULL,
    replaced_by_token_id UUID
);

CREATE INDEX idx_refresh_tokens_hash ON refresh_tokens(tenant_id, token_hash);
CREATE INDEX idx_refresh_tokens_session ON refresh_tokens(session_id);
