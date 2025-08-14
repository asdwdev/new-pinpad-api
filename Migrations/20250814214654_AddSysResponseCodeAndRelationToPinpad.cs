using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewPinpadApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSysResponseCodeAndRelationToPinpad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PpadStatusRepair",
                table: "Pinpads",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "SysResponseCodes",
                columns: table => new
                {
                    RescodeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RescodeType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RescodeCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RescodeDesc = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RescodeCreateBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RescodeCreateDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RescodeUpdateBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RescodeUpdateDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysResponseCodes", x => x.RescodeId);
                    table.UniqueConstraint("AK_SysResponseCodes_RescodeCode", x => x.RescodeCode);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pinpads_PpadStatusRepair",
                table: "Pinpads",
                column: "PpadStatusRepair");

            migrationBuilder.AddForeignKey(
                name: "FK_Pinpads_SysResponseCodes_PpadStatusRepair",
                table: "Pinpads",
                column: "PpadStatusRepair",
                principalTable: "SysResponseCodes",
                principalColumn: "RescodeCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pinpads_SysResponseCodes_PpadStatusRepair",
                table: "Pinpads");

            migrationBuilder.DropTable(
                name: "SysResponseCodes");

            migrationBuilder.DropIndex(
                name: "IX_Pinpads_PpadStatusRepair",
                table: "Pinpads");

            migrationBuilder.AlterColumn<string>(
                name: "PpadStatusRepair",
                table: "Pinpads",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
