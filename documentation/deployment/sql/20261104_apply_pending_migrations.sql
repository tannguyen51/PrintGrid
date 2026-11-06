-- ============================================================================
-- PrintGrid — apply pending migrations to a database that was deployed before
-- 2026-11-04 (Railway). Idempotent: safe to run more than once.
--
-- Covers:
--   20261101143739_EnforceSingleLabPerOwner
--   20261102050636_AddLabStaffMembership
--   20261104021425_AddModerationPrivacyAndContracts
--
-- Run in Railway → Postgres → Data → Query, or:
--   psql "<DATABASE_PUBLIC_URL>" -f 20261104_apply_pending_migrations.sql
--
-- PRE-CHECK (must return no rows). If it returns rows, two labs share one owner
-- and the unique index below will fail — merge/fix them first:
--   SELECT "OwnerUserId", count(*) FROM scheduling.labs
--   WHERE "OwnerUserId" IS NOT NULL GROUP BY 1 HAVING count(*) > 1;
-- ============================================================================

BEGIN;

-- ── 1. One lab per owner (partial unique index) ─────────────────────────────
DROP INDEX IF EXISTS scheduling."IX_labs_OwnerUserId";
CREATE UNIQUE INDEX "IX_labs_OwnerUserId"
    ON scheduling.labs ("OwnerUserId")
    WHERE ("OwnerUserId" IS NOT NULL);

-- ── 2. Lab staff membership ─────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS scheduling.lab_staff (
    "Id"           uuid                     NOT NULL,
    "LabId"        uuid                     NOT NULL,
    "UserId"       uuid                     NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_lab_staff" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_lab_staff_labs_LabId" FOREIGN KEY ("LabId")
        REFERENCES scheduling.labs ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_lab_staff_LabId_UserId"
    ON scheduling.lab_staff ("LabId", "UserId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_lab_staff_UserId"
    ON scheduling.lab_staff ("UserId");

-- ── 3. Moderation / privacy / contract ─────────────────────────────────────
ALTER TABLE customer.quotes
    ADD COLUMN IF NOT EXISTS "CustomerPlacementSummary" character varying(300);

ALTER TABLE customer.orders
    ADD COLUMN IF NOT EXISTS "ContractNumber" character varying(64);
ALTER TABLE customer.orders
    ADD COLUMN IF NOT EXISTS "TermsAccepted" boolean NOT NULL DEFAULT false;
ALTER TABLE customer.orders
    ADD COLUMN IF NOT EXISTS "TermsAcceptedAtUtc" timestamp with time zone;

ALTER TABLE customer.models
    ADD COLUMN IF NOT EXISTS "ReviewReason" character varying(500);
-- Existing models are grandfathered; the default is removed right after so that
-- new inserts must send the entity value (Pending).
ALTER TABLE customer.models
    ADD COLUMN IF NOT EXISTS "ReviewStatus" character varying(16) NOT NULL DEFAULT 'Approved';
ALTER TABLE customer.models
    ALTER COLUMN "ReviewStatus" DROP DEFAULT;
ALTER TABLE customer.models
    ADD COLUMN IF NOT EXISTS "ReviewedAtUtc" timestamp with time zone;
ALTER TABLE customer.models
    ADD COLUMN IF NOT EXISTS "ReviewedBy" uuid;
ALTER TABLE customer.models
    ADD COLUMN IF NOT EXISTS "ReviewedSha256" character varying(64);

ALTER TABLE scheduling.labs
    ADD COLUMN IF NOT EXISTS "District" character varying(100);

-- Backfills (idempotent: only rows that still need it)
UPDATE customer.orders
   SET "TermsAccepted" = TRUE,
       "TermsAcceptedAtUtc" = "CreatedAt"
 WHERE "TermsAcceptedAtUtc" IS NULL;

UPDATE customer.orders
   SET "ContractNumber" = 'PG-CT-'
       || to_char("ConfirmedAt" AT TIME ZONE 'UTC', 'YYYYMMDD')
       || '-' || upper(substr(replace("Id"::text, '-', ''), 1, 12))
 WHERE "ConfirmedAt" IS NOT NULL
   AND "ContractNumber" IS NULL;

-- Indexes (after the backfill so the unique index can be built)
CREATE UNIQUE INDEX IF NOT EXISTS "IX_orders_ContractNumber"
    ON customer.orders ("ContractNumber")
    WHERE ("ContractNumber" IS NOT NULL);
CREATE INDEX IF NOT EXISTS "IX_models_ReviewStatus_CreatedAt"
    ON customer.models ("ReviewStatus", "CreatedAt");

-- ── 4. Record the migrations so `dotnet ef database update` stays a no-op ──
INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES
    ('20261101143739_EnforceSingleLabPerOwner',          '8.0.11'),
    ('20261102050636_AddLabStaffMembership',             '8.0.11'),
    ('20261104021425_AddModerationPrivacyAndContracts',  '8.0.11')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;
