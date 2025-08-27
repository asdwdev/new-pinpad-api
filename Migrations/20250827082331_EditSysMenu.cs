using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewPinpadApi.Migrations
{
    /// <inheritdoc />
    public partial class EditSysMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "SysMenus",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SysMenus_ParentId",
                table: "SysMenus",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_SysMenus_SysMenus_ParentId",
                table: "SysMenus",
                column: "ParentId",
                principalTable: "SysMenus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SysMenus_SysMenus_ParentId",
                table: "SysMenus");

            migrationBuilder.DropIndex(
                name: "IX_SysMenus_ParentId",
                table: "SysMenus");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "SysMenus");
        }
    }
}
