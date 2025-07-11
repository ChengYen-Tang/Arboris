using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arboris.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class V04 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cxx_DefineLocations_FilePath_StartLine_StartColumn_EndLine_EndColumn",
                table: "Cxx_DefineLocations");

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_DefineLocations_FilePath_StartLine_StartColumn_EndLine_EndColumn",
                table: "Cxx_DefineLocations",
                columns: new[] { "FilePath", "StartLine", "StartColumn", "EndLine", "EndColumn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cxx_DefineLocations_FilePath_StartLine_StartColumn_EndLine_EndColumn",
                table: "Cxx_DefineLocations");

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_DefineLocations_FilePath_StartLine_StartColumn_EndLine_EndColumn",
                table: "Cxx_DefineLocations",
                columns: new[] { "FilePath", "StartLine", "StartColumn", "EndLine", "EndColumn" },
                unique: true);
        }
    }
}
