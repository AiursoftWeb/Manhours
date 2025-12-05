using Aiursoft.Manhours.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.Manhours.Models.ContributionsViewModels;

public class MyContributionsViewModel : UiStackLayoutViewModel
{
    public MyContributionsViewModel()
    {
        PageTitle = "Contributions";
    }

    public string ContributorName { get; set; } = "My";
    public string Email { get; set; } = string.Empty;
    public User? User { get; set; }
    public double TotalWorkHours { get; set; }
    public int TotalCommits { get; set; }
    public int TotalActiveDays { get; set; }
    public List<RepoContribution> Contributions { get; set; } = new();
}
