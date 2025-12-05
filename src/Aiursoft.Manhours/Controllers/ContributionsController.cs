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
    TemplateDbContext dbContext,
    RepoService repoService) : Controller
{
    [Authorize]
    [Route("mycontributions")]
    [RenderInNavBar(
        NavGroupName = "Settings",
        NavGroupOrder = 9998,
        CascadedLinksGroupName = "Personal",
        CascadedLinksIcon = "user-circle",
        CascadedLinksOrder = 1,
        LinkText = "My Contributions",
        LinkOrder = 0)]
    public async Task<IActionResult> MyContributions()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        return await RenderContributions(user.Email, "My");
    }

    [Authorize]
    [Route("myweeklyreport")]
    [RenderInNavBar(
        NavGroupName = "Settings",
        NavGroupOrder = 9998,
        CascadedLinksGroupName = "Personal",
        CascadedLinksIcon = "user-circle",
        CascadedLinksOrder = 1,
        LinkText = "My Weekly Report",
        LinkOrder = 1)]
    public async Task<IActionResult> MyWeeklyReport()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-7);

        // Get all repos that the user has contributed to
        var contributions = await dbContext.RepoContributions
            .AsNoTracking()
            .Include(c => c.Repo)
            .Include(c => c.Contributor)
            .Where(c => c.Contributor!.Email == user.Email)
            .ToListAsync();

        List<WeeklyRepoContribution> weeklyContributions;

        // Process repos in parallel for better performance
        var tasks = contributions
            .Where(c => c.Repo != null)
            .Select(async contribution =>
            {
                try
                {
                    var repo = contribution.Repo!;
                    var repoUrl = repo.Url;
                    var repoName = repoUrl.Replace("https://", "").Replace("http://", "");
                    if (repoName.EndsWith(".git"))
                    {
                        repoName = repoName[..^4];
                    }

                    // Get stats for the last 7 days - this will use cache if available
                    var stats = await repoService.GetRepoStatsInRangeAsync(repoName, repoUrl, startDate, endDate);

                    // Find the contributor's stats in the result
                    var contributorStat = stats.Contributors.FirstOrDefault(c =>
                        c.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase));

                    if (contributorStat != null && contributorStat.WorkTime.TotalHours > 0)
                    {
                        return new WeeklyRepoContribution
                        {
                            Repo = repo,
                            TotalWorkHours = contributorStat.WorkTime.TotalHours,
                            CommitCount = contributorStat.CommitCount,
                            ActiveDays = contributorStat.ContributionDays
                        };
                    }

                    return null;
                }
                catch
                {
                    // Skip repos that fail to load
                    return null;
                }
            });

        var results = await Task.WhenAll(tasks);
        weeklyContributions = results
            .Where(r => r != null && r.TotalWorkHours > 0)
            .OrderByDescending(r => r!.TotalWorkHours)
            .ToList()!;

        var model = new MyWeeklyReportViewModel
        {
            ContributorName = user.DisplayName,
            Email = user.Email ?? string.Empty,
            User = user,
            StartDate = startDate,
            EndDate = endDate,
            TotalWorkHours = weeklyContributions.Sum(c => c.TotalWorkHours),
            TotalCommits = weeklyContributions.Sum(c => c.CommitCount),
            TotalActiveDays = weeklyContributions.Sum(c => c.ActiveDays),
            Contributions = weeklyContributions
        };

        return this.StackView(model, "MyWeeklyReport");
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

        var user = await userManager.FindByEmailAsync(email ?? string.Empty);

        var model = new MyContributionsViewModel
        {
            ContributorName = name,
            Email = email ?? string.Empty,
            User = user,
            TotalWorkHours = contributions.Sum(c => c.TotalWorkHours),
            TotalCommits = contributions.Sum(c => c.CommitCount),
            TotalActiveDays = contributions.Sum(c => c.ActiveDays),
            Contributions = contributions
        };

        return this.StackView(model, "MyContributions");
    }
}
