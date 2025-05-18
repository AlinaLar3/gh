using Microsoft.EntityFrameworkCore;

namespace FileAnalysisService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<AnalysisResult> AnalysisResults { get; set; }
    }
}