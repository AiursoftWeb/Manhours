namespace Aiursoft.Manhours.Services.Background;

public class RepoUpdateTask
{
    public string RepoName { get; set; } = string.Empty;
    public string RepoUrl { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
