using System.ComponentModel.DataAnnotations;

namespace MultipleChoiceManager.Models.ViewModels;

public class ExamEditViewModel
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public string CourseTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte ein Prüfungsdatum angeben.")]
    [DataType(DataType.Date)]
    [Display(Name = "Prüfungsdatum")]
    public DateTime? Date { get; set; }

    public List<int> SelectedQuestionIds { get; set; } = [];

    public List<ExamEditQuestionViewModel> SelectedQuestions { get; set; } = [];

    public List<ExamEditQuestionViewModel> AvailableQuestions { get; set; } = [];
}
