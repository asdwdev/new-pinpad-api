using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewPinpadApi.Migrations
{
    /// <inheritdoc />
    public partial class KelupaanDaftarinSysMenuDanLinkLevelMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_linkLevelMenu_SysMenu_MenuId",
                table: "linkLevelMenu");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SysMenu",
                table: "SysMenu");

            migrationBuilder.RenameTable(
                name: "SysMenu",
                newName: "SysMenus");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SysMenus",
                table: "SysMenus",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_linkLevelMenu_SysMenus_MenuId",
                table: "linkLevelMenu",
                column: "MenuId",
                principalTable: "SysMenus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_linkLevelMenu_SysMenus_MenuId",
                table: "linkLevelMenu");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SysMenus",
                table: "SysMenus");

            migrationBuilder.RenameTable(
                name: "SysMenus",
                newName: "SysMenu");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SysMenu",
                table: "SysMenu",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_linkLevelMenu_SysMenu_MenuId",
                table: "linkLevelMenu",
                column: "MenuId",
                principalTable: "SysMenu",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
