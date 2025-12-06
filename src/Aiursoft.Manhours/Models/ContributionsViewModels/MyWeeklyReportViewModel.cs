using Aiursoft.Manhours.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.Manhours.Models.ContributionsViewModels;

public class MyWeeklyReportViewModel : UiStackLayoutViewModel
{
    public MyWeeklyReportViewModel()
    {
        PageTitle = "My Weekly Report";
    }

    public string ContributorName { get; set; } = "My";
    public string Email { get; set; } = string.Empty;
    public User? User { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public double TotalWorkHours { get; set; }
    public int TotalCommits { get; set; }
    public int TotalActiveDays { get; set; }
    public List<WeeklyRepoContribution> Contributions { get; set; } = new();
    public bool Loading { get; set; }
}

public class WeeklyRepoContribution
{
    public Repo? Repo { get; set; }
    public double TotalWorkHours { get; set; }
    public int CommitCount { get; set; }
    public int ActiveDays { get; set; }
}
