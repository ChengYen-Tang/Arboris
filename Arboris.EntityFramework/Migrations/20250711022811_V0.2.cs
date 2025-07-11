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
            migrationBuilder.DropIndex(
                name: "IX_Cxx_Nodes_Id_ProjectId",
                table: "Cxx_Nodes");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_Nodes_Id_Spelling",
                table: "Cxx_Nodes");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_Nodes_ProjectId_Spelling_Id",
                table: "Cxx_Nodes");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_ImplementationLocations_EndColumn_NodeId_Id_StartLine_EndLine",
                table: "Cxx_ImplementationLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_ImplementationLocations_EndColumn_StartLine_EndLine_StartColumn_NodeId",
                table: "Cxx_ImplementationLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_ImplementationLocations_EndLine_StartColumn_EndColumn_StartLine_Id_NodeId",
                table: "Cxx_ImplementationLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_ImplementationLocations_Id_StartLine_EndLine_StartColumn",
                table: "Cxx_ImplementationLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_ImplementationLocations_NodeId_EndLine_StartColumn_EndColumn",
                table: "Cxx_ImplementationLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_ImplementationLocations_NodeId_Id_StartLine_StartColumn",
                table: "Cxx_ImplementationLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_ImplementationLocations_StartColumn_NodeId_Id_StartLine",
                table: "Cxx_ImplementationLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_ImplementationLocations_StartColumn_StartLine",
                table: "Cxx_ImplementationLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_ImplementationLocations_StartLine_EndLine_StartColumn_EndColumn_NodeId",
                table: "Cxx_ImplementationLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_DefineLocations_EndColumn_NodeId_Id_StartLine_EndLine_StartColumn",
                table: "Cxx_DefineLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_DefineLocations_EndColumn_StartLine_EndLine",
                table: "Cxx_DefineLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_DefineLocations_EndColumn_StartLine_StartColumn",
                table: "Cxx_DefineLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_DefineLocations_EndLine_StartColumn_EndColumn",
                table: "Cxx_DefineLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_DefineLocations_Id_StartLine_EndLine_StartColumn_EndColumn",
                table: "Cxx_DefineLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_DefineLocations_NodeId_EndLine_StartColumn_EndColumn",
                table: "Cxx_DefineLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_DefineLocations_NodeId_Id_StartLine_EndLine",
                table: "Cxx_DefineLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_DefineLocations_NodeId_StartLine_EndLine_StartColumn",
                table: "Cxx_DefineLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_DefineLocations_StartColumn_NodeId_Id_StartLine_EndLine",
                table: "Cxx_DefineLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_DefineLocations_StartColumn_StartLine_EndLine_EndColumn_NodeId",
                table: "Cxx_DefineLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_DefineLocations_StartLine_EndLine_StartColumn_EndColumn_NodeId_Id",
                table: "Cxx_DefineLocations");

            migrationBuilder.AlterColumn<string>(
                name: "VcProjectName",
                table: "Cxx_Nodes",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "NameSpace",
                table: "Cxx_Nodes",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CxType",
                table: "Cxx_Nodes",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CursorKindSpelling",
                table: "Cxx_Nodes",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FilePath",
                table: "Cxx_ImplementationLocations",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "FilePath",
                table: "Cxx_DefineLocations",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_Nodes_ProjectId_VcProjectName_CursorKindSpelling_Spelling_CxType_NameSpace",
                table: "Cxx_Nodes",
                columns: new[] { "ProjectId", "VcProjectName", "CursorKindSpelling", "Spelling", "CxType", "NameSpace" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_ImplementationLocations_FilePath_StartLine_EndLine",
                table: "Cxx_ImplementationLocations",
                columns: new[] { "FilePath", "StartLine", "EndLine" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_ImplementationLocations_NodeId_FilePath_StartLine_StartColumn_EndLine_EndColumn",
                table: "Cxx_ImplementationLocations",
                columns: new[] { "NodeId", "FilePath", "StartLine", "StartColumn", "EndLine", "EndColumn" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_DefineLocations_FilePath_StartLine_StartColumn_EndLine_EndColumn",
                table: "Cxx_DefineLocations",
                columns: new[] { "FilePath", "StartLine", "StartColumn", "EndLine", "EndColumn" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_DefineLocations_NodeId_FilePath_StartLine_StartColumn_EndLine_EndColumn",
                table: "Cxx_DefineLocations",
                columns: new[] { "NodeId", "FilePath", "StartLine", "StartColumn", "EndLine", "EndColumn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cxx_Nodes_ProjectId_VcProjectName_CursorKindSpelling_Spelling_CxType_NameSpace",
                table: "Cxx_Nodes");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_ImplementationLocations_FilePath_StartLine_EndLine",
                table: "Cxx_ImplementationLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_ImplementationLocations_NodeId_FilePath_StartLine_StartColumn_EndLine_EndColumn",
                table: "Cxx_ImplementationLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_DefineLocations_FilePath_StartLine_StartColumn_EndLine_EndColumn",
                table: "Cxx_DefineLocations");

            migrationBuilder.DropIndex(
                name: "IX_Cxx_DefineLocations_NodeId_FilePath_StartLine_StartColumn_EndLine_EndColumn",
                table: "Cxx_DefineLocations");

            migrationBuilder.AlterColumn<string>(
                name: "VcProjectName",
                table: "Cxx_Nodes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "NameSpace",
                table: "Cxx_Nodes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CxType",
                table: "Cxx_Nodes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CursorKindSpelling",
                table: "Cxx_Nodes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FilePath",
                table: "Cxx_ImplementationLocations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "FilePath",
                table: "Cxx_DefineLocations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_Nodes_Id_ProjectId",
                table: "Cxx_Nodes",
                columns: new[] { "Id", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_Nodes_Id_Spelling",
                table: "Cxx_Nodes",
                columns: new[] { "Id", "Spelling" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_Nodes_ProjectId_Spelling_Id",
                table: "Cxx_Nodes",
                columns: new[] { "ProjectId", "Spelling", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_ImplementationLocations_EndColumn_NodeId_Id_StartLine_EndLine",
                table: "Cxx_ImplementationLocations",
                columns: new[] { "EndColumn", "NodeId", "Id", "StartLine", "EndLine" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_ImplementationLocations_EndColumn_StartLine_EndLine_StartColumn_NodeId",
                table: "Cxx_ImplementationLocations",
                columns: new[] { "EndColumn", "StartLine", "EndLine", "StartColumn", "NodeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_ImplementationLocations_EndLine_StartColumn_EndColumn_StartLine_Id_NodeId",
                table: "Cxx_ImplementationLocations",
                columns: new[] { "EndLine", "StartColumn", "EndColumn", "StartLine", "Id", "NodeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_ImplementationLocations_Id_StartLine_EndLine_StartColumn",
                table: "Cxx_ImplementationLocations",
                columns: new[] { "Id", "StartLine", "EndLine", "StartColumn" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_ImplementationLocations_NodeId_EndLine_StartColumn_EndColumn",
                table: "Cxx_ImplementationLocations",
                columns: new[] { "NodeId", "EndLine", "StartColumn", "EndColumn" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_ImplementationLocations_NodeId_Id_StartLine_StartColumn",
                table: "Cxx_ImplementationLocations",
                columns: new[] { "NodeId", "Id", "StartLine", "StartColumn" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_ImplementationLocations_StartColumn_NodeId_Id_StartLine",
                table: "Cxx_ImplementationLocations",
                columns: new[] { "StartColumn", "NodeId", "Id", "StartLine" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_ImplementationLocations_StartColumn_StartLine",
                table: "Cxx_ImplementationLocations",
                columns: new[] { "StartColumn", "StartLine" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_ImplementationLocations_StartLine_EndLine_StartColumn_EndColumn_NodeId",
                table: "Cxx_ImplementationLocations",
                columns: new[] { "StartLine", "EndLine", "StartColumn", "EndColumn", "NodeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_DefineLocations_EndColumn_NodeId_Id_StartLine_EndLine_StartColumn",
                table: "Cxx_DefineLocations",
                columns: new[] { "EndColumn", "NodeId", "Id", "StartLine", "EndLine", "StartColumn" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_DefineLocations_EndColumn_StartLine_EndLine",
                table: "Cxx_DefineLocations",
                columns: new[] { "EndColumn", "StartLine", "EndLine" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_DefineLocations_EndColumn_StartLine_StartColumn",
                table: "Cxx_DefineLocations",
                columns: new[] { "EndColumn", "StartLine", "StartColumn" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_DefineLocations_EndLine_StartColumn_EndColumn",
                table: "Cxx_DefineLocations",
                columns: new[] { "EndLine", "StartColumn", "EndColumn" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_DefineLocations_Id_StartLine_EndLine_StartColumn_EndColumn",
                table: "Cxx_DefineLocations",
                columns: new[] { "Id", "StartLine", "EndLine", "StartColumn", "EndColumn" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_DefineLocations_NodeId_EndLine_StartColumn_EndColumn",
                table: "Cxx_DefineLocations",
                columns: new[] { "NodeId", "EndLine", "StartColumn", "EndColumn" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_DefineLocations_NodeId_Id_StartLine_EndLine",
                table: "Cxx_DefineLocations",
                columns: new[] { "NodeId", "Id", "StartLine", "EndLine" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_DefineLocations_NodeId_StartLine_EndLine_StartColumn",
                table: "Cxx_DefineLocations",
                columns: new[] { "NodeId", "StartLine", "EndLine", "StartColumn" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_DefineLocations_StartColumn_NodeId_Id_StartLine_EndLine",
                table: "Cxx_DefineLocations",
                columns: new[] { "StartColumn", "NodeId", "Id", "StartLine", "EndLine" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_DefineLocations_StartColumn_StartLine_EndLine_EndColumn_NodeId",
                table: "Cxx_DefineLocations",
                columns: new[] { "StartColumn", "StartLine", "EndLine", "EndColumn", "NodeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_DefineLocations_StartLine_EndLine_StartColumn_EndColumn_NodeId_Id",
                table: "Cxx_DefineLocations",
                columns: new[] { "StartLine", "EndLine", "StartColumn", "EndColumn", "NodeId", "Id" });
        }
    }
}
