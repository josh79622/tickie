using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tickie.Manager.Migrations
{
    /// <inheritdoc />
    public partial class AddPhaseNameUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Phases_ProjectId_Name",
                table: "Phases",
                columns: new[] { "ProjectId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Phases_ProjectId_Name",
                table: "Phases");
        }
    }
}
