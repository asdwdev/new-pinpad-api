using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewPinpadApi.Migrations
{
    /// <inheritdoc />
    public partial class SyncModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "AccessLevel",
                table: "Users",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // Ensure index exists; avoid error if it already exists (created by previous migration)
            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_AccessLevel' AND object_id = OBJECT_ID(N'[Users]')
)
BEGIN
    CREATE INDEX [IX_Users_AccessLevel] ON [Users] ([AccessLevel]);
END
");

            // Ensure FK exists; avoid error if it already exists (created by previous migration)
            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_Users_SysLevels_AccessLevel' AND parent_object_id = OBJECT_ID(N'[Users]')
)
BEGIN
    ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_SysLevels_AccessLevel]
    FOREIGN KEY ([AccessLevel]) REFERENCES [SysLevels]([Id]) ON DELETE CASCADE;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_SysLevels_AccessLevel",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_AccessLevel",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "AccessLevel",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
