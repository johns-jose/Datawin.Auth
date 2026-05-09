-- Regional DB: Encryption keys (tenant-scoped DEKs)
CREATE TABLE encryption_keys (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL,
    key_id          VARCHAR(100) NOT NULL,
    wrapped_key     BYTEA NOT NULL,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    destroyed_at    TIMESTAMPTZ
);

CREATE UNIQUE INDEX idx_encryption_keys_tenant ON encryption_keys(tenant_id, key_id);
