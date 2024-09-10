using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arboris.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class V05 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExampleCode",
                table: "Cxx_Nodes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExampleCode",
                table: "Cxx_Nodes",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
