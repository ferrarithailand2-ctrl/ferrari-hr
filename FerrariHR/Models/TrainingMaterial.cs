using System.ComponentModel.DataAnnotations;

namespace FerrariHR.Models
{
    public class TrainingMaterial
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(500)]
        [Display(Name = "https://drive.google.com/drive/folders/1YqHEVhM_YgCKpbyzlHkQKf42fex_ELR4?usp=sharing")]
        public string OneDriveUrl { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
