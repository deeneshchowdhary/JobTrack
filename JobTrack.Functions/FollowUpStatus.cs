using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace JobTrack.Functions;

public class FollowUpStatus
{
    private readonly ILogger<FollowUpStatus> _logger;

    public FollowUpStatus(ILogger<FollowUpStatus> logger)
    {
        _logger = logger;
    }

    [Function("FollowUpStatus")]
    public async Task<IActionResult> Run(
        [HttpTrigger(
            AuthorizationLevel.Function,
            "get",
            Route = "follow-ups/status")]
        HttpRequest request)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("SqlConnectionString");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new ObjectResult(new
            {
                success = false,
                error = "SQL configuration is missing."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }

        const string sql = """
            SELECT COUNT(*)
            FROM [dbo].[JobApplications]
            WHERE [Status] = 'Applied'
              AND [AppliedDate] <= DATEADD(day, -7, SYSUTCDATETIME());
            """;

        try
        {
            await using var connection =
                new SqlConnection(connectionString);

            await connection.OpenAsync();

            await using var command =
                new SqlCommand(sql, connection);

            var result = await command.ExecuteScalarAsync();
            var count = Convert.ToInt32(result);

            _logger.LogInformation(
                "Follow-up status checked successfully. Count: {Count}",
                count);

            return new OkObjectResult(new
            {
                success = true,
                applicationsRequiringFollowUp = count,
                checkedAtUtc = DateTime.UtcNow
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Follow-up status check failed.");

            return new ObjectResult(new
            {
                success = false,
                error = exception.Message
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}