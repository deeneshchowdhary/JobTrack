using JobTrack.Api.Data;
using JobTrack.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobApplicationsController : ControllerBase
{
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
    public async Task<ActionResult<IEnumerable<JobApplication>>> GetAll(
        [FromQuery] string? status)
    {
        IQueryable<JobApplication> query =
            _context.JobApplications.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        return await query
            .OrderByDescending(x => x.AppliedDate)
            .ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<JobApplication>> GetById(int id)
    {
        var application = await _context.JobApplications.FindAsync(id);

        return application is null
            ? NotFound()
            : Ok(application);
    }

    [HttpPost]
    public async Task<ActionResult<JobApplication>> Create(
        JobApplication application)
    {
        application.Id = 0;
        application.AppliedDate = application.AppliedDate.ToUniversalTime();

        _context.JobApplications.Add(application);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created job application {ApplicationId} for {Company}",
            application.Id,
            application.Company);

        return CreatedAtAction(
            nameof(GetById),
            new { id = application.Id },
            application);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        JobApplication application)
    {
        if (id != application.Id)
        {
            return BadRequest();
        }

        _context.Entry(application).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.JobApplications.AnyAsync(x => x.Id == id))
            {
                return NotFound();
            }

            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var application = await _context.JobApplications.FindAsync(id);

        if (application is null)
        {
            return NotFound();
        }

        _context.JobApplications.Remove(application);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var counts = await _context.JobApplications
            .AsNoTracking()
            .GroupBy(x => x.Status)
            .Select(group => new
            {
                status = group.Key,
                count = group.Count()
            })
            .ToListAsync();

        return Ok(counts);
    }
}