using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewPinpadApi.Migrations
{
    /// <inheritdoc />
    public partial class RenamePinpandsFieldtoPinpadsField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Pinpands",
                table: "Pinpands");

            migrationBuilder.RenameTable(
                name: "Pinpands",
                newName: "Pinpads");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Pinpads",
                table: "Pinpads",
                column: "PpadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Pinpads",
                table: "Pinpads");

            migrationBuilder.RenameTable(
                name: "Pinpads",
                newName: "Pinpands");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Pinpands",
                table: "Pinpands",
                column: "PpadId");
        }
    }
}
