namespace MultipleChoiceManager.Services.Ai;

public interface IQuestionAiService
{
    Task<GeneratedQuestionDto> GenerateQuestionAsync(
        byte[] pdfBytes,
        string chapterTitle,
        CancellationToken cancellationToken = default);

    Task<QuestionReviewResultDto> ReviewQuestionAsync(
        string questionText,
        IEnumerable<AnswerOptionReviewDto> answerOptions,
        CancellationToken cancellationToken = default);
}
