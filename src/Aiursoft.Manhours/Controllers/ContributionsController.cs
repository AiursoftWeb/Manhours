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
    ManhoursDbContext dbContext,
    RepoService repoService,
    Services.Background.IBackgroundTaskQueue backgroundQueue) : Controller
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

        var emails = await GetUserContributionEmailsAsync(user);
        return await RenderContributions(emails, "My", user);
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

        var (model, loading) = await BuildWeeklyReportViewModel(user);
        model.Loading = loading;

        return this.StackView(model, "MyWeeklyReport");
    }

    [Route("weekly-report-status")]
    public async Task<IActionResult> WeeklyReportStatus()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        // Simple long polling: wait up to 10 seconds for completion
        for (var i = 0; i < 20; i++)
        {
            var (_, loading) = await BuildWeeklyReportViewModel(user);
            if (!loading)
            {
                return Json(new { loading = false });
            }
            await Task.Delay(500);
        }

        return Json(new { loading = true });
    }

    private async Task<(MyWeeklyReportViewModel, bool)> BuildWeeklyReportViewModel(User user)
    {
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-7);

        var emails = await GetUserContributionEmailsAsync(user);
        var normalizedEmails = emails.Select(e => e.ToLowerInvariant()).ToList();

        var contributions = await dbContext.RepoContributions
            .AsNoTracking()
            .Include(c => c.Repo)
            .Include(c => c.Contributor)
            .Where(c => normalizedEmails.Contains(c.Contributor!.Email.ToLower()))
            .ToListAsync();

        var weeklyContributions = new List<WeeklyRepoContribution>();
        var loading = false;

        var contributionsByRepo = contributions
            .Where(c => c.Repo != null)
            .GroupBy(c => c.Repo!.Id);

        foreach (var repoGroup in contributionsByRepo)
        {
            var contribution = repoGroup.First();
            var repo = contribution.Repo!;
            var repoUrl = repo.Url;
            var repoName = repoUrl.Replace("https://", "").Replace("http://", "");
            if (repoName.EndsWith(".git"))
            {
                repoName = repoName[..^4];
            }

            if (repoService.TryGetCachedStats(repoUrl, startDate, endDate, out var stats))
            {
                foreach (var email in emails)
                {
                    var contributorStat = stats?.Contributors.FirstOrDefault(c =>
                        c.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

                    if (contributorStat != null && contributorStat.WorkTime.TotalHours > 0)
                    {
                        weeklyContributions.Add(new WeeklyRepoContribution
                        {
                            Repo = repo,
                            Email = email,
                            TotalWorkHours = contributorStat.WorkTime.TotalHours,
                            CommitCount = contributorStat.CommitCount,
                            ActiveDays = contributorStat.ContributionDays
                        });
                    }
                }
            }
            else
            {
                loading = true;
                await backgroundQueue.QueueBackgroundWorkItemAsync(new Services.Background.RepoUpdateTask
                {
                    RepoName = repoName,
                    RepoUrl = repoUrl,
                    StartDate = startDate,
                    EndDate = endDate
                });
            }
        }

        // Deduplicate forked/mirrored repositories
        weeklyContributions = RepoDeduplicationService.Deduplicate(weeklyContributions);

        weeklyContributions = weeklyContributions
            .OrderByDescending(r => r.TotalWorkHours)
            .ToList();

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

        return (model, loading);
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
        var emails = string.IsNullOrWhiteSpace(email) ? Array.Empty<string>() : [email];
        var user = await userManager.FindByEmailAsync(email ?? string.Empty);
        return await RenderContributions(emails, name, user);
    }

    private async Task<IActionResult> RenderContributions(IEnumerable<string> emails, string name, User? user)
    {
        var emailList = emails
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var normalizedEmails = emailList.Select(e => e.ToLowerInvariant()).ToList();

        var contributions = await dbContext.RepoContributions
            .AsNoTracking()
            .Include(c => c.Repo)
            .Include(c => c.Contributor)
            .Where(c => normalizedEmails.Contains(c.Contributor!.Email.ToLower()))
            .OrderByDescending(c => c.TotalWorkHours)
            .ToListAsync();

        var contributionRows = contributions
            .GroupBy(c => c.Contributor!.Email, StringComparer.OrdinalIgnoreCase)
            .SelectMany(g => RepoDeduplicationService.Deduplicate(g))
            .Select(c => new RepoContributionViewModel
            {
                Repo = c.Repo,
                Email = c.Contributor!.Email,
                TotalWorkHours = c.TotalWorkHours,
                CommitCount = c.CommitCount,
                ActiveDays = c.ActiveDays
            })
            .OrderByDescending(c => c.TotalWorkHours)
            .ToList();

        var model = new MyContributionsViewModel
        {
            ContributorName = name,
            Email = emailList.FirstOrDefault() ?? string.Empty,
            User = user,
            TotalWorkHours = contributionRows.Sum(c => c.TotalWorkHours),
            TotalCommits = contributionRows.Sum(c => c.CommitCount),
            TotalActiveDays = contributionRows.Sum(c => c.ActiveDays),
            Contributions = contributionRows
        };

        return this.StackView(model, "MyContributions");
    }

    private async Task<List<string>> GetUserContributionEmailsAsync(User user)
    {
        var emails = await dbContext.UserEmails
            .AsNoTracking()
            .Where(e => e.UserId == user.Id)
            .Select(e => e.Email)
            .ToListAsync();

        return emails
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
