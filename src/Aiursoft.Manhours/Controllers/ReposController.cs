using Aiursoft.Manhours.Entities;
using Aiursoft.Manhours.Models.ReposViewModels;
using Aiursoft.Manhours.Services;
using Aiursoft.UiStack.Navigation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.Manhours.Controllers;

[Route("repos")]
public class ReposController(
    TemplateDbContext dbContext,
    UserManager<User> userManager) : Controller
{
    [Route("index")]
    [RenderInNavBar(
        NavGroupName = "Home",
        NavGroupOrder = 1,
        CascadedLinksGroupName = "Home",
        CascadedLinksIcon = "home",
        CascadedLinksOrder = 1,
        LinkText = "Top Repos",
        LinkOrder = 3)]
    public async Task<IActionResult> Index()
    {
        var user = await userManager.GetUserAsync(User);
        var userEmail = user?.Email;

        var repos = await dbContext.Repos
            .AsNoTracking()
            .Include(r => r.Contributions)
            .ThenInclude(c => c.Contributor)
            .ToListAsync();

        var repoModels = repos.Select(r =>
        {
            var topContribution = r.Contributions.MaxBy(c => c.TotalWorkHours);
            return new RepoDisplayModel
            {
                Name = r.Name ?? r.Url,
                Url = r.Url,
                TotalWorkHours = r.Contributions.Sum(c => c.TotalWorkHours),
                ContributorCount = r.Contributions.Count(),
                TopContributorEmail = topContribution?.Contributor?.Email,
                TopContributorName = topContribution?.Contributor?.Name,
                TopContributorId = topContribution?.Contributor?.Id,
                ContributedByMe = !string.IsNullOrEmpty(userEmail) &&
                                  r.Contributions.Any(c => string.Equals(c.Contributor?.Email, userEmail, StringComparison.OrdinalIgnoreCase))
            };
        })
        .OrderByDescending(r => r.TotalWorkHours)
        .ToList();

        var myTopRepos = new List<RepoDisplayModel>();
        if (!string.IsNullOrEmpty(userEmail))
        {
            myTopRepos = repoModels
                .Where(r => r.ContributedByMe)
                .Take(3)
                .ToList();
        }

        var model = new TopReposViewModel
        {
            AllRepos = repoModels,
            MyTopRepos = myTopRepos
        };

        return this.StackView(model);
    }
}
