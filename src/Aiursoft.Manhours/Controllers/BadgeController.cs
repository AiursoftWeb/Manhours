using Aiursoft.Canon;
using Aiursoft.GitRunner;
using Aiursoft.GitRunner.Models;
using Aiursoft.ManHours.Models;
using Aiursoft.ManHours.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.ManHours.Controllers;

public class BadgeController : ControllerBase
{
    private readonly ILogger<BadgeController> _logger;
    private readonly WorkspaceManager _workspaceManager;
    private readonly CacheService _cacheService;
    private readonly WorkTimeService _workTimeService;
    private static readonly string[] ValidExtensions = { "git", "svg", "json" };
    private static readonly Dictionary<string, SemaphoreSlim > Lockers = new();
    private readonly string _workspaceFolder;

    public BadgeController(
        ILogger<BadgeController> logger,
        WorkspaceManager workspaceManager,
        CacheService cacheService,
        WorkTimeService workTimeService)
    {
        _logger = logger;
        _workspaceManager = workspaceManager;
        _cacheService = cacheService;
        _workTimeService = workTimeService;
        _workspaceFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ManHoursWorkspace");
    }

    [Route("r/{**repo}")]
    public async Task<IActionResult> GitLabRepo([FromRoute] string repo)
    {
        var extension = repo.Split('.').LastOrDefault();
        if (string.IsNullOrWhiteSpace(extension) || !ValidExtensions.Contains(extension.ToLower()))
        {
            _logger.LogInformation($"Invalid extension: {extension}");
            return NotFound();
        }

        // Trim path splitters
        repo = repo.Replace('\\', '/').Trim('/');

        // Trim HTTPS
        if (repo.StartsWith("https://"))
        {
            repo = repo["https://".Length..];
        }

        // Don't allow '..'
        if (repo.Contains(".."))
        {
            _logger.LogInformation($"Invalid repo: {repo}");
            return NotFound();
        }

        // At least one Path separator
        if (!repo.Contains('/'))
        {
            _logger.LogInformation($"Invalid repo: {repo}");
            return NotFound();
        }

        var repoWithoutExtension =
            repo[..(repo.Length - extension.Length - 1)].ToLower().Trim();
        
        _logger.LogInformation($"Requesting repo: {repoWithoutExtension}");
        var hours = await _cacheService.RunWithCache(
            $"git-{repoWithoutExtension}", async () =>
            {
                // var locker = _lockers.GetOrAdd(repoWithoutExtension, new object());
                SemaphoreSlim? locker;
                if (Lockers.TryGetValue(repoWithoutExtension, out var l))
                {
                    _logger.LogInformation($"Found locker for repo: {repoWithoutExtension}");
                    locker = l;
                }
                else
                {
                    _logger.LogInformation($"Create locker for repo: {repoWithoutExtension}");
                    locker = new SemaphoreSlim(1, 1);
                    Lockers.Add(repoWithoutExtension, locker);
                }

                _logger.LogInformation($"Waiting for locker for repo: {repoWithoutExtension}");
                await locker.WaitAsync();
                try
                {
                    var repoLocalPath = repoWithoutExtension.Replace('/', Path.DirectorySeparatorChar);
                    var workPath = Path.GetFullPath(Path.Combine(_workspaceFolder, repoLocalPath));
                    if (!Directory.Exists(workPath))
                    {
                        _logger.LogInformation($"Create folder for repo: {repoWithoutExtension} on {workPath}");
                        Directory.CreateDirectory(workPath);
                    }

                    _logger.LogInformation($"Resetting repo: {repoWithoutExtension} on {workPath}");
                    await _workspaceManager.ResetRepo(workPath, null, $"https://{repoWithoutExtension}.git",
                        CloneMode.BareWithOnlyCommits);
                    
                    _logger.LogInformation($"Getting commits for repo: {repoWithoutExtension} on {workPath}");
                    var commits = await _workspaceManager.GetCommitTimes(workPath);
                    
                    _logger.LogInformation($"Calculating work time for repo: {repoWithoutExtension} on {workPath}");
                    var workTime = _workTimeService.CalculateWorkTime(commits.ToList());
                    return workTime.TotalHours;
                }
                finally
                {
                    _logger.LogInformation($"Release locker for repo: {repoWithoutExtension}");
                    locker.Release();
                }
            }, cachedMinutes: manHours => TimeSpan.FromMinutes((int)manHours)); // For 1 man hour, cache 1 minute.

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

        switch (extension)
        {
            case "svg":
            case "git":
                return File(badge.Draw(), "image/svg+xml");
            case "json":
                return Ok(badge);
            default:
                return NotFound();
        }
    }
}