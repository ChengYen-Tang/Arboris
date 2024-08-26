using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arboris.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class V03 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Projects");

            migrationBuilder.RenameColumn(
                name: "CodeDefine",
                table: "Cxx_ImplementationLocations",
                newName: "DisplayName");

            migrationBuilder.RenameColumn(
                name: "CodeDefine",
                table: "Cxx_DefineLocations",
                newName: "DisplayName");

            migrationBuilder.AddColumn<string>(
                name: "LLMDescription",
                table: "Cxx_Nodes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserDescription",
                table: "Cxx_Nodes",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LLMDescription",
                table: "Cxx_Nodes");

            migrationBuilder.DropColumn(
                name: "UserDescription",
                table: "Cxx_Nodes");

            migrationBuilder.RenameColumn(
                name: "DisplayName",
                table: "Cxx_ImplementationLocations",
                newName: "CodeDefine");

            migrationBuilder.RenameColumn(
                name: "DisplayName",
                table: "Cxx_DefineLocations",
                newName: "CodeDefine");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
