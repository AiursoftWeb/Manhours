using Microsoft.AspNetCore.Identity;

namespace Aiursoft.Manhours.Models.RolesViewModels;

public class IdentityRoleWithCount
{
    public required IdentityRole Role { get; init; }
    public required int UserCount { get; init; }
}
