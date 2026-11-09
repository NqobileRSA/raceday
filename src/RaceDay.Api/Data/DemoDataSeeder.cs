using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/// <summary>
/// Adds a few upcoming events (dated relative to today) when the database has none, so the MVC home page
/// always has events to browse in a demo. The Part 1 seed data stays exactly as migrated.
/// Controlled by the DemoData:Enabled setting; off unless configured.
/// </summary>
public static class DemoDataSeeder
{
    public static async Task SeedUpcomingEventsAsync(RaceDayDbContext db, ILogger logger, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (await db.Events.AnyAsync(e => e.EventDate >= today && e.Status == EventStatuses.Published, ct)) return;

        var thabo = await db.Users.FirstOrDefaultAsync(u => u.Email == "thabo.mokoena@raceday.co.za", ct);
        var lerato = await db.Users.FirstOrDefaultAsync(u => u.Email == "lerato.dlamini@raceday.co.za", ct);
        if (thabo is null || lerato is null) return;

        var events = new[]
        {
            NewEvent(thabo.UserId, "Soweto Freedom Run", "A fast, flat community road race through Orlando and past the Hector Pieterson Memorial. Water points every 2.5km and a finish-line festival.",
                today.AddDays(21), 21.1m, "Orlando Stadium, Soweto", "Gauteng", "Running", "run@sowetofreedom.co.za",
                new (string, decimal, decimal, int?, int?)[] { ("21.1km Half Marathon", 21.1m, 220m, 16, (int?)null), ("10km Road Race", 10m, 140m, 12, null), ("5km Fun Run", 5m, 60m, (int?)null, null) },
                (-26.2350m, 27.9050m, -26.2405m, 27.9083m, "Two laps of Orlando West and Orlando East on closed tar roads, finishing inside Orlando Stadium.", 60)),
            NewEvent(lerato.UserId, "Stellenbosch Wine Route Classic", "A scenic cycle through the Cape Winelands with rolling climbs, vineyard views and a sprint finish in the old town.",
                today.AddDays(45), 100m, "Coetzenburg, Stellenbosch", "Western Cape", "Cycling", "ride@winerouteclassic.co.za",
                new (string, decimal, decimal, int?, int?)[] { ("100km Classic", 100m, 480m, 18, (int?)null), ("50km Challenge", 50m, 320m, 16, null) },
                (-33.9420m, 18.8710m, -33.9360m, 18.8610m, "Out via the Helshoogte Pass to Franschhoek and back along the R44. Rolling terrain with one major climb.", 1150)),
            NewEvent(thabo.UserId, "Durban Golden Mile 10k", "Sunrise 10k along the beachfront promenade from uShaka to the Moses Mabhida Stadium and back.",
                today.AddDays(75), 10m, "uShaka Marine World, Durban", "KwaZulu-Natal", "Running", "info@goldenmile10k.co.za",
                new (string, decimal, decimal, int?, int?)[] { ("10km Open", 10m, 150m, 16, (int?)null), ("Under 20 - 10km", 10m, 80m, 12, 19), ("5km Promenade Walk", 5m, 50m, (int?)null, null) },
                (-29.8680m, 31.0450m, -29.8290m, 31.0300m, "Flat out-and-back on the promenade. Sea breeze, very fast course.", 15)),
        };

        db.Events.AddRange(events);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Demo data: added {Count} upcoming events.", events.Length);
    }

    private static Event NewEvent(int organiserId, string title, string description, DateOnly date, decimal distance,
        string location, string province, string type, string email,
        (string Name, decimal Km, decimal Fee, int? MinAge, int? MaxAge)[] categories,
        (decimal Lat1, decimal Lon1, decimal Lat2, decimal Lon2, string Description, int Elevation) route)
    {
        var ev = new Event
        {
            OrganiserId = organiserId, Title = title, Description = description, EventDate = date, DistanceKm = distance,
            Location = location, Province = province, EventType = type, Status = EventStatuses.Published, ContactEmail = email,
            CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now,
            Route = new EventRoute
            {
                StartLatitude = route.Lat1, StartLongitude = route.Lon1, EndLatitude = route.Lat2, EndLongitude = route.Lon2,
                Description = route.Description, ElevationGainM = route.Elevation, TerrainType = "Road"
            }
        };
        foreach (var c in categories)
            ev.Categories.Add(new Category { Name = c.Name, DistanceKm = c.Km, EntryFee = c.Fee, MaxParticipants = 500, MinAge = c.MinAge, MaxAge = c.MaxAge });
        return ev;
    }
}
