using System.ComponentModel.DataAnnotations;

namespace SofiaTripAdvisor.ViewModels
{
    public class AddSuggestionInput
    {
        [Required(ErrorMessage = "Description is required.")]
        [Display(Name = " I want a place that is...")]
        public string Description { get; set; } = string.Empty;
    }
}
