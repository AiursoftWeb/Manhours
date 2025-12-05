using Aiursoft.UiStack.Layout;

namespace Aiursoft.Manhours.Models.ReposViewModels;

public class RepoDisplayModel
{
    public required string Name { get; set; }
    public required string Url { get; set; }
    public double TotalWorkHours { get; set; }
    public int ContributorCount { get; set; }
    public string? TopContributorEmail { get; set; }
    public string? TopContributorName { get; set; }
    public Guid? TopContributorId { get; set; }
    public bool ContributedByMe { get; set; }

    public string StatsUrl
    {
        get
        {
            var cleanUrl = Url.Replace("https://", "").Replace("http://", "");
            if (cleanUrl.EndsWith(".git")) cleanUrl = cleanUrl[..^4];
            return $"/r/{cleanUrl}.html";
        }
    }
}

public class TopReposViewModel : UiStackLayoutViewModel
{
    public TopReposViewModel()
    {
        PageTitle = "Top Repos";
    }

    public List<RepoDisplayModel> AllRepos { get; set; } = new();
    public List<RepoDisplayModel> MyTopRepos { get; set; } = new();

    // Pagination properties
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalRepos { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalRepos / PageSize);
}
