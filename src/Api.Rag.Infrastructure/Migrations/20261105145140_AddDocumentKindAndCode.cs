using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Rag.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentKindAndCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentCode",
                schema: "rag",
                table: "ManualDocuments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "MANUAL");

            migrationBuilder.AddColumn<int>(
                name: "DocumentKind",
                schema: "rag",
                table: "ManualDocuments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ManualDocuments_LayoutFamily_DocumentCode_Status",
                schema: "rag",
                table: "ManualDocuments",
                columns: new[] { "LayoutFamily", "DocumentCode", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ManualDocuments_LayoutFamily_DocumentCode_Status",
                schema: "rag",
                table: "ManualDocuments");

            migrationBuilder.DropColumn(
                name: "DocumentCode",
                schema: "rag",
                table: "ManualDocuments");

            migrationBuilder.DropColumn(
                name: "DocumentKind",
                schema: "rag",
                table: "ManualDocuments");
        }
    }
}
