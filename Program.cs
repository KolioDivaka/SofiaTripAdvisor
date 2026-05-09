using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SofiaTripAdvisor.Data;
using SofiaTripAdvisor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
// Enable MVC controllers with views so the HomeController and Views/Home/* are reachable
builder.Services.AddControllersWithViews();

// Register KernelFactory as a singleton service
builder.Services.AddSingleton<KernelFactory>();
builder.Services.AddScoped<SuggestionAgent>();
builder.Services.AddScoped<SearchContextService>();

//Configuration of the SqlServer and a InMemoryDb fallback 
var connectionString = builder.Configuration.GetConnectionString("AppDbContext");
if (!connectionString.IsNullOrEmpty()) {
    builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(connectionString));
}
else
{
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("TripAdvisorDb"));
}


//Google APIs
builder.Services.AddScoped<SearchContextService>();
builder.Services.AddHttpClient<GeocodingService>();
builder.Services.AddHttpClient<GetNearbyPlacesService>();



var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if(!db.Database.IsInMemory())
    {
        db.Database.Migrate();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

// Map controller routes for HomeController and other MVC controllers
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages()
   .WithStaticAssets();

// Fallback to Home controller for unmatched routes (helps when root may not resolve)
app.MapFallbackToController("Index", "Home");

app.Run();
