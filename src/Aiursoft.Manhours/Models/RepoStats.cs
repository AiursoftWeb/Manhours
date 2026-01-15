namespace Aiursoft.Manhours.Models;

public class RepoStats
{
    public TimeSpan TotalWorkTime { get; set; }
    public List<ContributorStat> Contributors { get; set; } = new();
}
