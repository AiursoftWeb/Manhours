using System.Text.Json.Serialization;

namespace Aiursoft.ManHours.Models.GitLab;

public class GitLabGroup
{
    [JsonPropertyName("id")]
    public required int Id { get; init;  }
    
    [JsonPropertyName("path")]
    public string? Path { get; init; }
}