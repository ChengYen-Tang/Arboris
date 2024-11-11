using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arboris.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class V010 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cxx_NodeDependencies_Cxx_Nodes_FromId",
                table: "Cxx_NodeDependencies");

            migrationBuilder.DropForeignKey(
                name: "FK_Cxx_NodeMembers_Cxx_Nodes_MemberId",
                table: "Cxx_NodeMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_Cxx_NodeTypes_Cxx_Nodes_TypeId",
                table: "Cxx_NodeTypes");

            migrationBuilder.AddForeignKey(
                name: "FK_Cxx_NodeDependencies_Cxx_Nodes_FromId",
                table: "Cxx_NodeDependencies",
                column: "FromId",
                principalTable: "Cxx_Nodes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Cxx_NodeMembers_Cxx_Nodes_MemberId",
                table: "Cxx_NodeMembers",
                column: "MemberId",
                principalTable: "Cxx_Nodes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Cxx_NodeTypes_Cxx_Nodes_TypeId",
                table: "Cxx_NodeTypes",
                column: "TypeId",
                principalTable: "Cxx_Nodes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cxx_NodeDependencies_Cxx_Nodes_FromId",
                table: "Cxx_NodeDependencies");

            migrationBuilder.DropForeignKey(
                name: "FK_Cxx_NodeMembers_Cxx_Nodes_MemberId",
                table: "Cxx_NodeMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_Cxx_NodeTypes_Cxx_Nodes_TypeId",
                table: "Cxx_NodeTypes");

            migrationBuilder.AddForeignKey(
                name: "FK_Cxx_NodeDependencies_Cxx_Nodes_FromId",
                table: "Cxx_NodeDependencies",
                column: "FromId",
                principalTable: "Cxx_Nodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Cxx_NodeMembers_Cxx_Nodes_MemberId",
                table: "Cxx_NodeMembers",
                column: "MemberId",
                principalTable: "Cxx_Nodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Cxx_NodeTypes_Cxx_Nodes_TypeId",
                table: "Cxx_NodeTypes",
                column: "TypeId",
                principalTable: "Cxx_Nodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
