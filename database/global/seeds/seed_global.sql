-- =============================================================================
-- DataWin.Auth — Global Database Seed
-- Target: datawin_auth_global
-- Run AFTER all global migrations and stored procedures.
-- =============================================================================

-- ─────────────────────────────────────────────────────────────────────────────
-- Regional Endpoints
-- Define connection strings for each compliance region.
-- In production, replace with real credentials managed via secrets vault.
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO regional_endpoints (id, region_code, connection_string, is_active) VALUES
  ('019568a0-0001-7000-8000-000000000001', 'us-east-1',      'Host=localhost;Port=5432;Database=datawin_auth_us_east_1;Username=datawin;Password=changeme',      TRUE),
  ('019568a0-0002-7000-8000-000000000002', 'eu-west-1',      'Host=localhost;Port=5432;Database=datawin_auth_eu_west_1;Username=datawin;Password=changeme',      TRUE),
  ('019568a0-0003-7000-8000-000000000003', 'ap-southeast-1', 'Host=localhost;Port=5432;Database=datawin_auth_ap_southeast_1;Username=datawin;Password=changeme', TRUE),
  ('019568a0-0004-7000-8000-000000000004', 'ap-south-1',     'Host=localhost;Port=5432;Database=datawin_auth_ap_south_1;Username=datawin;Password=changeme',    TRUE),
  ('019568a0-0005-7000-8000-000000000005', 'sa-east-1',      'Host=localhost;Port=5432;Database=datawin_auth_sa_east_1;Username=datawin;Password=changeme',     TRUE)
ON CONFLICT (region_code) DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- Seed Tenants
-- ─────────────────────────────────────────────────────────────────────────────
-- Tenant 1: DataWin Platform (internal / system tenant)
INSERT INTO tenants (id, name, slug, domain, status, created_at, updated_at) VALUES
  ('019568b0-0001-7000-8000-000000000001',
   'DataWin Platform', 'datawin', 'auth.datawin.io', 0, NOW(), NOW())
ON CONFLICT (slug) DO NOTHING;

-- Tenant 2: Acme Corporation (sample customer — US)
INSERT INTO tenants (id, name, slug, domain, status, created_at, updated_at) VALUES
  ('019568b0-0002-7000-8000-000000000002',
   'Acme Corporation', 'acme-corp', 'acme-corp.datawin.io', 0, NOW(), NOW())
ON CONFLICT (slug) DO NOTHING;

-- Tenant 3: Europa GmbH (sample customer — EU, GDPR-primary)
INSERT INTO tenants (id, name, slug, domain, status, created_at, updated_at) VALUES
  ('019568b0-0003-7000-8000-000000000003',
   'Europa GmbH', 'europa-gmbh', 'europa.datawin.io', 0, NOW(), NOW())
ON CONFLICT (slug) DO NOTHING;

-- Tenant 4: Tokyo Systems (sample customer — APAC)
INSERT INTO tenants (id, name, slug, domain, status, created_at, updated_at) VALUES
  ('019568b0-0004-7000-8000-000000000004',
   'Tokyo Systems', 'tokyo-systems', 'tokyo.datawin.io', 0, NOW(), NOW())
ON CONFLICT (slug) DO NOTHING;

-- Tenant 5: Suspended tenant (testing suspended state)
INSERT INTO tenants (id, name, slug, domain, status, created_at, updated_at) VALUES
  ('019568b0-0005-7000-8000-000000000005',
   'Inactive Corp', 'inactive-corp', NULL, 1, NOW(), NOW())
ON CONFLICT (slug) DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- Tenant → Region Mappings
-- ─────────────────────────────────────────────────────────────────────────────
-- DataWin Platform: primary US, replicated to EU
INSERT INTO tenant_regions (id, tenant_id, region_code, is_primary, created_at) VALUES
  ('019568c0-0001-7000-8000-000000000001', '019568b0-0001-7000-8000-000000000001', 'us-east-1', TRUE,  NOW()),
  ('019568c0-0002-7000-8000-000000000002', '019568b0-0001-7000-8000-000000000001', 'eu-west-1', FALSE, NOW())
ON CONFLICT (tenant_id, region_code) DO NOTHING;

-- Acme Corporation: primary US
INSERT INTO tenant_regions (id, tenant_id, region_code, is_primary, created_at) VALUES
  ('019568c0-0003-7000-8000-000000000003', '019568b0-0002-7000-8000-000000000002', 'us-east-1', TRUE, NOW())
ON CONFLICT (tenant_id, region_code) DO NOTHING;

-- Europa GmbH: primary EU (GDPR), secondary US
INSERT INTO tenant_regions (id, tenant_id, region_code, is_primary, created_at) VALUES
  ('019568c0-0004-7000-8000-000000000004', '019568b0-0003-7000-8000-000000000003', 'eu-west-1', TRUE,  NOW()),
  ('019568c0-0005-7000-8000-000000000005', '019568b0-0003-7000-8000-000000000003', 'us-east-1', FALSE, NOW())
ON CONFLICT (tenant_id, region_code) DO NOTHING;

-- Tokyo Systems: primary APAC
INSERT INTO tenant_regions (id, tenant_id, region_code, is_primary, created_at) VALUES
  ('019568c0-0006-7000-8000-000000000006', '019568b0-0004-7000-8000-000000000004', 'ap-southeast-1', TRUE, NOW())
ON CONFLICT (tenant_id, region_code) DO NOTHING;

-- Inactive Corp: primary US (suspended but still mapped)
INSERT INTO tenant_regions (id, tenant_id, region_code, is_primary, created_at) VALUES
  ('019568c0-0007-7000-8000-000000000007', '019568b0-0005-7000-8000-000000000005', 'us-east-1', TRUE, NOW())
ON CONFLICT (tenant_id, region_code) DO NOTHING;
