using Aiursoft.Manhours.Entities;

namespace Aiursoft.Manhours.Models.ContributionsViewModels;

public class WeeklyRepoContribution
{
    public Repo? Repo { get; set; }
    public double TotalWorkHours { get; set; }
    public int CommitCount { get; set; }
    public int ActiveDays { get; set; }
}
