-- Regional DB: OAuth clients
CREATE TABLE oauth_clients (
    id                              UUID PRIMARY KEY,
    tenant_id                       UUID NOT NULL,
    client_id                       VARCHAR(100) NOT NULL,
    client_secret_hash              TEXT NOT NULL,
    display_name                    VARCHAR(200) NOT NULL,
    redirect_uris                   TEXT[] NOT NULL,
    allowed_scopes                  TEXT[] NOT NULL,
    allowed_grant_types             TEXT[] NOT NULL,
    access_token_lifetime_seconds   INT NOT NULL DEFAULT 900,
    refresh_token_lifetime_seconds  INT NOT NULL DEFAULT 86400,
    is_active                       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at                      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at                      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX idx_oauth_clients_client_id ON oauth_clients(tenant_id, client_id);
