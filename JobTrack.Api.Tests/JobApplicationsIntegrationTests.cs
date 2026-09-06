using System.Net;
using System.Net.Http.Json;
using JobTrack.Api.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JobTrack.Api.Tests;

public class JobApplicationsIntegrationTests
{
    [Fact]
    public async Task PostAndGet_RoundTripsApplicationThroughHttp()
    {
        await using var factory = new JobTrackApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/JobApplications",
            ValidCreateRequest());

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createResponse.Headers.Location);

        var created = await createResponse.Content
            .ReadFromJsonAsync<JobApplicationResponse>();
        Assert.NotNull(created);
        Assert.Equal("Example Corp", created.Company);
        Assert.Equal("Applied", created.Status);

        var getResponse = await client.GetAsync(createResponse.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetched = await getResponse.Content
            .ReadFromJsonAsync<JobApplicationResponse>();
        Assert.Equal(created, fetched);
    }

    [Fact]
    public async Task Post_MissingRequiredFields_ReturnsValidationProblemDetails()
    {
        await using var factory = new JobTrackApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/JobApplications",
            new { company = "", position = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content
            .ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("Company", problem.Errors.Keys);
        Assert.Contains("Position", problem.Errors.Keys);
    }

    [Fact]
    public async Task Get_InvalidPageSize_ReturnsValidationProblemDetails()
    {
        await using var factory = new JobTrackApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/JobApplications?pageSize=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content
            .ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("PageSize", problem.Errors.Keys);
    }

    [Fact]
    public async Task Get_FiltersSortsAndReturnsPaginationMetadata()
    {
        await using var factory = new JobTrackApiFactory();
        using var client = factory.CreateClient();

        await Create(client, "Beta Labs", "Platform Engineer", "Applied");
        await Create(client, "Alpha Labs", "Software Engineer", "Interview");
        await Create(client, "Gamma Studio", "Designer", "Interview");

        var page = await client.GetFromJsonAsync<
            PagedResponse<JobApplicationResponse>>(
            "/api/JobApplications?status=interview&search=engineer" +
            "&sortBy=company&sortDirection=asc&page=1&pageSize=1");

        Assert.NotNull(page);
        Assert.Single(page.Items);
        Assert.Equal("Alpha Labs", page.Items[0].Company);
        Assert.Equal(1, page.TotalItems);
        Assert.Equal(1, page.TotalPages);
    }

    [Fact]
    public async Task PutThenDelete_UpdatesAndRemovesApplicationThroughHttp()
    {
        await using var factory = new JobTrackApiFactory();
        using var client = factory.CreateClient();
        var created = await Create(
            client, "Old Company", "Developer", "Applied");

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/JobApplications/{created.Id}",
            new UpdateJobApplicationRequest
            {
                Company = "New Company",
                Position = "Senior Developer",
                Status = "offer",
                AppliedDate = created.AppliedDate,
                Salary = 150000
            });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content
            .ReadFromJsonAsync<JobApplicationResponse>();
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Offer", updated.Status);

        var deleteResponse = await client.DeleteAsync(
            $"/api/JobApplications/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync(
            $"/api/JobApplications/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        Assert.Equal("application/problem+json",
            getResponse.Content.Headers.ContentType?.MediaType);
    }

    private static CreateJobApplicationRequest ValidCreateRequest() => new()
    {
        Company = "Example Corp",
        Position = "Software Engineer",
        Status = "applied",
        Salary = 120000
    };

    private static async Task<JobApplicationResponse> Create(
        HttpClient client,
        string company,
        string position,
        string status)
    {
        var response = await client.PostAsJsonAsync(
            "/api/JobApplications",
            new CreateJobApplicationRequest
            {
                Company = company,
                Position = position,
                Status = status
            });

        response.EnsureSuccessStatusCode();
        return (await response.Content
            .ReadFromJsonAsync<JobApplicationResponse>())!;
    }
}
