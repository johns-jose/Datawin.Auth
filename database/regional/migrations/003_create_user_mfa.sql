-- Regional DB: MFA enrollments
CREATE TABLE user_mfa (
    id              UUID PRIMARY KEY,
    user_id         UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    tenant_id       UUID NOT NULL,
    method          INT NOT NULL,
    secret_cipher   BYTEA NOT NULL,
    secret_nonce    BYTEA NOT NULL,
    secret_tag      BYTEA NOT NULL,
    secret_key_id   VARCHAR(100) NOT NULL,
    is_verified     BOOLEAN NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_user_mfa_user ON user_mfa(tenant_id, user_id);
