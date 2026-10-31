using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompleteStripeTopUps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StripePaymentIntentId",
                table: "top_ups",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_top_ups_StripePaymentIntentId",
                table: "top_ups",
                column: "StripePaymentIntentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_top_ups_StripePaymentIntentId",
                table: "top_ups");

            migrationBuilder.DropColumn(
                name: "StripePaymentIntentId",
                table: "top_ups");
        }
    }
}
