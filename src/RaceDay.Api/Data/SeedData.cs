using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/// <summary>
/// The same seed rows as the Part 1 SQL script, applied through EF Core HasData so the
/// Code-First migration produces a database that is ready to demo.
/// Every seeded account uses the password: Password123!
/// </summary>
public static class SeedData
{
    /// <summary>
    /// ASP.NET Core Identity PasswordHasher (PBKDF2, salted) hash of "Password123!".
    /// Replaces the placeholder bcrypt string from Part 1 so the seeded accounts can actually log in.
    /// </summary>
    public const string DemoPasswordHash =
        "AQAAAAIAAYagAAAAEM3yVBdri1JnLOwPQYTjOeQevMHqGYNhUYdxMp9joXYol06nEC2EY4Oj8Ot0Fl7yOQ==";

    private static readonly DateTime SeededAt = new(2026, 7, 1, 8, 0, 0);

    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasData(
            NewUser(1, "thabo.mokoena@raceday.co.za", "Thabo", "Mokoena", Roles.Organiser, "082 555 0101", new DateOnly(1985, 3, 15), "Male", "Gauteng"),
            NewUser(2, "lerato.dlamini@raceday.co.za", "Lerato", "Dlamini", Roles.Organiser, "083 555 0202", new DateOnly(1990, 7, 22), "Female", "Western Cape"),
            NewUser(3, "sipho.ndlovu@gmail.com", "Sipho", "Ndlovu", Roles.Participant, "071 555 0303", new DateOnly(1995, 11, 8), "Male", "KwaZulu-Natal"),
            NewUser(4, "naledi.khumalo@gmail.com", "Naledi", "Khumalo", Roles.Participant, "072 555 0404", new DateOnly(1998, 2, 14), "Female", "Gauteng"),
            NewUser(5, "james.venter@outlook.com", "James", "Venter", Roles.Participant, "082 555 0505", new DateOnly(1988, 9, 30), "Male", "Western Cape"),
            NewUser(6, "amahle.zulu@yahoo.com", "Amahle", "Zulu", Roles.Participant, "061 555 0606", new DateOnly(2000, 6, 18), "Female", "Eastern Cape"));

        modelBuilder.Entity<Event>().HasData(
            new Event
            {
                EventId = 1, OrganiserId = 1, Title = "Joburg City Marathon 2026",
                Description = "A flagship road running event through the heart of Johannesburg, starting at the FNB Stadium and finishing at Sandton City.",
                EventDate = new DateOnly(2026, 9, 20), Location = "FNB Stadium, Johannesburg", Province = "Gauteng",
                EventType = "Running", Status = EventStatuses.Published, ContactEmail = "info@joburgmarathon.co.za",
                CreatedAt = SeededAt, UpdatedAt = SeededAt
            },
            new Event
            {
                EventId = 2, OrganiserId = 2, Title = "Cape Town Coastal Cycle",
                Description = "A scenic cycling event along the Atlantic Seaboard from the V&A Waterfront to Hout Bay and back.",
                EventDate = new DateOnly(2026, 10, 15), Location = "V&A Waterfront, Cape Town", Province = "Western Cape",
                EventType = "Cycling", Status = EventStatuses.Published, ContactEmail = "events@capecoastalcycle.co.za",
                CreatedAt = SeededAt, UpdatedAt = SeededAt
            },
            new Event
            {
                EventId = 3, OrganiserId = 1, Title = "Soweto Heritage Walk",
                Description = "A community walking event celebrating Soweto heritage, passing through Vilakazi Street and Freedom Square.",
                EventDate = new DateOnly(2026, 11, 1), Location = "Vilakazi Street, Soweto", Province = "Gauteng",
                EventType = "Walking", Status = EventStatuses.Published, ContactEmail = "walk@sowetoheritage.org.za",
                CreatedAt = SeededAt, UpdatedAt = SeededAt
            });

        modelBuilder.Entity<Category>().HasData(
            new Category { CategoryId = 1, EventId = 1, Name = "42.2km Full Marathon", DistanceKm = 42.20m, EntryFee = 350.00m, MaxParticipants = 5000, MinAge = 18 },
            new Category { CategoryId = 2, EventId = 1, Name = "21.1km Half Marathon", DistanceKm = 21.10m, EntryFee = 250.00m, MaxParticipants = 8000, MinAge = 16 },
            new Category { CategoryId = 3, EventId = 1, Name = "10km Fun Run", DistanceKm = 10.00m, EntryFee = 120.00m, MaxParticipants = 10000, MinAge = 12 },
            new Category { CategoryId = 4, EventId = 2, Name = "80km Full Route", DistanceKm = 80.00m, EntryFee = 450.00m, MaxParticipants = 3000, MinAge = 18 },
            new Category { CategoryId = 5, EventId = 2, Name = "40km Half Route", DistanceKm = 40.00m, EntryFee = 300.00m, MaxParticipants = 5000, MinAge = 16 },
            new Category { CategoryId = 6, EventId = 3, Name = "10km Heritage Trail", DistanceKm = 10.00m, EntryFee = 50.00m, MaxParticipants = 2000 },
            new Category { CategoryId = 7, EventId = 3, Name = "5km Family Walk", DistanceKm = 5.00m, EntryFee = 0.00m, MaxParticipants = 5000 });

        modelBuilder.Entity<EventRoute>().HasData(
            new EventRoute
            {
                RouteId = 1, EventId = 1, StartLatitude = -26.235900m, StartLongitude = 27.982500m, EndLatitude = -26.107300m, EndLongitude = 28.056700m,
                Description = "Start at FNB Stadium, head north through Braamfontein, past the Johannesburg CBD, through Rosebank, and finish at Sandton City. Mostly tar roads with gentle elevation changes.",
                ElevationGainM = 185, TerrainType = "Road"
            },
            new EventRoute
            {
                RouteId = 2, EventId = 2, StartLatitude = -33.903200m, StartLongitude = 18.420700m, EndLatitude = -34.043900m, EndLongitude = 18.353800m,
                Description = "Start at V&A Waterfront, ride along the Atlantic Seaboard via Sea Point, Camps Bay, and Llandudno to Hout Bay. Return via Constantia Nek. Rolling coastal terrain.",
                ElevationGainM = 640, TerrainType = "Road"
            },
            new EventRoute
            {
                RouteId = 3, EventId = 3, StartLatitude = -26.336700m, StartLongitude = 27.896200m, EndLatitude = -26.341500m, EndLongitude = 27.905400m,
                Description = "Start at Vilakazi Street, loop through Orlando West, past the Hector Pieterson Museum, through Freedom Square, and return. Flat urban terrain.",
                ElevationGainM = 25, TerrainType = "Road"
            });

        modelBuilder.Entity<Enrolment>().HasData(
            new Enrolment { EnrolmentId = 1, ParticipantId = 3, CategoryId = 2, EnrolmentDate = new DateTime(2026, 7, 1, 10, 30, 0), BibNumber = "JBM-2145", Status = EnrolmentStatuses.Confirmed, EmergencyContactName = "Nomsa Ndlovu", EmergencyContactPhone = "073 111 2233" },
            new Enrolment { EnrolmentId = 2, ParticipantId = 4, CategoryId = 3, EnrolmentDate = new DateTime(2026, 7, 5, 14, 15, 0), BibNumber = "JBM-5032", Status = EnrolmentStatuses.Confirmed, EmergencyContactName = "David Khumalo", EmergencyContactPhone = "082 444 5566" },
            new Enrolment { EnrolmentId = 3, ParticipantId = 5, CategoryId = 4, EnrolmentDate = new DateTime(2026, 8, 10, 9, 0, 0), BibNumber = "CCC-0789", Status = EnrolmentStatuses.Confirmed, EmergencyContactName = "Sarah Venter", EmergencyContactPhone = "083 777 8899" },
            new Enrolment { EnrolmentId = 4, ParticipantId = 6, CategoryId = 2, EnrolmentDate = new DateTime(2026, 7, 12, 16, 45, 0), BibNumber = "JBM-2301", Status = EnrolmentStatuses.Confirmed, EmergencyContactName = "Bongani Zulu", EmergencyContactPhone = "071 222 3344" },
            new Enrolment { EnrolmentId = 5, ParticipantId = 3, CategoryId = 6, EnrolmentDate = new DateTime(2026, 8, 20, 11, 0, 0), Status = EnrolmentStatuses.Pending, EmergencyContactName = "Nomsa Ndlovu", EmergencyContactPhone = "073 111 2233" },
            new Enrolment { EnrolmentId = 6, ParticipantId = 4, CategoryId = 5, EnrolmentDate = new DateTime(2026, 8, 15, 8, 30, 0), BibNumber = "CCC-1456", Status = EnrolmentStatuses.Confirmed, EmergencyContactName = "David Khumalo", EmergencyContactPhone = "082 444 5566" },
            new Enrolment { EnrolmentId = 7, ParticipantId = 5, CategoryId = 7, EnrolmentDate = new DateTime(2026, 8, 25, 12, 0, 0), Status = EnrolmentStatuses.Pending, EmergencyContactName = "Sarah Venter", EmergencyContactPhone = "083 777 8899" });

        modelBuilder.Entity<Result>().HasData(
            new Result { ResultId = 1, EnrolmentId = 1, FinishTime = new TimeSpan(1, 42, 35), Position = 1, Status = ResultStatuses.Finished, RecordedById = 1, RecordedAt = new DateTime(2026, 9, 20, 12, 30, 0) },
            new Result { ResultId = 2, EnrolmentId = 4, FinishTime = new TimeSpan(2, 5, 18), Position = 2, Status = ResultStatuses.Finished, RecordedById = 1, RecordedAt = new DateTime(2026, 9, 20, 12, 45, 0) },
            new Result { ResultId = 3, EnrolmentId = 2, FinishTime = new TimeSpan(0, 52, 10), Position = 1, Status = ResultStatuses.Finished, RecordedById = 1, RecordedAt = new DateTime(2026, 9, 20, 11, 15, 0) });

        modelBuilder.Entity<EventAnnouncement>().HasData(
            new EventAnnouncement { AnnouncementId = 1, EventId = 1, AuthorId = 1, Title = "Route Change Notice", Message = "Due to road works on Jan Smuts Avenue, the 42.2km route will be diverted via Oxford Road between km 18 and km 22. Updated maps are available on the event page.", Priority = "High", CreatedAt = new DateTime(2026, 9, 10, 9, 0, 0) },
            new EventAnnouncement { AnnouncementId = 2, EventId = 1, AuthorId = 1, Title = "Race Pack Collection", Message = "Race packs can be collected from FNB Stadium on 18-19 September between 09:00 and 17:00. Please bring your ID and confirmation email.", Priority = "Normal", CreatedAt = new DateTime(2026, 9, 12, 14, 0, 0) },
            new EventAnnouncement { AnnouncementId = 3, EventId = 2, AuthorId = 2, Title = "Registration Closing Soon", Message = "Online registration for the Cape Town Coastal Cycle closes on 10 October. Late entries will be accepted at the venue for an additional R100.", Priority = "Normal", CreatedAt = new DateTime(2026, 9, 28, 10, 0, 0) },
            new EventAnnouncement { AnnouncementId = 4, EventId = 3, AuthorId = 1, Title = "Event Confirmed", Message = "The Soweto Heritage Walk is confirmed for 1 November 2026. Water stations will be placed every 2km along the route.", Priority = "Low", CreatedAt = new DateTime(2026, 10, 1, 8, 0, 0) });
    }

    private static User NewUser(int id, string email, string first, string last, string role, string phone, DateOnly dob, string gender, string province) => new()
    {
        UserId = id,
        Email = email,
        PasswordHash = DemoPasswordHash,
        FirstName = first,
        LastName = last,
        Role = role,
        Phone = phone,
        DateOfBirth = dob,
        Gender = gender,
        Province = province,
        CreatedAt = SeededAt,
        UpdatedAt = SeededAt
    };
}
