using System.Text.Json.Serialization;

namespace Aiursoft.ManHours;

public class GitLabLink
{
    public GitLabLink(string repositoryUrl)
    {
        if (!repositoryUrl.ToLower().StartsWith("http"))
        {
            repositoryUrl = "https://" + repositoryUrl;
        }
        var url = new Uri(repositoryUrl);
        var path = url.AbsolutePath.Split('/');
        if (path.Length != 3)
        {
            throw new ArgumentException("Invalid GitLab repository URL!");
        }

        Server = $"https://{url.Host}";
        Group = path[1];
        Project = path[2];
    }

    public string Server { get; }
    public string Group { get; }
    public string Project { get; }

    public async Task<bool> IsGroupProject()
    {
        var http = new HttpClient();
        var response = await http.GetFromJsonAsync<GitLabGroup[]>(Server + $"/api/v4/groups?search={Group}");
        return response != null && response.Any();
    }

    public async Task<int> GetGroupId()
    {
        var http = new HttpClient();
        if (await IsGroupProject())
        {
            var response = await http.GetFromJsonAsync<GitLabGroup[]>(Server + $"/api/v4/groups?search={Group}");
            return response?.FirstOrDefault(
                       t => string.Equals(t.Path, Group, StringComparison.CurrentCultureIgnoreCase))?.Id
                   ?? throw new Exception("Cannot find the organization!");
        }
        else
        {
            var response = await http.GetFromJsonAsync<GitLabGroup[]>(Server + $"/api/v4/users?username={Group}");
            return response?.First().Id ?? throw new Exception("Cannot find the organization!");
        }
    }

    public async Task<GitLabProject> GetProjectDetails()
    {
        var http = new HttpClient();
        var group = await GetGroupId();

        string link;
        if (await IsGroupProject())
        {
            link = $"/api/v4/groups/{group}/projects?search={Project}";
        }
        else
        {
            link = $"/api/v4/users/{group}/projects?search={Project}";
        }

        var response =
            await http.GetFromJsonAsync<GitLabProject[]>(Server + link);
        return response?.FirstOrDefault(t =>
                   string.Equals(t.Path, Project, StringComparison.CurrentCultureIgnoreCase)) ??
               throw new Exception("Cannot find the project!");
    }

    public async IAsyncEnumerable<GitLabCommit> GetCommits()
    {
        var http = new HttpClient();
        var project = await GetProjectDetails();
        var apiLink = $"/api/v4/projects/{project.Id}/repository/commits";
        var page = 1;
        while (true)
        {
            var response = await http.GetFromJsonAsync<GitLabCommit[]>(Server + apiLink + $"?page={page}");
            if (response == null)
            {
                break;
            }

            if (response.Length == 0)
            {
                break;
            }

            foreach (var commit in response)
            {
                yield return commit;
            }

            page++;
        }
    }
}

public class GitLabCommit
{
    [JsonPropertyName("committed_date")]
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public DateTime CommittedDate { get; init; }
}