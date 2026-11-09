using Aiursoft.UiStack.Layout;

namespace Aiursoft.Manhours.Models.ReposViewModels;

public class TopReposViewModel : UiStackLayoutViewModel
{
    public TopReposViewModel()
    {
        PageTitle = "Top Repos";
    }

    public List<RepoDisplayModel> AllRepos { get; set; } = new();
    public List<PersonalRepoDisplayModel> MyTopRepos { get; set; } = new();

    // Pagination properties
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalRepos { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalRepos / PageSize);
}

public class PersonalRepoDisplayModel
{
    public required RepoDisplayModel Repository { get; set; }
    public double WorkHours { get; set; }
}
