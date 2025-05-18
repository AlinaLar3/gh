// FileStoringService/Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;

namespace FileStoringService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<FileMetadata> FilesMetadata { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FileMetadata>()
                .HasIndex(f => f.FileHash)
                .IsUnique();
        }
    }
}