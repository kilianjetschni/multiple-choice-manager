using System.ComponentModel.DataAnnotations;

namespace MultipleChoiceManager.Models.ViewModels;

public class AiQuestionProposalViewModel
{
    public int ChapterId { get; set; }

    [Required]
    [StringLength(1000)]
    public string QuestionText { get; set; } = string.Empty;

    public List<AiAnswerOptionProposalViewModel> Options { get; set; } = [];

    [Required(ErrorMessage = "Bitte genau eine Antwortoption als richtig markieren.")]
    [Range(0, QuestionFormViewModel.AnswerOptionCount - 1, ErrorMessage = "Bitte genau eine Antwortoption als richtig markieren.")]
    public int? CorrectOptionIndex { get; set; }
}
