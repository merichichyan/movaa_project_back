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
        public DbSet<Admin> Admins => Set<Admin>();
        public DbSet<Salon> Salons => Set<Salon>();
        public DbSet<Specialist> Specialists => Set<Specialist>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Admin>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.HasIndex(a => a.Username).IsUnique();
                entity.Property(a => a.Username).IsRequired().HasMaxLength(50);
                entity.Property(a => a.FullName).HasMaxLength(150);
                entity.Property(a => a.Email).HasMaxLength(255);
            });

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
                entity.ToTable("Salons");
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Name).HasColumnName("Name").IsRequired().HasMaxLength(150);
                entity.Property(s => s.Address).HasColumnName("Address").IsRequired().HasMaxLength(250);
                entity.Property(s => s.PhoneNumber).HasColumnName("PhoneNumber").IsRequired().HasMaxLength(50);
                entity.Property(s => s.OwnerFullName).HasColumnName("OwnerFullName").HasMaxLength(150);
                entity.Property(s => s.OwnerPhoneNumber).HasColumnName("OwnerPhoneNumber").HasMaxLength(50);
                entity.Property(s => s.TaxId).HasColumnName("TaxId").HasMaxLength(50);
                entity.Property(s => s.LogoUrl).HasColumnName("LogoUrl");
                entity.Property(s => s.Description).HasColumnName("Description");
                entity.Property(s => s.Email).HasColumnName("Email");
                entity.Property(s => s.IsBlocked).HasColumnName("IsBlocked");
                entity.Property(s => s.CreatedAt).HasColumnName("CreatedAt");
                entity.Property(s => s.UpdatedAt).HasColumnName("UpdatedAt");

                entity.Ignore(s => s.Phone);
                entity.Ignore(s => s.OwnerName);
                entity.Ignore(s => s.OwnerPhone);
                entity.Ignore(s => s.Rating);
                entity.Ignore(s => s.ReviewCount);
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
