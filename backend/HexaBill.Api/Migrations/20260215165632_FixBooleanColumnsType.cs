using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HexaBill.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixBooleanColumnsType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) != true)
                return;
            // PostgreSQL cannot auto-cast integer to boolean (42804). Only alter when column exists and is integer.
            // Use LOWER() for case-insensitive checks (PostgreSQL may store names in different cases)
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND LOWER(table_name)=LOWER('Routes') AND LOWER(column_name)=LOWER('IsActive') AND data_type IN ('integer','smallint')) THEN
                        ALTER TABLE ""Routes"" ALTER COLUMN ""IsActive"" DROP DEFAULT, ALTER COLUMN ""IsActive"" TYPE boolean USING (CASE WHEN ""IsActive""::int = 0 THEN false ELSE true END), ALTER COLUMN ""IsActive"" SET DEFAULT false;
                    ELSIF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND LOWER(table_name)=LOWER('Routes') AND LOWER(column_name)=LOWER('IsActive')) AND EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='public' AND LOWER(table_name)=LOWER('Routes')) THEN
                        ALTER TABLE ""Routes"" ADD COLUMN ""IsActive"" boolean NOT NULL DEFAULT true;
                    END IF;
                END $$;
            ");
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND LOWER(table_name)=LOWER('Branches') AND LOWER(column_name)=LOWER('IsActive') AND data_type IN ('integer','smallint')) THEN
                        ALTER TABLE ""Branches"" ALTER COLUMN ""IsActive"" DROP DEFAULT, ALTER COLUMN ""IsActive"" TYPE boolean USING (CASE WHEN ""IsActive""::int = 0 THEN false ELSE true END), ALTER COLUMN ""IsActive"" SET DEFAULT false;
                    ELSIF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND LOWER(table_name)=LOWER('Branches') AND LOWER(column_name)=LOWER('IsActive')) AND EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='public' AND LOWER(table_name)=LOWER('Branches')) THEN
                        ALTER TABLE ""Branches"" ADD COLUMN ""IsActive"" boolean NOT NULL DEFAULT true;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) != true)
                return;
            migrationBuilder.Sql(@"
                ALTER TABLE ""Routes"" ALTER COLUMN ""IsActive"" TYPE integer USING (CASE WHEN ""IsActive"" THEN 1 ELSE 0 END);
            ");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Branches"" ALTER COLUMN ""IsActive"" TYPE integer USING (CASE WHEN ""IsActive"" THEN 1 ELSE 0 END);
            ");
        }
    }
}
