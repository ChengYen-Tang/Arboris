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
                name: "CppLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartLine = table.Column<int>(type: "int", nullable: false),
                    EndLine = table.Column<int>(type: "int", nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CppLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HeaderLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartLine = table.Column<int>(type: "int", nullable: false),
                    EndLine = table.Column<int>(type: "int", nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeaderLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HppLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartLine = table.Column<int>(type: "int", nullable: false),
                    EndLine = table.Column<int>(type: "int", nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HppLocations", x => x.Id);
                });

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
                name: "CxxNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CursorKindSpelling = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Spelling = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CxType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HeaderLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CppLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HppLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CxxNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CxxNodes_CppLocations_CppLocationId",
                        column: x => x.CppLocationId,
                        principalTable: "CppLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CxxNodes_HeaderLocations_HeaderLocationId",
                        column: x => x.HeaderLocationId,
                        principalTable: "HeaderLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CxxNodes_HppLocations_HppLocationId",
                        column: x => x.HppLocationId,
                        principalTable: "HppLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CxxNodes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Dependency",
                columns: table => new
                {
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dependency", x => new { x.NodeId, x.FromId });
                    table.UniqueConstraint("AK_Dependency_FromId", x => x.FromId);
                    table.ForeignKey(
                        name: "FK_Dependency_CxxNodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "CxxNodes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NodeMember",
                columns: table => new
                {
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeMember", x => new { x.NodeId, x.MemberId });
                    table.UniqueConstraint("AK_NodeMember_MemberId", x => x.MemberId);
                    table.ForeignKey(
                        name: "FK_NodeMember_CxxNodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "CxxNodes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NodeType",
                columns: table => new
                {
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeType", x => new { x.NodeId, x.TypeId });
                    table.UniqueConstraint("AK_NodeType_TypeId", x => x.TypeId);
                    table.ForeignKey(
                        name: "FK_NodeType_CxxNodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "CxxNodes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CxxNodes_CppLocationId",
                table: "CxxNodes",
                column: "CppLocationId",
                unique: true,
                filter: "[CppLocationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CxxNodes_HeaderLocationId",
                table: "CxxNodes",
                column: "HeaderLocationId",
                unique: true,
                filter: "[HeaderLocationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CxxNodes_HppLocationId",
                table: "CxxNodes",
                column: "HppLocationId",
                unique: true,
                filter: "[HppLocationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CxxNodes_ProjectId",
                table: "CxxNodes",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_CxxNodes_Dependency_Id",
                table: "CxxNodes",
                column: "Id",
                principalTable: "Dependency",
                principalColumn: "FromId");

            migrationBuilder.AddForeignKey(
                name: "FK_CxxNodes_NodeMember_Id",
                table: "CxxNodes",
                column: "Id",
                principalTable: "NodeMember",
                principalColumn: "MemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_CxxNodes_NodeType_Id",
                table: "CxxNodes",
                column: "Id",
                principalTable: "NodeType",
                principalColumn: "TypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CxxNodes_CppLocations_CppLocationId",
                table: "CxxNodes");

            migrationBuilder.DropForeignKey(
                name: "FK_CxxNodes_Dependency_Id",
                table: "CxxNodes");

            migrationBuilder.DropForeignKey(
                name: "FK_CxxNodes_HeaderLocations_HeaderLocationId",
                table: "CxxNodes");

            migrationBuilder.DropForeignKey(
                name: "FK_CxxNodes_HppLocations_HppLocationId",
                table: "CxxNodes");

            migrationBuilder.DropForeignKey(
                name: "FK_CxxNodes_NodeMember_Id",
                table: "CxxNodes");

            migrationBuilder.DropForeignKey(
                name: "FK_CxxNodes_NodeType_Id",
                table: "CxxNodes");

            migrationBuilder.DropTable(
                name: "CppLocations");

            migrationBuilder.DropTable(
                name: "Dependency");

            migrationBuilder.DropTable(
                name: "HeaderLocations");

            migrationBuilder.DropTable(
                name: "HppLocations");

            migrationBuilder.DropTable(
                name: "NodeMember");

            migrationBuilder.DropTable(
                name: "NodeType");

            migrationBuilder.DropTable(
                name: "CxxNodes");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
