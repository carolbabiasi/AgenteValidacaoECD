using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Agente.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialOperational : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "operational");

            migrationBuilder.CreateTable(
                name: "StoredFiles",
                schema: "operational",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sha256 = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ValidationExecutions",
                schema: "operational",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedByUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OrganizationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    OriginalFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DetectedLayoutVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    DetectedPeriodStart = table.Column<DateOnly>(type: "date", nullable: true),
                    DetectedPeriodEnd = table.Column<DateOnly>(type: "date", nullable: true),
                    RecordsAnalyzedCount = table.Column<int>(type: "int", nullable: false),
                    RulesExecutedCount = table.Column<int>(type: "int", nullable: false),
                    FindingsCount = table.Column<int>(type: "int", nullable: false),
                    ManualVersionUsed = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RagSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LlmResponseSchemaVersion = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidationExecutions_StoredFiles_OriginalFileId",
                        column: x => x.OriginalFileId,
                        principalSchema: "operational",
                        principalTable: "StoredFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ValidationJobs",
                schema: "operational",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Attempt = table.Column<int>(type: "int", nullable: false),
                    LockedUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    State = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidationJobs_ValidationExecutions_ExecutionId",
                        column: x => x.ExecutionId,
                        principalSchema: "operational",
                        principalTable: "ValidationExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoredFiles_Sha256",
                schema: "operational",
                table: "StoredFiles",
                column: "Sha256");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationExecutions_CorrelationId",
                schema: "operational",
                table: "ValidationExecutions",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationExecutions_OriginalFileId",
                schema: "operational",
                table: "ValidationExecutions",
                column: "OriginalFileId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationExecutions_Status",
                schema: "operational",
                table: "ValidationExecutions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationJobs_ExecutionId_Attempt",
                schema: "operational",
                table: "ValidationJobs",
                columns: new[] { "ExecutionId", "Attempt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValidationJobs_State_LockedUntil",
                schema: "operational",
                table: "ValidationJobs",
                columns: new[] { "State", "LockedUntil" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ValidationJobs",
                schema: "operational");

            migrationBuilder.DropTable(
                name: "ValidationExecutions",
                schema: "operational");

            migrationBuilder.DropTable(
                name: "StoredFiles",
                schema: "operational");
        }
    }
}
