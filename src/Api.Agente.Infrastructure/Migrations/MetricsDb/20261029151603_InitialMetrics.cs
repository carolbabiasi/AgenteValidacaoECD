using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Agente.Infrastructure.Migrations.MetricsDb
{
    /// <inheritdoc />
    public partial class InitialMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "metrics");

            migrationBuilder.CreateTable(
                name: "LlmUsageRecords",
                schema: "metrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    Service = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Model = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Operation = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DurationMs = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PromptTokens = table.Column<int>(type: "integer", nullable: true),
                    CompletionTokens = table.Column<int>(type: "integer", nullable: true),
                    EmbeddingTokens = table.Column<int>(type: "integer", nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    PriceTableRef = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IsEstimate = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LlmUsageRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LlmUsageRecords_CreatedAt",
                schema: "metrics",
                table: "LlmUsageRecords",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LlmUsageRecords_ExecutionId",
                schema: "metrics",
                table: "LlmUsageRecords",
                column: "ExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_LlmUsageRecords_ExecutionId_AttemptNumber_Operation",
                schema: "metrics",
                table: "LlmUsageRecords",
                columns: new[] { "ExecutionId", "AttemptNumber", "Operation" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LlmUsageRecords",
                schema: "metrics");
        }
    }
}
