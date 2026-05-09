-- Regional DB: User credentials
CREATE TABLE user_credentials (
    id              UUID PRIMARY KEY,
    user_id         UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    tenant_id       UUID NOT NULL,
    password_hash   TEXT NOT NULL,
    algorithm       VARCHAR(20) NOT NULL DEFAULT 'argon2id',
    recovery_codes  TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX idx_user_credentials_user ON user_credentials(tenant_id, user_id);
