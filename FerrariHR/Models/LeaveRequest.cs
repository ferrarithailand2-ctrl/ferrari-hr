using System.ComponentModel.DataAnnotations;

namespace FerrariHR.Models
{
    public class LeaveRequest
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty; // e.g. Annual, Sick, etc.

        [MaxLength(500)]
        public string? Reason { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Admin decision metadata 
        public string? DecisionByUserId { get; set; }
        public DateTime? DecisionAt { get; set; }
    }
}
