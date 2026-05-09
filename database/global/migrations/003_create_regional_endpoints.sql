-- Global DB: Regional endpoints
CREATE TABLE regional_endpoints (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    region_code         VARCHAR(50) NOT NULL UNIQUE,
    connection_string   TEXT NOT NULL,
    is_active           BOOLEAN NOT NULL DEFAULT TRUE,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
