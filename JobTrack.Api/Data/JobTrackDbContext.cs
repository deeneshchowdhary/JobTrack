using JobTrack.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JobTrack.Api.Data;

public class JobTrackDbContext : IdentityDbContext<ApplicationUser>
{
    public JobTrackDbContext(DbContextOptions<JobTrackDbContext> options)
        : base(options)
    {
    }

    public DbSet<JobApplication> JobApplications =>
        Set<JobApplication>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<JobApplication>()
            .HasOne(application => application.User)
            .WithMany()
            .HasForeignKey(application => application.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<JobApplication>()
            .HasIndex(application => new
            {
                application.UserId,
                application.AppliedDate
            });

        builder.Entity<JobApplication>()
            .Property(application => application.Salary)
            .HasPrecision(18, 2);
    }
}
