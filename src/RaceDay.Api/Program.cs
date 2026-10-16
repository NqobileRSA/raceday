using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------- Controllers and JSON ----------------
// snake_case JSON (first_name, event_date, ...) to match the Part 1 endpoint plan request bodies.
builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        o.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
    });
builder.Services.AddProblemDetails();

// ---------------- Database (EF Core, Code-First) ----------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing from appsettings.json.");
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(3)));

// ---------------- Session-based authentication ----------------
// The session lives on the server (in memory). The browser or MVC client only holds an opaque cookie.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(builder.Configuration.GetValue("Session:IdleTimeoutMinutes", 30));
});

// ---------------- Application services ----------------
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddHttpClient<IWeatherService, OpenMeteoWeatherService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Weather:BaseUrl"] ?? "https://api.open-meteo.com/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

// ---------------- Swagger ----------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description =
            "RESTful API for RaceDay, the event management platform for the South African road running, walking and cycling community.\n\n" +
            "**How to authenticate in Swagger:** call `POST /api/auth/login` (for example `thabo.mokoena@raceday.co.za` / `Password123!` " +
            "for an Organiser, or `sipho.ndlovu@gmail.com` / `Password123!` for a Participant). The server stores your user ID and role in a " +
            "session and returns a session cookie, which your browser then sends automatically with every request you try out. " +
            "Call `POST /api/auth/logout` to end the session.\n\n" +
            "**Role key:** endpoints return `401` when you are not logged in and `403` when your role is not allowed."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// ---------------- Apply migrations on startup ----------------
if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        if (db.Database.GetMigrations().Any())
        {
            db.Database.Migrate();
            logger.LogInformation("Database is up to date.");
        }
        else
        {
            logger.LogWarning("No EF Core migrations found. Run: dotnet ef migrations add InitialCreate --project src/RaceDay.Api");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Could not apply migrations. Check the DefaultConnection string and that SQL Server is running.");
    }
}

// ---------------- HTTP pipeline ----------------
app.UseExceptionHandler();
app.UseStatusCodePages();

// Swagger is enabled in every environment so the marker can test all endpoints in the browser.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
    c.DocumentTitle = "RaceDay API";
    c.DisplayRequestDuration();
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseSession();
app.MapControllers();
app.MapGet("/", () => Microsoft.AspNetCore.Http.Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();

/// <summary>Exposed so integration tests can reference the entry point.</summary>
public partial class Program { }
