using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewPinpadApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSysMkey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SysMkey",
                columns: table => new
                {
                    mkey_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    mkey_code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    mkey_number = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    mkey_desc = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    mkey_createby = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    mkey_createdate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    mkey_updateby = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    mkey_updatedate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysMkey", x => x.mkey_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SysMkey");
        }
    }
}
