using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Data;
using System.Text.Json.Serialization;

namespace SofiaTripAdvisor.Services
{
    public class SearchContextService
    {
        private readonly KernelFactory _kernelFactory;
        public SearchContextService(KernelFactory kernelFactory) {
               _kernelFactory = kernelFactory;
        }

        public async Task<SearchContext> GetLocationAsync (string input, CancellationToken ct)
        {
            var kernel = _kernelFactory.CreateKernel();
            var chat = kernel.GetRequiredService<IChatCompletionService>();

            var systemPromt= $$"""
                You are a search parameter extractor for a place recommendation app in Sofia, Bulgaria.
                Analyze the user's request and extract exactly 3 parameters.
                Return ONLY a valid JSON object with no extra text, markdown, or explanation.

                Parameters to extract:

                1. "location" — The specific landmark, street, neighborhood, or area the user mentions.
                   - If a specific location is mentioned, use it exactly (e.g. "Technical University of Sofia")
                   - If only a neighborhood is mentioned, use it (e.g. "Studentski Grad", "Lozenets")
                   - If NO location is mentioned, return "Sofia center"

                2. "placeType" — The type of place the user is looking for.
                   Must be exactly one of these values:
                   restaurant | cafe | bar | park | museum | hotel | gym | bakery | night_club | shopping_mall
                   - If unclear, infer from context (e.g. "eat" → restaurant, "coffee" → cafe, "drink" → bar)
                   - If truly impossible to determine, return "restaurant"

                3. "keywords" — Any extra preferences, mood, or constraints the user mentions.
                   Examples: "cheap", "romantic", "outdoor seating", "quiet", "pet friendly", "open late"
                   - If none mentioned, return an empty string ""

                Response format:
                {
                  "location": "...",
                  "placeType": "...",
                  "keywords": "..."
                }
                """;

            var userPrompt = $"Extract the search parameters from this input: {input}";

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(systemPromt);
            chatHistory.AddUserMessage(userPrompt);

            var response = await chat.GetChatMessageContentAsync(chatHistory, cancellationToken: ct);
            var context = System.Text.Json.JsonSerializer.Deserialize<SearchContext>(response.ToString()) ?? throw new InvalidOperationException("AI returned invalid JSON for SearchContext.");

            return context;
        }
    }

    public record SearchContext(
     [property: JsonPropertyName("location")] string Location,
     [property: JsonPropertyName("placeType")] string PlaceType,
     [property: JsonPropertyName("keywords")] string Keywords
        
     );

}
