using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewPinpadApi.Migrations
{
    /// <inheritdoc />
    public partial class TambahRelasiModelUserDanSysLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "AccessLevel",
                table: "Users",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Users_AccessLevel",
                table: "Users",
                column: "AccessLevel");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_SysLevels_AccessLevel",
                table: "Users",
                column: "AccessLevel",
                principalTable: "SysLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_SysLevels_AccessLevel",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_AccessLevel",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "AccessLevel",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
