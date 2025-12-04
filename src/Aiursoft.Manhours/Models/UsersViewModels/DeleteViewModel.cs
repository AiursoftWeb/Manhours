using Aiursoft.Manhours.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.Manhours.Models.UsersViewModels;

public class DeleteViewModel : UiStackLayoutViewModel
{
    public DeleteViewModel()
    {
        PageTitle = "Delete User";
    }

    public required User User { get; set; }
}
