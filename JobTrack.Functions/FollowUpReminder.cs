using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace JobTrack.Functions;

public class FollowUpReminder
{
    private readonly ILogger<FollowUpReminder> _logger;

    public FollowUpReminder(ILogger<FollowUpReminder> logger)
    {
        _logger = logger;
    }

    [Function("FollowUpReminder")]
    public async Task Run(
        [TimerTrigger("0 */5 * * * *")] TimerInfo timer)
    {
        _logger.LogInformation(
            "JobTrack follow-up check started at {Time}",
            DateTime.UtcNow);

        var connectionString =
            Environment.GetEnvironmentVariable("SqlConnectionString");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogError(
                "The SqlConnectionString setting is missing.");

            return;
        }

        const string sql = """
            SELECT
                [Id],
                [Company],
                [Position],
                [AppliedDate]
            FROM [dbo].[JobApplications]
            WHERE [Status] = 'Applied'
              AND [AppliedDate] <= DATEADD(day, -7, SYSUTCDATETIME())
            ORDER BY [AppliedDate];
            """;

        try
        {
            await using var connection =
                new SqlConnection(connectionString);

            await connection.OpenAsync();

            await using var command =
                new SqlCommand(sql, connection);

            await using var reader =
                await command.ExecuteReaderAsync();

            var followUpCount = 0;

            while (await reader.ReadAsync())
            {
                followUpCount++;

                var id = reader.GetInt32(0);
                var company = reader.GetString(1);
                var position = reader.GetString(2);
                var appliedDate = reader.GetDateTime(3);

                _logger.LogInformation(
                    "Follow-up required: Application {Id}, " +
                    "{Company}, {Position}, applied {AppliedDate}",
                    id,
                    company,
                    position,
                    appliedDate);
            }

            _logger.LogInformation(
                "JobTrack follow-up check completed. " +
                "{Count} application(s) require follow-up.",
                followUpCount);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "JobTrack follow-up check failed.");

            throw;
        }
    }
}