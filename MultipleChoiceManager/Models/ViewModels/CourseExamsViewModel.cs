namespace MultipleChoiceManager.Models.ViewModels;

public class CourseExamsViewModel
{
    public int CourseId { get; set; }

    public string CourseTitle { get; set; } = string.Empty;

    public int AvailableQuestionCount { get; set; }

    public List<ExamListItemViewModel> Exams { get; set; } = [];
}
