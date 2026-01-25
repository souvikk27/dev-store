using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Intellidevstore.Libs.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleCodeLevelAndIsSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "roles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true
            );

            migrationBuilder.AddColumn<bool>(
                name: "is_system",
                table: "roles",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<int>(
                name: "level",
                table: "roles",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.CreateIndex(name: "IX_Roles_Code", table: "roles", column: "code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Roles_Code", table: "roles");

            migrationBuilder.DropColumn(name: "code", table: "roles");

            migrationBuilder.DropColumn(name: "is_system", table: "roles");

            migrationBuilder.DropColumn(name: "level", table: "roles");
        }
    }
}
