using Aiursoft.UiStack.Layout;
using Aiursoft.Manhours.Entities;

namespace Aiursoft.Manhours.Models.ContributionsViewModels;

public class MyContributionsViewModel : UiStackLayoutViewModel
{
    public MyContributionsViewModel()
    {
        PageTitle = "My Contributions";
    }

    public string Email { get; set; } = string.Empty;
    public double TotalWorkHours { get; set; }
    public int TotalCommits { get; set; }
    public int TotalActiveDays { get; set; }
    public List<RepoContribution> Contributions { get; set; } = new();
}
