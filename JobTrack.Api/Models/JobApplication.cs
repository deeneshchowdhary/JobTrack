using System.ComponentModel.DataAnnotations;

namespace JobTrack.Api.Models;

public class JobApplication
{
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Company { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Position { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Status { get; set; } = "Applied";

    public DateTime AppliedDate { get; set; } = DateTime.UtcNow;

    public decimal? Salary { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}