namespace SofiaTripAdvisor.Models
{
    public class SuggestionSession
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Mood { get; set; }
        public string? Preferences { get; set; }
        public string UserInput { get; set; } = string.Empty;
        public List<SavedPlace> Places { get; set; } = new();
        
        public string? Location {  get; set; }
        public string? CachePlacesJson { get; set; }
    }
}