using Aiursoft.Manhours.Entities;

namespace Aiursoft.Manhours.Models.UsersViewModels;

public class UserWithRolesViewModel
{
    public required User User { get; set; }
    public required IList<string> Roles { get; set; }
}
