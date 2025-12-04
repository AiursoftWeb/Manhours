using Aiursoft.GitRunner.Models;
using Aiursoft.Scanner.Abstractions;

namespace Aiursoft.ManHours.Services;

public class WorkTimeService : ITransientDependency
{
    public static TimeSpan CalculateWorkTime(IEnumerable<Commit> commits)
    {
        var commitList = commits.ToList();
        if (commitList.Count == 0)
            return TimeSpan.Zero;

        var totalWorkTime = TimeSpan.Zero;
        var groups = commitList.GroupBy(t => t.Email.ToLower().Trim());

        foreach (var group in groups)
        {
            var commitTimes = group.Select(t => t.Time).ToList();
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
            totalWorkTime += currentWorkTime;
        }

        return totalWorkTime;
    }
}
