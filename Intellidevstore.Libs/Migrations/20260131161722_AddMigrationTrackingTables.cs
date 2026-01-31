using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Intellidevstore.Libs.Migrations
{
    /// <inheritdoc />
    public partial class AddMigrationTrackingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "migration_locks",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lock_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    lock_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    acquired_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_migration_locks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "migration_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    migration_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    applied_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    execution_time_ms = table.Column<long>(type: "bigint", nullable: false),
                    is_success = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    checksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_migration_records", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_migration_locks_expires_at",
                table: "migration_locks",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_migration_locks_lock_id",
                table: "migration_locks",
                column: "lock_id");

            migrationBuilder.CreateIndex(
                name: "ix_migration_locks_lock_name",
                table: "migration_locks",
                column: "lock_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_migration_records_applied_at",
                table: "migration_records",
                column: "applied_at");

            migrationBuilder.CreateIndex(
                name: "ix_migration_records_is_success",
                table: "migration_records",
                column: "is_success");

            migrationBuilder.CreateIndex(
                name: "ix_migration_records_migration_id",
                table: "migration_records",
                column: "migration_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "migration_locks");

            migrationBuilder.DropTable(
                name: "migration_records");
        }
    }
}
