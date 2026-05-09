-- Global DB: Tenant regions
CREATE TABLE tenant_regions (
    id              UUID PRIMARY KEY,
    tenant_id       UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    region_code     VARCHAR(50) NOT NULL,
    is_primary      BOOLEAN NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(tenant_id, region_code)
);

CREATE INDEX idx_tenant_regions_tenant ON tenant_regions(tenant_id);
CREATE INDEX idx_tenant_regions_primary ON tenant_regions(tenant_id) WHERE is_primary = TRUE;
