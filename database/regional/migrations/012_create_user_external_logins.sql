-- Regional DB: External identity provider links
CREATE TABLE user_external_logins (
    id                      UUID PRIMARY KEY,
    user_id                 UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    tenant_id               UUID NOT NULL,
    provider                INT NOT NULL,
    provider_key            VARCHAR(256) NOT NULL,
    provider_display_name   VARCHAR(200),
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX idx_external_login ON user_external_logins(tenant_id, provider, provider_key);
CREATE INDEX idx_external_login_user ON user_external_logins(tenant_id, user_id);
