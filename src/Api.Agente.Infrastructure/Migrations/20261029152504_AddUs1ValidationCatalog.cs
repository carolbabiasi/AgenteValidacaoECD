using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Agente.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUs1ValidationCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProgressPercent",
                schema: "operational",
                table: "ValidationExecutions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProgressPhase",
                schema: "operational",
                table: "ValidationExecutions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReportJson",
                schema: "operational",
                table: "ValidationExecutions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SubmittedAt",
                schema: "operational",
                table: "ValidationExecutions",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateTable(
                name: "Findings",
                schema: "operational",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    PrimaryRecordType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    LineNumber = table.Column<int>(type: "int", nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceRuleRunIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RuleVersion = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    EvidenceJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false)
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
                name: "ValidationRules",
                schema: "operational",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    SeverityDefault = table.Column<int>(type: "int", nullable: false),
                    OfficialReference = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    ImplementationKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuleLayoutBindings",
                schema: "operational",
                columns: table => new
                {
                    RuleId = table.Column<string>(type: "nvarchar(32)", nullable: false),
                    LayoutVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
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
                name: "ValidationRules",
                schema: "operational");

            migrationBuilder.DropColumn(
                name: "ProgressPercent",
                schema: "operational",
                table: "ValidationExecutions");

            migrationBuilder.DropColumn(
                name: "ProgressPhase",
                schema: "operational",
                table: "ValidationExecutions");

            migrationBuilder.DropColumn(
                name: "ReportJson",
                schema: "operational",
                table: "ValidationExecutions");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                schema: "operational",
                table: "ValidationExecutions");
        }
    }
}
