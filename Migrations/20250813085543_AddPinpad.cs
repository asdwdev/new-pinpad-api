using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewPinpadApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPinpad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pinpands",
                columns: table => new
                {
                    PpadId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PpadSn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PpadBranch = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PpadBranchLama = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PpadStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PpadStatusRepair = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PpadStatusLama = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PpadTid = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PpadFlag = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PpadLastLogin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PpadLastActivity = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PpadCreateBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PpadCreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PpadUpdateBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PpadUpdateDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pinpands", x => x.PpadId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pinpands");
        }
    }
}
