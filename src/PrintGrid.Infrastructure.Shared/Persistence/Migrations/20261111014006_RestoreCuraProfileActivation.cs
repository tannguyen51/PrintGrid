using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RestoreCuraProfileActivation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The original registry migration created this column, but the
            // static CLR property removed it from EF's snapshot. Preserve any
            // existing activation flags while restoring the instance mapping.
            migrationBuilder.Sql("""
                ALTER TABLE scheduling.cura_profiles
                    ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT TRUE;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // IsActive is part of the original registry schema. Keep the column
            // and its data when rolling back this mapping repair.
        }
    }
}
