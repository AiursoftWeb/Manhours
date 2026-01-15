namespace Aiursoft.Manhours.Models;

public class ContributorStat
{
    public Guid? Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public TimeSpan WorkTime { get; set; }
    public int CommitCount { get; set; }
    public int ContributionDays { get; set; }
}
