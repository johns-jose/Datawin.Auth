-- Regional DB: Users table (PII fields stored encrypted)
CREATE TABLE users (
    id                      UUID PRIMARY KEY,
    tenant_id               UUID NOT NULL,
    email_cipher            BYTEA NOT NULL,
    email_nonce             BYTEA NOT NULL,
    email_tag               BYTEA NOT NULL,
    email_key_id            VARCHAR(100) NOT NULL,
    email_hash              VARCHAR(64) NOT NULL,
    display_name_cipher     BYTEA,
    display_name_nonce      BYTEA,
    display_name_tag        BYTEA,
    display_name_key_id     VARCHAR(100),
    phone_cipher            BYTEA,
    phone_nonce             BYTEA,
    phone_tag               BYTEA,
    phone_key_id            VARCHAR(100),
    is_active               BOOLEAN NOT NULL DEFAULT TRUE,
    email_confirmed         BOOLEAN NOT NULL DEFAULT FALSE,
    mfa_enabled             BOOLEAN NOT NULL DEFAULT FALSE,
    failed_login_attempts   INT NOT NULL DEFAULT 0,
    lockout_end             TIMESTAMPTZ,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_users_tenant ON users(tenant_id);
CREATE UNIQUE INDEX idx_users_email_hash ON users(tenant_id, email_hash);
