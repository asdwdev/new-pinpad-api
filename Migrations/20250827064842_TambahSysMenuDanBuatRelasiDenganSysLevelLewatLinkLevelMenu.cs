using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewPinpadApi.Migrations
{
    /// <inheritdoc />
    public partial class TambahSysMenuDanBuatRelasiDenganSysLevelLewatLinkLevelMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SysMenu",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Urls = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysMenu", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "linkLevelMenu",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LevelId = table.Column<int>(type: "int", nullable: false),
                    MenuId = table.Column<int>(type: "int", nullable: false),
                    GrantedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linkLevelMenu", x => x.Id);
                    table.ForeignKey(
                        name: "FK_linkLevelMenu_SysLevels_LevelId",
                        column: x => x.LevelId,
                        principalTable: "SysLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_linkLevelMenu_SysMenu_MenuId",
                        column: x => x.MenuId,
                        principalTable: "SysMenu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_linkLevelMenu_LevelId",
                table: "linkLevelMenu",
                column: "LevelId");

            migrationBuilder.CreateIndex(
                name: "IX_linkLevelMenu_MenuId",
                table: "linkLevelMenu",
                column: "MenuId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "linkLevelMenu");

            migrationBuilder.DropTable(
                name: "SysMenu");
        }
    }
}
