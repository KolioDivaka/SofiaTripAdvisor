namespace SofiaTripAdvisor.Services
{
    public class AgentContracts
    {
        public sealed record SuggestedPlace
        {
            string ? Name;
            string ? Description;
            string ? Mood;
            float ? Budget;
            string ? GoogleMapsLink;
        }
    }
}
