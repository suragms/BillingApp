using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HexaBill.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSessionVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use idempotent SQL for PostgreSQL to prevent "column already exists" errors
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                // Wrap in exception handling to catch "column already exists" errors (SQLSTATE 42701)
                migrationBuilder.Sql(@"
                    DO $$ BEGIN
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND LOWER(table_name)=LOWER('Users') AND LOWER(column_name)=LOWER('SessionVersion')) THEN
                            ALTER TABLE ""Users"" ADD COLUMN ""SessionVersion"" integer NOT NULL DEFAULT 0;
                        END IF;
                    EXCEPTION WHEN SQLSTATE '42701' THEN NULL;
                    END $$");
                
                migrationBuilder.Sql(@"
                    DO $$ BEGIN
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND LOWER(table_name)=LOWER('Users') AND LOWER(column_name)=LOWER('LastLoginAt')) THEN
                            ALTER TABLE ""Users"" ADD COLUMN ""LastLoginAt"" timestamp with time zone NULL;
                        END IF;
                    EXCEPTION WHEN SQLSTATE '42701' THEN NULL;
                    END $$");
                
                migrationBuilder.Sql(@"
                    DO $$ BEGIN
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND LOWER(table_name)=LOWER('Users') AND LOWER(column_name)=LOWER('LastActiveAt')) THEN
                            ALTER TABLE ""Users"" ADD COLUMN ""LastActiveAt"" timestamp with time zone NULL;
                        END IF;
                    EXCEPTION WHEN SQLSTATE '42701' THEN NULL;
                    END $$");
                
                migrationBuilder.Sql(@"
                    DO $$ BEGIN
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND LOWER(table_name)=LOWER('Customers') AND LOWER(column_name)=LOWER('PaymentTerms')) THEN
                            ALTER TABLE ""Customers"" ADD COLUMN ""PaymentTerms"" character varying(100) NULL;
                        END IF;
                    EXCEPTION WHEN SQLSTATE '42701' THEN NULL;
                    END $$");
            }
            else
            {
                // SQLite - use standard AddColumn (will be caught by try-catch if exists)
                migrationBuilder.AddColumn<int>(
                    name: "SessionVersion",
                    table: "Users",
                    type: "INTEGER",
                    nullable: false,
                    defaultValue: 0);

                migrationBuilder.AddColumn<DateTime>(
                    name: "LastLoginAt",
                    table: "Users",
                    type: "TEXT",
                    nullable: true);

                migrationBuilder.AddColumn<DateTime>(
                    name: "LastActiveAt",
                    table: "Users",
                    type: "TEXT",
                    nullable: true);

                migrationBuilder.AddColumn<string>(
                    name: "PaymentTerms",
                    table: "Customers",
                    type: "TEXT",
                    maxLength: 100,
                    nullable: true);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SessionVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PaymentTerms",
                table: "Customers");
        }
    }
}
