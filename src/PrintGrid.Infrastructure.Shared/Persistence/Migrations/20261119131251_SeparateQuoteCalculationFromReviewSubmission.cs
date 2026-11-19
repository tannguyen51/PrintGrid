using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeparateQuoteCalculationFromReviewSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Keep existing customer submissions in the Ops queue; untouched calculations stay private drafts.
            migrationBuilder.Sql("""
                UPDATE customer.quotes AS q SET "Status" = 'PendingReview'
                WHERE q."Status" = 'Draft' AND EXISTS (
                    SELECT 1 FROM customer.orders AS o
                    WHERE o."QuoteId" = q."Id" AND o."Status" = 'AwaitingApproval'
                );
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE customer.quotes SET \"Status\" = 'Draft' WHERE \"Status\" = 'PendingReview';");

        }
    }
}
