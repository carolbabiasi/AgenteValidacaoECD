using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Rag.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIngestionCountersAndFullText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ActivateOnSuccess",
                schema: "rag",
                table: "IngestionJobs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ChunksCreated",
                schema: "rag",
                table: "IngestionJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ChunksSkipped",
                schema: "rag",
                table: "IngestionJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                ALTER TABLE rag."DocumentChunks"
                ADD COLUMN IF NOT EXISTS "SearchVector" tsvector
                GENERATED ALWAYS AS (to_tsvector('portuguese', coalesce("Content", ''))) STORED;
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_DocumentChunks_SearchVector"
                ON rag."DocumentChunks" USING GIN ("SearchVector");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS rag."IX_DocumentChunks_SearchVector";""");
            migrationBuilder.Sql("""ALTER TABLE rag."DocumentChunks" DROP COLUMN IF EXISTS "SearchVector";""");

            migrationBuilder.DropColumn(
                name: "ActivateOnSuccess",
                schema: "rag",
                table: "IngestionJobs");

            migrationBuilder.DropColumn(
                name: "ChunksCreated",
                schema: "rag",
                table: "IngestionJobs");

            migrationBuilder.DropColumn(
                name: "ChunksSkipped",
                schema: "rag",
                table: "IngestionJobs");
        }
    }
}
