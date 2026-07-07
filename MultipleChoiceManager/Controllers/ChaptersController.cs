using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultipleChoiceManager.Data;
using MultipleChoiceManager.Models;
using MultipleChoiceManager.Models.ViewModels;
using MultipleChoiceManager.Services;

namespace MultipleChoiceManager.Controllers;

public class ChaptersController(ApplicationDbContext context, IFileStorageService fileStorage) : Controller
{
    private const long MaxSlidesFileSizeInBytes = 20 * 1024 * 1024;

    private readonly ApplicationDbContext _context = context;

    private readonly IFileStorageService _fileStorage = fileStorage;

    public async Task<IActionResult> Index(int courseId)
    {
        var course = await _context.Courses
            .Include(c => c.Chapters.OrderBy(ch => ch.ChapterNumber))
            .ThenInclude(ch => ch.Questions)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course is null)
        {
            return NotFound();
        }

        // Gespeicherte Blob-URIs nur für die Anzeige durch abrufbare Download-URLs
        // ersetzen (SAS); die Entities sind hier nicht getrackt, es wird nichts gespeichert.
        foreach (var chapter in course.Chapters)
        {
            if (chapter.SlidesBlobUrl is not null)
            {
                chapter.SlidesBlobUrl = _fileStorage.GetDownloadUrl(chapter.SlidesBlobUrl);
            }
        }

        return View(course);
    }

    public async Task<IActionResult> Create(int courseId)
    {
        var course = await _context.Courses
            .Include(c => c.Chapters)
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course is null)
        {
            return NotFound();
        }

        var viewModel = new ChapterFormViewModel
        {
            CourseId = course.Id,
            CourseTitle = course.Title,
            ChapterNumber = course.Chapters.Count == 0
                ? 1
                : course.Chapters.Max(ch => ch.ChapterNumber) + 1
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ChapterFormViewModel viewModel)
    {
        var course = await _context.Courses.FindAsync(viewModel.CourseId);

        if (course is null)
        {
            return NotFound();
        }

        ValidateSlidesFile(viewModel.SlidesFile);
        await ValidateUniqueChapterNumberAsync(viewModel);

        if (!ModelState.IsValid)
        {
            viewModel.CourseTitle = course.Title;
            return View(viewModel);
        }

        var chapter = new Chapter
        {
            Title = viewModel.Title,
            ChapterNumber = viewModel.ChapterNumber,
            CourseId = course.Id
        };

        if (viewModel.SlidesFile is not null)
        {
            chapter.SlidesBlobUrl = await _fileStorage.SaveAsync(viewModel.SlidesFile);
        }

        _context.Chapters.Add(chapter);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { courseId = course.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var chapter = await _context.Chapters
            .Include(ch => ch.Course)
            .FirstOrDefaultAsync(ch => ch.Id == id);

        if (chapter is null)
        {
            return NotFound();
        }

        var viewModel = new ChapterFormViewModel
        {
            Id = chapter.Id,
            CourseId = chapter.CourseId,
            CourseTitle = chapter.Course!.Title,
            Title = chapter.Title,
            ChapterNumber = chapter.ChapterNumber,
            ExistingSlidesUrl = chapter.SlidesBlobUrl is null
                ? null
                : _fileStorage.GetDownloadUrl(chapter.SlidesBlobUrl)
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ChapterFormViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return BadRequest();
        }

        var chapter = await _context.Chapters
            .Include(ch => ch.Course)
            .FirstOrDefaultAsync(ch => ch.Id == id);

        if (chapter is null)
        {
            return NotFound();
        }

        viewModel.CourseId = chapter.CourseId;
        ValidateSlidesFile(viewModel.SlidesFile);
        await ValidateUniqueChapterNumberAsync(viewModel, chapter.Id);

        if (!ModelState.IsValid)
        {
            viewModel.CourseId = chapter.CourseId;
            viewModel.CourseTitle = chapter.Course!.Title;
            viewModel.ExistingSlidesUrl = chapter.SlidesBlobUrl is null
                ? null
                : _fileStorage.GetDownloadUrl(chapter.SlidesBlobUrl);
            return View(viewModel);
        }

        chapter.Title = viewModel.Title;
        chapter.ChapterNumber = viewModel.ChapterNumber;

        if (viewModel.RemoveExistingFile && chapter.SlidesBlobUrl is not null)
        {
            await _fileStorage.DeleteAsync(chapter.SlidesBlobUrl);
            chapter.SlidesBlobUrl = null;
        }

        if (viewModel.SlidesFile is not null && chapter.SlidesBlobUrl is null)
        {
            chapter.SlidesBlobUrl = await _fileStorage.SaveAsync(viewModel.SlidesFile);
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { courseId = chapter.CourseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadSlides(int id, IFormFile? slidesFile)
    {
        var chapter = await _context.Chapters.FindAsync(id);

        if (chapter is null)
        {
            return NotFound();
        }

        if (chapter.SlidesBlobUrl is not null)
        {
            TempData["SlidesUploadInfo"] = "Für dieses Kapitel ist bereits eine PDF-Datei hinterlegt.";
            return RedirectToAction(nameof(Index), new { courseId = chapter.CourseId });
        }

        if (slidesFile is null)
        {
            TempData["SlidesUploadError"] = "Bitte eine PDF-Datei auswählen.";
            return RedirectToAction(nameof(Index), new { courseId = chapter.CourseId });
        }

        ValidateSlidesFile(slidesFile);

        if (!ModelState.IsValid)
        {
            TempData["SlidesUploadError"] = ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .FirstOrDefault() ?? "Die PDF-Datei konnte nicht hochgeladen werden.";

            return RedirectToAction(nameof(Index), new { courseId = chapter.CourseId });
        }

        chapter.SlidesBlobUrl = await _fileStorage.SaveAsync(slidesFile);
        await _context.SaveChangesAsync();

        TempData["SlidesUploadInfo"] = "Die PDF-Datei wurde hochgeladen.";
        return RedirectToAction(nameof(Index), new { courseId = chapter.CourseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        // Fragen samt Prüfungen mitladen, damit EF die ExamQuestion-Join-Zeilen clientseitig
        // löscht (FK ExamQuestion -> Question ist ClientCascade, siehe ApplicationDbContext).
        var chapter = await _context.Chapters
            .Include(ch => ch.Questions).ThenInclude(q => q.Exams)
            .FirstOrDefaultAsync(ch => ch.Id == id);

        if (chapter is null)
        {
            return NotFound();
        }

        var courseId = chapter.CourseId;
        var slidesUrl = chapter.SlidesBlobUrl;

        _context.Chapters.Remove(chapter);
        await _context.SaveChangesAsync();

        if (slidesUrl is not null)
        {
            await _fileStorage.DeleteAsync(slidesUrl);
        }

        return RedirectToAction(nameof(Index), new { courseId });
    }

    private void ValidateSlidesFile(IFormFile? file)
    {
        if (file is null)
        {
            return;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (extension != ".pdf" || file.ContentType != "application/pdf")
        {
            ModelState.AddModelError(nameof(ChapterFormViewModel.SlidesFile),
                "Es können nur PDF-Dateien hochgeladen werden.");
        }

        if (file.Length > MaxSlidesFileSizeInBytes)
        {
            ModelState.AddModelError(nameof(ChapterFormViewModel.SlidesFile),
                "Die Datei darf höchstens 20 MB groß sein.");
        }
    }

    private async Task ValidateUniqueChapterNumberAsync(ChapterFormViewModel viewModel, int? ignoredChapterId = null)
    {
        var chapterNumberExists = await _context.Chapters.AnyAsync(ch =>
            ch.CourseId == viewModel.CourseId
            && ch.ChapterNumber == viewModel.ChapterNumber
            && (!ignoredChapterId.HasValue || ch.Id != ignoredChapterId.Value));

        if (chapterNumberExists)
        {
            ModelState.AddModelError(nameof(ChapterFormViewModel.ChapterNumber),
                "Diese Kapitelnummer ist in der Lehrveranstaltung bereits vergeben.");
        }
    }
}
