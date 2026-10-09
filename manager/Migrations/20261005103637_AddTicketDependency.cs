using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tickie.Manager.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketDependency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TicketDependencies",
                columns: table => new
                {
                    TicketId = table.Column<int>(type: "INTEGER", nullable: false),
                    PrerequisiteTicketId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketDependencies", x => new { x.TicketId, x.PrerequisiteTicketId });
                    table.CheckConstraint("CKTicketDepndency_NotSelf", "\"TicketId\" <> \"PrerequisiteTicketId\"");
                    table.ForeignKey(
                        name: "FK_TicketDependencies_Tickets_PrerequisiteTicketId",
                        column: x => x.PrerequisiteTicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketDependencies_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketDependencies_PrerequisiteTicketId",
                table: "TicketDependencies",
                column: "PrerequisiteTicketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketDependencies");
        }
    }
}
