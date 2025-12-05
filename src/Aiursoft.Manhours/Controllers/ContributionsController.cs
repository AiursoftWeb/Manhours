using Aiursoft.Manhours.Entities;
using Aiursoft.Manhours.Models.ContributionsViewModels;
using Aiursoft.UiStack.Navigation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Aiursoft.Manhours.Services;

namespace Aiursoft.Manhours.Controllers;

[Route("contributions")]
public class ContributionsController(
    UserManager<User> userManager,
    TemplateDbContext dbContext) : Controller
{
    [Authorize]
    [Route("mycontributions")]
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

        return await RenderContributions(user.Email, "My");
    }

    [Route("id/{id}")]
    public async Task<IActionResult> UserContributions(Guid id)
    {
        var contributor = await dbContext.Contributors.FindAsync(id);
        if (contributor == null)
        {
            return NotFound();
        }

        return await RenderContributions(contributor.Email, contributor.Name ?? "Unknown");
    }

    private async Task<IActionResult> RenderContributions(string? email, string name)
    {
        var contributions = await dbContext.RepoContributions
            .AsNoTracking()
            .Include(c => c.Repo)
            .Include(c => c.Contributor)
            .Where(c => c.Contributor!.Email == email)
            .OrderByDescending(c => c.TotalWorkHours)
            .ToListAsync();

        var model = new MyContributionsViewModel
        {
            ContributorName = name,
            TotalWorkHours = contributions.Sum(c => c.TotalWorkHours),
            TotalCommits = contributions.Sum(c => c.CommitCount),
            TotalActiveDays = contributions.Sum(c => c.ActiveDays),
            Contributions = contributions
        };

        return this.StackView(model, "MyContributions");
    }
}
