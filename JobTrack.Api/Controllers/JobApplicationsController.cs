using System.Security.Claims;
using JobTrack.Api.Contracts;
using JobTrack.Api.Data;
using JobTrack.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace JobTrack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class JobApplicationsController : ControllerBase
{
    private static readonly HashSet<string> SortFields =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "appliedDate", "company", "position", "salary", "status"
        };

    private readonly JobTrackDbContext _context;
    private readonly ILogger<JobApplicationsController> _logger;

    public JobApplicationsController(
        JobTrackDbContext context,
        ILogger<JobApplicationsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<JobApplicationResponse>>> GetAll(
        [FromQuery] JobApplicationQuery request)
    {
        var userId = CurrentUserId;
        if (!SortFields.Contains(request.SortBy))
        {
            return InvalidField(nameof(request.SortBy),
                $"SortBy must be one of: {string.Join(", ", SortFields)}.");
        }

        if (!request.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase) &&
            !request.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase))
        {
            return InvalidField(nameof(request.SortDirection),
                "SortDirection must be either 'asc' or 'desc'.");
        }

        IQueryable<JobApplication> query =
            _context.JobApplications
                .AsNoTracking()
                .Where(application => application.UserId == userId);

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!JobApplicationStatuses.TryNormalize(request.Status, out var status))
            {
                return InvalidStatus(nameof(request.Status));
            }

            query = query.Where(application => application.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(application =>
                application.Company.ToLower().Contains(search) ||
                application.Position.ToLower().Contains(search));
        }

        var totalItems = await query.CountAsync();
        var descending = request.SortDirection.Equals(
            "desc", StringComparison.OrdinalIgnoreCase);
        query = ApplySorting(query, request.SortBy, descending);

        var applications = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return Ok(new PagedResponse<JobApplicationResponse>(
            applications.Select(JobApplicationResponse.FromEntity).ToList(),
            request.Page,
            request.PageSize,
            totalItems,
            (int)Math.Ceiling(totalItems / (double)request.PageSize)));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<JobApplicationResponse>> GetById(int id)
    {
        var userId = CurrentUserId;
        var application = await _context.JobApplications
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId);

        return application is null
            ? Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Job application not found",
                detail: $"No job application with ID {id} exists.")
            : Ok(JobApplicationResponse.FromEntity(application));
    }

    [HttpPost]
    public async Task<ActionResult<JobApplicationResponse>> Create(
        CreateJobApplicationRequest request)
    {
        var userId = CurrentUserId;
        if (!JobApplicationStatuses.TryNormalize(request.Status, out var status))
        {
            return InvalidStatus(nameof(request.Status));
        }

        var application = new JobApplication
        {
            Company = request.Company.Trim(),
            Position = request.Position.Trim(),
            Status = status,
            AppliedDate = (request.AppliedDate ?? DateTime.UtcNow).ToUniversalTime(),
            Salary = request.Salary,
            Notes = NormalizeOptionalText(request.Notes),
            UserId = userId
        };

        _context.JobApplications.Add(application);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created job application {ApplicationId} for {Company}",
            application.Id, application.Company);

        var response = JobApplicationResponse.FromEntity(application);
        return CreatedAtAction(nameof(GetById), new { id = application.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<JobApplicationResponse>> Update(
        int id,
        UpdateJobApplicationRequest request)
    {
        var userId = CurrentUserId;
        if (!JobApplicationStatuses.TryNormalize(request.Status, out var status))
        {
            return InvalidStatus(nameof(request.Status));
        }

        var application = await _context.JobApplications
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId);
        if (application is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Job application not found",
                detail: $"No job application with ID {id} exists.");
        }

        application.Company = request.Company.Trim();
        application.Position = request.Position.Trim();
        application.Status = status;
        application.AppliedDate = request.AppliedDate!.Value.ToUniversalTime();
        application.Salary = request.Salary;
        application.Notes = NormalizeOptionalText(request.Notes);

        await _context.SaveChangesAsync();
        return Ok(JobApplicationResponse.FromEntity(application));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = CurrentUserId;
        var application = await _context.JobApplications
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId);
        if (application is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Job application not found",
                detail: $"No job application with ID {id} exists.");
        }

        _context.JobApplications.Remove(application);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<IReadOnlyList<StatusCountResponse>>> GetDashboard()
    {
        var userId = CurrentUserId;
        var counts = await _context.JobApplications
            .AsNoTracking()
            .Where(application => application.UserId == userId)
            .GroupBy(application => application.Status)
            .Select(group => new StatusCountResponse(group.Key, group.Count()))
            .OrderBy(item => item.Status)
            .ToListAsync();

        return Ok(counts);
    }

    private ActionResult InvalidStatus(string fieldName) =>
        InvalidField(fieldName,
            $"Status must be one of: {string.Join(", ", JobApplicationStatuses.All)}.");

    private ActionResult InvalidField(string fieldName, string error) =>
        BadRequest(new ValidationProblemDetails(
            new Dictionary<string, string[]> { [fieldName] = [error] })
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred."
        });

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException(
            "The authenticated user has no name identifier claim.");

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IQueryable<JobApplication> ApplySorting(
        IQueryable<JobApplication> query,
        string sortBy,
        bool descending) => sortBy.ToLowerInvariant() switch
        {
            "company" => descending
                ? query.OrderByDescending(item => item.Company).ThenByDescending(item => item.Id)
                : query.OrderBy(item => item.Company).ThenBy(item => item.Id),
            "position" => descending
                ? query.OrderByDescending(item => item.Position).ThenByDescending(item => item.Id)
                : query.OrderBy(item => item.Position).ThenBy(item => item.Id),
            "salary" => descending
                ? query.OrderByDescending(item => item.Salary).ThenByDescending(item => item.Id)
                : query.OrderBy(item => item.Salary).ThenBy(item => item.Id),
            "status" => descending
                ? query.OrderByDescending(item => item.Status).ThenByDescending(item => item.Id)
                : query.OrderBy(item => item.Status).ThenBy(item => item.Id),
            _ => descending
                ? query.OrderByDescending(item => item.AppliedDate).ThenByDescending(item => item.Id)
                : query.OrderBy(item => item.AppliedDate).ThenBy(item => item.Id)
        };
}
