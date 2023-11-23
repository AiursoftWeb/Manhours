using System.Text.Json.Serialization;

namespace Aiursoft.ManHours.Models.GitHub;

public class GitHubCommit
{
    [JsonPropertyName("commit")]
    public GitHubCommitDetail Commit { get; set; } = null!;
}