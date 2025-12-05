using Aiursoft.GitRunner.Models;
using Aiursoft.ManHours.Models;
using Aiursoft.Scanner.Abstractions;

namespace Aiursoft.ManHours.Services;

public class WorkTimeService : ITransientDependency
{
    public static RepoStats CalculateWorkTime(IEnumerable<Commit> commits)
    {
        var commitList = commits.ToList();
        var stats = new RepoStats();

        if (commitList.Count == 0)
            return stats;

        var groups = commitList.GroupBy(t => t.Email.ToLower().Trim());

        foreach (var group in groups)
        {
            var authorCommits = group.ToList();
            var commitTimes = authorCommits.Select(t => t.Time).ToList();
            commitTimes.Sort();

            var previousCommitTime = commitTimes[0];
            var currentWorkTime = TimeSpan.FromMinutes(30);
            for (var i = 1; i < commitTimes.Count; i++)
            {
                var timeBetweenCommits = commitTimes[i] - previousCommitTime;

                if (timeBetweenCommits.TotalMinutes <= 30)
                {
                    currentWorkTime += timeBetweenCommits;
                }
                else
                {
                    currentWorkTime += TimeSpan.FromMinutes(30);
                }

                previousCommitTime = commitTimes[i];
            }

            // Calculate commit count
            var commitCount = authorCommits.Count;

            // Calculate contribution days (unique UTC dates)
            var contributionDays = commitTimes
                .Select(t => t.Date) // Get only the date part (UTC)
                .Distinct()
                .Count();

            stats.TotalWorkTime += currentWorkTime;
            stats.Contributors.Add(new ContributorStat
            {
                Name = authorCommits.First().Author, // Use the first name found for this email
                Email = group.Key,
                WorkTime = currentWorkTime,
                CommitCount = commitCount,
                ContributionDays = contributionDays
            });
        }

        // Sort contributors by work time descending
        stats.Contributors = stats.Contributors.OrderByDescending(c => c.WorkTime).ToList();

        return stats;
    }

    public static RepoStats CalculateWorkTimeInRange(IEnumerable<Commit> commits, DateTime startDate, DateTime endDate)
    {
        // Filter commits within the date range
        var filteredCommits = commits.Where(c => c.Time >= startDate && c.Time <= endDate);
        return CalculateWorkTime(filteredCommits);
    }
}

