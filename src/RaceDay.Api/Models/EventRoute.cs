using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

/// <summary>
/// Route information for an event (maps to the Routes table).
/// Named EventRoute in C# so it does not clash with ASP.NET Core's [Route] attribute.
/// </summary>
[Table("Routes")]
public class EventRoute
{
    [Key]
    [Column("route_id")]
    public int RouteId { get; set; }

    [Column("event_id")]
    public int EventId { get; set; }

    [Column("start_latitude", TypeName = "decimal(9,6)")]
    public decimal? StartLatitude { get; set; }

    [Column("start_longitude", TypeName = "decimal(9,6)")]
    public decimal? StartLongitude { get; set; }

    [Column("end_latitude", TypeName = "decimal(9,6)")]
    public decimal? EndLatitude { get; set; }

    [Column("end_longitude", TypeName = "decimal(9,6)")]
    public decimal? EndLongitude { get; set; }

    [Column("description", TypeName = "text")]
    public string? Description { get; set; }

    [Column("elevation_gain_m")]
    public int? ElevationGainM { get; set; }

    [MaxLength(50)]
    [Column("terrain_type", TypeName = "varchar(50)")]
    public string? TerrainType { get; set; }

    public Event? Event { get; set; }
}
