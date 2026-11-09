using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Enter your first name."), StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your last name."), StringLength(100)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose a password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Use at least 8 characters.")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Use at least one letter and one number.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "The passwords don't match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose how you'll use RaceDay.")]
    public string Role { get; set; } = "Participant";

    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    [StringLength(20)]
    [Phone(ErrorMessage = "Enter a valid phone number.")]
    public string? Phone { get; set; }

    public string? Province { get; set; }
}

public class HomeViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public string? Province { get; set; }
    public string? EventType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool HasFilters => !string.IsNullOrEmpty(Province) || !string.IsNullOrEmpty(EventType) || FromDate is not null || ToDate is not null;
}

public class EventDetailsViewModel
{
    public EventDetailDto Event { get; set; } = new();
    public WeatherDto? Weather { get; set; }

    /// <summary>The current Participant's active enrolment in this event, if any.</summary>
    public EnrolmentDto? MyEnrolment { get; set; }

    public EnrolInput Enrol { get; set; } = new();
}

public class EnrolInput
{
    [Required(ErrorMessage = "Choose a category.")]
    public int? CategoryId { get; set; }

    [StringLength(100)]
    [Display(Name = "Emergency contact name")]
    public string? EmergencyContactName { get; set; }

    [StringLength(20)]
    [Phone(ErrorMessage = "Enter a valid phone number.")]
    [Display(Name = "Emergency contact phone")]
    public string? EmergencyContactPhone { get; set; }
}

public class MyResultsViewModel
{
    public List<ResultDto> Results { get; set; } = new();
    public int Finished => Results.Count(r => r.Status == "Finished");
    public int Podiums => Results.Count(r => r.Status == "Finished" && r.Position is <= 3);
    public decimal TotalKm => Results.Where(r => r.Status == "Finished").Sum(r => r.DistanceKm ?? 0);
}

public class ProfileViewModel
{
    public UserDto User { get; set; } = new();

    [Required(ErrorMessage = "Enter your first name."), StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your last name."), StringLength(100)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(20)]
    [Phone(ErrorMessage = "Enter a valid phone number.")]
    public string? Phone { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    public string? Gender { get; set; }
    public string? Province { get; set; }
}

public class DashboardViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public IEnumerable<EventDto> Upcoming => Events.Where(e => e.IsUpcoming).OrderBy(e => e.EventDate);
    public IEnumerable<EventDto> Past => Events.Where(e => !e.IsUpcoming).OrderByDescending(e => e.EventDate);
    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
    public EventDto? Next => Upcoming.FirstOrDefault();
}

public class EventFormViewModel
{
    public int? EventId { get; set; }
    public string? CurrentImageUrl { get; set; }

    [Required(ErrorMessage = "Give the event a name."), StringLength(255)]
    [Display(Name = "Event name")]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Choose the event date.")]
    [DataType(DataType.Date)]
    [Display(Name = "Event date")]
    public DateTime? EventDate { get; set; }

    [Required(ErrorMessage = "Enter the distance.")]
    [Range(0.1, 999.99, ErrorMessage = "Distance must be between 0.1 and 999.99 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Required(ErrorMessage = "Enter where the event starts."), StringLength(255)]
    public string Location { get; set; } = string.Empty;

    public string? Province { get; set; }

    [Required(ErrorMessage = "Choose the event type.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Running";

    [Required]
    public string Status { get; set; } = "Published";

    [EmailAddress(ErrorMessage = "Enter a valid email address."), StringLength(255)]
    [Display(Name = "Contact email")]
    public string? ContactEmail { get; set; }

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }
}

public class CategoriesViewModel
{
    public EventDto Event { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public CategoryInput NewCategory { get; set; } = new();
}

public class CategoryInput
{
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Name the category."), StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the distance.")]
    [Range(0.1, 999.99, ErrorMessage = "Distance must be between 0.1 and 999.99 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Range(0, 100000, ErrorMessage = "Entry fee can't be negative.")]
    [Display(Name = "Entry fee (R)")]
    public decimal? EntryFee { get; set; }

    [Range(1, 100000, ErrorMessage = "Capacity must be at least 1.")]
    [Display(Name = "Capacity")]
    public int? MaxParticipants { get; set; }

    [Range(0, 120)]
    [Display(Name = "Min age")]
    public int? MinAge { get; set; }

    [Range(0, 120)]
    [Display(Name = "Max age")]
    public int? MaxAge { get; set; }

    [Display(Name = "Gender")]
    public string? GenderRestriction { get; set; }
}

public class EnrolmentsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<EnrolmentDto> Enrolments { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public int? CategoryId { get; set; }
    public string? Status { get; set; }
}

public class ResultsEntryViewModel
{
    public EventDto Event { get; set; } = new();
    public List<ResultRow> Rows { get; set; } = new();
    public bool EventHasHappened => !Event.IsUpcoming || Event.EventDate == DateOnly.FromDateTime(DateTime.Today);
}

public class ResultRow
{
    public EnrolmentDto Enrolment { get; set; } = new();
    public ResultDto? Result { get; set; }
}

public class ResultInput
{
    public int EnrolmentId { get; set; }
    public int? ResultId { get; set; }
    public int EventId { get; set; }

    [Required]
    public string Status { get; set; } = "Finished";

    [RegularExpression(@"^\d{1,2}:[0-5]\d:[0-5]\d$", ErrorMessage = "Use hh:mm:ss, e.g. 01:42:35.")]
    public string? FinishTime { get; set; }

    [Range(1, 100000, ErrorMessage = "Position must be 1 or higher.")]
    public int? Position { get; set; }
}

public static class Choices
{
    public static readonly string[] Provinces =
    {
        "Eastern Cape", "Free State", "Gauteng", "KwaZulu-Natal", "Limpopo",
        "Mpumalanga", "North West", "Northern Cape", "Western Cape"
    };

    public static readonly string[] EventTypes = { "Running", "Walking", "Cycling" };
    public static readonly string[] EventStatuses = { "Draft", "Published", "Completed", "Cancelled" };
    public static readonly string[] Genders = { "Male", "Female", "Other" };
    public static readonly string[] ResultStatuses = { "Finished", "DNF", "DNS", "DQ" };
}
