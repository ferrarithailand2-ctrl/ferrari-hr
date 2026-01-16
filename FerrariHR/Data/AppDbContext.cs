using FerrariHR.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FerrariHR.Models;

namespace FerrariHR.Data
{
    public class AppDbContext : IdentityDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<LeaveRequest> LeaveRequests { get; set; } = null!;
        public DbSet<OvertimeRequest> OvertimeRequests { get; set; } = null!;
        public DbSet<TrainingMaterial> TrainingMaterials { get; set; } = null!;
        public DbSet<LateRecord> LateRecords { get; set; } = null!;
        public DbSet<SalaryConfig> SalaryConfigs { get; set; } = default!;

    }
}
