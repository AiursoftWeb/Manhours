namespace Aiursoft.ManHours.Models;

public class ContributorStat
{
    public Guid? Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public TimeSpan WorkTime { get; set; }
    public int CommitCount { get; set; }
    public int ContributionDays { get; set; }
}

public class RepoStats
{
    public TimeSpan TotalWorkTime { get; set; }
    public List<ContributorStat> Contributors { get; set; } = new();
}
