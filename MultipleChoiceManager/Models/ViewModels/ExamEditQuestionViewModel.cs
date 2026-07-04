namespace MultipleChoiceManager.Models.ViewModels;

public class ExamEditQuestionViewModel
{
    public int Id { get; set; }

    public required string Text { get; set; }

    public int ChapterNumber { get; set; }

    public required string ChapterTitle { get; set; }
}
