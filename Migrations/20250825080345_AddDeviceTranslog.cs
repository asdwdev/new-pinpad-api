using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewPinpadApi.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceTranslog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PpadSn",
                table: "Pinpads",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Pinpads_PpadSn",
                table: "Pinpads",
                column: "PpadSn");

            migrationBuilder.CreateTable(
                name: "DeviceTranslogs",
                columns: table => new
                {
                    TranslogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TranslogSn = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TranslogBranch = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TranslogTrxType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TranslogCardnum = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TranslogAcctnum = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TranslogAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TranslogCreateby = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TranslogCreatedate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TranslogRc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TranslogRrn = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceTranslogs", x => x.TranslogId);
                    table.ForeignKey(
                        name: "FK_DeviceTranslogs_Pinpads_TranslogSn",
                        column: x => x.TranslogSn,
                        principalTable: "Pinpads",
                        principalColumn: "PpadSn",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeviceTranslogs_SysBranches_TranslogBranch",
                        column: x => x.TranslogBranch,
                        principalTable: "SysBranches",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeviceTranslogs_SysResponseCodes_TranslogTrxType",
                        column: x => x.TranslogTrxType,
                        principalTable: "SysResponseCodes",
                        principalColumn: "RescodeCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTranslogs_TranslogBranch",
                table: "DeviceTranslogs",
                column: "TranslogBranch");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTranslogs_TranslogSn",
                table: "DeviceTranslogs",
                column: "TranslogSn");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTranslogs_TranslogTrxType",
                table: "DeviceTranslogs",
                column: "TranslogTrxType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceTranslogs");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Pinpads_PpadSn",
                table: "Pinpads");

            migrationBuilder.AlterColumn<string>(
                name: "PpadSn",
                table: "Pinpads",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
