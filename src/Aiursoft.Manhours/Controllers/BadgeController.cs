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
    private static readonly Dictionary<string, SemaphoreSlim > _lockers = new();
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
    // sample value: gitlab.aiursoft.cn/anduin/flyclass.git
    // sample value: gitlab.aiursoft.cn/anduin/flyclass.svg
    // sample value: gitlab.aiursoft.cn/anduin/flyclass.json
    {
        var extension = repo.Split('.').LastOrDefault();
        if (string.IsNullOrWhiteSpace(extension) || !ValidExtensions.Contains(extension.ToLower()))
        {
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
            return NotFound();
        }

        // At least one Path separator
        if (!repo.Contains('/'))
        {
            return NotFound();
        }

        var repoWithoutExtension =
            repo[..(repo.Length - extension.Length - 1)].ToLower().Trim(); // gitlab.aiursoft.cn/anduin/flyclass
        
        _logger.LogInformation($"Requesting repo: {repoWithoutExtension}");
        var hours = await _cacheService.RunWithCache(
            $"git-{repoWithoutExtension}", async () =>
            {
                // var locker = _lockers.GetOrAdd(repoWithoutExtension, new object());
                SemaphoreSlim? locker;
                if (_lockers.TryGetValue(repoWithoutExtension, out var l))
                {
                    _logger.LogInformation($"Found locker for repo: {repoWithoutExtension}");
                    locker = l;
                }
                else
                {
                    _logger.LogInformation($"Create locker for repo: {repoWithoutExtension}");
                    locker = new SemaphoreSlim(1, 1);
                    _lockers.Add(repoWithoutExtension, locker);
                }

                _logger.LogInformation($"Waiting for locker for repo: {repoWithoutExtension}");
                await locker.WaitAsync();
                try
                {
                    var repoLocalPath = repoWithoutExtension.Replace('/', Path.DirectorySeparatorChar);
                    var workPath = Path.Combine(_workspaceFolder, repoLocalPath);
                    if (!Directory.Exists(workPath))
                    {
                        _logger.LogInformation($"Create folder for repo: {repoWithoutExtension}");
                        Directory.CreateDirectory(workPath);
                    }

                    _logger.LogInformation($"Resetting repo: {repoWithoutExtension}");
                    await _workspaceManager.ResetRepo(workPath, null, $"https://{repoWithoutExtension}.git",
                        CloneMode.BareWithOnlyCommits);
                    
                    _logger.LogInformation($"Getting commits for repo: {repoWithoutExtension}");
                    var commits = await _workspaceManager.GetCommitTimes(workPath);
                    
                    _logger.LogInformation($"Calculating work time for repo: {repoWithoutExtension}");
                    var workTime = _workTimeService.CalculateWorkTime(commits.ToList());
                    return workTime.TotalHours;
                }
                finally
                {
                    _logger.LogInformation($"Release locker for repo: {repoWithoutExtension}");
                    locker.Release();
                }
            }, cachedMinutes: r => r < 100 ? TimeSpan.FromMinutes(10) : TimeSpan.FromMinutes(100));

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