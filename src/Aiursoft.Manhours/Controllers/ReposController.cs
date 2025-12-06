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
    public async Task<IActionResult> Index([FromQuery] int page = 1)
    {
        var user = await userManager.GetUserAsync(User);
        var userEmail = user?.Email;

        const int pageSize = 20;
        page = Math.Max(1, page); // Ensure page is at least 1

        var repos = await dbContext.Repos
            .AsNoTracking()
            .Include(r => r.Contributions)
            .ThenInclude(c => c.Contributor)
            .ToListAsync();

        var allRepoModels = repos.Select(r =>
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

        var totalRepos = allRepoModels.Count;
        var pagedRepos = allRepoModels
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var myTopRepos = new List<RepoDisplayModel>();
        if (!string.IsNullOrEmpty(userEmail))
        {
            var myContributedRepos = allRepoModels
                .Where(r => r.ContributedByMe)
                .ToList();

            // Deduplicate forked/mirrored repositories
            myTopRepos = RepoDeduplicationService.Deduplicate(myContributedRepos)
                .Take(20)
                .ToList();
        }

        var model = new TopReposViewModel
        {
            AllRepos = pagedRepos,
            MyTopRepos = myTopRepos,
            CurrentPage = page,
            PageSize = pageSize,
            TotalRepos = totalRepos
        };

        return this.StackView(model);
    }
}
