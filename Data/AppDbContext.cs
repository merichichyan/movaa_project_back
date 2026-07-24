using Microsoft.EntityFrameworkCore;
using movaa_project_back.Domain.Entities;

namespace movaa_project_back.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Salon> Salons => Set<Salon>();
        public DbSet<Specialist> Specialists => Set<Specialist>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.HasIndex(u => u.Phone).IsUnique();
                entity.Property(u => u.Phone).IsRequired().HasMaxLength(50);
                entity.Property(u => u.FullName).HasMaxLength(150);
                entity.Property(u => u.Email).HasMaxLength(255);
                entity.Property(u => u.Role).HasMaxLength(50);
            });

            modelBuilder.Entity<Salon>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Name).IsRequired().HasMaxLength(150);
                entity.Property(s => s.Address).IsRequired().HasMaxLength(250);
                entity.Property(s => s.Phone).IsRequired().HasMaxLength(50);
            });

            modelBuilder.Entity<Specialist>(entity =>
            {
                entity.HasKey(sp => sp.Id);
                entity.Property(sp => sp.Name).IsRequired().HasMaxLength(150);
                entity.Property(sp => sp.Category).IsRequired().HasMaxLength(100);
                entity.Property(sp => sp.Phone).IsRequired().HasMaxLength(50);
            });
        }
    }
}
