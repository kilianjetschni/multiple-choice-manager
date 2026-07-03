namespace MultipleChoiceManager.Services.Ai;

public interface IQuestionAiService
{
    Task<GeneratedQuestionDto> GenerateQuestionAsync(
        byte[] pdfBytes,
        string chapterTitle,
        CancellationToken cancellationToken = default);

    Task<string> ReviewQuestionAsync(
        string questionText,
        IEnumerable<string> answerOptions,
        CancellationToken cancellationToken = default);
}
