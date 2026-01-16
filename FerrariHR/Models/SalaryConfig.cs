using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace FerrariHR.Models
{
    public class SalaryConfig
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public IdentityUser? User { get; set; }

        [Required]
        [Display(Name = "Base monthly salary")]
        public decimal BaseMonthlySalary { get; set; }

        [Required]
        [Display(Name = "OT rate per hour")]
        public decimal OvertimeHourlyRate { get; set; }

        [Required]
        [Display(Name = "Late deduction per minute")]
        public decimal LateDeductionPerMinute { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
