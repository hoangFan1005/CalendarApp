using CalendarApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CalendarApp.Data
{
    public class CalendarDbContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Appointment> Appointments { get; set; } = null!;
        public DbSet<Reminder> Reminders { get; set; } = null!;
        public DbSet<GroupMeeting> GroupMeetings { get; set; } = null!;
        public DbSet<Participant> Participants { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(@"Server=localhost\SQLEXPRESS;Database=CalendarAppDB;Trusted_Connection=True;TrustServerCertificate=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.User)
                .WithMany(u => u.Appointments)
                .HasForeignKey(a => a.UserId);

            modelBuilder.Entity<Reminder>()
                .HasOne(r => r.Appointment)
                .WithMany(a => a.Reminders)
                .HasForeignKey(r => r.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Participant>()
                .HasOne(p => p.GroupMeeting)
                .WithMany(g => g.Participants)
                .HasForeignKey(p => p.GroupMeetingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Participant>()
                .HasOne(p => p.User)
                .WithMany(u => u.Participations)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
