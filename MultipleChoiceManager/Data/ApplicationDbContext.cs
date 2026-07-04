using Microsoft.EntityFrameworkCore;
using MultipleChoiceManager.Models;

namespace MultipleChoiceManager.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Course> Courses => Set<Course>();

    public DbSet<Chapter> Chapters => Set<Chapter>();

    public DbSet<Question> Questions => Set<Question>();

    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();

    public DbSet<Exam> Exams => Set<Exam>();

    public DbSet<ExamQuestion> ExamQuestions => Set<ExamQuestion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>(course =>
        {
            course.Property(c => c.Title).HasMaxLength(200);
            course.Property(c => c.LecturerName).HasMaxLength(200);

            course.HasMany(c => c.Chapters)
                .WithOne(ch => ch.Course)
                .HasForeignKey(ch => ch.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            course.HasMany(c => c.Exams)
                .WithOne(e => e.Course)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Chapter>(chapter =>
        {
            chapter.Property(ch => ch.Title).HasMaxLength(200);
            chapter.Property(ch => ch.SlidesBlobUrl).HasMaxLength(500);

            chapter.HasMany(ch => ch.Questions)
                .WithOne(q => q.Chapter)
                .HasForeignKey(q => q.ChapterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Question>(question =>
        {
            question.Property(q => q.Text).HasMaxLength(1000);
            question.Property(q => q.AiReviewResultJson);

            question.HasMany(q => q.AnswerOptions)
                .WithOne(a => a.Question)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            // n:m Frage <-> Prüfung über explizite Join-Entity. Der FK auf Question ist
            // ClientCascade statt Cascade, weil SQL Server sonst zwei Kaskadenpfade
            // (Course->Exam->ExamQuestion und Course->Chapter->Question->ExamQuestion) ablehnt.
            question.HasMany(q => q.Exams)
                .WithMany(e => e.Questions)
                .UsingEntity<ExamQuestion>(
                    join => join.HasOne(eq => eq.Exam)
                        .WithMany()
                        .HasForeignKey(eq => eq.ExamId)
                        .OnDelete(DeleteBehavior.Cascade),
                    join => join.HasOne(eq => eq.Question)
                        .WithMany()
                        .HasForeignKey(eq => eq.QuestionId)
                        .OnDelete(DeleteBehavior.ClientCascade),
                    join =>
                    {
                        join.ToTable("ExamQuestion");
                        join.HasKey(eq => new { eq.ExamId, eq.QuestionId });
                        join.Property(eq => eq.SortOrder);
                    });
        });

        modelBuilder.Entity<AnswerOption>(option =>
        {
            option.Property(a => a.Text).HasMaxLength(1000);
        });
    }
}
