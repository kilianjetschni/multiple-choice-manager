using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultipleChoiceManager.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueChapterNumberPerCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Chapters_CourseId",
                table: "Chapters");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_CourseId_ChapterNumber",
                table: "Chapters",
                columns: new[] { "CourseId", "ChapterNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Chapters_CourseId_ChapterNumber",
                table: "Chapters");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_CourseId",
                table: "Chapters",
                column: "CourseId");
        }
    }
}
