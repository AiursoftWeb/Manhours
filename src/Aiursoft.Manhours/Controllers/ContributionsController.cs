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

        var allEmails = await GetAllUserEmailsAsync(user);
        return await RenderContributions(allEmails, "My", user);
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

        // Collect all emails associated with this user: primary email + additional emails from UserEmails table.
        var allEmails = await GetAllUserEmailsAsync(user);

        // Get all repos that the user has contributed to via any of their emails
        var contributions = await dbContext.RepoContributions
            .AsNoTracking()
            .Include(c => c.Repo)
            .Include(c => c.Contributor)
            .Where(c => allEmails.Contains(c.Contributor!.Email))
            .ToListAsync();

        var weeklyContributions = new List<WeeklyRepoContribution>();
        var loading = false;

        // Group contributions by repo so we can merge stats from multiple emails for the same repo
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
                // Aggregate stats from all emails belonging to this user in this repo
                double totalHours = 0;
                int totalCommits = 0;
                int totalActiveDays = 0;

                foreach (var email in allEmails)
                {
                    var contributorStat = stats?.Contributors.FirstOrDefault(c =>
                        c.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

                    if (contributorStat != null && contributorStat.WorkTime.TotalHours > 0)
                    {
                        totalHours += contributorStat.WorkTime.TotalHours;
                        totalCommits += contributorStat.CommitCount;
                        totalActiveDays = Math.Max(totalActiveDays, contributorStat.ContributionDays);
                    }
                }

                if (totalHours > 0)
                {
                    weeklyContributions.Add(new WeeklyRepoContribution
                    {
                        Repo = repo,
                        TotalWorkHours = totalHours,
                        CommitCount = totalCommits,
                        ActiveDays = totalActiveDays
                    });
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
        if (contributor == null) return NotFound();

        var emailSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(contributor.Email))
            emailSet.Add(contributor.Email);

        return await RenderContributions(emailSet, contributor.Name ?? "Unknown");
    }

    private async Task<HashSet<string>> GetAllUserEmailsAsync(User user)
    {
        var userEmails = await dbContext.UserEmails
            .AsNoTracking()
            .Where(e => e.UserId == user.Id)
            .Select(e => e.Email)
            .ToListAsync();

        var allEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(user.Email))
            allEmails.Add(user.Email);
        foreach (var email in userEmails)
        {
            if (!string.IsNullOrEmpty(email))
                allEmails.Add(email);
        }
        return allEmails;
    }

    private async Task<IActionResult> RenderContributions(HashSet<string> emails, string name, User? user = null)
    {
        var contributions = await dbContext.RepoContributions
            .AsNoTracking()
            .Include(c => c.Repo)
            .Include(c => c.Contributor)
            .Where(c => emails.Contains(c.Contributor!.Email))
            .OrderByDescending(c => c.TotalWorkHours)
            .ToListAsync();

        // Deduplicate forked/mirrored repositories
        contributions = RepoDeduplicationService.Deduplicate(contributions);

        user ??= await userManager.FindByEmailAsync(emails.FirstOrDefault() ?? string.Empty);

        var model = new MyContributionsViewModel
        {
            ContributorName = name,
            Email = user?.Email ?? emails.FirstOrDefault() ?? string.Empty,
            User = user,
            TotalWorkHours = contributions.Sum(c => c.TotalWorkHours),
            TotalCommits = contributions.Sum(c => c.CommitCount),
            TotalActiveDays = contributions.Sum(c => c.ActiveDays),
            Contributions = contributions
        };

        return this.StackView(model, "MyContributions");
    }
}
