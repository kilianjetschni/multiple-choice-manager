namespace MultipleChoiceManager.Services.Ai;

public class QuestionReviewResultDto
{
    public string OverallVerdict { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public bool HasCriticalIssues { get; set; }

    public List<string> Strengths { get; set; } = [];

    public List<string> Issues { get; set; } = [];

    public List<string> Suggestions { get; set; } = [];
}
