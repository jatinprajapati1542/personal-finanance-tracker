using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace personal_finance_Tracker.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueBudgetConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Budgets_UserId",
                table: "Budgets");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_UserId_CategoryId_Month",
                table: "Budgets",
                columns: new[] { "UserId", "CategoryId", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Budgets_UserId_CategoryId_Month",
                table: "Budgets");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_UserId",
                table: "Budgets",
                column: "UserId");
        }
    }
}
