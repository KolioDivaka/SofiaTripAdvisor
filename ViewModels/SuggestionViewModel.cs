using SofiaTripAdvisor.Models;

namespace SofiaTripAdvisor.ViewModels
{
    public class SuggestionViewModel
    {
        public List<SuggestionSession> Sessions { get; set; } = new();
        public AddSuggestionInput Input { get; set; } = new();
    }
}
