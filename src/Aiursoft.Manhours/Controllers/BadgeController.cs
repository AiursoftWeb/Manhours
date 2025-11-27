using Aiursoft.Canon;
using Aiursoft.CSTools.Tools;
using Aiursoft.GitRunner;
using Aiursoft.GitRunner.Models;
using Aiursoft.ManHours.Models;
using Aiursoft.ManHours.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace Aiursoft.Manhours.Controllers;

public class BadgeController(
    IConfiguration configuration,
    ILogger<BadgeController> logger,
    WorkspaceManager workspaceManager,
    CacheService cacheService,
    WorkTimeService workTimeService)
    : ControllerBase
{
    private static readonly string[] ValidExtensions = ["git", "svg", "json"];
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Lockers = new();
    private readonly string _workspaceFolder = Path.Combine(configuration["Storage:Path"]!, "Repos");

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

        var repoWithoutExtension =
            repo[..(repo.Length - extension.Length - 1)].ToLower().Trim();

        logger.LogInformation("Requesting repo: {Repo}", repoWithoutExtension);
        var hours = await cacheService.RunWithCache(
            $"git-{repoWithoutExtension}",
            async () =>
            {
                var locker = Lockers.GetOrAdd(repoWithoutExtension, _ => new SemaphoreSlim(1, 1));
                logger.LogInformation("Waiting for locker for repo: {Repo}", repoWithoutExtension);
                await locker.WaitAsync();
                try
                {
                    var repoLocalPath = repoWithoutExtension.Replace('/', Path.DirectorySeparatorChar);
                    var workPath = Path.GetFullPath(Path.Combine(_workspaceFolder, repoLocalPath));
                    if (!Directory.Exists(workPath))
                    {
                        logger.LogInformation("Create folder for repo: {Repo} on {Path}", repoWithoutExtension, workPath);
                        Directory.CreateDirectory(workPath);
                    }

                    return await GetWorkHoursFromGitPath(repoWithoutExtension, workPath);
                }
                finally
                {
                    logger.LogInformation("Release locker for repo: {Repo}", repoWithoutExtension);
                    locker.Release();
                }
            },
            cachedMinutes: manHours => TimeSpan.FromMinutes((int)manHours)); // For 1 manhour, cache 1 minute.

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
                "4c1"
        };

        // Access-Control-Allow-Origin:
        Response.Headers.Append("Access-Control-Allow-Origin", "*");

        return extension.ToLower() switch
        {
            "svg" or "git" => File(badge.Draw(), "image/svg+xml"),
            "json" => Ok(badge),
            _ => NotFound()
        };
    }

    private async Task<double> GetWorkHoursFromGitPath(
        string repoWithoutExtension,
        string workPath,
        bool autoCleanIfError = true)
    {
        try
        {
            logger.LogInformation("Resetting repo: {Repo} on {Path}", repoWithoutExtension, workPath);
            await workspaceManager.ResetRepo(
                workPath,
                null,
                $"https://{repoWithoutExtension}.git",
                CloneMode.BareWithOnlyCommits);

            logger.LogInformation("Getting commits for repo: {Repo} on {Path}", repoWithoutExtension, workPath);
            var commits = await workspaceManager.GetCommitTimes(workPath);

            logger.LogInformation("Calculating work time for repo: {Repo} on {Path}", repoWithoutExtension, workPath);
            var workTime = WorkTimeService.CalculateWorkTime(commits.ToList());
            return workTime.TotalHours;
        }
        catch (Exception e)
        {
            if (!autoCleanIfError) throw;
            logger.LogError(e, "Error on repo: {Repo} on {Path}", repoWithoutExtension, workPath);
            logger.LogInformation("Cleaning repo: {Repo} on {Path}", repoWithoutExtension, workPath);
            FolderDeleter.DeleteByForce(workPath, keepFolder: true);
            return await GetWorkHoursFromGitPath(repoWithoutExtension, workPath, false);

        }
    }
}


