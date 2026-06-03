using Aiursoft.Manhours.Entities;

namespace Aiursoft.Manhours.Models.ContributionsViewModels;

public class RepoContributionViewModel
{
    public Repo? Repo { get; set; }
    public string Email { get; set; } = string.Empty;
    public double TotalWorkHours { get; set; }
    public int CommitCount { get; set; }
    public int ActiveDays { get; set; }
}
