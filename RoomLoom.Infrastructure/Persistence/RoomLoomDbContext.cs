using Microsoft.EntityFrameworkCore;
using RoomLoom.Core.Models;

namespace RoomLoom.Infrastructure.Persistence;

public class RoomLoomDbContext : DbContext
{
    public RoomLoomDbContext(DbContextOptions<RoomLoomDbContext> options)
        : base(options) { }

    public DbSet<ScheduledSession> ScheduledSessions => Set<ScheduledSession>();
    public DbSet<Participant> Participants => Set<Participant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScheduledSession>(session =>
        {
            session.HasKey(s => s.Id);

            session.Property(s => s.Id)
                   .ValueGeneratedNever()
                   .HasMaxLength(36);

            session.Property(s => s.Title)
                   .IsRequired()
                   .HasMaxLength(200);

            session.Property(s => s.Description)
                   .IsRequired()
                   .HasMaxLength(2000);

            session.Property(s => s.StartTime)
                   .IsRequired();

            session.Property(s => s.EndTime)
                   .IsRequired();

            session.Property(s => s.PlannedStatus)
                   .HasConversion<string>()
                   .HasMaxLength(20);

            session.HasOne(s => s.Host)
                   .WithMany()
                   .HasForeignKey("HostId")
                   .OnDelete(DeleteBehavior.Restrict);

            session.HasMany(s => s.Participants)
                   .WithMany();
        });

        modelBuilder.Entity<Participant>(participant =>
        {
            participant.HasKey(p => p.Id);

            participant.Property(p => p.Id)
                       .ValueGeneratedNever()
                       .HasMaxLength(36);

            participant.Property(p => p.Name)
                       .IsRequired()
                       .HasMaxLength(150);

            participant.Property(p => p.Email)
                       .IsRequired()
                       .HasMaxLength(254);
        });
    }
}