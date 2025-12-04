namespace Aiursoft.ManHours.Models;

public class ContributorStat
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public TimeSpan WorkTime { get; set; }
}

public class RepoStats
{
    public TimeSpan TotalWorkTime { get; set; }
    public List<ContributorStat> Contributors { get; set; } = new();
}
