using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewPinpadApi.Migrations
{
    /// <inheritdoc />
    public partial class AddRelationFromPinpadToBranch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "SysBranches",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "PpadSeq",
                table: "SysBranches",
                newName: "ppad_seq");

            migrationBuilder.RenameColumn(
                name: "PpadIplow",
                table: "SysBranches",
                newName: "ppad_iplow");

            migrationBuilder.RenameColumn(
                name: "PpadIphigh",
                table: "SysBranches",
                newName: "ppad_iphigh");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "SysBranches",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "PpadBranch",
                table: "Pinpads",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_SysBranches_Code",
                table: "SysBranches",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Pinpads_PpadBranch",
                table: "Pinpads",
                column: "PpadBranch");

            migrationBuilder.AddForeignKey(
                name: "FK_Pinpads_SysBranches_PpadBranch",
                table: "Pinpads",
                column: "PpadBranch",
                principalTable: "SysBranches",
                principalColumn: "Code",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pinpads_SysBranches_PpadBranch",
                table: "Pinpads");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_SysBranches_Code",
                table: "SysBranches");

            migrationBuilder.DropIndex(
                name: "IX_Pinpads_PpadBranch",
                table: "Pinpads");

            migrationBuilder.RenameColumn(
                name: "ID",
                table: "SysBranches",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "ppad_seq",
                table: "SysBranches",
                newName: "PpadSeq");

            migrationBuilder.RenameColumn(
                name: "ppad_iplow",
                table: "SysBranches",
                newName: "PpadIplow");

            migrationBuilder.RenameColumn(
                name: "ppad_iphigh",
                table: "SysBranches",
                newName: "PpadIphigh");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "SysBranches",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "PpadBranch",
                table: "Pinpads",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
