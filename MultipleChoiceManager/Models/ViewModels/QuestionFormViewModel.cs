using System.ComponentModel.DataAnnotations;

namespace MultipleChoiceManager.Models.ViewModels;

public class QuestionFormViewModel
{
    // Laut Lastenheft hat jede Frage genau vier Antwortoptionen.
    public const int AnswerOptionCount = 4;

    public int Id { get; set; }

    public int ChapterId { get; set; }

    // Nur zur Anzeige im Formularkopf.
    public string ChapterTitle { get; set; } = string.Empty;

    public string CourseTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte einen Fragentext angeben.")]
    [StringLength(1000, ErrorMessage = "Der Fragentext darf höchstens 1000 Zeichen lang sein.")]
    [Display(Name = "Fragentext")]
    public string Text { get; set; } = string.Empty;

    public List<AnswerOptionInputViewModel> Options { get; set; } =
        [.. Enumerable.Range(0, AnswerOptionCount).Select(_ => new AnswerOptionInputViewModel())];

    // Index der als richtig markierten Option; null, solange keine ausgewählt ist.
    [Required(ErrorMessage = "Bitte genau eine Antwortoption als richtig markieren.")]
    [Range(0, AnswerOptionCount - 1, ErrorMessage = "Bitte genau eine Antwortoption als richtig markieren.")]
    [Display(Name = "Richtige Antwort")]
    public int? CorrectOptionIndex { get; set; }
}
