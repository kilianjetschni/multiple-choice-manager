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

        var exam = new Exam
        {
            Date = viewModel.Date!.Value,
            CourseId = course.Id
        };

        _context.Exams.Add(exam);

        _context.ExamQuestions.AddRange(selectedIds.Select((questionId, index) => new ExamQuestion
        {
            Exam = exam,
            QuestionId = questionId,
            SortOrder = index + 1
        }));

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = exam.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var exam = await _context.Exams
            .Include(e => e.Course)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exam is null)
        {
            return NotFound();
        }

        var examQuestions = await _context.ExamQuestions
            .Where(eq => eq.ExamId == id)
            .Include(eq => eq.Question!)
            .ThenInclude(q => q.AnswerOptions.OrderBy(a => a.Id))
            .OrderBy(eq => eq.SortOrder)
            .ThenBy(eq => eq.QuestionId)
            .AsNoTracking()
            .ToListAsync();

        exam.Questions = examQuestions
            .Select(eq => eq.Question!)
            .ToList();

        return View(exam);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var viewModel = await BuildEditViewModelAsync(id);

        if (viewModel is null)
        {
            return NotFound();
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ExamEditViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return BadRequest();
        }

        var exam = await _context.Exams
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exam is null)
        {
            return NotFound();
        }

        var selectedQuestionIds = DistinctPreservingOrder(viewModel.SelectedQuestionIds);

        if (selectedQuestionIds.Count == 0)
        {
            ModelState.AddModelError(nameof(ExamEditViewModel.SelectedQuestionIds),
                "Bitte mindestens eine Frage auswählen.");
        }

        var validQuestionIds = await _context.Questions
            .Where(q => selectedQuestionIds.Contains(q.Id) && q.Chapter!.CourseId == exam.CourseId)
            .Select(q => q.Id)
            .ToListAsync();

        if (validQuestionIds.Count != selectedQuestionIds.Count)
        {
            ModelState.AddModelError(nameof(ExamEditViewModel.SelectedQuestionIds),
                "Die Prüfung darf nur Fragen aus der zugehörigen Lehrveranstaltung enthalten.");
        }

        if (!ModelState.IsValid)
        {
            var invalidViewModel = await BuildEditViewModelAsync(id, selectedQuestionIds, viewModel.Date);
            return invalidViewModel is null ? NotFound() : View(invalidViewModel);
        }

        exam.Date = viewModel.Date!.Value;

        var existingExamQuestions = await _context.ExamQuestions
            .Where(eq => eq.ExamId == exam.Id)
            .ToListAsync();

        var selectedQuestionIdSet = selectedQuestionIds.ToHashSet();
        var existingExamQuestionsByQuestionId = existingExamQuestions.ToDictionary(eq => eq.QuestionId);

        _context.ExamQuestions.RemoveRange(existingExamQuestions
            .Where(eq => !selectedQuestionIdSet.Contains(eq.QuestionId)));

        foreach (var (questionId, index) in selectedQuestionIds.Select((questionId, index) => (questionId, index)))
        {
            if (existingExamQuestionsByQuestionId.TryGetValue(questionId, out var existingExamQuestion))
            {
                existingExamQuestion.SortOrder = index + 1;
            }
            else
            {
                _context.ExamQuestions.Add(new ExamQuestion
                {
                    ExamId = exam.Id,
                    QuestionId = questionId,
                    SortOrder = index + 1
                });
            }
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = exam.Id });
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

    private async Task<ExamEditViewModel?> BuildEditViewModelAsync(
        int examId,
        IReadOnlyCollection<int>? selectedQuestionIds = null,
        DateTime? dateOverride = null)
    {
        var exam = await _context.Exams
            .Include(e => e.Course)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == examId);

        if (exam is null)
        {
            return null;
        }

        var orderedSelectedQuestionIds = selectedQuestionIds?.ToList()
            ?? await _context.ExamQuestions
                .Where(eq => eq.ExamId == exam.Id)
                .OrderBy(eq => eq.SortOrder)
                .ThenBy(eq => eq.QuestionId)
                .Select(eq => eq.QuestionId)
                .ToListAsync();

        var selectedIdSet = orderedSelectedQuestionIds.ToHashSet();

        var courseQuestions = await _context.Questions
            .Where(q => q.Chapter!.CourseId == exam.CourseId)
            .Include(q => q.Chapter)
            .OrderBy(q => q.Chapter!.ChapterNumber)
            .ThenBy(q => q.Id)
            .AsNoTracking()
            .Select(q => new ExamEditQuestionViewModel
            {
                Id = q.Id,
                Text = q.Text,
                ChapterNumber = q.Chapter!.ChapterNumber,
                ChapterTitle = q.Chapter.Title
            })
            .ToListAsync();

        var questionById = courseQuestions.ToDictionary(question => question.Id);
        var selectedQuestions = orderedSelectedQuestionIds
            .Where(questionById.ContainsKey)
            .Select(questionId => questionById[questionId])
            .ToList();

        return new ExamEditViewModel
        {
            Id = exam.Id,
            CourseId = exam.CourseId,
            CourseTitle = exam.Course!.Title,
            Date = dateOverride ?? exam.Date,
            SelectedQuestionIds = selectedQuestions.Select(question => question.Id).ToList(),
            SelectedQuestions = selectedQuestions,
            AvailableQuestions = courseQuestions
                .Where(question => !selectedIdSet.Contains(question.Id))
                .ToList()
        };
    }

    private static List<int> DistinctPreservingOrder(IEnumerable<int> questionIds)
    {
        var distinctIds = new List<int>();
        var seenIds = new HashSet<int>();

        foreach (var questionId in questionIds)
        {
            if (seenIds.Add(questionId))
            {
                distinctIds.Add(questionId);
            }
        }

        return distinctIds;
    }

    private static void PopulateDisplayInfo(ExamFormViewModel viewModel, Course course, int availableCount)
    {
        viewModel.CourseTitle = course.Title;
        viewModel.AvailableQuestionCount = availableCount;
    }
}
