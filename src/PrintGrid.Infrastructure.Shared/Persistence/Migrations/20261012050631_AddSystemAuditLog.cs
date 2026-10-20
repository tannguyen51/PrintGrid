using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "admin");

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorDisplay = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    ActorRole = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ActionClass = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    OldValuesJson = table.Column<string>(type: "jsonb", nullable: true),
                    NewValuesJson = table.Column<string>(type: "jsonb", nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TraceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RequirementCodesJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_ActionClass_CreatedAtUtc",
                schema: "admin",
                table: "audit_logs",
                columns: new[] { "ActionClass", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_ActorId_CreatedAtUtc",
                schema: "admin",
                table: "audit_logs",
                columns: new[] { "ActorId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_CreatedAtUtc",
                schema: "admin",
                table: "audit_logs",
                column: "CreatedAtUtc");

            // BR-CONFIG-004: application code is not the trust boundary. Even direct SQL
            // users cannot rewrite or delete compliance evidence.
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION admin.audit_logs_append_only()
                RETURNS trigger AS $$
                BEGIN
                    RAISE EXCEPTION 'admin.audit_logs is append-only (BR-CONFIG-004)';
                END;
                $$ LANGUAGE plpgsql;
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER audit_logs_no_update
                    BEFORE UPDATE OR DELETE ON admin.audit_logs
                    FOR EACH ROW EXECUTE FUNCTION admin.audit_logs_append_only();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS audit_logs_no_update ON admin.audit_logs;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS admin.audit_logs_append_only();");

            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "admin");
        }
    }
}
