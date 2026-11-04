DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'shared') THEN
        CREATE SCHEMA shared;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS shared.__ef_migrations_history (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___ef_migrations_history" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'customer') THEN
        CREATE SCHEMA customer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'scheduling') THEN
        CREATE SCHEMA scheduling;
    END IF;
END $EF$;

CREATE TABLE customer.customers (
    "Id" uuid NOT NULL,
    "Email" character varying(256) NOT NULL,
    "PasswordHash" character varying(256) NOT NULL,
    "FullName" character varying(200) NOT NULL,
    "PhoneNumber" character varying(32),
    "CreatedAt" timestamp with time zone NOT NULL,
    "LastLoginAt" timestamp with time zone,
    CONSTRAINT "PK_customers" PRIMARY KEY ("Id")
);

CREATE TABLE scheduling.jobs (
    "Id" uuid NOT NULL,
    "OrderItemId" uuid NOT NULL,
    "ModelId" uuid NOT NULL,
    required_width_mm numeric(8,2) NOT NULL,
    required_depth_mm numeric(8,2) NOT NULL,
    required_height_mm numeric(8,2) NOT NULL,
    material_code character varying(32) NOT NULL,
    color_code character varying(32) NOT NULL,
    layer_height_mm numeric(4,3) NOT NULL,
    tolerance_mm numeric(5,3) NOT NULL,
    technology character varying(16) NOT NULL,
    material_grams numeric(10,2) NOT NULL,
    "Status" character varying(32) NOT NULL,
    "InternalDueDate" date NOT NULL,
    "EstimatedPrintMinutes" integer NOT NULL,
    "ActualPrintMinutes" integer,
    "AttemptNumber" integer NOT NULL,
    "LabId" uuid,
    "MachineId" uuid,
    "PlannedStartUtc" timestamp with time zone,
    "PlannedEndUtc" timestamp with time zone,
    "StartedAtUtc" timestamp with time zone,
    "CompletedAtUtc" timestamp with time zone,
    "FailureReason" character varying(500),
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_jobs" PRIMARY KEY ("Id")
);

CREATE TABLE scheduling.labs (
    "Id" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "City" character varying(100) NOT NULL,
    "IsActive" boolean NOT NULL,
    "OnTimeDeliveryRate" numeric(5,4) NOT NULL,
    "FirstPassYield" numeric(5,4) NOT NULL,
    "TransitDaysToHub" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_labs" PRIMARY KEY ("Id")
);

CREATE TABLE customer.orders (
    "Id" uuid NOT NULL,
    "CustomerId" uuid NOT NULL,
    "QuoteId" uuid NOT NULL,
    "OrderNumber" character varying(32) NOT NULL,
    "Status" character varying(32) NOT NULL,
    total_amount numeric(18,2) NOT NULL,
    total_currency character varying(3) NOT NULL,
    "PromisedDeliveryDate" date NOT NULL,
    delivery_street character varying(200) NOT NULL,
    delivery_ward character varying(100) NOT NULL,
    delivery_district character varying(100) NOT NULL,
    delivery_city character varying(100) NOT NULL,
    delivery_postal_code character varying(20) NOT NULL,
    delivery_country character varying(2) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "ConfirmedAt" timestamp with time zone,
    "PaymentTransactionId" character varying(128),
    CONSTRAINT "PK_orders" PRIMARY KEY ("Id")
);

CREATE TABLE customer.quotes (
    "Id" uuid NOT NULL,
    "CustomerId" uuid NOT NULL,
    "Status" character varying(32) NOT NULL,
    total_amount numeric(18,2) NOT NULL,
    total_currency character varying(3) NOT NULL,
    "PromisedDeliveryDate" date NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "FailureReason" character varying(500),
    CONSTRAINT "PK_quotes" PRIMARY KEY ("Id")
);

CREATE TABLE scheduling.machines (
    "Id" uuid NOT NULL,
    "LabId" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Model" character varying(100) NOT NULL,
    "Technology" character varying(16) NOT NULL,
    build_width_mm numeric(8,2) NOT NULL,
    build_depth_mm numeric(8,2) NOT NULL,
    build_height_mm numeric(8,2) NOT NULL,
    "MinLayerHeightMm" numeric(4,3) NOT NULL,
    "AchievableToleranceMm" numeric(5,3) NOT NULL,
    "Status" character varying(16) NOT NULL,
    "SpeedFactor" numeric(5,3) NOT NULL,
    supported_materials text[] NOT NULL,
    CONSTRAINT "PK_machines" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_machines_labs_LabId" FOREIGN KEY ("LabId") REFERENCES scheduling.labs ("Id") ON DELETE CASCADE
);

CREATE TABLE customer.order_items (
    "Id" uuid NOT NULL,
    "OrderId" uuid NOT NULL,
    "ModelId" uuid NOT NULL,
    "Quantity" integer NOT NULL,
    material_code character varying(32) NOT NULL,
    color_code character varying(32) NOT NULL,
    layer_height_mm numeric(4,3) NOT NULL,
    infill_percent integer NOT NULL,
    tolerance_mm numeric(5,3) NOT NULL,
    unit_amount numeric(18,2) NOT NULL,
    unit_currency character varying(3) NOT NULL,
    CONSTRAINT "PK_order_items" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_order_items_orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES customer.orders ("Id") ON DELETE CASCADE
);

CREATE TABLE customer.quote_items (
    "Id" uuid NOT NULL,
    "QuoteId" uuid NOT NULL,
    "ModelId" uuid NOT NULL,
    "Quantity" integer NOT NULL,
    material_code character varying(32) NOT NULL,
    color_code character varying(32) NOT NULL,
    layer_height_mm numeric(4,3) NOT NULL,
    infill_percent integer NOT NULL,
    tolerance_mm numeric(5,3) NOT NULL,
    unit_amount numeric(18,2) NOT NULL,
    unit_currency character varying(3) NOT NULL,
    "EstimatedPrintMinutes" integer NOT NULL,
    "EstimatedMaterialGrams" numeric(10,2) NOT NULL,
    CONSTRAINT "PK_quote_items" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_quote_items_quotes_QuoteId" FOREIGN KEY ("QuoteId") REFERENCES customer.quotes ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_customers_Email" ON customer.customers ("Email");

CREATE INDEX "IX_jobs_InternalDueDate" ON scheduling.jobs ("InternalDueDate");

CREATE INDEX "IX_jobs_OrderItemId" ON scheduling.jobs ("OrderItemId");

CREATE INDEX "IX_jobs_Status_MachineId" ON scheduling.jobs ("Status", "MachineId");

CREATE INDEX "IX_labs_IsActive" ON scheduling.labs ("IsActive");

CREATE INDEX "IX_machines_LabId_Status" ON scheduling.machines ("LabId", "Status");

CREATE INDEX "IX_order_items_OrderId" ON customer.order_items ("OrderId");

CREATE INDEX "IX_orders_CustomerId_Status" ON customer.orders ("CustomerId", "Status");

CREATE UNIQUE INDEX "IX_orders_OrderNumber" ON customer.orders ("OrderNumber");

CREATE INDEX "IX_quote_items_QuoteId" ON customer.quote_items ("QuoteId");

CREATE INDEX "IX_quotes_CustomerId_Status" ON customer.quotes ("CustomerId", "Status");

CREATE INDEX "IX_quotes_ExpiresAt" ON customer.quotes ("ExpiresAt");

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20260902013652_InitialCreate', '8.0.30');

COMMIT;

START TRANSACTION;

CREATE TABLE customer.models (
    "Id" uuid NOT NULL,
    "CustomerId" uuid NOT NULL,
    "Name" character varying(120) NOT NULL,
    "Description" character varying(2000),
    "FileName" character varying(255) NOT NULL,
    "FileFormat" character varying(10) NOT NULL,
    "SizeBytes" bigint NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "Tags" text[] NOT NULL,
    CONSTRAINT "PK_models" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_models_CustomerId_CreatedAt" ON customer.models ("CustomerId", "CreatedAt");

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20260915045332_AddCustomerModels', '8.0.30');

COMMIT;

START TRANSACTION;

ALTER TABLE customer.models ADD "BoundingDepthMm" numeric(8,2);

ALTER TABLE customer.models ADD "BoundingHeightMm" numeric(8,2);

ALTER TABLE customer.models ADD "BoundingWidthMm" numeric(8,2);

ALTER TABLE customer.models ADD "EstimatedPrintMinutes" integer;

ALTER TABLE customer.models ADD "GeometryMessage" character varying(500);

ALTER TABLE customer.models ADD "GeometryStatus" character varying(16) NOT NULL DEFAULT '';

ALTER TABLE customer.models ADD "VolumeCm3" numeric(12,3);

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20260916064849_AddModelGeometryAnalysis', '8.0.30');

COMMIT;

START TRANSACTION;

ALTER TABLE customer.models ADD "StorageKey" character varying(512);

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20260919124939_AddModelStorageKey', '8.0.30');

COMMIT;

START TRANSACTION;

ALTER TABLE scheduling.jobs ADD "AssignedAtUtc" timestamp with time zone;

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20260927120954_AddJobAssignedAtUtc', '8.0.30');

COMMIT;

START TRANSACTION;

ALTER TABLE customer.customers ADD "IsActive" boolean NOT NULL DEFAULT TRUE;

ALTER TABLE customer.customers ADD "Roles" text[] NOT NULL DEFAULT ARRAY[]::text[];

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20260928070119_AddUserRoles', '8.0.30');

COMMIT;

START TRANSACTION;

ALTER TABLE customer.customers ADD "IsEmailVerified" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE customer.customers ADD "VerificationTokenExpiresAt" timestamp with time zone;

ALTER TABLE customer.customers ADD "VerificationTokenHash" text;

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20260929062801_AddEmailVerification', '8.0.30');

COMMIT;

START TRANSACTION;

ALTER TABLE customer.orders ADD "IsDelayed" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE customer.order_items ADD "ItemStatus" character varying(32) NOT NULL DEFAULT '';

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20260930100944_AddOrderTrackingFields', '8.0.30');

COMMIT;

START TRANSACTION;

ALTER TABLE customer.models ADD "EstimatedMaterialGrams" numeric(12,2);

ALTER TABLE customer.models ADD "IsManifold" boolean;

ALTER TABLE customer.models ADD "IsPrintable" boolean;

ALTER TABLE customer.models ADD "IsWatertight" boolean;

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20261001152858_AddModelPrintabilityAnalysis', '8.0.30');

COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'shared') THEN
        CREATE SCHEMA shared;
    END IF;
END $EF$;

ALTER TABLE customer.quotes ADD "PlacementBasis" character varying(300);

ALTER TABLE customer.quotes ADD "PricingVersion" character varying(16) NOT NULL DEFAULT '';

ALTER TABLE customer.quote_items ADD "BoundingDepthMm" numeric(8,2);

ALTER TABLE customer.quote_items ADD "BoundingHeightMm" numeric(8,2);

ALTER TABLE customer.quote_items ADD "BoundingWidthMm" numeric(8,2);

ALTER TABLE customer.quote_items ADD "MachineTimeCostAmount" numeric(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE customer.quote_items ADD "MaterialCostAmount" numeric(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE customer.order_items ADD "BoundingDepthMm" numeric(8,2);

ALTER TABLE customer.order_items ADD "BoundingHeightMm" numeric(8,2);

ALTER TABLE customer.order_items ADD "BoundingWidthMm" numeric(8,2);

ALTER TABLE customer.order_items ADD "EstimatedMaterialGrams" numeric(10,2) NOT NULL DEFAULT 0.0;

ALTER TABLE customer.order_items ADD "EstimatedPrintMinutes" integer NOT NULL DEFAULT 0;

ALTER TABLE customer.models ADD "Sha256" character varying(64);

CREATE TABLE shared.email_outbox (
    "Id" uuid NOT NULL,
    "ToEmail" character varying(254) NOT NULL,
    "Subject" character varying(500) NOT NULL,
    "Body" text NOT NULL,
    "Status" character varying(16) NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "SentAtUtc" timestamp with time zone,
    CONSTRAINT "PK_email_outbox" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_email_outbox_Status_CreatedAtUtc" ON shared.email_outbox ("Status", "CreatedAtUtc");

CREATE EXTENSION IF NOT EXISTS btree_gist;

ALTER TABLE scheduling.jobs
    ADD CONSTRAINT jobs_no_machine_overlap
    EXCLUDE USING gist (
        "MachineId" WITH =,
        tstzrange("PlannedStartUtc", "PlannedEndUtc") WITH &&
    )
    WHERE (
        "MachineId" IS NOT NULL
        AND "PlannedStartUtc" IS NOT NULL
        AND "PlannedEndUtc" IS NOT NULL
        AND "Status" IN ('Assigned','Accepted','InProgress')
    );

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20261002124948_GoldenPathR1', '8.0.30');

COMMIT;

START TRANSACTION;

ALTER TABLE scheduling.jobs ADD "ParentJobId" uuid;

ALTER TABLE scheduling.jobs ADD "Quantity" integer NOT NULL DEFAULT 1;

CREATE INDEX "IX_jobs_ParentJobId" ON scheduling.jobs ("ParentJobId");

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20261004051538_AddScheduleRiskRepair', '8.0.30');

COMMIT;

START TRANSACTION;

ALTER TABLE customer.quotes ALTER COLUMN "ExpiresAt" DROP NOT NULL;

ALTER TABLE customer.quotes ADD "AdjustmentReason" character varying(500);

ALTER TABLE customer.quotes ADD "ApprovedAt" timestamp with time zone;

ALTER TABLE customer.quotes ADD "AutoApproved" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE customer.quotes ADD "EnginePromisedDeliveryDate" date;

ALTER TABLE customer.quotes ADD "EngineTotalAmount" numeric(18,2);

ALTER TABLE customer.quotes ADD "ReviewedBy" uuid;

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20261004123414_QuoteReviewGate', '8.0.30');

COMMIT;

START TRANSACTION;

ALTER TABLE scheduling.labs ADD "IsInGoodStanding" boolean NOT NULL DEFAULT TRUE;

CREATE TABLE scheduling.material_stocks (
    "Id" uuid NOT NULL,
    "LabId" uuid NOT NULL,
    "MaterialCode" character varying(32) NOT NULL,
    "ColorCode" character varying(32) NOT NULL,
    "AvailableGrams" numeric(12,2) NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_material_stocks" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_material_stocks_labs_LabId" FOREIGN KEY ("LabId") REFERENCES scheduling.labs ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_material_stocks_LabId_MaterialCode_ColorCode" ON scheduling.material_stocks ("LabId", "MaterialCode", "ColorCode");

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20261005090000_AddFeasibilityStockAndStanding', '8.0.30');

COMMIT;

START TRANSACTION;

ALTER TABLE scheduling.jobs ADD "QcProofPhotoKeys" character varying(4000);

ALTER TABLE scheduling.jobs ADD "QcProofStatus" character varying(16) NOT NULL DEFAULT 'None';

ALTER TABLE scheduling.jobs ADD "QcRejectionReason" character varying(1000);

ALTER TABLE scheduling.jobs ADD "QcReviewedAtUtc" timestamp with time zone;

ALTER TABLE scheduling.jobs ADD "QcReviewedBy" uuid;

ALTER TABLE scheduling.jobs ADD "QcSelfReport" character varying(2000);

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20261005112701_AddLabQcProofApproval', '8.0.30');

COMMIT;

START TRANSACTION;

ALTER TABLE scheduling.material_stocks ADD "ReorderPointGrams" numeric(12,2) NOT NULL DEFAULT 0.0;

ALTER TABLE scheduling.material_stocks ADD "ReservedGrams" numeric(12,2) NOT NULL DEFAULT 0.0;

ALTER TABLE scheduling.jobs ADD "ActualMaterialGrams" numeric(10,2);

CREATE TABLE scheduling.material_reservations (
    "Id" uuid NOT NULL,
    "LabId" uuid NOT NULL,
    "MaterialStockId" uuid NOT NULL,
    "JobId" uuid NOT NULL,
    "ReservedGrams" numeric(12,2) NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_material_reservations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_material_reservations_labs_LabId" FOREIGN KEY ("LabId") REFERENCES scheduling.labs ("Id") ON DELETE CASCADE
);

CREATE TABLE scheduling.stock_transactions (
    "Id" uuid NOT NULL,
    "LabId" uuid NOT NULL,
    "MaterialStockId" uuid NOT NULL,
    "TransactionCode" character varying(32) NOT NULL,
    "DeltaGrams" numeric(12,2) NOT NULL,
    "RunningTotalGrams" numeric(12,2) NOT NULL,
    "Reason" character varying(250) NOT NULL,
    "JobId" uuid,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_stock_transactions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_stock_transactions_labs_LabId" FOREIGN KEY ("LabId") REFERENCES scheduling.labs ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_material_reservations_JobId" ON scheduling.material_reservations ("JobId");

CREATE INDEX "IX_material_reservations_LabId" ON scheduling.material_reservations ("LabId");

CREATE INDEX "IX_stock_transactions_LabId_CreatedAtUtc" ON scheduling.stock_transactions ("LabId", "CreatedAtUtc");

CREATE UNIQUE INDEX "IX_stock_transactions_TransactionCode" ON scheduling.stock_transactions ("TransactionCode");

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20261005124358_CompleteLabInventoryAndShopFloor', '8.0.30');

COMMIT;

START TRANSACTION;

ALTER TABLE customer.orders ADD "DeliveredAt" timestamp with time zone;

CREATE TABLE customer.reprint_requests (
    "Id" uuid NOT NULL,
    "OrderId" uuid NOT NULL,
    "CustomerId" uuid NOT NULL,
    "Reason" character varying(64) NOT NULL,
    "Description" character varying(2000) NOT NULL,
    "Photos" text[] NOT NULL,
    "Status" character varying(32) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "ResolutionNote" character varying(2000),
    CONSTRAINT "PK_reprint_requests" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_reprint_requests_customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES customer.customers ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_reprint_requests_orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES customer.orders ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_reprint_requests_CustomerId_Status" ON customer.reprint_requests ("CustomerId", "Status");

CREATE INDEX "IX_reprint_requests_OrderId_CreatedAt" ON customer.reprint_requests ("OrderId", "CreatedAt");

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20261005130202_AddCustomerReprintRequests', '8.0.30');

COMMIT;

START TRANSACTION;

ALTER TABLE scheduling.jobs ADD "CostBearer" character varying(16) NOT NULL DEFAULT 'Customer';

ALTER TABLE scheduling.jobs ADD "FaultLabId" uuid;

ALTER TABLE scheduling.jobs ADD "OriginalJobId" uuid;

ALTER TABLE scheduling.jobs ADD "Priority" character varying(16) NOT NULL DEFAULT 'Normal';

ALTER TABLE scheduling.jobs ADD "ReprintIndex" integer NOT NULL DEFAULT 0;

CREATE TABLE scheduling.assignment_decisions (
    "Id" uuid NOT NULL,
    "JobId" uuid NOT NULL,
    "OrderItemId" uuid NOT NULL,
    "AttemptNumber" integer NOT NULL,
    "Trigger" character varying(64) NOT NULL,
    "ActorType" character varying(16) NOT NULL,
    "ActorId" character varying(128),
    "Outcome" character varying(32) NOT NULL,
    "Reason" character varying(500),
    "CandidatesJson" jsonb NOT NULL,
    "RankingJson" jsonb NOT NULL,
    "ChosenLabId" uuid,
    "ChosenMachineId" uuid,
    "ChosenScore" numeric(9,4),
    "ScoringConfigVersion" character varying(16) NOT NULL,
    "ScoringConfigJson" jsonb NOT NULL,
    "TimeBudgetMs" integer NOT NULL,
    "ElapsedMs" integer NOT NULL,
    "BudgetExceeded" boolean NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_assignment_decisions" PRIMARY KEY ("Id")
);

CREATE TABLE scheduling.date_change_requests (
    "Id" uuid NOT NULL,
    "JobId" uuid NOT NULL,
    "OrderItemId" uuid NOT NULL,
    "OriginalDeliveryDate" date NOT NULL,
    "ProposedDeliveryDate" date NOT NULL,
    "Reason" character varying(500) NOT NULL,
    "Token" character varying(64) NOT NULL,
    "Status" character varying(16) NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "DecidedAtUtc" timestamp with time zone,
    CONSTRAINT "PK_date_change_requests" PRIMARY KEY ("Id")
);

CREATE TABLE scheduling.ops_escalations (
    "Id" uuid NOT NULL,
    "JobId" uuid NOT NULL,
    "OrderItemId" uuid NOT NULL,
    "Kind" character varying(32) NOT NULL,
    "Reason" character varying(500) NOT NULL,
    "Status" character varying(16) NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "ResolvedAtUtc" timestamp with time zone,
    CONSTRAINT "PK_ops_escalations" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_jobs_OriginalJobId" ON scheduling.jobs ("OriginalJobId");

CREATE INDEX "IX_assignment_decisions_JobId_CreatedAtUtc" ON scheduling.assignment_decisions ("JobId", "CreatedAtUtc");

CREATE INDEX "IX_assignment_decisions_OrderItemId" ON scheduling.assignment_decisions ("OrderItemId");

CREATE INDEX "IX_date_change_requests_JobId_CreatedAtUtc" ON scheduling.date_change_requests ("JobId", "CreatedAtUtc");

CREATE UNIQUE INDEX "IX_date_change_requests_Token" ON scheduling.date_change_requests ("Token");

CREATE INDEX "IX_ops_escalations_JobId" ON scheduling.ops_escalations ("JobId");

CREATE INDEX "IX_ops_escalations_Status_CreatedAtUtc" ON scheduling.ops_escalations ("Status", "CreatedAtUtc");

CREATE OR REPLACE FUNCTION scheduling.assignment_decisions_append_only()
RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION 'scheduling.assignment_decisions is append-only (FR-SCHED-009)';
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER assignment_decisions_no_update
    BEFORE UPDATE OR DELETE ON scheduling.assignment_decisions
    FOR EACH ROW EXECUTE FUNCTION scheduling.assignment_decisions_append_only();

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20261006164643_RescheduleAndDecisionLog', '8.0.30');

COMMIT;

START TRANSACTION;

ALTER TABLE customer.orders ADD "ReceiptConfirmedBy" uuid;

ALTER TABLE customer.orders ADD "TrackingNumber" character varying(128);

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20261007025105_AddOrderShipmentReceiptFields', '8.0.30');

COMMIT;

START TRANSACTION;

CREATE TABLE customer.customer_addresses (
    "Id" uuid NOT NULL,
    "CustomerId" uuid NOT NULL,
    "Label" character varying(50) NOT NULL,
    "RecipientName" character varying(200) NOT NULL,
    "PhoneNumber" character varying(32) NOT NULL,
    "Street" character varying(300) NOT NULL,
    "Ward" character varying(100) NOT NULL,
    "District" character varying(100) NOT NULL,
    "City" character varying(100) NOT NULL,
    "PostalCode" character varying(20) NOT NULL,
    "Country" character varying(2) NOT NULL,
    "IsDefault" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_customer_addresses" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_customer_addresses_customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES customer.customers ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_customer_addresses_CustomerId_IsDefault" ON customer.customer_addresses ("CustomerId", "IsDefault");

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20261007034235_AddCustomerAddresses', '8.0.30');

COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'admin') THEN
        CREATE SCHEMA admin;
    END IF;
END $EF$;

CREATE TABLE admin.audit_logs (
    "Id" uuid NOT NULL,
    "ActorId" uuid,
    "ActorDisplay" character varying(254) NOT NULL,
    "ActorRole" character varying(64) NOT NULL,
    "ActionClass" character varying(64) NOT NULL,
    "Action" character varying(128) NOT NULL,
    "EntityType" character varying(128) NOT NULL,
    "EntityId" character varying(128),
    "OldValuesJson" jsonb,
    "NewValuesJson" jsonb,
    "Reason" character varying(1000),
    "IpAddress" character varying(64),
    "TraceId" character varying(128),
    "RequirementCodesJson" jsonb NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_audit_logs" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_audit_logs_ActionClass_CreatedAtUtc" ON admin.audit_logs ("ActionClass", "CreatedAtUtc");

CREATE INDEX "IX_audit_logs_ActorId_CreatedAtUtc" ON admin.audit_logs ("ActorId", "CreatedAtUtc");

CREATE INDEX "IX_audit_logs_CreatedAtUtc" ON admin.audit_logs ("CreatedAtUtc");

CREATE OR REPLACE FUNCTION admin.audit_logs_append_only()
RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION 'admin.audit_logs is append-only (BR-CONFIG-004)';
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER audit_logs_no_update
    BEFORE UPDATE OR DELETE ON admin.audit_logs
    FOR EACH ROW EXECUTE FUNCTION admin.audit_logs_append_only();

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20261012050631_AddSystemAuditLog', '8.0.30');

COMMIT;

START TRANSACTION;

ALTER TABLE scheduling.labs ADD "Status" character varying(24) NOT NULL DEFAULT 'Active';

ALTER TABLE scheduling.labs ADD "OperatingHours" character varying(120) NOT NULL DEFAULT 'T2-T7 08:00-18:00';

ALTER TABLE scheduling.labs ADD "OwnerUserId" uuid;

ALTER TABLE scheduling.labs ADD "ContactPhone" character varying(32);

ALTER TABLE scheduling.labs ADD "Address" character varying(300);

ALTER TABLE scheduling.labs ADD "RejectionReason" character varying(500);

ALTER TABLE scheduling.labs ADD "ApprovedAtUtc" timestamp with time zone;

ALTER TABLE scheduling.labs ADD "ApprovedByUserId" uuid;

CREATE INDEX "IX_labs_Status" ON scheduling.labs ("Status");

CREATE INDEX "IX_labs_OwnerUserId" ON scheduling.labs ("OwnerUserId");

ALTER TABLE scheduling.machines ADD "MaintenanceFromUtc" timestamp with time zone;

ALTER TABLE scheduling.machines ADD "MaintenanceUntilUtc" timestamp with time zone;

ALTER TABLE scheduling.machines ADD "MaintenanceReason" character varying(300);

ALTER TABLE customer.orders ADD "ReadyForPackingAtUtc" timestamp with time zone;

ALTER TABLE customer.orders ADD "ConsolidatedShipmentId" uuid;

ALTER TABLE customer.orders ADD "RefundedAmount" numeric(18,2) NOT NULL DEFAULT 0.0;

CREATE INDEX "IX_orders_ConsolidatedShipmentId" ON customer.orders ("ConsolidatedShipmentId");

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20261012070000_AddWeek4LabAndHubConsolidation', '8.0.30');

COMMIT;

START TRANSACTION;

ALTER TABLE scheduling.labs ADD "TransferDaysOfWeek" character varying(128) NOT NULL DEFAULT 'Monday,Wednesday,Friday';

ALTER TABLE scheduling.labs ADD "TransportZone" character varying(32) NOT NULL DEFAULT 'LOCAL';

ALTER TABLE scheduling.jobs ADD "ShipmentBatchId" uuid;

CREATE TABLE scheduling.shipment_batches (
    "Id" uuid NOT NULL,
    "LabId" uuid NOT NULL,
    "ScheduledTransferDate" date NOT NULL,
    "TransportZone" character varying(32) NOT NULL,
    "WeightClass" character varying(16) NOT NULL,
    "TotalWeightGrams" numeric(12,2) NOT NULL,
    "ZoneRate" numeric(14,2) NOT NULL,
    "WeightMultiplier" numeric(8,3) NOT NULL,
    "TransportAllowance" numeric(14,2) NOT NULL,
    "JobCount" integer NOT NULL,
    "Currency" character varying(3) NOT NULL,
    "WaybillNumber" character varying(64) NOT NULL,
    "Status" character varying(16) NOT NULL,
    "SealedAtUtc" timestamp with time zone NOT NULL,
    "SealedBy" uuid NOT NULL,
    "ReceivedAtUtc" timestamp with time zone,
    CONSTRAINT "PK_shipment_batches" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_jobs_ShipmentBatchId" ON scheduling.jobs ("ShipmentBatchId");

CREATE INDEX "IX_shipment_batches_LabId_ScheduledTransferDate" ON scheduling.shipment_batches ("LabId", "ScheduledTransferDate");

CREATE UNIQUE INDEX "IX_shipment_batches_WaybillNumber" ON scheduling.shipment_batches ("WaybillNumber");

ALTER TABLE scheduling.jobs ADD CONSTRAINT "FK_jobs_shipment_batches_ShipmentBatchId" FOREIGN KEY ("ShipmentBatchId") REFERENCES scheduling.shipment_batches ("Id") ON DELETE RESTRICT;

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20261012073547_AddLabShipmentBatches', '8.0.30');

COMMIT;

START TRANSACTION;

CREATE TABLE scheduling.scoring_weight_configurations (
    "Id" uuid NOT NULL,
    "Version" character varying(64) NOT NULL,
    "DueDateSlack" numeric(8,6) NOT NULL,
    "LabReliability" numeric(8,6) NOT NULL,
    "LoadBalance" numeric(8,6) NOT NULL,
    "Cost" numeric(8,6) NOT NULL,
    "Transit" numeric(8,6) NOT NULL,
    "IsActive" boolean NOT NULL,
    "Reason" character varying(500) NOT NULL,
    "ChangedBy" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_scoring_weight_configurations" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_scoring_weight_configurations_IsActive" ON scheduling.scoring_weight_configurations ("IsActive") WHERE "IsActive" = TRUE;

CREATE UNIQUE INDEX "IX_scoring_weight_configurations_Version" ON scheduling.scoring_weight_configurations ("Version");

INSERT INTO scheduling.scoring_weight_configurations ("Id", "Version", "DueDateSlack", "LabReliability", "LoadBalance", "Cost", "Transit", "IsActive", "Reason", "ChangedBy", "CreatedAtUtc")
VALUES ('aa1ddfba-2c27-4f47-91c2-66d255841026', '2026.10', 0.35, 0.25, 0.2, 0.1, 0.1, TRUE, 'Initial scoring configuration migrated from the assignment engine', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-10-12T00:00:00Z');

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20261012090415_AddVersionedScoringWeights', '8.0.30');

COMMIT;

START TRANSACTION;

ALTER TABLE scheduling.date_change_requests ADD "DecidedByCustomerId" uuid;

ALTER TABLE scheduling.date_change_requests ADD "ReasonCode" character varying(48) NOT NULL DEFAULT 'ReprintDeadlineUnrecoverable';

CREATE TABLE customer.refund_requests (
    "Id" uuid NOT NULL,
    "OrderId" uuid NOT NULL,
    "OrderItemId" uuid NOT NULL,
    "DateChangeRequestId" uuid NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "Currency" character varying(3) NOT NULL,
    "PaymentTransactionId" character varying(128) NOT NULL,
    "Reason" character varying(500) NOT NULL,
    "Status" character varying(24) NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_refund_requests" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_refund_requests_DateChangeRequestId" ON customer.refund_requests ("DateChangeRequestId");

CREATE INDEX "IX_refund_requests_Status_CreatedAtUtc" ON customer.refund_requests ("Status", "CreatedAtUtc");

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20261012130521_AddB16DateChangeConsentAndRefundQueue', '8.0.30');

COMMIT;

START TRANSACTION;

UPDATE customer.customers
SET "Roles" = ARRAY(
    SELECT DISTINCT
        CASE WHEN role_name = 'OrderStaff' THEN 'OpsManager' ELSE role_name END
    FROM unnest("Roles") AS role_name
)
WHERE 'OrderStaff' = ANY("Roles");

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20261013043138_MergeOrderStaffIntoOpsManager', '8.0.30');

COMMIT;

START TRANSACTION;

CREATE TABLE admin.commercial_configuration_versions (
    "Id" uuid NOT NULL,
    "VersionNumber" integer NOT NULL,
    "Name" character varying(120) NOT NULL,
    "ParametersJson" jsonb NOT NULL,
    "ChangeSummary" character varying(1000) NOT NULL,
    "Status" character varying(16) NOT NULL,
    "CreatedBy" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "ActivatedAtUtc" timestamp with time zone,
    CONSTRAINT "PK_commercial_configuration_versions" PRIMARY KEY ("Id")
);

CREATE TABLE admin.shared_catalog_items (
    "Id" uuid NOT NULL,
    "CatalogType" character varying(32) NOT NULL,
    "Code" character varying(32) NOT NULL,
    "Revision" integer NOT NULL,
    "Name" character varying(120) NOT NULL,
    "Description" character varying(1000),
    "UnitRate" numeric(18,2),
    "IsPremium" boolean NOT NULL,
    "PropertiesJson" jsonb NOT NULL,
    "IsDeprecated" boolean NOT NULL,
    "IsCurrent" boolean NOT NULL,
    "CreatedBy" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_shared_catalog_items" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_commercial_configuration_versions_Status" ON admin.commercial_configuration_versions ("Status") WHERE "Status" = 'Active';

CREATE UNIQUE INDEX "IX_commercial_configuration_versions_VersionNumber" ON admin.commercial_configuration_versions ("VersionNumber");

CREATE UNIQUE INDEX "IX_shared_catalog_items_CatalogType_Code_IsCurrent" ON admin.shared_catalog_items ("CatalogType", "Code", "IsCurrent") WHERE "IsCurrent" = TRUE;

CREATE UNIQUE INDEX "IX_shared_catalog_items_CatalogType_Code_Revision" ON admin.shared_catalog_items ("CatalogType", "Code", "Revision");

CREATE INDEX "IX_shared_catalog_items_CatalogType_IsCurrent_IsDeprecated" ON admin.shared_catalog_items ("CatalogType", "IsCurrent", "IsDeprecated");

INSERT INTO admin.commercial_configuration_versions
    ("Id", "VersionNumber", "Name", "ParametersJson", "ChangeSummary", "Status", "CreatedBy", "CreatedAtUtc", "ActivatedAtUtc")
VALUES
    ('10000000-0000-0000-0000-000000000001', 1, 'Cấu hình thương mại ban đầu',
     '{"materialRates":{"PLA":470,"PETG":575,"ABS":520,"TPU":680,"RESIN":1550},"materialDensities":{"PLA":1.24,"PETG":1.27,"ABS":1.04,"TPU":1.21,"RESIN":1.10},"pricePerPrintHour":94500,"hubHandlingFee":25000,"platformMarginPercent":12,"prioritySurchargePercent":20,"autoLaneThreshold":300000,"quoteValidityHours":48,"assignmentWeights":{"time":0.30,"load":0.20,"quality":0.20,"cost":0.15,"logistics":0.15},"maxBatchSize":100,"schedulingTimeBudgetSeconds":30,"slaRiskThresholdHours":24}'::jsonb,
     'Khởi tạo từ bộ tham số đang vận hành trước khi có màn quản trị phiên bản.', 'Active',
     '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-10-13 00:00:00+00', TIMESTAMPTZ '2026-10-13 00:00:00+00');

INSERT INTO admin.shared_catalog_items
    ("Id", "CatalogType", "Code", "Revision", "Name", "Description", "UnitRate", "IsPremium", "PropertiesJson", "IsDeprecated", "IsCurrent", "CreatedBy", "CreatedAtUtc")
VALUES
    ('20000000-0000-0000-0000-000000000001','Material','PLA',1,'PLA','Dễ in, phù hợp mẫu phổ thông',470,FALSE,'{"density":1.24}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
    ('20000000-0000-0000-0000-000000000002','Material','PETG',1,'PETG','Bền và chịu ẩm tốt',575,FALSE,'{"density":1.27}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
    ('20000000-0000-0000-0000-000000000003','Material','ABS',1,'ABS','Chịu nhiệt, phù hợp chi tiết kỹ thuật',520,FALSE,'{"density":1.04}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
    ('20000000-0000-0000-0000-000000000004','Material','TPU',1,'TPU','Vật liệu dẻo',680,TRUE,'{"density":1.21}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
    ('20000000-0000-0000-0000-000000000005','Material','RESIN',1,'Resin','Chi tiết cao cho công nghệ SLA',1550,TRUE,'{"density":1.10}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
    ('20000000-0000-0000-0000-000000000011','Color','BLACK',1,'Đen',NULL,NULL,FALSE,'{"hex":"#111111"}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
    ('20000000-0000-0000-0000-000000000012','Color','WHITE',1,'Trắng',NULL,NULL,FALSE,'{"hex":"#F5F5F5"}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
    ('20000000-0000-0000-0000-000000000013','Color','GRAY',1,'Xám',NULL,NULL,FALSE,'{"hex":"#808080"}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
    ('20000000-0000-0000-0000-000000000014','Color','RED',1,'Đỏ',NULL,NULL,TRUE,'{"hex":"#D32F2F"}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
    ('20000000-0000-0000-0000-000000000021','Technology','FFF',1,'FFF/FDM','Đùn sợi nhựa nóng chảy',NULL,FALSE,'{}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
    ('20000000-0000-0000-0000-000000000022','Technology','SLA',1,'SLA','Quang trùng hợp nhựa resin',NULL,TRUE,'{}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
    ('20000000-0000-0000-0000-000000000023','Technology','SLS',1,'SLS','Thiêu kết bột bằng laser',NULL,TRUE,'{}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
    ('20000000-0000-0000-0000-000000000031','QualityGrade','STANDARD',1,'Tiêu chuẩn','Chất lượng mặc định',NULL,FALSE,'{}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
    ('20000000-0000-0000-0000-000000000032','QualityGrade','FINE',1,'Chi tiết cao','Bề mặt và dung sai tốt hơn',NULL,TRUE,'{}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
    ('20000000-0000-0000-0000-000000000041','PostProcessing','SANDING',1,'Chà nhám',NULL,NULL,FALSE,'{}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
    ('20000000-0000-0000-0000-000000000042','PostProcessing','PAINTING',1,'Sơn hoàn thiện',NULL,NULL,TRUE,'{}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00');

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20261013044249_AddCommercialConfigurationAndSharedCatalogs', '8.0.30');

COMMIT;

START TRANSACTION;

ALTER TABLE scheduling.jobs ADD "InspectedAtUtc" timestamp with time zone;

ALTER TABLE scheduling.jobs ADD "InspectionChecklistResultsJson" jsonb;

ALTER TABLE scheduling.jobs ADD "InspectionChecklistVersionId" uuid;

ALTER TABLE scheduling.jobs ADD "InspectionFaultAttribution" character varying(16);

ALTER TABLE scheduling.jobs ADD "InspectionNote" character varying(500);

ALTER TABLE scheduling.jobs ADD inspection_defect_code character varying(32);

CREATE TABLE admin.defect_taxonomy_items (
    "Id" uuid NOT NULL,
    "Code" character varying(32) NOT NULL,
    "Name" character varying(160) NOT NULL,
    "Description" character varying(1000),
    "DefaultFaultAttribution" character varying(16),
    "IsDeprecated" boolean NOT NULL,
    "CreatedBy" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedBy" uuid,
    "UpdatedAtUtc" timestamp with time zone,
    CONSTRAINT "PK_defect_taxonomy_items" PRIMARY KEY ("Id"),
    CONSTRAINT "AK_defect_taxonomy_items_Code" UNIQUE ("Code")
);

CREATE TABLE admin.inspection_checklist_versions (
    "Id" uuid NOT NULL,
    "QualityGradeCode" character varying(32) NOT NULL,
    "VersionNumber" integer NOT NULL,
    "ItemsJson" jsonb NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedBy" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_inspection_checklist_versions" PRIMARY KEY ("Id")
);

INSERT INTO admin.inspection_checklist_versions
    ("Id", "QualityGradeCode", "VersionNumber", "ItemsJson", "IsActive", "CreatedBy", "CreatedAtUtc")
VALUES
    ('30000000-0000-0000-0000-000000000001', 'STANDARD', 1,
     '[{"code":"DIMENSION","name":"Kích thước hình học và dung sai","passCriteria":"Sai lệch không vượt quá ±0,5 mm","isMandatory":true,"displayOrder":1},{"code":"SURFACE","name":"Bề mặt và hoàn thiện","passCriteria":"Không nứt, rỗ hoặc ba via ảnh hưởng sử dụng","isMandatory":true,"displayOrder":2},{"code":"MATERIAL_COLOR","name":"Đúng vật liệu và màu sắc","passCriteria":"Khớp cấu hình trên lệnh sản xuất","isMandatory":true,"displayOrder":3},{"code":"LAYER_ADHESION","name":"Độ bám dính lớp in","passCriteria":"Không tách lớp khi kiểm tra trực quan","isMandatory":true,"displayOrder":4}]'::jsonb,
     TRUE, '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-10-13 00:00:00+00'),
    ('30000000-0000-0000-0000-000000000002', 'FINE', 1,
     '[{"code":"DIMENSION","name":"Kích thước hình học và dung sai","passCriteria":"Sai lệch không vượt quá ±0,2 mm","isMandatory":true,"displayOrder":1},{"code":"SURFACE","name":"Bề mặt chi tiết cao","passCriteria":"Bề mặt đồng đều, không có khuyết tật nhìn thấy ở khoảng cách 30 cm","isMandatory":true,"displayOrder":2},{"code":"MATERIAL_COLOR","name":"Đúng vật liệu và màu sắc","passCriteria":"Khớp cấu hình trên lệnh sản xuất","isMandatory":true,"displayOrder":3},{"code":"LAYER_ADHESION","name":"Độ bám dính lớp in","passCriteria":"Không tách lớp hoặc đường lớp bất thường","isMandatory":true,"displayOrder":4},{"code":"FINE_DETAIL","name":"Chi tiết nhỏ","passCriteria":"Các chi tiết thiết kế từ 0,5 mm trở lên thể hiện rõ","isMandatory":true,"displayOrder":5}]'::jsonb,
     TRUE, '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-10-13 00:00:00+00');

INSERT INTO admin.defect_taxonomy_items
    ("Id", "Code", "Name", "Description", "DefaultFaultAttribution", "IsDeprecated", "CreatedBy", "CreatedAtUtc", "UpdatedBy", "UpdatedAtUtc")
VALUES
    ('31000000-0000-0000-0000-000000000001','DIMENSIONAL','Sai kích thước','Kích thước hoặc dung sai vượt tiêu chuẩn của cấp chất lượng.','Lab',FALSE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00',NULL,NULL),
    ('31000000-0000-0000-0000-000000000002','SURFACE_DEFECT','Lỗi bề mặt','Rỗ, nứt, xước hoặc bề mặt hoàn thiện không đạt.','Lab',FALSE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00',NULL,NULL),
    ('31000000-0000-0000-0000-000000000003','WRONG_MATERIAL_COLOR','Sai vật liệu hoặc màu','Sản phẩm không khớp vật liệu hoặc màu đã đặt.','Lab',FALSE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00',NULL,NULL),
    ('31000000-0000-0000-0000-000000000004','LAYER_FAILURE','Tách lớp','Các lớp in không liên kết hoặc xuất hiện layer shift.','Lab',FALSE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00',NULL,NULL),
    ('31000000-0000-0000-0000-000000000005','WARPING','Cong vênh','Đế hoặc thân sản phẩm bị cong vênh ngoài dung sai.','Lab',FALSE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00',NULL,NULL),
    ('31000000-0000-0000-0000-000000000006','TRANSIT_DAMAGE','Hư hỏng tại Hub','Sản phẩm bị va đập, trầy xước hoặc hư hỏng trong xử lý tại Hub.','Hub',FALSE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00',NULL,NULL),
    ('31000000-0000-0000-0000-000000000007','SOURCE_MODEL','Lỗi mô hình nguồn','Hình học hoặc thiết kế file khách hàng khiến sản phẩm không thể đạt.','Customer',FALSE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00',NULL,NULL);

CREATE INDEX "IX_jobs_inspection_defect_code" ON scheduling.jobs (inspection_defect_code);

CREATE INDEX "IX_jobs_InspectionChecklistVersionId" ON scheduling.jobs ("InspectionChecklistVersionId");

CREATE INDEX "IX_defect_taxonomy_items_IsDeprecated_Name" ON admin.defect_taxonomy_items ("IsDeprecated", "Name");

CREATE UNIQUE INDEX "IX_inspection_checklist_versions_QualityGradeCode_IsActive" ON admin.inspection_checklist_versions ("QualityGradeCode", "IsActive") WHERE "IsActive" = TRUE;

CREATE UNIQUE INDEX "IX_inspection_checklist_versions_QualityGradeCode_VersionNumber" ON admin.inspection_checklist_versions ("QualityGradeCode", "VersionNumber");

ALTER TABLE scheduling.jobs ADD CONSTRAINT "FK_jobs_defect_taxonomy_items_inspection_defect_code" FOREIGN KEY (inspection_defect_code) REFERENCES admin.defect_taxonomy_items ("Code") ON DELETE RESTRICT;

ALTER TABLE scheduling.jobs ADD CONSTRAINT "FK_jobs_inspection_checklist_versions_InspectionChecklistVersi~" FOREIGN KEY ("InspectionChecklistVersionId") REFERENCES admin.inspection_checklist_versions ("Id") ON DELETE RESTRICT;

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20261013053810_AddQcKnowledgeBase', '8.0.30');

COMMIT;

START TRANSACTION;

ALTER TABLE scheduling.labs ADD "MaxConcurrentJobs" integer;

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20261013063314_AddLabApprovalAndCapacity', '8.0.30');

COMMIT;

START TRANSACTION;

ALTER TABLE scheduling.labs ADD "ClosingTime" character varying(5) NOT NULL DEFAULT '17:00';

ALTER TABLE scheduling.labs ADD "OpeningTime" character varying(5) NOT NULL DEFAULT '08:00';

ALTER TABLE scheduling.labs ADD "WorkingDays" character varying(100) NOT NULL DEFAULT 'Monday,Tuesday,Wednesday,Thursday,Friday';

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20261013064342_AddLabWorkingCalendar', '8.0.30');

COMMIT;

START TRANSACTION;

ALTER TABLE scheduling.machines ADD "CalibrationComputedAtUtc" timestamp with time zone;

ALTER TABLE scheduling.machines ADD "CalibrationFactor" numeric(5,3) NOT NULL DEFAULT 1.0;

ALTER TABLE scheduling.machines ADD "CalibrationMape" numeric(6,4) NOT NULL DEFAULT 0.0;

ALTER TABLE scheduling.machines ADD "CalibrationSampleCount" integer NOT NULL DEFAULT 0;

CREATE TABLE scheduling.calibration_samples (
    "Id" uuid NOT NULL,
    "JobId" uuid NOT NULL,
    "MachineId" uuid NOT NULL,
    "ModelId" uuid NOT NULL,
    "MaterialCode" character varying(32) NOT NULL,
    "EstimatedMinutes" numeric(10,2) NOT NULL,
    "ActualMinutes" numeric(10,2) NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_calibration_samples" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_calibration_samples_machines_MachineId" FOREIGN KEY ("MachineId") REFERENCES scheduling.machines ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_calibration_samples_JobId" ON scheduling.calibration_samples ("JobId");

CREATE INDEX "IX_calibration_samples_MachineId_CreatedAtUtc" ON scheduling.calibration_samples ("MachineId", "CreatedAtUtc");

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20261019143438_AddEstimationCalibration', '8.0.30');

COMMIT;

START TRANSACTION;

ALTER TABLE customer.orders ADD "DepositAmount" numeric(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE customer.orders ADD "PaidAmount" numeric(18,2) NOT NULL DEFAULT 0.0;

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20261019152855_AddDepositSettlement', '8.0.30');

COMMIT;

START TRANSACTION;

CREATE TABLE customer.design_requests (
    "Id" uuid NOT NULL,
    "CustomerId" uuid NOT NULL,
    "Title" character varying(200) NOT NULL,
    "Description" character varying(4000) NOT NULL,
    "Status" character varying(24) NOT NULL,
    "QuoteId" uuid,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    reference_image_keys text[] NOT NULL,
    CONSTRAINT "PK_design_requests" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_design_requests_customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES customer.customers ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_design_requests_CustomerId_Status" ON customer.design_requests ("CustomerId", "Status");

CREATE INDEX "IX_design_requests_Status_CreatedAtUtc" ON customer.design_requests ("Status", "CreatedAtUtc");

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20261019152934_AddDesignRequests', '8.0.30');

COMMIT;

START TRANSACTION;

ALTER TABLE customer.customers ADD "GoogleSubject" character varying(255);

ALTER TABLE customer.customers ADD "PasswordResetTokenExpiresAt" timestamp with time zone;

ALTER TABLE customer.customers ADD "PasswordResetTokenHash" character varying(256);

ALTER TABLE customer.customers ADD "SessionVersion" integer NOT NULL DEFAULT 0;

CREATE UNIQUE INDEX "IX_customers_GoogleSubject" ON customer.customers ("GoogleSubject");

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20261021081311_AddGoogleAndPasswordResetAuth', '8.0.30');

COMMIT;

START TRANSACTION;

CREATE TABLE admin.sample_models (
    "Id" uuid NOT NULL,
    "Slug" character varying(140) NOT NULL,
    "Name" character varying(160) NOT NULL,
    "Description" character varying(2000),
    "Category" character varying(64) NOT NULL,
    "FileName" character varying(260) NOT NULL,
    "FileFormat" character varying(16) NOT NULL,
    "SizeBytes" bigint NOT NULL,
    "StorageKey" character varying(500) NOT NULL,
    "Sha256" character varying(64),
    "CoverImageUrl" character varying(1000),
    "BoundingWidthMm" numeric(18,2),
    "BoundingDepthMm" numeric(18,2),
    "BoundingHeightMm" numeric(18,2),
    "VolumeCm3" numeric(18,3),
    "EstimatedPrintMinutes" integer,
    "EstimatedMaterialGrams" numeric(18,2),
    "IsPublished" boolean NOT NULL,
    "SortOrder" integer NOT NULL,
    "Tags" text[] NOT NULL,
    "CreatedBy" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_sample_models" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_sample_models_IsPublished_SortOrder" ON admin.sample_models ("IsPublished", "SortOrder");

CREATE UNIQUE INDEX "IX_sample_models_Slug" ON admin.sample_models ("Slug");

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20261028142428_AddSampleLibrary', '8.0.30');

COMMIT;

