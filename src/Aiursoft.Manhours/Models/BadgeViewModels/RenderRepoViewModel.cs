using Aiursoft.UiStack.Layout;

namespace Aiursoft.ManHours.Models.BadgeViewModels;

public class RenderRepoViewModel : UiStackLayoutViewModel
{
    public RenderRepoViewModel(string repoName)
    {
        PageTitle = repoName;
    }

    public required RepoStats Stats { get; init; }
    public string? CurrentUserEmail { get; set; }
}
