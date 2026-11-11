using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RepairQuoteReviewSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AddCustomerModelSelection was shipped without migration metadata,
            // so EF never included it in deployment scripts. Its snapshot also
            // included Quote fields that its Up method did not create. Repair
            // both fresh databases and installations patched manually in place.
            migrationBuilder.Sql("""
                ALTER TABLE customer.quotes
                    ADD COLUMN IF NOT EXISTS "ProfileId" uuid NULL,
                    ADD COLUMN IF NOT EXISTS "ProfileVersion" text NULL,
                    ADD COLUMN IF NOT EXISTS "PinSurchargeCoefficient" numeric NULL,
                    ADD COLUMN IF NOT EXISTS "PinnedModelMachineId" uuid NULL,
                    ADD COLUMN IF NOT EXISTS "UnpinnedPriceVnd" bigint NULL,
                    ADD COLUMN IF NOT EXISTS "UnpinnedDeliveryDay" date NULL;

                DO $repair$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'customer' AND table_name = 'quote_items'
                        AND column_name = 'ProfileVersion')
                        AND NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'customer' AND table_name = 'quote_items'
                        AND column_name = 'profile_version') THEN
                        ALTER TABLE customer.quote_items RENAME COLUMN "ProfileVersion" TO profile_version;
                    END IF;
                END $repair$;

                ALTER TABLE customer.quote_items
                    ADD COLUMN IF NOT EXISTS profile_id uuid NULL,
                    ADD COLUMN IF NOT EXISTS profile_version character varying(100) NULL,
                    ADD COLUMN IF NOT EXISTS "SurchargePinchCoef" numeric(5,3) NULL,
                    ADD COLUMN IF NOT EXISTS "AlternativePriceVnd" numeric(18,0) NULL,
                    ADD COLUMN IF NOT EXISTS "AlternativeDeliveryDate" date NULL;

                ALTER TABLE customer.order_items
                    ADD COLUMN IF NOT EXISTS "TechConstraintsJson" jsonb NULL,
                    ADD COLUMN IF NOT EXISTS "ModelBindingKind" character varying(16) NOT NULL DEFAULT 'không',
                    ADD COLUMN IF NOT EXISTS "PinnedModelId" uuid NULL,
                    ADD COLUMN IF NOT EXISTS "BindingLevel" character varying(16) NOT NULL DEFAULT 'bắt_buộc',
                    ADD COLUMN IF NOT EXISTS "ReferenceOrderLineId" uuid NULL;

                ALTER TABLE scheduling.machines
                    ADD COLUMN IF NOT EXISTS "AllowsCustomerPinning" boolean NOT NULL DEFAULT TRUE;

                CREATE TABLE IF NOT EXISTS scheduling.machine_assignment_decisions (
                    "Id" uuid NOT NULL,
                    "JobId" uuid NOT NULL,
                    "MachineId" uuid NOT NULL,
                    "Decision" character varying(16) NOT NULL,
                    "RejectionReason" character varying(500) NULL,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_machine_assignment_decisions" PRIMARY KEY ("Id")
                );

                CREATE INDEX IF NOT EXISTS "IX_machine_assignment_decisions_JobId_MachineId"
                    ON scheduling.machine_assignment_decisions ("JobId", "MachineId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Some of these columns may predate this repair. Removing them would
            // destroy existing quote/profile data, so rollback needs a new migration.
            throw new NotSupportedException("RepairQuoteReviewSchema cannot be rolled back without risking existing data.");
        }
    }
}
