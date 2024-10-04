using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arboris.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class V07 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Spelling",
                table: "Cxx_Nodes",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_Nodes_Id_Spelling",
                table: "Cxx_Nodes",
                columns: new[] { "Id", "Spelling" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_ImplementationLocations_NodeId_FilePath_StartLine_EndLine",
                table: "Cxx_ImplementationLocations",
                columns: new[] { "NodeId", "FilePath", "StartLine", "EndLine" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_ImplementationLocations_NodeId_StartLine",
                table: "Cxx_ImplementationLocations",
                columns: new[] { "NodeId", "StartLine" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_DefineLocations_NodeId_EndLine_FilePath",
                table: "Cxx_DefineLocations",
                columns: new[] { "NodeId", "EndLine", "FilePath" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_DefineLocations_NodeId_FilePath_StartLine",
                table: "Cxx_DefineLocations",
                columns: new[] { "NodeId", "FilePath", "StartLine" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_DefineLocations_NodeId_StartLine_EndLine_FilePath",
                table: "Cxx_DefineLocations",
                columns: new[] { "NodeId", "StartLine", "EndLine", "FilePath" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cxx_Nodes_Id_Spelling",
                table: "Cxx_Nodes");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_ImplementationLocations_NodeId_FilePath_StartLine_EndLine",
                table: "Cxx_ImplementationLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_ImplementationLocations_NodeId_StartLine",
                table: "Cxx_ImplementationLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_DefineLocations_NodeId_EndLine_FilePath",
                table: "Cxx_DefineLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_DefineLocations_NodeId_FilePath_StartLine",
                table: "Cxx_DefineLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_DefineLocations_NodeId_StartLine_EndLine_FilePath",
                table: "Cxx_DefineLocations");

            migrationBuilder.AlterColumn<string>(
                name: "Spelling",
                table: "Cxx_Nodes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
