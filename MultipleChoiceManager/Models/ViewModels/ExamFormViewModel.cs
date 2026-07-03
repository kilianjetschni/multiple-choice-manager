using System.ComponentModel.DataAnnotations;

namespace MultipleChoiceManager.Models.ViewModels;

public class ExamFormViewModel
{
    public int CourseId { get; set; }

    // Nur Anzeige, wird serverseitig neu befüllt.
    public string CourseTitle { get; set; } = string.Empty;

    // Anzahl der in der Veranstaltung hinterlegten Fragen; wird serverseitig
    // aus der Datenbank ermittelt und nie aus dem Formular übernommen.
    public int AvailableQuestionCount { get; set; }

    // Steuert den Warnhinweis, wenn mehr Fragen angefordert als hinterlegt sind.
    public bool ShowAvailabilityWarning { get; set; }

    [Required(ErrorMessage = "Bitte ein Prüfungsdatum angeben.")]
    [DataType(DataType.Date)]
    [Display(Name = "Prüfungsdatum")]
    public DateTime? Date { get; set; }

    [Required(ErrorMessage = "Bitte angeben, wie viele Fragen generiert werden sollen.")]
    [Range(1, 1000, ErrorMessage = "Die Anzahl muss zwischen 1 und 1000 liegen.")]
    [Display(Name = "Anzahl der Fragen")]
    public int? QuestionCount { get; set; }

    // Wird über den Bestätigungs-Button im Warnhinweis gesetzt: trotz zu hoher
    // Wunschanzahl mit allen verfügbaren Fragen fortfahren.
    public bool ProceedWithAvailable { get; set; }
}
