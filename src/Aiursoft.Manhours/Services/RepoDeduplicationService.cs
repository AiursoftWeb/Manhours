using Aiursoft.Manhours.Entities;
using Aiursoft.Manhours.Models.ContributionsViewModels;

namespace Aiursoft.Manhours.Services;

/// <summary>
/// Service for deduplicating forked/mirrored repositories in contribution statistics.
/// </summary>
public static class RepoDeduplicationService
{
    /// <summary>
    /// Deduplicates RepoContributions by removing forked/mirrored repositories.
    /// Keeps the one with more commits when duplicates are found.
    /// </summary>
    /// <param name="contributions">List of repo contributions to deduplicate</param>
    /// <returns>Deduplicated list of contributions</returns>
    public static List<RepoContribution> Deduplicate(IEnumerable<RepoContribution> contributions)
    {
        var contributionsList = contributions.ToList();
        var result = new List<RepoContribution>();
        var processed = new HashSet<Guid>();

        foreach (var contribution in contributionsList.OrderByDescending(c => c.CommitCount))
        {
            if (processed.Contains(contribution.Id))
            {
                continue;
            }

            // Find potential duplicates
            var repoName = GetRepoName(contribution.Repo?.Url);
            var duplicates = contributionsList
                .Where(c => !processed.Contains(c.Id) &&
                           c.Id != contribution.Id &&
                           GetRepoName(c.Repo?.Url) == repoName &&
                           Math.Abs(c.CommitCount - contribution.CommitCount) <= 2)
                .ToList();

            // Mark all duplicates as processed
            foreach (var duplicate in duplicates)
            {
                processed.Add(duplicate.Id);
            }

            // Add the current contribution (which has more commits due to ordering)
            result.Add(contribution);
            processed.Add(contribution.Id);
        }

        return result;
    }

    /// <summary>
    /// Deduplicates WeeklyRepoContributions by removing forked/mirrored repositories.
    /// Keeps the one with more commits when duplicates are found.
    /// </summary>
    /// <param name="contributions">List of weekly repo contributions to deduplicate</param>
    /// <returns>Deduplicated list of contributions</returns>
    public static List<WeeklyRepoContribution> Deduplicate(IEnumerable<WeeklyRepoContribution> contributions)
    {
        var contributionsList = contributions.ToList();
        var result = new List<WeeklyRepoContribution>();

        foreach (var contribution in contributionsList.OrderByDescending(c => c.CommitCount))
        {
            var repoName = GetRepoName(contribution.Repo?.Url);

            // Check if we already have a repo with this name
            var existingIndex = result.FindIndex(c => GetRepoName(c.Repo?.Url) == repoName);

            if (existingIndex >= 0)
            {
                var existing = result[existingIndex];

                // Check if this is a potential duplicate (commit count difference <= 2)
                if (Math.Abs(contribution.CommitCount - existing.CommitCount) > 2)
                {
                    // Different repos with same name, keep both
                    result.Add(contribution);
                }
                // Otherwise skip this duplicate (already have the better one)
            }
            else
            {
                // New repo, add it
                result.Add(contribution);
            }
        }

        return result;
    }

    /// <summary>
    /// Extracts the repository name from a URL.
    /// Example: "https://gitlab.aiursoft.com/aiursoft/kahla.git" -> "kahla"
    /// </summary>
    /// <param name="url">Repository URL</param>
    /// <returns>Repository name</returns>
    private static string GetRepoName(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        // Remove protocol
        var normalized = url.Replace("https://", "").Replace("http://", "");

        // Remove .git suffix
        if (normalized.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^4];
        }

        // Get the last part of the path (repo name)
        var parts = normalized.Split('/');
        return parts.Length > 0 ? parts[^1].ToLowerInvariant() : string.Empty;
    }
}
