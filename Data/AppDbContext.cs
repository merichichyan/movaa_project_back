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
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Offer> Offers => Set<Offer>();
        public DbSet<UserFavorite> UserFavorites => Set<UserFavorite>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<SalonResource> SalonResources => Set<SalonResource>();
        public DbSet<ServiceResource> ServiceResources => Set<ServiceResource>();
        public DbSet<SpecialistPhoneChangeRequest> SpecialistPhoneChangeRequests => Set<SpecialistPhoneChangeRequest>();
        public DbSet<SpecialistSocialLink> SpecialistSocialLinks => Set<SpecialistSocialLink>();
        public DbSet<SalonSocialLink> SalonSocialLinks => Set<SalonSocialLink>();

        public DbSet<Organization> Organizations => Set<Organization>();
        public DbSet<Branch> Branches => Set<Branch>();
        public DbSet<OrganizationMembership> OrganizationMemberships => Set<OrganizationMembership>();
        public DbSet<SpecialistBranch> SpecialistBranches => Set<SpecialistBranch>();
        public DbSet<SpecialistInvitation> SpecialistInvitations => Set<SpecialistInvitation>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Offer>(entity =>
            {
                entity.ToTable("Offers");
                entity.HasKey(o => o.Id);
                entity.Property(o => o.Title).IsRequired().HasMaxLength(200);
            });

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

                entity.Property(s => s.Id).HasColumnName("Id");
                entity.Property(s => s.Category).HasColumnName("Category").IsRequired().HasMaxLength(100).HasDefaultValue("Salon");
                entity.Property(s => s.Name).HasColumnName("Name").IsRequired().HasMaxLength(150);
                entity.Property(s => s.PhoneNumber).HasColumnName("PhoneNumber").IsRequired().HasMaxLength(30);
                entity.Property(s => s.Email).HasColumnName("Email").HasMaxLength(255);
                entity.Property(s => s.Address).HasColumnName("Address").IsRequired().HasMaxLength(255);
                entity.Property(s => s.WorkingHours).HasColumnName("WorkingHours").IsRequired().HasMaxLength(100).HasDefaultValue("09:00 - 18:00");
                entity.Property(s => s.LogoUrl).HasColumnName("LogoUrl");
                entity.Property(s => s.OwnerFullName).HasColumnName("OwnerFullName").IsRequired().HasMaxLength(150);
                entity.Property(s => s.OwnerPhoneNumber).HasColumnName("OwnerPhoneNumber").IsRequired().HasMaxLength(30);
                entity.Property(s => s.TaxId).HasColumnName("TaxId").IsRequired().HasMaxLength(50).HasDefaultValue("00000000");
                entity.Property(s => s.Description).HasColumnName("Description");
                entity.Property(s => s.IsApproved).HasColumnName("IsApproved").IsRequired().HasDefaultValue(false);
                entity.Property(s => s.IsActive).HasColumnName("IsActive").IsRequired().HasDefaultValue(true);
                entity.Property(s => s.CreatedAt).HasColumnName("CreatedAt").IsRequired().HasDefaultValueSql("now()");
                entity.Property(s => s.UpdatedAt).HasColumnName("UpdatedAt");
                entity.Property(s => s.IsBlocked).HasColumnName("IsBlocked").IsRequired().HasDefaultValue(false);
                entity.Property(s => s.OwnerName).HasColumnName("OwnerName");
                entity.Property(s => s.OwnerPhone).HasColumnName("OwnerPhone");

                entity.Ignore(s => s.Phone);
                entity.Ignore(s => s.Rating);
                entity.Ignore(s => s.ReviewCount);
            });

            modelBuilder.Entity<Specialist>(entity =>
            {
                entity.HasKey(sp => sp.Id);
                entity.Property(sp => sp.Name).IsRequired().HasMaxLength(150);
                entity.Property(sp => sp.Category).IsRequired().HasMaxLength(100);
                entity.Property(sp => sp.Phone).IsRequired().HasMaxLength(50);
                entity.Property(sp => sp.IsActivated).IsRequired().HasDefaultValue(false);
            });

            modelBuilder.Entity<UserFavorite>(entity =>
            {
                entity.ToTable("UserFavorites");
                entity.HasKey(uf => uf.Id);
                entity.HasIndex(uf => new { uf.UserId, uf.TargetId, uf.Type }).IsUnique();
            });

            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasIndex(b => new { b.SalonId, b.BookingDate });
                entity.HasIndex(b => b.SpecialistId);
            });

            modelBuilder.Entity<SalonResource>(entity =>
            {
                entity.ToTable("SalonResources");
                entity.HasKey(sr => sr.Id);
                entity.Property(sr => sr.Name).IsRequired().HasMaxLength(150);
                entity.Property(sr => sr.Quantity).IsRequired();
                entity.Property(sr => sr.IsActive).IsRequired().HasDefaultValue(true);
                entity.HasIndex(sr => sr.SalonId);
            });

            modelBuilder.Entity<ServiceResource>(entity =>
            {
                entity.ToTable("ServiceResources");
                entity.HasKey(sr => sr.Id);
                entity.Property(sr => sr.ServiceId).IsRequired().HasMaxLength(150);
                entity.HasIndex(sr => sr.ServiceId);
                entity.HasIndex(sr => sr.ResourceId);
                entity.HasOne(sr => sr.Resource)
                      .WithMany()
                      .HasForeignKey(sr => sr.ResourceId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SpecialistSocialLink>(entity =>
            {
                entity.ToTable("SpecialistSocialLinks");
                entity.HasKey(sl => sl.Id);
                entity.Property(sl => sl.Url).IsRequired().HasMaxLength(500);
                entity.Property(sl => sl.Platform).IsRequired().HasConversion<string>();
                entity.HasIndex(sl => new { sl.SpecialistId, sl.Platform }).IsUnique();
                entity.HasIndex(sl => sl.SpecialistId);
                entity.HasOne<Specialist>()
                      .WithMany(s => s.SocialLinks)
                      .HasForeignKey(sl => sl.SpecialistId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Organization>(entity =>
            {
                entity.ToTable("Organizations");
                entity.HasKey(o => o.Id);
                entity.Property(o => o.Name).IsRequired().HasMaxLength(200);
                entity.HasIndex(o => o.Slug).IsUnique();
            });

            modelBuilder.Entity<Branch>(entity =>
            {
                entity.ToTable("Branches");
                entity.HasKey(b => b.Id);
                entity.Property(b => b.Name).IsRequired().HasMaxLength(200);
                entity.Property(b => b.Address).IsRequired().HasMaxLength(300);
                entity.HasIndex(b => b.OrganizationId);
                entity.HasIndex(b => new { b.OrganizationId, b.Slug }).IsUnique();
            });

            modelBuilder.Entity<OrganizationMembership>(entity =>
            {
                entity.ToTable("OrganizationMemberships");
                entity.HasKey(m => m.Id);
                entity.HasIndex(m => new { m.UserId, m.OrganizationId }).IsUnique();
                entity.HasIndex(m => m.OrganizationId);
            });

            modelBuilder.Entity<SpecialistBranch>(entity =>
            {
                entity.ToTable("SpecialistBranches");
                entity.HasKey(sb => sb.Id);
                entity.HasIndex(sb => new { sb.SpecialistId, sb.BranchId }).IsUnique();
                entity.HasIndex(sb => sb.OrganizationId);
            });

            modelBuilder.Entity<SpecialistInvitation>(entity =>
            {
                entity.ToTable("SpecialistInvitations");
                entity.HasKey(si => si.Id);
                entity.HasIndex(si => new { si.OrganizationId, si.SpecialistId, si.Status });
            });
        }
    }
}
