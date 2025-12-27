using System.Text;
using Newtonsoft.Json;

namespace Aiursoft.Manhours.Models;

public class Badge
{
    [JsonProperty("schemaVersion")]
    public int SchemaVersion { get; } = 1;

    [JsonProperty("label")]
    public required string Label { get; init; }

    [JsonProperty("message")]
    public required string Message { get; init; }

    [JsonProperty("color")]
    public required string Color { get; init; }

    [JsonIgnore]
    public string? Link { get; init; }

    public byte[] Draw()
    {
        var content = $@"
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
    </g>";

        if (!string.IsNullOrWhiteSpace(Link))
        {
            content = $@"<a xlink:href=""{Link}"" target=""_top"">{content}</a>";
        }

        var svg = $@"
<svg xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink"" width=""108"" height=""20"" role=""img"" aria-label=""{Label}: {Message}"">
    <title>{Label}: {Message}</title>
{content}
</svg>
";
        return Encoding.UTF8.GetBytes(svg);
    }
}
