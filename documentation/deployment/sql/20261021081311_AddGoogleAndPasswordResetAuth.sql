START TRANSACTION;

ALTER TABLE customer.customers ADD "GoogleSubject" character varying(255);

ALTER TABLE customer.customers ADD "PasswordResetTokenExpiresAt" timestamp with time zone;

ALTER TABLE customer.customers ADD "PasswordResetTokenHash" character varying(256);

ALTER TABLE customer.customers ADD "SessionVersion" integer NOT NULL DEFAULT 0;

CREATE UNIQUE INDEX "IX_customers_GoogleSubject" ON customer.customers ("GoogleSubject");

INSERT INTO shared.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20261021081311_AddGoogleAndPasswordResetAuth', '8.0.30');

COMMIT;

