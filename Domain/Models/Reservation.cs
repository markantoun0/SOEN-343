using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YourProject.Models;

namespace SUMMS.Api.Domain.Models;

public class Reservation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int MobilityLocationId { get; set; }

    public DateTime ReservationTime { get; set; }
    
    public DateTime StartDate { get; set; }
    
    public DateTime EndDate { get; set; }

    public string City { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public ReservationStatus Status { get; set; } = ReservationStatus.Active;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeleteReason { get; set; }

    public DateTime? ExpirationWarningSentAt { get; set; }

    public int? UserId { get; set; }

    [ForeignKey(nameof(MobilityLocationId))]
    public MobilityLocation? MobilityLocation { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}
