using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultipleChoiceManager.Migrations
{
    /// <inheritdoc />
    public partial class AddExamQuestionSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "ExamQuestion",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                WITH OrderedExamQuestions AS (
                    SELECT
                        ExamId,
                        QuestionId,
                        ROW_NUMBER() OVER (PARTITION BY ExamId ORDER BY QuestionId) AS RowNumber
                    FROM [ExamQuestion]
                )
                UPDATE eq
                SET SortOrder = ordered.RowNumber
                FROM [ExamQuestion] eq
                INNER JOIN OrderedExamQuestions ordered
                    ON ordered.ExamId = eq.ExamId
                    AND ordered.QuestionId = eq.QuestionId;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "ExamQuestion");
        }
    }
}
