using JobTrack.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace JobTrack.Api.Data;

public class JobTrackDbContext : DbContext
{
    public JobTrackDbContext(DbContextOptions<JobTrackDbContext> options)
        : base(options)
    {
    }

    public DbSet<JobApplication> JobApplications =>
        Set<JobApplication>();
}