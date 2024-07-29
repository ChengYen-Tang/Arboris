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
                name: "Cxx_CppLocations",
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
                    table.PrimaryKey("PK_Cxx_CppLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cxx_CppLocations_Cxx_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Cxx_Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cxx_HeaderLocations",
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
                    table.PrimaryKey("PK_Cxx_HeaderLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cxx_HeaderLocations_Cxx_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Cxx_Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cxx_HppLocations",
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
                    table.PrimaryKey("PK_Cxx_HppLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cxx_HppLocations_Cxx_Nodes_NodeId",
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
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_Cxx_NodeDependencies_Cxx_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Cxx_Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
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
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_Cxx_NodeMembers_Cxx_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Cxx_Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
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
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_Cxx_NodeTypes_Cxx_Nodes_TypeId",
                        column: x => x.TypeId,
                        principalTable: "Cxx_Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_CppLocations_NodeId",
                table: "Cxx_CppLocations",
                column: "NodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_HeaderLocations_NodeId",
                table: "Cxx_HeaderLocations",
                column: "NodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_HppLocations_NodeId",
                table: "Cxx_HppLocations",
                column: "NodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_NodeDependencies_FromId",
                table: "Cxx_NodeDependencies",
                column: "FromId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_NodeMembers_MemberId",
                table: "Cxx_NodeMembers",
                column: "MemberId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_Nodes_ProjectId",
                table: "Cxx_Nodes",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Cxx_NodeTypes_TypeId",
                table: "Cxx_NodeTypes",
                column: "TypeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cxx_CppLocations");

            migrationBuilder.DropTable(
                name: "Cxx_HeaderLocations");

            migrationBuilder.DropTable(
                name: "Cxx_HppLocations");

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
