using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YourProject.Models;

namespace SUMMS.Api.Domain.Models;

public class CarbonFootprint
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int UserId { get; set; }

    /// <summary>
    /// Total carbon footprint in kg CO2
    /// </summary>
    public double TotalCarbonKg { get; set; }

    /// <summary>
    /// Number of trips completed
    /// </summary>
    public int TripsCompleted { get; set; }

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime LastUpdated { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}

