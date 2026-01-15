using Aiursoft.UiStack.Layout;

namespace Aiursoft.Manhours.Models.ManageViewModels;

public class IndexViewModel: UiStackLayoutViewModel
{
    public IndexViewModel()
    {
        PageTitle = "Manage";
    }

    public bool AllowUserAdjustNickname { get; set; }
}
