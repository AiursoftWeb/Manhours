using System.Text.Json.Serialization;

namespace Aiursoft.ManHours.Models.GitHub;

public class GitHubCommitDetail
{
    [JsonPropertyName("committer")]
    public GitHubCommitter Committer { get; set; } = null!;
}