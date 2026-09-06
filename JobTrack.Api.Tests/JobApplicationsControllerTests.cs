using JobTrack.Api.Contracts;
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

    private static JobApplicationsController CreateController(
        JobTrackDbContext context) =>
        new(context, NullLogger<JobApplicationsController>.Instance);

    [Fact]
    public async Task Create_ValidRequest_NormalizesAndSavesApplication()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);

        var response = await controller.Create(new CreateJobApplicationRequest
        {
            Company = "  Test Company  ",
            Position = " Software Developer ",
            Status = "applied",
            Salary = 100000,
            Notes = " Created by automated test "
        });

        var created = Assert.IsType<CreatedAtActionResult>(response.Result);
        var application = Assert.IsType<JobApplicationResponse>(created.Value);

        Assert.True(application.Id > 0);
        Assert.Equal("Test Company", application.Company);
        Assert.Equal(JobApplicationStatuses.Applied, application.Status);
        Assert.Equal(1, await context.JobApplications.CountAsync());
    }

    [Fact]
    public async Task Create_InvalidStatus_ReturnsValidationProblem()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);

        var response = await controller.Create(new CreateJobApplicationRequest
        {
            Company = "Test Company",
            Position = "Developer",
            Status = "Maybe"
        });

        var result = Assert.IsAssignableFrom<ObjectResult>(response.Result);
        Assert.Equal(400, result.StatusCode);
        Assert.IsType<ValidationProblemDetails>(result.Value);
        Assert.Empty(context.JobApplications);
    }

    [Fact]
    public async Task GetAll_FiltersSearchesSortsAndPaginates()
    {
        await using var context = CreateContext();
        context.JobApplications.AddRange(
            Application("Beta Labs", "Developer", "Applied", 2),
            Application("Alpha Labs", "Senior Developer", "Interview", 3),
            Application("Gamma Inc", "Designer", "Interview", 1));
        await context.SaveChangesAsync();

        var response = await CreateController(context).GetAll(
            new JobApplicationQuery
            {
                Status = "interview",
                Search = "developer",
                SortBy = "company",
                SortDirection = "asc",
                Page = 1,
                PageSize = 1
            });

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var page = Assert.IsType<PagedResponse<JobApplicationResponse>>(ok.Value);

        Assert.Single(page.Items);
        Assert.Equal("Alpha Labs", page.Items[0].Company);
        Assert.Equal(1, page.TotalItems);
        Assert.Equal(1, page.TotalPages);
    }

    [Fact]
    public async Task Update_MapsAllowedFieldsOntoExistingApplication()
    {
        await using var context = CreateContext();
        var existing = Application("Old Company", "Developer", "Applied", 1);
        context.JobApplications.Add(existing);
        await context.SaveChangesAsync();

        var response = await CreateController(context).Update(
            existing.Id,
            new UpdateJobApplicationRequest
            {
                Company = "New Company",
                Position = "Senior Developer",
                Status = "offer",
                AppliedDate = DateTime.UtcNow,
                Salary = 150000
            });

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var updated = Assert.IsType<JobApplicationResponse>(ok.Value);

        Assert.Equal(existing.Id, updated.Id);
        Assert.Equal("New Company", updated.Company);
        Assert.Equal(JobApplicationStatuses.Offer, updated.Status);
    }

    [Fact]
    public async Task GetById_MissingApplication_ReturnsProblemDetails()
    {
        await using var context = CreateContext();

        var response = await CreateController(context).GetById(999);

        var result = Assert.IsType<ObjectResult>(response.Result);
        Assert.Equal(404, result.StatusCode);
        Assert.IsType<ProblemDetails>(result.Value);
    }

    private static JobApplication Application(
        string company,
        string position,
        string status,
        int daysAgo) => new()
        {
            Company = company,
            Position = position,
            Status = status,
            AppliedDate = DateTime.UtcNow.AddDays(-daysAgo)
        };
}
