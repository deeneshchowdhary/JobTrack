namespace JobTrack.Api.Models;

public static class JobApplicationStatuses
{
    public const string Saved = "Saved";
    public const string Applied = "Applied";
    public const string Interview = "Interview";
    public const string Offer = "Offer";
    public const string Rejected = "Rejected";
    public const string Withdrawn = "Withdrawn";

    public static readonly IReadOnlyList<string> All =
        [Saved, Applied, Interview, Offer, Rejected, Withdrawn];

    public static bool TryNormalize(string? value, out string normalized)
    {
        normalized = All.FirstOrDefault(status =>
            string.Equals(status, value?.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? string.Empty;

        return normalized.Length > 0;
    }
}
