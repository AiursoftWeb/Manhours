using Aiursoft.Manhours.Authorization;
using Aiursoft.Manhours.Entities;
using Aiursoft.UiStack.Layout;
using Microsoft.AspNetCore.Identity;

namespace Aiursoft.Manhours.Models.UsersViewModels;

public class DetailsViewModel : UiStackLayoutViewModel
{
    public DetailsViewModel()
    {
    }

    public DetailsViewModel(string userDisplayName)
    {
        PageTitle = userDisplayName;
    }

    public required User User { get; set; }

    public required IList<IdentityRole> Roles { get; set; }

    public required List<PermissionDescriptor> Permissions { get; set; }
}
