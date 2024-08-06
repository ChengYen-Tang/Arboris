using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arboris.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class V01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cxx_Nodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CursorKindSpelling = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Spelling = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CxType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cxx_Nodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cxx_Nodes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cxx_DefineLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StartLine = table.Column<long>(type: "bigint", nullable: false),
                    EndLine = table.Column<long>(type: "bigint", nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cxx_DefineLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cxx_DefineLocations_Cxx_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Cxx_Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cxx_ImplementationLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StartLine = table.Column<long>(type: "bigint", nullable: false),
                    EndLine = table.Column<long>(type: "bigint", nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cxx_ImplementationLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cxx_ImplementationLocations_Cxx_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Cxx_Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cxx_NodeDependencies",
                columns: table => new
                {
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cxx_NodeDependencies", x => new { x.NodeId, x.FromId });
                    table.ForeignKey(
                        name: "FK_Cxx_NodeDependencies_Cxx_Nodes_FromId",
                        column: x => x.FromId,
                        principalTable: "Cxx_Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cxx_NodeDependencies_Cxx_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Cxx_Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cxx_NodeMembers",
                columns: table => new
                {
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cxx_NodeMembers", x => new { x.NodeId, x.MemberId });
                    table.ForeignKey(
                        name: "FK_Cxx_NodeMembers_Cxx_Nodes_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Cxx_Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cxx_NodeMembers_Cxx_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Cxx_Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cxx_NodeTypes",
                columns: table => new
                {
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cxx_NodeTypes", x => new { x.NodeId, x.TypeId });
                    table.ForeignKey(
                        name: "FK_Cxx_NodeTypes_Cxx_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Cxx_Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Cxx_NodeTypes_Cxx_Nodes_TypeId",
                        column: x => x.TypeId,
                        principalTable: "Cxx_Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_DefineLocations_FilePath_StartLine",
                table: "Cxx_DefineLocations",
                columns: new[] { "FilePath", "StartLine" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_DefineLocations_FilePath_StartLine_EndLine",
                table: "Cxx_DefineLocations",
                columns: new[] { "FilePath", "StartLine", "EndLine" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_DefineLocations_NodeId",
                table: "Cxx_DefineLocations",
                column: "NodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_ImplementationLocations_FilePath_StartLine",
                table: "Cxx_ImplementationLocations",
                columns: new[] { "FilePath", "StartLine" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_ImplementationLocations_FilePath_StartLine_EndLine",
                table: "Cxx_ImplementationLocations",
                columns: new[] { "FilePath", "StartLine", "EndLine" });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_ImplementationLocations_NodeId",
                table: "Cxx_ImplementationLocations",
                column: "NodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_NodeDependencies_FromId",
                table: "Cxx_NodeDependencies",
                column: "FromId");

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_NodeMembers_MemberId",
                table: "Cxx_NodeMembers",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_Nodes_ProjectId",
                table: "Cxx_Nodes",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_NodeTypes_TypeId",
                table: "Cxx_NodeTypes",
                column: "TypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cxx_DefineLocations");

            migrationBuilder.DropTable(
                name: "Cxx_ImplementationLocations");

            migrationBuilder.DropTable(
                name: "Cxx_NodeDependencies");

            migrationBuilder.DropTable(
                name: "Cxx_NodeMembers");

            migrationBuilder.DropTable(
                name: "Cxx_NodeTypes");

            migrationBuilder.DropTable(
                name: "Cxx_Nodes");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
