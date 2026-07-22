using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FilterUniqueIndexOnSessionsAvailabilitySlotId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sessions_AvailabilitySlotId",
                table: "Sessions");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_AvailabilitySlotId",
                table: "Sessions",
                column: "AvailabilitySlotId",
                unique: true,
                filter: "\"Status\" <> 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sessions_AvailabilitySlotId",
                table: "Sessions");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_AvailabilitySlotId",
                table: "Sessions",
                column: "AvailabilitySlotId",
                unique: true);
        }
    }
}
