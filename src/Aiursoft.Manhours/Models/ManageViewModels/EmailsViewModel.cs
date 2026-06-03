using Aiursoft.Manhours.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.Manhours.Models.ManageViewModels;

public class EmailsViewModel : UiStackLayoutViewModel
{
    public EmailsViewModel()
    {
        PageTitle = "Manage Emails";
    }

    public List<UserEmail> Emails { get; set; } = [];
}
