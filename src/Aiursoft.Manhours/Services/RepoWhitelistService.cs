using Aiursoft.Manhours.Configuration;
using Aiursoft.Scanner.Abstractions;

namespace Aiursoft.Manhours.Services;

public class RepoWhitelistService(GlobalSettingsService settingsService) : IScopedDependency
{
    public async Task<bool> IsWhitelistedAsync(string repoUrl)
    {
        var domains = await GetWhitelistDomainsAsync();
        if (domains.Count == 0)
        {
            // Empty whitelist means allow all (backward compatible)
            return true;
        }

        var host = ExtractHost(repoUrl);
        if (host == null)
        {
            return false;
        }

        return domains.Contains(host);
    }

    public async Task<HashSet<string>> GetWhitelistDomainsAsync()
    {
        var raw = await settingsService.GetSettingValueAsync(SettingsMap.RepoWhitelistDomains);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return raw
            .Split(';')
            .Select(d => d.Trim().ToLowerInvariant())
            .Where(d => d.Length > 0)
            .ToHashSet();
    }

    private static string? ExtractHost(string repoUrl)
    {
        if (Uri.TryCreate(repoUrl, UriKind.Absolute, out var uri))
        {
            return uri.Host.ToLowerInvariant();
        }
        return null;
    }
}
