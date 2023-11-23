using System.Text.Json.Serialization;

namespace Aiursoft.ManHours.Models.GitHub;

public class GitHubCommitter
{
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }
}