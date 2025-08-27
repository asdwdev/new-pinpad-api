using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewPinpadApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNamaTabelLinkLevelMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_linkLevelMenu_SysLevels_LevelId",
                table: "linkLevelMenu");

            migrationBuilder.DropForeignKey(
                name: "FK_linkLevelMenu_SysMenus_MenuId",
                table: "linkLevelMenu");

            migrationBuilder.DropPrimaryKey(
                name: "PK_linkLevelMenu",
                table: "linkLevelMenu");

            migrationBuilder.RenameTable(
                name: "linkLevelMenu",
                newName: "LinkLevelMenus");

            migrationBuilder.RenameIndex(
                name: "IX_linkLevelMenu_MenuId",
                table: "LinkLevelMenus",
                newName: "IX_LinkLevelMenus_MenuId");

            migrationBuilder.RenameIndex(
                name: "IX_linkLevelMenu_LevelId",
                table: "LinkLevelMenus",
                newName: "IX_LinkLevelMenus_LevelId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LinkLevelMenus",
                table: "LinkLevelMenus",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LinkLevelMenus_SysLevels_LevelId",
                table: "LinkLevelMenus",
                column: "LevelId",
                principalTable: "SysLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LinkLevelMenus_SysMenus_MenuId",
                table: "LinkLevelMenus",
                column: "MenuId",
                principalTable: "SysMenus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LinkLevelMenus_SysLevels_LevelId",
                table: "LinkLevelMenus");

            migrationBuilder.DropForeignKey(
                name: "FK_LinkLevelMenus_SysMenus_MenuId",
                table: "LinkLevelMenus");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LinkLevelMenus",
                table: "LinkLevelMenus");

            migrationBuilder.RenameTable(
                name: "LinkLevelMenus",
                newName: "linkLevelMenu");

            migrationBuilder.RenameIndex(
                name: "IX_LinkLevelMenus_MenuId",
                table: "linkLevelMenu",
                newName: "IX_linkLevelMenu_MenuId");

            migrationBuilder.RenameIndex(
                name: "IX_LinkLevelMenus_LevelId",
                table: "linkLevelMenu",
                newName: "IX_linkLevelMenu_LevelId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_linkLevelMenu",
                table: "linkLevelMenu",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_linkLevelMenu_SysLevels_LevelId",
                table: "linkLevelMenu",
                column: "LevelId",
                principalTable: "SysLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_linkLevelMenu_SysMenus_MenuId",
                table: "linkLevelMenu",
                column: "MenuId",
                principalTable: "SysMenus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
