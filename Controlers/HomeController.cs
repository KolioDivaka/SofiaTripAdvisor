using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using SofiaTripAdvisor.Data;
using SofiaTripAdvisor.Models;
using SofiaTripAdvisor.Services;
using SofiaTripAdvisor.ViewModels;  
using System.Numerics;
using System.Text.Json;
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

        [HttpPost]
        public async Task<IActionResult> Index()
        {
             return View(new SuggestionViewModel());
        }
        [HttpPost]
        public async Task<IActionResult> GetSuggestions(AddSuggestionInput input, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                var viewModel = new SuggestionViewModel
                {
                    Input = input,
                    Sessions = await _db.SuggestionSessions
                    .Include(s => s.Places)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync(ct)
                };

                return View("Index", viewModel);
            }

            var searchContext = await _searchContextService.GetLocationAsync(input.Description, ct);

            var cordinates = await _geocodingService.GetCoordinatesAsync(searchContext.Location, ct);

            var places = await _getNearbyPlacesService.GetNearbyPlacesAsync(cordinates.Latitude, cordinates.Longitude, searchContext.PlaceType,searchContext.Keywords, ct);

            var session = new SuggestionSession
            {
                UserInput = input.Description,
                Mood = searchContext.Keywords,
                Preferences = searchContext.PlaceType,
                CreatedAt = DateTime.UtcNow,
                Location = searchContext.Location
            };

            session.CachePlacesJson = JsonSerializer.Serialize(places);

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

        [HttpPost]
        public async Task<IActionResult> Regenerate(int sessionId, CancellationToken ct)
        {
            var session = await _db.SuggestionSessions.Include(s => s.Places)
                .FirstOrDefaultAsync(s => s.Id == sessionId);
            if(session == null)
            {
                return NotFound();
            }

            var places = JsonSerializer.Deserialize<List<PlaceResult>>(session.CachePlacesJson ?? "[]") ?? new List<PlaceResult>();

            var searchContext = new SearchContext(
                session.Location ?? string.Empty,
                session.Preferences ?? string.Empty,
                session.Mood ?? string.Empty
            );

            var exludedNames = session.Places.Select(p => p.Name).ToList();

            var newSuggestions = await _suggestionAgent.GetSuggestionsAsync(places, searchContext, session.Id, ct, exludedNames);

            _db.SavedPlaces.RemoveRange(session.Places);
            _db.SavedPlaces.AddRange(newSuggestions);
            await _db.SaveChangesAsync(ct);

            return RedirectToAction("Index");

        }

        [HttpPost]
        public async Task<IActionResult> ClearHistory(CancellationToken ct)
        {
            var seesions = await _db.SuggestionSessions.Include(s => s.Places).ToListAsync(ct);

            _db.SavedPlaces.RemoveRange(seesions.SelectMany(s => s.Places));
            _db.SuggestionSessions.RemoveRange(seesions);
            await _db.SaveChangesAsync(ct);
            return RedirectToAction("Index");
        }
    }
}
