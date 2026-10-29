using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace Api.Rag.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialRag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "rag");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "ManualDocuments",
                schema: "rag",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceUri = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    FileName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ManualVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PublicationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    LayoutFamily = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManualDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentChunks",
                schema: "rag",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ManualDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChunkKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ContentHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    HierarchyPath = table.Column<string>(type: "text", nullable: false),
                    SourceLocation = table.Column<string>(type: "text", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(1536)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentChunks_ManualDocuments_ManualDocumentId",
                        column: x => x.ManualDocumentId,
                        principalSchema: "rag",
                        principalTable: "ManualDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IngestionJobs",
                schema: "rag",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ManualDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ErrorLog = table.Column<string>(type: "text", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngestionJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IngestionJobs_ManualDocuments_ManualDocumentId",
                        column: x => x.ManualDocumentId,
                        principalSchema: "rag",
                        principalTable: "ManualDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_ManualDocumentId_ChunkKey",
                schema: "rag",
                table: "DocumentChunks",
                columns: new[] { "ManualDocumentId", "ChunkKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IngestionJobs_ManualDocumentId",
                schema: "rag",
                table: "IngestionJobs",
                column: "ManualDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ManualDocuments_ManualVersion_LayoutFamily_Status",
                schema: "rag",
                table: "ManualDocuments",
                columns: new[] { "ManualVersion", "LayoutFamily", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentChunks",
                schema: "rag");

            migrationBuilder.DropTable(
                name: "IngestionJobs",
                schema: "rag");

            migrationBuilder.DropTable(
                name: "ManualDocuments",
                schema: "rag");
        }
    }
}
