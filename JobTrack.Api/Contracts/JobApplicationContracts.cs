using System.ComponentModel.DataAnnotations;
using JobTrack.Api.Models;

namespace JobTrack.Api.Contracts;

public sealed class CreateJobApplicationRequest
{
    [Required, MaxLength(150)] public string Company { get; init; } = string.Empty;
    [Required, MaxLength(150)] public string Position { get; init; } = string.Empty;
    [MaxLength(50)] public string Status { get; init; } = JobApplicationStatuses.Applied;
    public DateTime? AppliedDate { get; init; }
    [Range(0, 9999999999999999.99)] public decimal? Salary { get; init; }
    [MaxLength(1000)] public string? Notes { get; init; }
}

public sealed class UpdateJobApplicationRequest
{
    [Required, MaxLength(150)] public string Company { get; init; } = string.Empty;
    [Required, MaxLength(150)] public string Position { get; init; } = string.Empty;
    [Required, MaxLength(50)] public string Status { get; init; } = string.Empty;
    [Required] public DateTime? AppliedDate { get; init; }
    [Range(0, 9999999999999999.99)] public decimal? Salary { get; init; }
    [MaxLength(1000)] public string? Notes { get; init; }
}

public sealed record JobApplicationResponse(
    int Id, string Company, string Position, string Status,
    DateTime AppliedDate, decimal? Salary, string? Notes)
{
    public static JobApplicationResponse FromEntity(JobApplication application) =>
        new(application.Id, application.Company, application.Position,
            application.Status, application.AppliedDate, application.Salary,
            application.Notes);
}

public sealed record StatusCountResponse(string Status, int Count);

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items, int Page, int PageSize,
    int TotalItems, int TotalPages);

public sealed class JobApplicationQuery
{
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;
    [MaxLength(150)] public string? Search { get; init; }
    [MaxLength(50)] public string? Status { get; init; }
    public string SortBy { get; init; } = "appliedDate";
    public string SortDirection { get; init; } = "desc";
}
