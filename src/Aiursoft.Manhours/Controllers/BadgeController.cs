using System.Text;
using Aiursoft.Canon;
using Aiursoft.ManHours.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.ManHours.Controllers;

public class BadgeController : ControllerBase
{
    private readonly WorkTimeService _workTimeService;

    public BadgeController(WorkTimeService workTimeService)
    {
        _workTimeService = workTimeService;
    }

    [Route("gitlab/{**repo}")]
    public async Task<IActionResult>
        GitLabRepo([FromRoute] string repo) // sample value: gitlab.aiursoft.cn/anduin/flyclass
    {
        Console.WriteLine(repo);
        var formattedLink = new GitLabLink(repo);
        var commits = await formattedLink.GetCommits().ToListAsync();
        var commitTimes = commits.Select(t => t.CommittedDate).ToList();
        var workTime = _workTimeService.CalculateWorkTime(commitTimes);
        var hours = workTime.TotalHours;

        // Draw a badge for readme based on the work time.
        var badge = new Badge
        {
            Label = "man-hours",
            Message = $"{(int)hours}",
            Color = hours < 8 ? "f00" : "4c1"
        };

        // Return the picture.
        return File(badge.Draw(), "image/svg+xml");
    }
}

public class Badge
{
    public required string Label { get; init; }
    public required string Message { get; init; }
    public required string Color { get; init; }

    public byte[] Draw()
    {
// <svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="108" height="20" role="img" aria-label="contributors: 442">
//     <title>man-hours: 442</title>
//     <linearGradient id="s" x2="0" y2="100%">
//         <stop offset="0" stop-color="#bbb" stop-opacity=".1"/>
//         <stop offset="1" stop-opacity=".1"/>
//     </linearGradient>
//     <clipPath id="r">
//         <rect width="108" height="20" rx="3" fill="#fff"/>
//     </clipPath>
//     <g clip-path="url(#r)">
//         <rect width="77" height="20" fill="#555"/>
//         <rect x="77" width="31" height="20" fill="#4c1"/>
//         <rect width="108" height="20" fill="url(#s)"/>
//     </g>
//     <g fill="#fff" text-anchor="middle" font-family="Verdana,Geneva,DejaVu Sans,sans-serif" text-rendering="geometricPrecision" font-size="110">
//         <text aria-hidden="true" x="395" y="150" fill="#010101" fill-opacity=".3" transform="scale(.1)" textLength="670">man-hours</text>
//         <text x="395" y="140" transform="scale(.1)" fill="#fff" textLength="670">man-hours</text>
//         <text aria-hidden="true" x="915" y="150" fill="#010101" fill-opacity=".3" transform="scale(.1)" textLength="200">123</text>
//         <text x="915" y="140" transform="scale(.1)" fill="#fff" textLength="200">123</text>
//     </g>
// </svg>

        
        var svg = $@"
<svg xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink"" width=""108"" height=""20"" role=""img"" aria-label=""{Label}: {Message}"">
    <title>{Label}: {Message}</title>
    <linearGradient id=""s"" x2=""0"" y2=""100%"">
          <stop offset=""0"" stop-color=""#bbb"" stop-opacity="".1""/>
          <stop offset=""1"" stop-opacity="".1""/>
    </linearGradient>
    <clipPath id=""r"">
          <rect width=""108"" height=""20"" rx=""3"" fill=""#fff""/> 
    </clipPath>
    <g clip-path=""url(#r)"">
          <rect width=""77"" height=""20"" fill=""#555""/>
          <rect x=""77"" width=""31"" height=""20"" fill=""#{Color}""/>
          <rect width=""108"" height=""20"" fill=""url(#s)""/>
    </g>
    <g fill=""#fff"" text-anchor=""middle"" font-family=""Verdana,Geneva,DejaVu Sans,sans-serif"" text-rendering=""geometricPrecision"" font-size=""110"">
          <text aria-hidden=""true"" x=""395"" y=""150"" fill=""#010101"" fill-opacity="".3"" transform=""scale(.1)"" textLength=""670"">{Label}</text>
          <text x=""395"" y=""140"" transform=""scale(.1)"" fill=""#fff"" textLength=""670"">{Label}</text>
          <text aria-hidden=""true"" x=""915"" y=""150"" fill=""#010101"" fill-opacity="".3"" transform=""scale(.1)"" textLength=""{Message.Length * 70}"">{Message}</text>
          <text x=""915"" y=""140"" transform=""scale(.1)"" fill=""#fff"" textLength=""{Message.Length * 70}"">{Message}</text>
    </g>
</svg>
";
        return Encoding.UTF8.GetBytes(svg);
    }
}