using Aiursoft.Canon;
using Aiursoft.ManHours.Models;
using Aiursoft.ManHours.Models.GitLab;
using Aiursoft.ManHours.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.ManHours.Controllers;

public class BadgeController : ControllerBase
{
    private readonly CacheService _cacheService;
    private readonly WorkTimeService _workTimeService;

    public BadgeController(
        CacheService cacheService,
        WorkTimeService workTimeService)
    {
        _cacheService = cacheService;
        _workTimeService = workTimeService;
    }

    [Route("gitlab/{**repo}")]
    public async Task<IActionResult>
        GitLabRepo([FromRoute] string repo) // sample value: gitlab.aiursoft.cn/anduin/flyclass
    {
        var hours = await _cacheService.RunWithCache($"gitlab-{repo}", async () =>
        {
            var formattedLink = new GitLabLink(repo);
            var commits = await formattedLink.GetCommits().ToListAsync();
            var commitTimes = commits.Select(t => t.CommittedDate).ToList();
            var workTime = _workTimeService.CalculateWorkTime(commitTimes);
            return workTime.TotalHours;
        }, cachedMinutes: r => r < 100 ? TimeSpan.FromMinutes(10) : TimeSpan.FromMinutes(100));

        var badge = new Badge
        {
            Label = "man-hours",
            Message = $"{(int)hours}",
            Color =
                hours < 10 ? "e05d44" :
                hours < 30 ? "fe7d37" :
                hours < 90 ? "dfb317" :
                "4c1"
        };

        //#e05d44 Red
        //#fe7d37 Orange
        //#dfb317 Yellow
        //#4c1 Green
        return File(badge.Draw(), "image/svg+xml");
    }
}