using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultipleChoiceManager.Data;
using MultipleChoiceManager.Models;
using MultipleChoiceManager.Models.ViewModels;

namespace MultipleChoiceManager.Controllers;

public class ExamsController(ApplicationDbContext context) : Controller
{
    private readonly ApplicationDbContext _context = context;

    public async Task<IActionResult> Index()
    {
        var courses = await _context.Courses
            .OrderBy(c => c.Title)
            .Select(c => new CourseExamsViewModel
            {
                CourseId = c.Id,
                CourseTitle = c.Title,
                AvailableQuestionCount = c.Chapters.SelectMany(ch => ch.Questions).Count(),
                Exams = c.Exams
                    .OrderBy(e => e.Date)
                    .ThenBy(e => e.Id)
                    .Select(e => new ExamListItemViewModel
                    {
                        Id = e.Id,
                        Date = e.Date,
                        QuestionCount = e.Questions.Count
                    })
                    .ToList()
            })
            .ToListAsync();

        return View(courses);
    }

    public async Task<IActionResult> Create(int courseId)
    {
        var course = await _context.Courses.FindAsync(courseId);

        if (course is null)
        {
            return NotFound();
        }

        var viewModel = new ExamFormViewModel
        {
            CourseId = course.Id,
            Date = DateTime.Today
        };
        PopulateDisplayInfo(viewModel, course, await CountCourseQuestionsAsync(course.Id));

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExamFormViewModel viewModel)
    {
        var course = await _context.Courses.FindAsync(viewModel.CourseId);

        if (course is null)
        {
            return NotFound();
        }

        var availableCount = await CountCourseQuestionsAsync(course.Id);

        if (availableCount == 0)
        {
            ModelState.AddModelError(nameof(ExamFormViewModel.QuestionCount),
                "In dieser Veranstaltung sind noch keine Fragen hinterlegt. Bitte zuerst Fragen anlegen.");
        }

        if (!ModelState.IsValid)
        {
            PopulateDisplayInfo(viewModel, course, availableCount);
            return View(viewModel);
        }

        var requestedCount = viewModel.QuestionCount!.Value;

        if (requestedCount > availableCount && !viewModel.ProceedWithAvailable)
        {
            PopulateDisplayInfo(viewModel, course, availableCount);
            viewModel.ShowAvailabilityWarning = true;
            return View(viewModel);
        }

        // Zufällige Auswahl ohne Duplikate: alle Fragen-Ids der Veranstaltung mischen
        // und die ersten n nehmen (höchstens so viele, wie hinterlegt sind).
        var questionIds = await _context.Questions
            .Where(q => q.Chapter!.CourseId == course.Id)
            .Select(q => q.Id)
            .ToArrayAsync();

        Random.Shared.Shuffle(questionIds);

        var selectedIds = questionIds.Take(Math.Min(requestedCount, questionIds.Length)).ToList();

        var selectedQuestions = await _context.Questions
            .Where(q => selectedIds.Contains(q.Id))
            .ToListAsync();

        var exam = new Exam
        {
            Date = viewModel.Date!.Value,
            CourseId = course.Id,
            Questions = selectedQuestions
        };

        _context.Exams.Add(exam);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = exam.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var exam = await _context.Exams
            .Include(e => e.Course)
            .Include(e => e.Questions.OrderBy(q => q.Id))
            .ThenInclude(q => q.AnswerOptions.OrderBy(a => a.Id))
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exam is null)
        {
            return NotFound();
        }

        return View(exam);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var exam = await _context.Exams.FindAsync(id);

        if (exam is null)
        {
            return NotFound();
        }

        // Die ExamQuestion-Join-Zeilen löscht die DB-Kaskade (FK ExamQuestion -> Exam
        // ist Cascade); die Fragen selbst bleiben unberührt.
        _context.Exams.Remove(exam);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task<int> CountCourseQuestionsAsync(int courseId)
    {
        return await _context.Questions.CountAsync(q => q.Chapter!.CourseId == courseId);
    }

    private static void PopulateDisplayInfo(ExamFormViewModel viewModel, Course course, int availableCount)
    {
        viewModel.CourseTitle = course.Title;
        viewModel.AvailableQuestionCount = availableCount;
    }
}
