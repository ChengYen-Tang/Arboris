using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arboris.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class V09 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cxx_Nodes_ProjectId",
                table: "Cxx_Nodes");

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_Nodes_ProjectId_Spelling_Id",
                table: "Cxx_Nodes",
                columns: new[] { "ProjectId", "Spelling", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cxx_Nodes_ProjectId_Spelling_Id",
                table: "Cxx_Nodes");

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_Nodes_ProjectId",
                table: "Cxx_Nodes",
                column: "ProjectId");
        }
    }
}
