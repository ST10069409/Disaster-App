using Disaster_App.Models;
using Microsoft.EntityFrameworkCore;

namespace Disaster_App.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = default!;
        public DbSet<Incident> Incidents { get; set; } = default!;
        public DbSet<Donation> Donations { get; set; } = default!;
        public DbSet<Volunteer> Volunteers { get; set; } = default!;
        public DbSet<VolunteerTask> VolunteerTasks { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure User -> Incidents relationship
            modelBuilder.Entity<Incident>()
                .HasOne(i => i.Reporter)
                .WithMany(u => u.Incidents)
                .HasForeignKey(i => i.ReportedBy)
                .OnDelete(DeleteBehavior.Restrict);
            // Configure User -> Volunteer relationship (One-to-One)
            modelBuilder.Entity<Volunteer>()
                .HasOne(v => v.User)
                .WithOne(u => u.VolunteerProfile)
                .HasForeignKey<Volunteer>(v => v.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Volunteer -> VolunteerTasks relationship (One-to-Many)
            modelBuilder.Entity<VolunteerTask>()
                .HasOne(t => t.AssignedVolunteer)
                .WithMany(v => v.Tasks)
                .HasForeignKey(t => t.AssignedTo)
                .OnDelete(DeleteBehavior.SetNull); // Set to null if volunteer is deleted

            base.OnModelCreating(modelBuilder);
        }
    }
}