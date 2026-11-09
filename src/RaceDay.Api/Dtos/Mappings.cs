using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

/// <summary>Converts entities to response DTOs so database entities are never serialised directly.</summary>
public static class Mappings
{
    public static UserResponse ToResponse(this User u) => new()
    {
        UserId = u.UserId,
        Email = u.Email,
        FirstName = u.FirstName,
        LastName = u.LastName,
        Role = u.Role,
        Phone = u.Phone,
        DateOfBirth = u.DateOfBirth,
        Gender = u.Gender,
        Province = u.Province,
        ProfilePictureUrl = u.ProfilePictureUrl,
        CreatedAt = u.CreatedAt,
        UpdatedAt = u.UpdatedAt
    };

    /// <summary>Requires Categories (and their Enrolments) to be loaded for the counts to be accurate.</summary>
    public static EventResponse ToResponse(this Event e) => Fill(new EventResponse(), e);

    public static EventDetailResponse ToDetailResponse(this Event e)
    {
        var detail = Fill(new EventDetailResponse(), e);
        detail.Categories = e.Categories.OrderBy(c => c.DistanceKm).Select(c => c.ToResponse()).ToList();
        detail.Route = e.Route?.ToResponse();
        detail.Announcements = e.Announcements.OrderByDescending(a => a.CreatedAt).Select(a => a.ToResponse()).ToList();
        return detail;
    }

    private static T Fill<T>(T r, Event e) where T : EventResponse
    {
        r.EventId = e.EventId;
        r.OrganiserId = e.OrganiserId;
        r.OrganiserName = e.Organiser?.FullName;
        r.Title = e.Title;
        r.Description = e.Description;
        r.EventDate = e.EventDate;
        r.DistanceKm = e.DistanceKm;
        r.Location = e.Location;
        r.Province = e.Province;
        r.EventType = e.EventType;
        r.Status = e.Status;
        r.ContactEmail = e.ContactEmail;
        r.ImageUrl = e.ImageUrl;
        r.DistancesKm = e.Categories.Select(c => c.DistanceKm).Distinct().OrderBy(d => d).ToList();
        r.CategoryCount = e.Categories.Count;
        r.EnrolmentCount = e.Categories.Sum(c => c.Enrolments.Count(en => en.Status != EnrolmentStatuses.Cancelled));
        r.CreatedAt = e.CreatedAt;
        r.UpdatedAt = e.UpdatedAt;
        return r;
    }

    public static CategoryResponse ToResponse(this Category c)
    {
        var active = c.Enrolments.Count(en => en.Status != EnrolmentStatuses.Cancelled);
        return new CategoryResponse
        {
            CategoryId = c.CategoryId,
            EventId = c.EventId,
            Name = c.Name,
            DistanceKm = c.DistanceKm,
            EntryFee = c.EntryFee,
            MaxParticipants = c.MaxParticipants,
            MinAge = c.MinAge,
            MaxAge = c.MaxAge,
            GenderRestriction = c.GenderRestriction,
            EnrolmentCount = active,
            SpotsRemaining = c.MaxParticipants is null ? null : Math.Max(0, c.MaxParticipants.Value - active)
        };
    }

    /// <summary>Requires Participant and Category.Event to be loaded.</summary>
    public static EnrolmentResponse ToResponse(this Enrolment en) => new()
    {
        EnrolmentId = en.EnrolmentId,
        ParticipantId = en.ParticipantId,
        ParticipantName = en.Participant?.FullName,
        ParticipantEmail = en.Participant?.Email,
        EventId = en.Category?.EventId ?? 0,
        EventTitle = en.Category?.Event?.Title,
        EventDate = en.Category?.Event?.EventDate,
        EventLocation = en.Category?.Event?.Location,
        CategoryId = en.CategoryId,
        CategoryName = en.Category?.Name,
        DistanceKm = en.Category?.DistanceKm,
        EnrolmentDate = en.EnrolmentDate,
        BibNumber = en.BibNumber,
        Status = en.Status,
        EmergencyContactName = en.EmergencyContactName,
        EmergencyContactPhone = en.EmergencyContactPhone,
        HasResult = en.Result is not null
    };

    /// <summary>Requires Enrolment.Participant and Enrolment.Category.Event to be loaded.</summary>
    public static ResultResponse ToResponse(this Result r, int totalFinishers) => new()
    {
        ResultId = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        ParticipantId = r.Enrolment?.ParticipantId ?? 0,
        ParticipantName = r.Enrolment?.Participant?.FullName,
        EventId = r.Enrolment?.Category?.EventId ?? 0,
        EventTitle = r.Enrolment?.Category?.Event?.Title,
        EventDate = r.Enrolment?.Category?.Event?.EventDate,
        CategoryId = r.Enrolment?.CategoryId ?? 0,
        CategoryName = r.Enrolment?.Category?.Name,
        DistanceKm = r.Enrolment?.Category?.DistanceKm,
        BibNumber = r.Enrolment?.BibNumber,
        FinishTime = r.FinishTime,
        Position = r.Position,
        TotalFinishers = totalFinishers,
        Status = r.Status,
        RecordedAt = r.RecordedAt
    };

    public static RouteResponse ToResponse(this EventRoute r) => new()
    {
        RouteId = r.RouteId,
        EventId = r.EventId,
        StartLatitude = r.StartLatitude,
        StartLongitude = r.StartLongitude,
        EndLatitude = r.EndLatitude,
        EndLongitude = r.EndLongitude,
        Description = r.Description,
        ElevationGainM = r.ElevationGainM,
        TerrainType = r.TerrainType
    };

    public static AnnouncementResponse ToResponse(this EventAnnouncement a) => new()
    {
        AnnouncementId = a.AnnouncementId,
        EventId = a.EventId,
        AuthorId = a.AuthorId,
        AuthorName = a.Author?.FullName,
        Title = a.Title,
        Message = a.Message,
        Priority = a.Priority,
        CreatedAt = a.CreatedAt
    };
}
