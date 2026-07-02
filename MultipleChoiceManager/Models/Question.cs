namespace MultipleChoiceManager.Models;

public class Question
{
    public int Id { get; set; }

    public required string Text { get; set; }

    public int ChapterId { get; set; }

    public Chapter? Chapter { get; set; }

    public List<AnswerOption> AnswerOptions { get; set; } = [];

    public List<Exam> Exams { get; set; } = [];
}
