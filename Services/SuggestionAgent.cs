using Microsoft.AspNetCore.Http.Features;
using Microsoft.SemanticKernel.ChatCompletion;
using SofiaTripAdvisor.Models;
using System.Text.Json;
using static SofiaTripAdvisor.ViewModels.AddSuggestionInput;

namespace SofiaTripAdvisor.Services
{
    public class SuggestionAgent
    {
        private readonly KernelFactory _kerenelFactory;

        public SuggestionAgent(KernelFactory kerenelFactory)
        {
            _kerenelFactory = kerenelFactory;
        }

        public async Task<List<SavedPlace>> GetSuggestionsAsync(List<PlaceResult> places, SearchContext context, int sessionId, CancellationToken ct) {
            
            var kernel = _kerenelFactory.CreateKernel();
            var chat = kernel.GetRequiredService<IChatCompletionService>();

            var placesText = string.Join("\n", places.Select((p, i) =>
             $"{i + 1}. {p.Name} | Rating: {p.Rating} ({p.UserRatingsTotal} reviews) | " +
             $"Address: {p.Address} | Price: {FormatPrice(p.PriceLevel)} | " +
             $"Open now: {(p.OpenNow ? "Yes" : "No")}"));

            var systemPrompt = """
            You are a local place recommendation assistant for Sofia, Bulgaria.
            Pick the 3 BEST places from the list based on the user's preferences.
        
            Ranking criteria:
            1. Relevance to user's keywords/preferences
            2. Rating combined with number of reviews
            3. Currently open
            4. Price level matches implied budget from keywords
        
            Return ONLY a valid JSON array with exactly 3 objects. No extra text.
        
            Format:
            [
              {
                "name": "Place Name",
                "description": "1-2 sentences on why this place fits the user's request",
                "mood": "the vibe/mood of this place (e.g. cozy, lively, romantic)",
                "budget": <price_level as float, -1 if unknown>,
                "rating": <rating as double>,
                "googleMapsLink": "https://www.google.com/maps/search/?api=1&query=Place+Name+Sofia"
              }
            ]
            """;

           var userPrompt = $"""
            User was looking for: {context.PlaceType} near {context.Location}
            User preferences: {context.Keywords}
        
            Available places:
            {placesText}
        
            Return the 3 best as a JSON array.
            """;

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(systemPrompt);
            chatHistory.AddSystemMessage(userPrompt);
           
            var response = await chat.GetChatMessageContentAsync(chatHistory, cancellationToken: ct);
            var json = response.Content ?? "[]";

            var items = new List<SavedPlace>();

            using var doc = JsonDocument.Parse(json);

            if( doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach( var elements in doc.RootElement.EnumerateArray())
                {
                    items.Add(new SavedPlace
                    {
                        Id = Guid.NewGuid(),
                        Name = elements.GetProperty("name").GetString() ?? "Unknown",
                        Description = elements.GetProperty("description").GetString() ?? "",
                        Mood = elements.GetProperty("mood").GetString(),
                        Budget = elements.GetProperty("budget").GetSingle() is var b && b >= 0 ? b : null,
                        Rating = elements.GetProperty("rating").GetDouble(),
                        GoogleMapsLink = elements.GetProperty("googleMapsLink").GetString(),
                        CreatedUtc = DateTime.UtcNow,
                        SuggestionSessionId = sessionId
                    });
                }
            }

            return items.OrderByDescending(p => p.Rating).Take(3).ToList();
            

        }



        private static string FormatPrice(int level) => level switch
        {
            0 => "Free",
            1 => "Inexpensive (€)",
            2 => "Moderate (€€)",
            3 => "Expensive (€€€)",
            4 => "Very Expensive (€€€€)",
            _ => "Price not available"
        };
    }

}
