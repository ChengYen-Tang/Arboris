using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arboris.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class V012 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SolutionName",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SolutionName",
                table: "Projects");
        }
    }
}
