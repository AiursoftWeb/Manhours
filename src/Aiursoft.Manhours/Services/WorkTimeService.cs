using Aiursoft.Scanner.Abstractions;

namespace Aiursoft.ManHours.Services;

public class WorkTimeService : ITransientDependency
{
    public static TimeSpan CalculateWorkTime(List<DateTime> commitTimes)
    {
        if (commitTimes.Count == 0)
            return TimeSpan.Zero;

        commitTimes.Sort();

        var previousCommitTime = commitTimes[0];
        var totalWorkTime = TimeSpan.FromMinutes(30);
        for (var i = 1; i < commitTimes.Count; i++)
        {
            var timeBetweenCommits = commitTimes[i] - previousCommitTime;

            if (timeBetweenCommits.TotalMinutes <= 30)
            {
                totalWorkTime += timeBetweenCommits;
            }
            else
            {
                totalWorkTime += TimeSpan.FromMinutes(30);
            }

            previousCommitTime = commitTimes[i];
        }
        return totalWorkTime;
    }
}
