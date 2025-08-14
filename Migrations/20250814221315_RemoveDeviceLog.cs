using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewPinpadApi.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDeviceLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceLogs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceLogs",
                columns: table => new
                {
                    DevlogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DevlogBranch = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DevlogCreateBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DevlogCreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DevlogSn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DevlogStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DevlogTrxCode = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceLogs", x => x.DevlogId);
                });
        }
    }
}
