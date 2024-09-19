using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arboris.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class V06 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Cxx_Nodes_Id_ProjectId",
                table: "Cxx_Nodes",
                columns: new[] { "Id", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_ImplementationLocations_EndLine_FilePath",
                table: "Cxx_ImplementationLocations",
                columns: new[] { "EndLine", "FilePath" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_ImplementationLocations_FilePath_StartLine_EndLine_Id_NodeId",
                table: "Cxx_ImplementationLocations",
                columns: new[] { "FilePath", "StartLine", "EndLine", "Id", "NodeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_ImplementationLocations_Id_FilePath_StartLine_EndLine_NodeId",
                table: "Cxx_ImplementationLocations",
                columns: new[] { "Id", "FilePath", "StartLine", "EndLine", "NodeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_ImplementationLocations_Id_NodeId_FilePath_StartLine",
                table: "Cxx_ImplementationLocations",
                columns: new[] { "Id", "NodeId", "FilePath", "StartLine" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_ImplementationLocations_StartLine_EndLine",
                table: "Cxx_ImplementationLocations",
                columns: new[] { "StartLine", "EndLine" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_DefineLocations_EndLine_FilePath",
                table: "Cxx_DefineLocations",
                columns: new[] { "EndLine", "FilePath" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_DefineLocations_FilePath_StartLine_EndLine_Id_NodeId",
                table: "Cxx_DefineLocations",
                columns: new[] { "FilePath", "StartLine", "EndLine", "Id", "NodeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_DefineLocations_Id_FilePath_StartLine",
                table: "Cxx_DefineLocations",
                columns: new[] { "Id", "FilePath", "StartLine" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_DefineLocations_Id_NodeId",
                table: "Cxx_DefineLocations",
                columns: new[] { "Id", "NodeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_DefineLocations_NodeId_Id_FilePath_StartLine",
                table: "Cxx_DefineLocations",
                columns: new[] { "NodeId", "Id", "FilePath", "StartLine" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_DefineLocations_StartLine_EndLine_FilePath_Id_NodeId",
                table: "Cxx_DefineLocations",
                columns: new[] { "StartLine", "EndLine", "FilePath", "Id", "NodeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cxx_Nodes_Id_ProjectId",
                table: "Cxx_Nodes");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_ImplementationLocations_EndLine_FilePath",
                table: "Cxx_ImplementationLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_ImplementationLocations_FilePath_StartLine_EndLine_Id_NodeId",
                table: "Cxx_ImplementationLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_ImplementationLocations_Id_FilePath_StartLine_EndLine_NodeId",
                table: "Cxx_ImplementationLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_ImplementationLocations_Id_NodeId_FilePath_StartLine",
                table: "Cxx_ImplementationLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_ImplementationLocations_StartLine_EndLine",
                table: "Cxx_ImplementationLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_DefineLocations_EndLine_FilePath",
                table: "Cxx_DefineLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_DefineLocations_FilePath_StartLine_EndLine_Id_NodeId",
                table: "Cxx_DefineLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_DefineLocations_Id_FilePath_StartLine",
                table: "Cxx_DefineLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_DefineLocations_Id_NodeId",
                table: "Cxx_DefineLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_DefineLocations_NodeId_Id_FilePath_StartLine",
                table: "Cxx_DefineLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_DefineLocations_StartLine_EndLine_FilePath_Id_NodeId",
                table: "Cxx_DefineLocations");
        }
    }
}
