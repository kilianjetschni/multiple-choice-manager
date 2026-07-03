using System.ComponentModel.DataAnnotations;

namespace MultipleChoiceManager.Models.ViewModels;

public class CourseFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Bitte einen Titel angeben.")]
    [StringLength(200, ErrorMessage = "Der Titel darf höchstens 200 Zeichen lang sein.")]
    [Display(Name = "Titel")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte einen Dozentennamen angeben.")]
    [StringLength(200, ErrorMessage = "Der Dozentenname darf höchstens 200 Zeichen lang sein.")]
    [Display(Name = "Dozentenname")]
    public string LecturerName { get; set; } = string.Empty;

    [Display(Name = "Niveau")]
    public CourseLevel Level { get; set; }
}
