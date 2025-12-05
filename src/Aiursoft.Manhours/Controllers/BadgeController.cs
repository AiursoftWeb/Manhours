using Aiursoft.Canon;
using Aiursoft.CSTools.Tools;
using Aiursoft.GitRunner;
using Aiursoft.GitRunner.Models;
using Aiursoft.ManHours.Models;
using Aiursoft.ManHours.Services;
using Aiursoft.Manhours.Entities;
using Aiursoft.ManHours.Models.BadgeViewModels;
using Aiursoft.Manhours.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Aiursoft.Manhours.Controllers;

public class BadgeController(
    ILogger<BadgeController> logger,
    RepoService repoService,
    UserManager<User> userManager)
    : Controller
{
    private static readonly Regex RepoNameRegex = new("^[a-zA-Z0-9-._/]+$", RegexOptions.Compiled);
    private static readonly string[] ValidExtensions = ["git", "svg", "json", "html"];

    [Route("r/{**repo}")]
    public async Task<IActionResult> RenderRepo([FromRoute] string repo)
    {
        var extension = repo.Split('.').LastOrDefault();
        if (string.IsNullOrWhiteSpace(extension) || !ValidExtensions.Contains(extension.ToLower()))
        {
            logger.LogInformation("Invalid extension: {Extension}", extension);
            return NotFound();
        }

        // Trim path splitters
        repo = repo.Replace('\\', '/').Trim('/');

        // Trim HTTPS
        if (repo.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            repo = repo["https://".Length..];
        }

        // Don't allow '..'
        if (repo.Contains("..", StringComparison.Ordinal))
        {
            logger.LogInformation("Invalid repo (contains ..): {Repo}", repo);
            return NotFound();
        }

        // At least one Path separator
        if (!repo.Contains('/', StringComparison.Ordinal))
        {
            logger.LogInformation("Invalid repo (no path separator): {Repo}", repo);
            return NotFound();
        }

        if (!RepoNameRegex.IsMatch(repo))
        {
            logger.LogWarning("Invalid repo characters: {Repo}", repo);
            return BadRequest("Invalid characters in repo name.");
        }

        var repoWithoutExtension =
            repo[..(repo.Length - extension.Length - 1)].ToLower().Trim();

        logger.LogInformation("Requesting repo: {Repo}", repoWithoutExtension);
        var repoUrl = $"https://{repoWithoutExtension}.git";
        var stats = await repoService.GetRepoStatsAsync(repoWithoutExtension, repoUrl);

        var hours = stats.TotalWorkTime.TotalHours;
        var badge = new Badge
        {
            Label = "man-hours",
            Message = $"{(int)hours}",
            //#e05d44 Red
            //#fe7d37 Orange
            //#dfb317 Yellow
            //#4c1 Green
            Color =
                hours < 10 ? "e05d44" :
                hours < 30 ? "fe7d37" :
                hours < 90 ? "dfb317" :
                "4c1",
            Link = $"{Request.Scheme}://{Request.Host}/r/{repoWithoutExtension}.html"
        };

        // Access-Control-Allow-Origin:
        Response.Headers.Append("Access-Control-Allow-Origin", "*");

        var lowerExtension = extension.ToLower();
        if (lowerExtension == "svg" || lowerExtension == "git")
        {
            return File(badge.Draw(), "image/svg+xml");
        }
        else if (lowerExtension == "json")
        {
            return Ok(badge);
        }
        else if (lowerExtension == "html")
        {
            var user = await userManager.GetUserAsync(User);
            stats.Contributors = stats.Contributors.OrderByDescending(c => c.WorkTime).ToList();
            return this.StackView(new RenderRepoViewModel(repoWithoutExtension)
            {
                Stats = stats,
                CurrentUserEmail = user?.Email
            }, "Render");
        }
        else
        {
            return NotFound();
        }
    }
}
