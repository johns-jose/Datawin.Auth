-- Global stored procedures for tenants

CREATE OR REPLACE FUNCTION sp_tenant_get_by_id(p_id UUID)
RETURNS SETOF tenants AS $$ BEGIN RETURN QUERY SELECT * FROM tenants WHERE id = p_id; END; $$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION sp_tenant_get_by_slug(p_slug VARCHAR)
RETURNS SETOF tenants AS $$ BEGIN RETURN QUERY SELECT * FROM tenants WHERE slug = p_slug; END; $$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION sp_tenant_get_by_domain(p_domain VARCHAR)
RETURNS SETOF tenants AS $$ BEGIN RETURN QUERY SELECT * FROM tenants WHERE domain = p_domain; END; $$ LANGUAGE plpgsql;

CREATE OR REPLACE PROCEDURE sp_tenant_create(
    p_id UUID, p_name VARCHAR, p_slug VARCHAR, p_domain VARCHAR,
    p_status INT, p_created_at TIMESTAMPTZ, p_updated_at TIMESTAMPTZ)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO tenants (id, name, slug, domain, status, created_at, updated_at)
    VALUES (p_id, p_name, p_slug, p_domain, p_status, p_created_at, p_updated_at);
END; $$;

CREATE OR REPLACE PROCEDURE sp_tenant_update(
    p_id UUID, p_name VARCHAR, p_slug VARCHAR, p_domain VARCHAR,
    p_status INT, p_updated_at TIMESTAMPTZ)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE tenants SET name = p_name, slug = p_slug, domain = p_domain,
        status = p_status, updated_at = p_updated_at WHERE id = p_id;
END; $$;

CREATE OR REPLACE FUNCTION sp_tenant_get_regions(p_tenant_id UUID)
RETURNS SETOF tenant_regions AS $$ BEGIN RETURN QUERY SELECT * FROM tenant_regions WHERE tenant_id = p_tenant_id; END; $$ LANGUAGE plpgsql;

CREATE OR REPLACE PROCEDURE sp_tenant_add_region(
    p_id UUID, p_tenant_id UUID, p_region_code VARCHAR, p_is_primary BOOLEAN, p_created_at TIMESTAMPTZ)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO tenant_regions (id, tenant_id, region_code, is_primary, created_at)
    VALUES (p_id, p_tenant_id, p_region_code, p_is_primary, p_created_at);
END; $$;

CREATE OR REPLACE PROCEDURE sp_tenant_remove_region(p_tenant_id UUID, p_region_code VARCHAR)
LANGUAGE plpgsql AS $$
BEGIN
    DELETE FROM tenant_regions WHERE tenant_id = p_tenant_id AND region_code = p_region_code;
END; $$;
