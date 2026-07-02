namespace MultipleChoiceManager.Models;

public class Chapter
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public int ChapterNumber { get; set; }

    // Referenz auf die hochgeladene PDF im Azure Blob Storage; null, wenn keine Datei vorhanden ist.
    public string? SlidesBlobUrl { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    public List<Question> Questions { get; set; } = [];
}
