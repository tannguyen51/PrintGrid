using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RescheduleAndDecisionLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTE: the generator also wanted to add customer.order_items.ItemStatus here —
            // that column already ships with 20260930100944_AddOrderTrackingFields, so adding
            // it again would fail on any database that has already run that migration.

            migrationBuilder.AddColumn<string>(
                name: "CostBearer",
                schema: "scheduling",
                table: "jobs",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Customer");

            migrationBuilder.AddColumn<Guid>(
                name: "FaultLabId",
                schema: "scheduling",
                table: "jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginalJobId",
                schema: "scheduling",
                table: "jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                schema: "scheduling",
                table: "jobs",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Normal");

            migrationBuilder.AddColumn<int>(
                name: "ReprintIndex",
                schema: "scheduling",
                table: "jobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "assignment_decisions",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    Trigger = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ActorType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ActorId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CandidatesJson = table.Column<string>(type: "jsonb", nullable: false),
                    RankingJson = table.Column<string>(type: "jsonb", nullable: false),
                    ChosenLabId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChosenMachineId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChosenScore = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: true),
                    ScoringConfigVersion = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ScoringConfigJson = table.Column<string>(type: "jsonb", nullable: false),
                    TimeBudgetMs = table.Column<int>(type: "integer", nullable: false),
                    ElapsedMs = table.Column<int>(type: "integer", nullable: false),
                    BudgetExceeded = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assignment_decisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "date_change_requests",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalDeliveryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ProposedDeliveryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_date_change_requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_escalations",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_escalations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_jobs_OriginalJobId",
                schema: "scheduling",
                table: "jobs",
                column: "OriginalJobId");

            migrationBuilder.CreateIndex(
                name: "IX_assignment_decisions_JobId_CreatedAtUtc",
                schema: "scheduling",
                table: "assignment_decisions",
                columns: new[] { "JobId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_assignment_decisions_OrderItemId",
                schema: "scheduling",
                table: "assignment_decisions",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_date_change_requests_JobId_CreatedAtUtc",
                schema: "scheduling",
                table: "date_change_requests",
                columns: new[] { "JobId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_date_change_requests_Token",
                schema: "scheduling",
                table: "date_change_requests",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ops_escalations_JobId",
                schema: "scheduling",
                table: "ops_escalations",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_escalations_Status_CreatedAtUtc",
                schema: "scheduling",
                table: "ops_escalations",
                columns: new[] { "Status", "CreatedAtUtc" });

            // FR-SCHED-009 AC3 / NFR-LEGAL-002: the decision log is evidence, so immutability
            // is enforced by the database itself, not only by the absence of an API route —
            // the same reasoning as the jobs_no_machine_overlap EXCLUDE constraint.
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION scheduling.assignment_decisions_append_only()
                RETURNS trigger AS $$
                BEGIN
                    RAISE EXCEPTION 'scheduling.assignment_decisions is append-only (FR-SCHED-009)';
                END;
                $$ LANGUAGE plpgsql;
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER assignment_decisions_no_update
                    BEFORE UPDATE OR DELETE ON scheduling.assignment_decisions
                    FOR EACH ROW EXECUTE FUNCTION scheduling.assignment_decisions_append_only();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS assignment_decisions_no_update ON scheduling.assignment_decisions;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS scheduling.assignment_decisions_append_only();");

            migrationBuilder.DropTable(
                name: "assignment_decisions",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "date_change_requests",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "ops_escalations",
                schema: "scheduling");

            migrationBuilder.DropIndex(
                name: "IX_jobs_OriginalJobId",
                schema: "scheduling",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "CostBearer",
                schema: "scheduling",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "FaultLabId",
                schema: "scheduling",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "OriginalJobId",
                schema: "scheduling",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "Priority",
                schema: "scheduling",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "ReprintIndex",
                schema: "scheduling",
                table: "jobs");
        }
    }
}
