using System.ComponentModel.DataAnnotations;

namespace MultipleChoiceManager.Models.ViewModels;

public class AiAnswerOptionProposalViewModel
{
    [Required]
    [StringLength(500)]
    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }
}
