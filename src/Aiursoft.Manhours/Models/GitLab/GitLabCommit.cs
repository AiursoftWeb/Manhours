using System.Text.Json.Serialization;

namespace Aiursoft.ManHours.Models.GitLab;

public class GitLabCommit
{
    [JsonPropertyName("committed_date")]
    public DateTime CommittedDate { get; init; }
}