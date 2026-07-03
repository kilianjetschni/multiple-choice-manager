using System.ComponentModel.DataAnnotations;

namespace MultipleChoiceManager.Models.ViewModels;

public class ChapterFormViewModel
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    // Nur zur Anzeige im Formularkopf.
    public string CourseTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte einen Titel angeben.")]
    [StringLength(200, ErrorMessage = "Der Titel darf höchstens 200 Zeichen lang sein.")]
    [Display(Name = "Titel")]
    public string Title { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Die Kapitelnummer muss mindestens 1 sein.")]
    [Display(Name = "Kapitelnummer")]
    public int ChapterNumber { get; set; } = 1;

    [Display(Name = "Vorlesungsfolien (PDF)")]
    public IFormFile? SlidesFile { get; set; }

    // Vorhandene Datei; steuert, ob im Edit-Modus Link + Löschen-Checkbox oder Upload-Feld erscheint.
    public string? ExistingSlidesUrl { get; set; }

    [Display(Name = "Vorhandene Datei löschen")]
    public bool RemoveExistingFile { get; set; }
}
