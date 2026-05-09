-- Regional DB: SAML configurations
CREATE TABLE saml_configurations (
    id                              UUID PRIMARY KEY,
    tenant_id                       UUID NOT NULL,
    entity_id                       VARCHAR(500) NOT NULL,
    metadata_url                    TEXT NOT NULL,
    assertion_consumer_service_url  TEXT NOT NULL,
    single_logout_service_url       TEXT NOT NULL,
    certificate_base64              TEXT NOT NULL,
    sign_requests                   BOOLEAN NOT NULL DEFAULT TRUE,
    is_active                       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at                      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at                      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX idx_saml_entity ON saml_configurations(tenant_id, entity_id);
