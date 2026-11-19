using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE customer.customers SET \"Roles\" = ARRAY['Admin'] WHERE \"Email\" = 'admin@test.com';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE customer.customers SET \"Roles\" = ARRAY['Customer'] WHERE \"Email\" = 'admin@test.com';");
        }
    }
}
