namespace MultipleChoiceManager.Services.Ai;

public class GeneratedQuestionDto
{
    public bool CanCreate { get; set; }

    public string RejectionReason { get; set; } = string.Empty;

    public string QuestionText { get; set; } = string.Empty;

    public List<GeneratedAnswerOptionDto> Options { get; set; } = [];
}
