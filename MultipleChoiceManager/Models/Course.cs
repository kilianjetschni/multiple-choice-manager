namespace MultipleChoiceManager.Models;

public class Course
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public required string LecturerName { get; set; }

    public CourseLevel Level { get; set; }

    public List<Chapter> Chapters { get; set; } = [];

    public List<Exam> Exams { get; set; } = [];
}
