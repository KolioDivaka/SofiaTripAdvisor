using Microsoft.EntityFrameworkCore;
using SofiaTripAdvisor.Models;
namespace SofiaTripAdvisor.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){ }

        public DbSet<SuggestionSession> SuggestionSessions { get; set; }
        public DbSet<SavedPlace> SavedPlaces { get; set; }

       
    }
}
