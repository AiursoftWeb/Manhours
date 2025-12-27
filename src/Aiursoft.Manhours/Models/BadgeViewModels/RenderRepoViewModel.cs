using Aiursoft.UiStack.Layout;

namespace Aiursoft.Manhours.Models.BadgeViewModels;

public class RenderRepoViewModel : UiStackLayoutViewModel
{
    public RenderRepoViewModel(string repoName)
    {
        PageTitle = repoName;
    }

    public required RepoStats Stats { get; init; }
    public string? CurrentUserEmail { get; set; }

    // Pagination properties
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalContributors { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalContributors / PageSize);
}
