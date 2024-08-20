using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arboris.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class V02 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodeDefine",
                table: "Cxx_ImplementationLocations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceCode",
                table: "Cxx_ImplementationLocations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodeDefine",
                table: "Cxx_DefineLocations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceCode",
                table: "Cxx_DefineLocations",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodeDefine",
                table: "Cxx_ImplementationLocations");

            migrationBuilder.DropColumn(
                name: "SourceCode",
                table: "Cxx_ImplementationLocations");

            migrationBuilder.DropColumn(
                name: "CodeDefine",
                table: "Cxx_DefineLocations");

            migrationBuilder.DropColumn(
                name: "SourceCode",
                table: "Cxx_DefineLocations");
        }
    }
}
