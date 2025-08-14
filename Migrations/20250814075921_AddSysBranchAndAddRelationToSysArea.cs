using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewPinpadApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSysBranchAndAddRelationToSysArea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Area",
                table: "SysBranches",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "SysAreas",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_SysAreas_Code",
                table: "SysAreas",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_SysBranches_Area",
                table: "SysBranches",
                column: "Area");

            migrationBuilder.AddForeignKey(
                name: "FK_SysBranches_SysAreas_Area",
                table: "SysBranches",
                column: "Area",
                principalTable: "SysAreas",
                principalColumn: "Code",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SysBranches_SysAreas_Area",
                table: "SysBranches");

            migrationBuilder.DropIndex(
                name: "IX_SysBranches_Area",
                table: "SysBranches");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_SysAreas_Code",
                table: "SysAreas");

            migrationBuilder.AlterColumn<string>(
                name: "Area",
                table: "SysBranches",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "SysAreas",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
