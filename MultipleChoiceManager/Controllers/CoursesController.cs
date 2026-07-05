using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultipleChoiceManager.Data;
using MultipleChoiceManager.Models;
using MultipleChoiceManager.Models.ViewModels;
using MultipleChoiceManager.Services;

namespace MultipleChoiceManager.Controllers;

public class CoursesController(ApplicationDbContext context, IFileStorageService fileStorage) : Controller
{
    private readonly ApplicationDbContext _context = context;

    private readonly IFileStorageService _fileStorage = fileStorage;

    public async Task<IActionResult> Index()
    {
        var courses = await _context.Courses
            .OrderBy(c => c.Title)
            .Select(c => new CourseListItemViewModel
            {
                Id = c.Id,
                Title = c.Title,
                LecturerName = c.LecturerName,
                Level = c.Level,
                ChapterCount = c.Chapters.Count,
                ExamCount = c.Exams.Count
            })
            .ToListAsync();

        return View(courses);
    }

    public IActionResult Create()
    {
        return View(new CourseFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CourseFormViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var course = new Course
        {
            Title = viewModel.Title,
            LecturerName = viewModel.LecturerName,
            Level = viewModel.Level
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var course = await _context.Courses.FindAsync(id);

        if (course is null)
        {
            return NotFound();
        }

        var viewModel = new CourseFormViewModel
        {
            Id = course.Id,
            Title = course.Title,
            LecturerName = course.LecturerName,
            Level = course.Level
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CourseFormViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var course = await _context.Courses.FindAsync(id);

        if (course is null)
        {
            return NotFound();
        }

        course.Title = viewModel.Title;
        course.LecturerName = viewModel.LecturerName;
        course.Level = viewModel.Level;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        // Exams samt Fragen mitladen, damit EF die ExamQuestion-Join-Zeilen clientseitig
        // löscht (FK ExamQuestion -> Question ist ClientCascade, siehe ApplicationDbContext).
        var course = await _context.Courses
            .Include(c => c.Chapters)
            .Include(c => c.Exams).ThenInclude(e => e.Questions)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course is null)
        {
            return NotFound();
        }

        var slidesUrls = course.Chapters
            .Where(ch => ch.SlidesBlobUrl is not null)
            .Select(ch => ch.SlidesBlobUrl!)
            .ToList();

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();

        foreach (var slidesUrl in slidesUrls)
        {
            await _fileStorage.DeleteAsync(slidesUrl);
        }

        return RedirectToAction(nameof(Index));
    }
}
