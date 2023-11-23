namespace Aiursoft.ManHours.Models.GitHub;

public class GitHubLink
{
    public string Group { get; }
    public string Project { get; }

    public GitHubLink(string link)
    {
        var split = link.Split('/');
        Group = split[0];
        Project = split[1];
    }

    public async IAsyncEnumerable<GitHubCommit> GetCommits()
    {
        var page = 1;
        while (true)
        {
            var commits = await GetCommits(page);
            if (commits.Count == 0)
            {
                break;
            }
            foreach (var commit in commits)
            {
                yield return commit;
            }
            page++;
        }
    }

    private async Task<List<GitHubCommit>> GetCommits(int page)
    {
        var url = $"https://api.github.com/repos/{Group}/{Project}/commits?page={page}&per_page=100";
        var http = new HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent", "Aiursoft-ManHours");
        var results = await http.GetFromJsonAsync<List<GitHubCommit>>(url);
        return results!;
    }
}