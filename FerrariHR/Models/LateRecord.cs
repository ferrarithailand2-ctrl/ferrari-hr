using System.ComponentModel.DataAnnotations;

namespace FerrariHR.Models
{
    public class LateRecord
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Range(2000, 2100)]
        public int Year { get; set; }

        [Required]
        [Range(1, 12)]
        public int Month { get; set; } // 1 = January ... 12 = December

        public int WorkDays { get; set; }

        public int LateDays { get; set; }

        [Required]
        [Display(Name = "Total Late (Minutes)")]
        public int TotalLateMinutes { get; set; }

        public double TotalLateHours => Math.Round(TotalLateMinutes / 60.0, 2);

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
