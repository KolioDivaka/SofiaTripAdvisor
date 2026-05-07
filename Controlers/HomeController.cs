using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SofiaTripAdvisor.ViewModels;  
using SofiaTripAdvisor.Services;
using SofiaTripAdvisor.Data;
using SofiaTripAdvisor.Models;
namespace SofiaTripAdvisor.Controlers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        private readonly SuggestionAgent _suggestionAgent;
        private readonly GetNearbyPlacesService _getNearbyPlacesService;
        private readonly SearchContextService _searchContextService;
        private readonly GeocodingService _geocodingService;

        public HomeController(AppDbContext db, SuggestionAgent suggestionAgent, GetNearbyPlacesService getNearbyPlacesService, SearchContextService searchContextService, GeocodingService geocodingService)
        {
            _db = db;
            _suggestionAgent = suggestionAgent;
            _getNearbyPlacesService = getNearbyPlacesService;
            _searchContextService = searchContextService;
            _geocodingService = geocodingService;
        }
        public IActionResult Index()
        {
            return View(new AddSuggestionInput());
        }

        [HttpPost]
        public async Task<IActionResult> GetSuggestions(AddSuggestionInput input, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", input);
            }

            var searchContext = await _searchContextService.GetLocationAsync(input.Description, ct);

            var cordinates = await _geocodingService.GetCoordinatesAsync(searchContext.Location, ct);

            var places = await _getNearbyPlacesService.GetNearbyPlacesAsync(cordinates.Latitude, cordinates.Longitude, searchContext.PlaceType,searchContext.Keywords, ct);

            var session = new SuggestionSession
            {
                UserInput = input.Description,
                Mood = searchContext.Keywords,
                Preferences = searchContext.PlaceType,
                CreatedAt = DateTime.UtcNow
            };

            _db.SuggestionSessions.Add(session);
            await _db.SaveChangesAsync(ct);

            var suggestions = await _suggestionAgent.GetSuggestionsAsync(places, searchContext,session.Id, ct);
            _db.SavedPlaces.AddRange(suggestions);
            await _db.SaveChangesAsync(ct);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var sessions = await _db.SuggestionSessions
                .Include(s => s.Places)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync(ct);

            return View(new SuggestionViewModel { Sessions = sessions });
        }
    }
}
