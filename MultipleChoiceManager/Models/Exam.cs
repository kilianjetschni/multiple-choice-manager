namespace MultipleChoiceManager.Models;

public class Exam
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    public List<Question> Questions { get; set; } = [];
}
