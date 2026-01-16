using System.ComponentModel.DataAnnotations;

namespace FerrariHR.Models
{
    public class OvertimeRequest
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required]
        [Range(0.25, 24)]
        public double Hours { get; set; }  // OT hours for that date

        [MaxLength(500)]
        public string? Reason { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? DecisionByUserId { get; set; }
        public DateTime? DecisionAt { get; set; }
    }
}
