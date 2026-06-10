-- =============================================================================
-- Additive schema change: provider catalog + company→provider mapping.
-- Safe to run on LIVE and LOCAL. Idempotent (re-runnable, no destructive ops).
--
-- Does NOT touch any existing table, column, or data. Only CREATEs two new
-- tables if they are absent. The PaymentProviderCode enum stays the source of
-- truth for identity; these tables hold editable metadata + per-company
-- assignment. After this runs, the app's startup seed/backfill populates rows.
--
-- Target: PostgreSQL. Run once per database, before deploying the new build.
-- =============================================================================

-- 1) Global provider catalog ---------------------------------------------------
--    PK = the PaymentProviderCode enum value (smallint). One row per provider.
CREATE TABLE IF NOT EXISTS payment_provider (
    code                       smallint                  NOT NULL,
    display_name               varchar(100)              NOT NULL,
    is_enabled                 boolean                   NOT NULL DEFAULT TRUE,
    default_base_url           varchar(200)              NULL,
    example_extra_config_json  jsonb                     NULL,
    sort_order                 integer                   NOT NULL DEFAULT 0,
    created_at                 timestamp with time zone  NOT NULL DEFAULT now(),
    updated_at                 timestamp with time zone  NOT NULL DEFAULT now(),
    CONSTRAINT pk_payment_provider PRIMARY KEY (code)
);

-- 2) Company → provider mapping (which providers a company may use) ------------
--    Unique per (company, provider). FK to company with cascade delete, matching
--    the existing company_provider_config relationship.
CREATE TABLE IF NOT EXISTS company_provider_mapping (
    id            uuid                      NOT NULL DEFAULT gen_random_uuid(),
    company_id    uuid                      NOT NULL,
    provider_code smallint                  NOT NULL,
    is_enabled    boolean                   NOT NULL DEFAULT TRUE,
    created_at    timestamp with time zone  NOT NULL DEFAULT now(),
    updated_at    timestamp with time zone  NOT NULL DEFAULT now(),
    CONSTRAINT pk_company_provider_mapping PRIMARY KEY (id),
    CONSTRAINT fk_company_provider_mapping_company
        FOREIGN KEY (company_id) REFERENCES company (id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_company_provider_mapping_company_provider
    ON company_provider_mapping (company_id, provider_code);

-- =============================================================================
-- NOTE: No INSERTs here. The application seeds payment_provider from the
-- registered providers and backfills company_provider_mapping from existing
-- company_provider_config rows automatically on startup (idempotent). If you
-- prefer to pre-seed manually, you can, but it is not required.
-- =============================================================================
