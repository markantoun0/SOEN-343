using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SUMMS.Api.Domain.Models;

public class Reservation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int MobilityLocationId { get; set; }

    public DateTime ReservationTime { get; set; }

    public string City { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    [ForeignKey(nameof(MobilityLocationId))]
    public MobilityLocation? MobilityLocation { get; set; }
}
