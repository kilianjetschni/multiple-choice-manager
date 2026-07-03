using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultipleChoiceManager.Migrations;

[Migration("20260703143000_AddQuestionAiReviewResult")]
public partial class AddQuestionAiReviewResult : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH('Questions', 'AiReviewResultJson') IS NULL
            BEGIN
                ALTER TABLE [Questions] ADD [AiReviewResultJson] nvarchar(max) NULL;
            END
            """);

        migrationBuilder.Sql("""
            IF COL_LENGTH('Questions', 'AiReviewedAtUtc') IS NULL
            BEGIN
                ALTER TABLE [Questions] ADD [AiReviewedAtUtc] datetime2 NULL;
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH('Questions', 'AiReviewResultJson') IS NOT NULL
            BEGIN
                ALTER TABLE [Questions] DROP COLUMN [AiReviewResultJson];
            END
            """);

        migrationBuilder.Sql("""
            IF COL_LENGTH('Questions', 'AiReviewedAtUtc') IS NOT NULL
            BEGIN
                ALTER TABLE [Questions] DROP COLUMN [AiReviewedAtUtc];
            END
            """);
    }
}
