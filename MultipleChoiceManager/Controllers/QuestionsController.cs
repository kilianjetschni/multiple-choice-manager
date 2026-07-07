using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultipleChoiceManager.Data;
using MultipleChoiceManager.Models;
using MultipleChoiceManager.Models.ViewModels;
using MultipleChoiceManager.Services;
using MultipleChoiceManager.Services.Ai;

namespace MultipleChoiceManager.Controllers;

public class QuestionsController(
    ApplicationDbContext context,
    IFileStorageService fileStorage,
    IQuestionAiService questionAiService) : Controller
{
    private const string GenericAiErrorMessage = "Es ist ein Fehler aufgetreten, bitte versuche es erneut.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ApplicationDbContext _context = context;

    private readonly IFileStorageService _fileStorage = fileStorage;

    private readonly IQuestionAiService _questionAiService = questionAiService;

    public async Task<IActionResult> Index(int chapterId)
    {
        var chapter = await _context.Chapters
            .Include(ch => ch.Course)
            .Include(ch => ch.Questions.OrderBy(q => q.Id))
            .ThenInclude(q => q.AnswerOptions.OrderBy(a => a.Id))
            .AsNoTracking()
            .FirstOrDefaultAsync(ch => ch.Id == chapterId);

        if (chapter is null)
        {
            return NotFound();
        }

        return View(chapter);
    }

    public async Task<IActionResult> Create(int chapterId)
    {
        var chapter = await _context.Chapters
            .Include(ch => ch.Course)
            .AsNoTracking()
            .FirstOrDefaultAsync(ch => ch.Id == chapterId);

        if (chapter is null)
        {
            return NotFound();
        }

        var viewModel = new QuestionFormViewModel
        {
            ChapterId = chapter.Id
        };
        PopulateDisplayInfo(viewModel, chapter);

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(QuestionFormViewModel viewModel)
    {
        if (viewModel.Options.Count != QuestionFormViewModel.AnswerOptionCount)
        {
            return BadRequest();
        }

        var chapter = await _context.Chapters
            .Include(ch => ch.Course)
            .FirstOrDefaultAsync(ch => ch.Id == viewModel.ChapterId);

        if (chapter is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            PopulateDisplayInfo(viewModel, chapter);
            return View(viewModel);
        }

        var question = new Question
        {
            Text = viewModel.Text,
            ChapterId = chapter.Id
        };

        for (var i = 0; i < viewModel.Options.Count; i++)
        {
            question.AnswerOptions.Add(new AnswerOption
            {
                Text = viewModel.Options[i].Text,
                IsCorrect = i == viewModel.CorrectOptionIndex
            });
        }

        _context.Questions.Add(question);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { chapterId = chapter.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var question = await _context.Questions
            .Include(q => q.Chapter!).ThenInclude(ch => ch.Course)
            .Include(q => q.AnswerOptions.OrderBy(a => a.Id))
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question is null)
        {
            return NotFound();
        }

        var correctIndex = question.AnswerOptions.FindIndex(a => a.IsCorrect);

        var viewModel = new QuestionFormViewModel
        {
            Id = question.Id,
            ChapterId = question.ChapterId,
            Text = question.Text,
            Options = [.. question.AnswerOptions
                .Select(a => new AnswerOptionInputViewModel { Id = a.Id, Text = a.Text })],
            CorrectOptionIndex = correctIndex >= 0 ? correctIndex : null
        };

        // Sicherheitsnetz für Altdaten mit weniger als vier Optionen.
        while (viewModel.Options.Count < QuestionFormViewModel.AnswerOptionCount)
        {
            viewModel.Options.Add(new AnswerOptionInputViewModel());
        }

        PopulateDisplayInfo(viewModel, question.Chapter!);

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, QuestionFormViewModel viewModel)
    {
        if (id != viewModel.Id || viewModel.Options.Count != QuestionFormViewModel.AnswerOptionCount)
        {
            return BadRequest();
        }

        var question = await _context.Questions
            .Include(q => q.Chapter!).ThenInclude(ch => ch.Course)
            .Include(q => q.AnswerOptions.OrderBy(a => a.Id))
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            viewModel.ChapterId = question.ChapterId;
            PopulateDisplayInfo(viewModel, question.Chapter!);
            return View(viewModel);
        }

        question.Text = viewModel.Text;
        question.AiReviewResultJson = null;
        question.AiReviewedAtUtc = null;

        // Bestehende Optionen anhand der mitgesendeten Ids aktualisieren; Optionen ohne
        // bekannte Id werden neu angelegt, nicht mehr enthaltene entfernt EF als Waisen.
        var existingOptions = question.AnswerOptions.ToDictionary(a => a.Id);
        var keptOptionIds = new HashSet<int>();

        for (var i = 0; i < viewModel.Options.Count; i++)
        {
            var input = viewModel.Options[i];
            var isCorrect = i == viewModel.CorrectOptionIndex;

            if (existingOptions.TryGetValue(input.Id, out var option))
            {
                option.Text = input.Text;
                option.IsCorrect = isCorrect;
                keptOptionIds.Add(option.Id);
            }
            else
            {
                question.AnswerOptions.Add(new AnswerOption { Text = input.Text, IsCorrect = isCorrect });
            }
        }

        question.AnswerOptions.RemoveAll(a => a.Id != 0 && !keptOptionIds.Contains(a.Id));

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { chapterId = question.ChapterId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        // Prüfungen mitladen, damit EF die ExamQuestion-Join-Zeilen clientseitig löscht
        // (FK ExamQuestion -> Question ist ClientCascade, siehe ApplicationDbContext).
        var question = await _context.Questions
            .Include(q => q.Exams)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question is null)
        {
            return NotFound();
        }

        var chapterId = question.ChapterId;

        _context.Questions.Remove(question);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { chapterId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(int chapterId, CancellationToken cancellationToken)
    {
        var chapter = await _context.Chapters
            .FirstOrDefaultAsync(ch => ch.Id == chapterId, cancellationToken);

        if (chapter is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(chapter.SlidesBlobUrl))
        {
            TempData["AiError"] = "Für dieses Kapitel ist keine PDF-Datei hinterlegt.";
            return RedirectToAction(nameof(Index), new { chapterId });
        }

        try
        {
            var pdfBytes = await _fileStorage.ReadAsync(chapter.SlidesBlobUrl);
            var generatedQuestion = await _questionAiService.GenerateQuestionAsync(
                pdfBytes,
                chapter.Title,
                cancellationToken);

            var proposal = new AiQuestionProposalViewModel
            {
                ChapterId = chapter.Id,
                QuestionText = generatedQuestion.QuestionText,
                Options = [.. generatedQuestion.Options.Select(option => new AiAnswerOptionProposalViewModel
                {
                    Text = option.AnswerText,
                    IsCorrect = option.IsCorrect
                })],
                CorrectOptionIndex = generatedQuestion.Options.FindIndex(option => option.IsCorrect)
            };

            TempData["AiQuestionProposal"] = JsonSerializer.Serialize(proposal, JsonOptions);
            TempData["AiSuccess"] = "Die KI hat einen Fragenvorschlag erzeugt. Bitte prüfe und bestätige ihn.";
        }
        catch (Exception ex) when (ex is InvalidOperationException or FileNotFoundException or HttpRequestException or ArgumentException)
        {
            TempData["AiError"] = GenericAiErrorMessage;
        }

        return RedirectToAction(nameof(Index), new { chapterId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmGenerated(AiQuestionProposalViewModel viewModel, CancellationToken cancellationToken)
    {
        var chapter = await _context.Chapters
            .FirstOrDefaultAsync(ch => ch.Id == viewModel.ChapterId, cancellationToken);

        if (chapter is null)
        {
            return NotFound();
        }

        if (viewModel.Options.Count != QuestionFormViewModel.AnswerOptionCount)
        {
            TempData["AiError"] = "Der KI-Vorschlag enthält nicht genau vier Antwortoptionen und wurde nicht gespeichert.";
            return RedirectToAction(nameof(Index), new { chapterId = viewModel.ChapterId });
        }

        if (viewModel.CorrectOptionIndex is null)
        {
            TempData["AiError"] = "Bitte markiere genau eine Antwort als richtig.";
            return RedirectToAction(nameof(Index), new { chapterId = viewModel.ChapterId });
        }

        if (!ModelState.IsValid)
        {
            TempData["AiError"] = "Der KI-Vorschlag ist unvollständig und wurde nicht gespeichert.";
            return RedirectToAction(nameof(Index), new { chapterId = viewModel.ChapterId });
        }

        var question = new Question
        {
            ChapterId = chapter.Id,
            Text = viewModel.QuestionText,
            AnswerOptions = [.. viewModel.Options.Select((option, index) => new AnswerOption
            {
                Text = option.Text,
                IsCorrect = index == viewModel.CorrectOptionIndex
            })]
        };

        _context.Questions.Add(question);
        await _context.SaveChangesAsync(cancellationToken);

        TempData["AiSuccess"] = "Die KI-Frage wurde gespeichert.";

        return RedirectToAction(nameof(Index), new { chapterId = viewModel.ChapterId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int id, CancellationToken cancellationToken)
    {
        var question = await _context.Questions
            .Include(q => q.AnswerOptions.OrderBy(a => a.Id))
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

        if (question is null)
        {
            return NotFound();
        }

        try
        {
            var review = await _questionAiService.ReviewQuestionAsync(
                question.Text,
                question.AnswerOptions.Select(a => new AnswerOptionReviewDto
                {
                    Text = a.Text,
                    IsCorrect = a.IsCorrect
                }),
                cancellationToken);

            question.AiReviewResultJson = JsonSerializer.Serialize(review, JsonOptions);
            question.AiReviewedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            TempData["AiReviewQuestionId"] = question.Id.ToString();
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or ArgumentException)
        {
            TempData["AiError"] = GenericAiErrorMessage;
        }

        return RedirectToAction(nameof(Index), new { chapterId = question.ChapterId });
    }

    private static void PopulateDisplayInfo(QuestionFormViewModel viewModel, Chapter chapter)
    {
        viewModel.ChapterTitle = $"{chapter.ChapterNumber}. {chapter.Title}";
        viewModel.CourseTitle = chapter.Course?.Title ?? string.Empty;
    }
}
