using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewPinpadApi.Migrations
{
    /// <inheritdoc />
    public partial class TambahRelasiModelUserDanSysLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure there is at least one SysLevel to reference
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [SysLevels])
BEGIN
    INSERT INTO [SysLevels] ([Name], [Description], [CreatedAt], [CreatedBy])
    VALUES (N'Default', N'Auto-created by migration for FK backfill', SYSUTCDATETIME(), N'migration')
END
");

            // Add a temporary int column to hold converted values
            migrationBuilder.Sql(@"ALTER TABLE [Users] ADD [AccessLevelTmp] INT NULL;");

            // Backfill ensuring the value exists in SysLevels; otherwise use the minimum SysLevel Id
            migrationBuilder.Sql(@"
UPDATE U
SET [AccessLevelTmp] = CASE
    WHEN EXISTS (
        SELECT 1
        FROM [SysLevels] SL
        WHERE SL.[Id] = TRY_CAST(NULLIF(LTRIM(RTRIM(CAST(U.[AccessLevel] AS NVARCHAR(100)))), N'') AS INT)
    ) THEN TRY_CAST(NULLIF(LTRIM(RTRIM(CAST(U.[AccessLevel] AS NVARCHAR(100)))), N'') AS INT)
    ELSE (SELECT MIN([Id]) FROM [SysLevels])
END
FROM [Users] U;
");

            // Drop old column and rename the temp column
            // Drop default constraint bound to [Users].[AccessLevel] if it exists
            migrationBuilder.Sql(@"
DECLARE @dc sysname;
SELECT @dc = d.name
FROM sys.default_constraints d
JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
WHERE d.parent_object_id = OBJECT_ID(N'[Users]') AND c.name = N'AccessLevel';
IF @dc IS NOT NULL EXEC('ALTER TABLE [Users] DROP CONSTRAINT [' + @dc + ']');
ALTER TABLE [Users] DROP COLUMN [AccessLevel];
");
            migrationBuilder.Sql(@"EXEC sp_rename 'Users.AccessLevelTmp', 'AccessLevel', 'COLUMN';");

            // Enforce NOT NULL after backfill
            migrationBuilder.Sql(@"ALTER TABLE [Users] ALTER COLUMN [AccessLevel] INT NOT NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_Users_AccessLevel",
                table: "Users",
                column: "AccessLevel");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_SysLevels_AccessLevel",
                table: "Users",
                column: "AccessLevel",
                principalTable: "SysLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
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
