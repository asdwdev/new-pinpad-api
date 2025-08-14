using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewPinpadApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPinpadBranchRelationshipWithDataCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, clean up any Pinpad records that reference non-existent Branch IDs
            migrationBuilder.Sql(@"
                DELETE FROM Pinpands 
                WHERE PpadBranch NOT IN (SELECT Id FROM Branches)
            ");

            // Create index
            migrationBuilder.CreateIndex(
                name: "IX_Pinpands_PpadBranch",
                table: "Pinpands",
                column: "PpadBranch");

            // Add foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "FK_Pinpands_Branches_PpadBranch",
                table: "Pinpands",
                column: "PpadBranch",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pinpands_Branches_PpadBranch",
                table: "Pinpands");

            migrationBuilder.DropIndex(
                name: "IX_Pinpands_PpadBranch",
                table: "Pinpands");
        }
    }
}
