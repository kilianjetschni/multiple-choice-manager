using System.ComponentModel.DataAnnotations;

namespace MultipleChoiceManager.Models.ViewModels;

public class AnswerOptionInputViewModel
{
    // Id der bestehenden Antwortoption; 0 bei neu angelegten Optionen.
    public int Id { get; set; }

    [Required(ErrorMessage = "Bitte einen Antworttext angeben.")]
    [StringLength(1000, ErrorMessage = "Der Antworttext darf höchstens 1000 Zeichen lang sein.")]
    public string Text { get; set; } = string.Empty;
}
