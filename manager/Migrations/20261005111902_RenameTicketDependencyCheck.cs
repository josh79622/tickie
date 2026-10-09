using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tickie.Manager.Migrations
{
    /// <inheritdoc />
    public partial class RenameTicketDependencyCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CKTicketDepndency_NotSelf",
                table: "TicketDependencies");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TicketDependencies_NotSelf",
                table: "TicketDependencies",
                sql: "\"TicketId\" <> \"PrerequisiteTicketId\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TicketDependencies_NotSelf",
                table: "TicketDependencies");

            migrationBuilder.AddCheckConstraint(
                name: "CKTicketDepndency_NotSelf",
                table: "TicketDependencies",
                sql: "\"TicketId\" <> \"PrerequisiteTicketId\"");
        }
    }
}
