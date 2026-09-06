using JobTrack.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JobTrack.Api.Tests;

public sealed class JobTrackApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Server=test;Database=JobTrack;User Id=test;Password=test;",
                ["Database:MigrateOnStartup"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<JobTrackDbContext>>();
            services.RemoveAll<
                IDbContextOptionsConfiguration<JobTrackDbContext>>();
            services.RemoveAll<JobTrackDbContext>();

            services.AddDbContext<JobTrackDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
