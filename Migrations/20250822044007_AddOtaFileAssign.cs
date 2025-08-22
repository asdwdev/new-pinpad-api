using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewPinpadApi.Migrations
{
    /// <inheritdoc />
    public partial class AddOtaFileAssign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OtaFileAssign",
                columns: table => new
                {
                    OtaassId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OtaassKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OtaassBranch = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtaFileAssign", x => x.OtaassId);
                    table.ForeignKey(
                        name: "FK_OtaFileAssign_SysBranches_OtaassBranch",
                        column: x => x.OtaassBranch,
                        principalTable: "SysBranches",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OtaFileAssign_OtaassBranch",
                table: "OtaFileAssign",
                column: "OtaassBranch");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OtaFileAssign");
        }
    }
}
