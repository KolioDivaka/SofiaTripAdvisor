namespace SofiaTripAdvisor.Models
{
    public class SavedPlace
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string? Mood { get; set; }

        public double Rating { get; set; }

        public float? Budget { get; set; }

        public string ? GoogleMapsLink { get; set; }

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

       
        public int SuggestionSessionId { get; set; }
        public SuggestionSession? SuggestionSession { get; set; }

    }
}
