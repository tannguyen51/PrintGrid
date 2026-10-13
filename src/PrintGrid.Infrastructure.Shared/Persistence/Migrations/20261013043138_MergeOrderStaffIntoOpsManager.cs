using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MergeOrderStaffIntoOpsManager : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE customer.customers
                SET "Roles" = ARRAY(
                    SELECT DISTINCT
                        CASE WHEN role_name = 'OrderStaff' THEN 'OpsManager' ELSE role_name END
                    FROM unnest("Roles") AS role_name
                )
                WHERE 'OrderStaff' = ANY("Roles");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This consolidation is intentionally irreversible: after migration there is no
            // reliable way to distinguish former OrderStaff accounts from OpsManager accounts.
        }
    }
}
