using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tickie.Manager.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTicketOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_PhaseId_Order",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "Tickets");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_PhaseId",
                table: "Tickets",
                column: "PhaseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_PhaseId",
                table: "Tickets");

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "Tickets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_PhaseId_Order",
                table: "Tickets",
                columns: new[] { "PhaseId", "Order" },
                unique: true);
        }
    }
}
