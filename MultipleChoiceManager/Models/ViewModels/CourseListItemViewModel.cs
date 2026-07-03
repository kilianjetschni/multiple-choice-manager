namespace MultipleChoiceManager.Models.ViewModels;

public class CourseListItemViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string LecturerName { get; set; } = string.Empty;

    public CourseLevel Level { get; set; }

    public int ChapterCount { get; set; }
}
