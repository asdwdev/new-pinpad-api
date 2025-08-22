using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewPinpadApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOtaFileAndAssignRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OtaFileAssign_SysBranches_OtaassBranch",
                table: "OtaFileAssign");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OtaFileAssign",
                table: "OtaFileAssign");

            migrationBuilder.RenameTable(
                name: "OtaFileAssign",
                newName: "OtaFileAssigns");

            migrationBuilder.RenameIndex(
                name: "IX_OtaFileAssign_OtaassBranch",
                table: "OtaFileAssigns",
                newName: "IX_OtaFileAssigns_OtaassBranch");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OtaFileAssigns",
                table: "OtaFileAssigns",
                column: "OtaassId");

            migrationBuilder.CreateTable(
                name: "OtaFiles",
                columns: table => new
                {
                    OtaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OtaDesc = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OtaKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OtaAttachment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OtaFilename = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OtaStatus = table.Column<int>(type: "int", nullable: false),
                    OtaCreateBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OtaCreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OtaUpdateBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OtaUpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtaFiles", x => x.OtaId);
                    table.UniqueConstraint("AK_OtaFiles_OtaKey", x => x.OtaKey);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OtaFileAssigns_OtaassKey",
                table: "OtaFileAssigns",
                column: "OtaassKey");

            migrationBuilder.AddForeignKey(
                name: "FK_OtaFileAssigns_OtaFiles_OtaassKey",
                table: "OtaFileAssigns",
                column: "OtaassKey",
                principalTable: "OtaFiles",
                principalColumn: "OtaKey",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OtaFileAssigns_SysBranches_OtaassBranch",
                table: "OtaFileAssigns",
                column: "OtaassBranch",
                principalTable: "SysBranches",
                principalColumn: "Code",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OtaFileAssigns_OtaFiles_OtaassKey",
                table: "OtaFileAssigns");

            migrationBuilder.DropForeignKey(
                name: "FK_OtaFileAssigns_SysBranches_OtaassBranch",
                table: "OtaFileAssigns");

            migrationBuilder.DropTable(
                name: "OtaFiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OtaFileAssigns",
                table: "OtaFileAssigns");

            migrationBuilder.DropIndex(
                name: "IX_OtaFileAssigns_OtaassKey",
                table: "OtaFileAssigns");

            migrationBuilder.RenameTable(
                name: "OtaFileAssigns",
                newName: "OtaFileAssign");

            migrationBuilder.RenameIndex(
                name: "IX_OtaFileAssigns_OtaassBranch",
                table: "OtaFileAssign",
                newName: "IX_OtaFileAssign_OtaassBranch");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OtaFileAssign",
                table: "OtaFileAssign",
                column: "OtaassId");

            migrationBuilder.AddForeignKey(
                name: "FK_OtaFileAssign_SysBranches_OtaassBranch",
                table: "OtaFileAssign",
                column: "OtaassBranch",
                principalTable: "SysBranches",
                principalColumn: "Code",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
