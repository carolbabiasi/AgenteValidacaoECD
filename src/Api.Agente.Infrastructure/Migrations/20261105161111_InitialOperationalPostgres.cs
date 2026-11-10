using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Agente.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialOperationalPostgres : Migration
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Sha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ValidationRules",
                schema: "operational",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Version = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    SeverityDefault = table.Column<int>(type: "integer", nullable: false),
                    OfficialReference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    ImplementationKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ValidationExecutions",
                schema: "operational",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedByUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    OriginalFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    DetectedLayoutVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    DetectedPeriodStart = table.Column<DateOnly>(type: "date", nullable: true),
                    DetectedPeriodEnd = table.Column<DateOnly>(type: "date", nullable: true),
                    RecordsAnalyzedCount = table.Column<int>(type: "integer", nullable: false),
                    RulesExecutedCount = table.Column<int>(type: "integer", nullable: false),
                    FindingsCount = table.Column<int>(type: "integer", nullable: false),
                    ManualVersionUsed = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RagSnapshotJson = table.Column<string>(type: "text", nullable: true),
                    LlmResponseSchemaVersion = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProgressPhase = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ProgressPercent = table.Column<int>(type: "integer", nullable: true),
                    ReportJson = table.Column<string>(type: "text", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
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
                name: "RuleLayoutBindings",
                schema: "operational",
                columns: table => new
                {
                    RuleId = table.Column<string>(type: "character varying(32)", nullable: false),
                    LayoutVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleLayoutBindings", x => new { x.RuleId, x.LayoutVersion });
                    table.ForeignKey(
                        name: "FK_RuleLayoutBindings_ValidationRules_RuleId",
                        column: x => x.RuleId,
                        principalSchema: "operational",
                        principalTable: "ValidationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Findings",
                schema: "operational",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    PrimaryRecordType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    LineNumber = table.Column<int>(type: "integer", nullable: true),
                    Summary = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Explanation = table.Column<string>(type: "text", nullable: true),
                    SourceRuleRunIdsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Findings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Findings_ValidationExecutions_ExecutionId",
                        column: x => x.ExecutionId,
                        principalSchema: "operational",
                        principalTable: "ValidationExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuleRunResults",
                schema: "operational",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RuleVersion = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Outcome = table.Column<int>(type: "integer", nullable: false),
                    EvidenceJson = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleRunResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleRunResults_ValidationExecutions_ExecutionId",
                        column: x => x.ExecutionId,
                        principalSchema: "operational",
                        principalTable: "ValidationExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ValidationJobs",
                schema: "operational",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Attempt = table.Column<int>(type: "integer", nullable: false),
                    LockedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    State = table.Column<int>(type: "integer", nullable: false)
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
                name: "IX_Findings_ExecutionId",
                schema: "operational",
                table: "Findings",
                column: "ExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleRunResults_ExecutionId",
                schema: "operational",
                table: "RuleRunResults",
                column: "ExecutionId");

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
                name: "Findings",
                schema: "operational");

            migrationBuilder.DropTable(
                name: "RuleLayoutBindings",
                schema: "operational");

            migrationBuilder.DropTable(
                name: "RuleRunResults",
                schema: "operational");

            migrationBuilder.DropTable(
                name: "ValidationJobs",
                schema: "operational");

            migrationBuilder.DropTable(
                name: "ValidationRules",
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
