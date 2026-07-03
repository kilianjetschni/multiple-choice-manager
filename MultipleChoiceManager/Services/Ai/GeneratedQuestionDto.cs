namespace MultipleChoiceManager.Services.Ai;

public class GeneratedQuestionDto
{
    public string QuestionText { get; set; } = string.Empty;

    public List<GeneratedAnswerOptionDto> Options { get; set; } = [];
}
