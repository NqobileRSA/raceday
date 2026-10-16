using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests.Helpers;

/// <summary>
/// Builds an isolated EF Core in-memory database per test and a small, known data set.
/// Dates are relative to today so the tests never go stale.
/// </summary>
public class TestData : IDisposable
{
    public const string Password = "Password123!";

    // Hashing is deliberately slow (PBKDF2, 100,000 iterations), so the test password is hashed once and reused.
    private static readonly Lazy<string> PasswordHash = new(() => new PasswordService().Hash(new User(), Password));

    public RaceDayDbContext Db { get; }
    public IPasswordService Passwords { get; } = new PasswordService();

    public User Organiser { get; private set; } = null!;
    public User OtherOrganiser { get; private set; } = null!;
    public User Participant { get; private set; } = null!;
    public User YoungParticipant { get; private set; } = null!;
    public Event UpcomingEvent { get; private set; } = null!;
    public Event PastEvent { get; private set; } = null!;
    public Event DraftEvent { get; private set; } = null!;
    public Event OtherOrganisersEvent { get; private set; } = null!;
    public Category HalfMarathon { get; private set; } = null!;
    public Category TenK { get; private set; } = null!;
    public Category FullCategory { get; private set; } = null!;
    public Category PastCategory { get; private set; } = null!;
    public Enrolment PastEnrolment { get; private set; } = null!;

    public static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    public TestData()
    {
        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseInMemoryDatabase($"RaceDayTests-{Guid.NewGuid()}")
            .Options;
        Db = new RaceDayDbContext(options);
        Seed();
    }

    private void Seed()
    {
        Organiser = AddUser("organiser@raceday.co.za", "Thabo", "Mokoena", Roles.Organiser, new DateOnly(1985, 3, 15), "Male");
        OtherOrganiser = AddUser("other.organiser@raceday.co.za", "Lerato", "Dlamini", Roles.Organiser, new DateOnly(1990, 7, 22), "Female");
        Participant = AddUser("participant@gmail.com", "Sipho", "Ndlovu", Roles.Participant, new DateOnly(1995, 11, 8), "Male");
        YoungParticipant = AddUser("young@gmail.com", "Amahle", "Zulu", Roles.Participant, Today.AddYears(-14), "Female");
        Db.SaveChanges();

        UpcomingEvent = AddEvent(Organiser, "Joburg City Marathon", Today.AddDays(30), EventStatuses.Published, "Running", "Gauteng");
        PastEvent = AddEvent(Organiser, "Soweto Heritage Walk", Today.AddDays(-7), EventStatuses.Completed, "Walking", "Gauteng");
        DraftEvent = AddEvent(Organiser, "Draft Trail Run", Today.AddDays(60), EventStatuses.Draft, "Running", "Gauteng");
        OtherOrganisersEvent = AddEvent(OtherOrganiser, "Cape Town Coastal Cycle", Today.AddDays(45), EventStatuses.Published, "Cycling", "Western Cape");
        Db.SaveChanges();

        HalfMarathon = new Category { EventId = UpcomingEvent.EventId, Name = "21.1km Half Marathon", DistanceKm = 21.1m, EntryFee = 250m, MaxParticipants = 100, MinAge = 16 };
        TenK = new Category { EventId = UpcomingEvent.EventId, Name = "10km Fun Run", DistanceKm = 10m, EntryFee = 120m, MaxParticipants = 100 };
        FullCategory = new Category { EventId = OtherOrganisersEvent.EventId, Name = "80km Full Route", DistanceKm = 80m, MaxParticipants = 1 };
        PastCategory = new Category { EventId = PastEvent.EventId, Name = "10km Heritage Trail", DistanceKm = 10m };
        Db.Categories.AddRange(HalfMarathon, TenK, FullCategory, PastCategory);
        Db.SaveChanges();

        // FullCategory allows one participant and that spot is already taken.
        var filler = AddUser("filler@gmail.com", "James", "Venter", Roles.Participant, new DateOnly(1988, 9, 30), "Male");
        Db.SaveChanges();
        Db.Enrolments.Add(new Enrolment { ParticipantId = filler.UserId, CategoryId = FullCategory.CategoryId, Status = EnrolmentStatuses.Confirmed });

        // The participant took part in the past event; the organiser can record a result for it.
        PastEnrolment = new Enrolment { ParticipantId = Participant.UserId, CategoryId = PastCategory.CategoryId, Status = EnrolmentStatuses.Confirmed, BibNumber = "SHW-001" };
        Db.Enrolments.Add(PastEnrolment);
        Db.SaveChanges();
    }

    public User AddUser(string email, string first, string last, string role, DateOnly? dob, string? gender)
    {
        var user = new User
        {
            Email = email, FirstName = first, LastName = last, Role = role,
            DateOfBirth = dob, Gender = gender, Province = "Gauteng"
        };
        user.PasswordHash = PasswordHash.Value;
        Db.Users.Add(user);
        return user;
    }

    private Event AddEvent(User organiser, string title, DateOnly date, string status, string type, string province)
    {
        var ev = new Event
        {
            OrganiserId = organiser.UserId, Title = title, EventDate = date, Status = status,
            EventType = type, Province = province, Location = $"{title} Start Line"
        };
        Db.Events.Add(ev);
        return ev;
    }

    /// <summary>Creates a controller wired to a fresh HTTP context and session.</summary>
    /// <param name="factory">Builds the controller from the database context.</param>
    /// <param name="loggedInAs">The user to log in as, or null for an anonymous request.</param>
    public T Controller<T>(Func<RaceDayDbContext, T> factory, User? loggedInAs = null) where T : ControllerBase
    {
        var controller = factory(Db);
        var httpContext = new DefaultHttpContext { Session = new TestSession() };
        if (loggedInAs is not null) LogIn(httpContext.Session, loggedInAs);
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    public static void LogIn(ISession session, User user)
    {
        session.SetInt32(SessionKeys.UserId, user.UserId);
        session.SetString(SessionKeys.Role, user.Role);
        session.SetString(SessionKeys.Email, user.Email);
    }

    public void Dispose() => Db.Dispose();
}
