using Aiursoft.Manhours.Entities;
using Aiursoft.Manhours.Models.ContributionsViewModels;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Aiursoft.Manhours.Services;

namespace Aiursoft.Manhours.Controllers;

[Authorize]
public class ContributionsController(
    UserManager<User> userManager,
    TemplateDbContext dbContext) : Controller
{
    [RenderInNavBar(
        NavGroupName = "Home",
        NavGroupOrder = 1,
        CascadedLinksGroupName = "Home",
        CascadedLinksIcon = "home",
        CascadedLinksOrder = 1,
        LinkText = "My Contributions",
        LinkOrder = 2)]
    public async Task<IActionResult> MyContributions()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var contributor = await dbContext.Contributors
            .AsNoTracking()
            .Include(c => c.Contributions)
            .ThenInclude(rc => rc.Repo)
            .FirstOrDefaultAsync(c => c.Email == user.Email);

        var model = new MyContributionsViewModel
        {
            Email = user.Email ?? string.Empty,
            Contributions = contributor?.Contributions.ToList() ?? new List<RepoContribution>()
        };

        if (contributor != null)
        {
            model.TotalWorkHours = contributor.Contributions.Sum(c => c.TotalWorkHours);
            model.TotalCommits = contributor.Contributions.Sum(c => c.CommitCount);
            model.TotalActiveDays = contributor.Contributions.Sum(c => c.ActiveDays);
        }

        return this.StackView(model);
    }
}
