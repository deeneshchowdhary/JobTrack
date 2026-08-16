using JobTrack.Api.Controllers;
using JobTrack.Api.Data;
using JobTrack.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobTrack.Api.Tests;

public class JobApplicationsControllerTests
{
    private static JobTrackDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<JobTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new JobTrackDbContext(options);
    }

    [Fact]
    public async Task Create_ValidApplication_ReturnsCreatedAndSavesApplication()
    {
        await using var context = CreateContext();

        var controller = new JobApplicationsController(
            context,
            NullLogger<JobApplicationsController>.Instance);

        var application = new JobApplication
        {
            Company = "Test Company",
            Position = "Software Developer",
            Status = "Applied",
            AppliedDate = DateTime.UtcNow,
            Salary = 100000,
            Notes = "Created by automated test"
        };

        var response = await controller.Create(application);

        var createdResult = Assert.IsType<CreatedAtActionResult>(
            response.Result);

        var createdApplication = Assert.IsType<JobApplication>(
            createdResult.Value);

        Assert.True(createdApplication.Id > 0);
        Assert.Equal("Test Company", createdApplication.Company);
        Assert.Equal(1, await context.JobApplications.CountAsync());
    }

    [Fact]
    public async Task GetAll_WithStatusFilter_ReturnsOnlyMatchingApplications()
    {
        await using var context = CreateContext();

        context.JobApplications.AddRange(
            new JobApplication
            {
                Company = "Company One",
                Position = "Developer",
                Status = "Applied",
                AppliedDate = DateTime.UtcNow
            },
            new JobApplication
            {
                Company = "Company Two",
                Position = "Senior Developer",
                Status = "Interview",
                AppliedDate = DateTime.UtcNow
            });

        await context.SaveChangesAsync();

        var controller = new JobApplicationsController(
            context,
            NullLogger<JobApplicationsController>.Instance);

        var response = await controller.GetAll("Interview");

        var applications = Assert.IsAssignableFrom<
            IEnumerable<JobApplication>>(response.Value);

        var results = applications.ToList();

        Assert.Single(results);
        Assert.Equal("Company Two", results[0].Company);
        Assert.Equal("Interview", results[0].Status);
    }

    [Fact]
    public async Task GetById_MissingApplication_ReturnsNotFound()
    {
        await using var context = CreateContext();

        var controller = new JobApplicationsController(
            context,
            NullLogger<JobApplicationsController>.Instance);

        var response = await controller.GetById(999);

        Assert.IsType<NotFoundResult>(response.Result);
    }
}