using System.Net;
using Aiursoft.CSTools.Tools;
using Aiursoft.DbTools;
using Aiursoft.Manhours.Configuration;
using Aiursoft.Manhours.Entities;
using Aiursoft.Manhours.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Aiursoft.WebTools.Extends;

namespace Aiursoft.Manhours.Tests.IntegrationTests;

[TestClass]
public class WhitelistServiceTests
{
    private readonly int _port;
    private readonly HttpClient _http;
    private IHost? _server;

    public WhitelistServiceTests()
    {
        var cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            AllowAutoRedirect = false
        };
        _port = Network.GetAvailablePort();
        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri($"http://localhost:{_port}")
        };
    }

    [TestInitialize]
    public async Task CreateServer()
    {
        _server = await AppAsync<Startup>([], port: _port);
        await _server.UpdateDbAsync<ManhoursDbContext>();
        await _server.SeedAsync();
        await _server.StartAsync();
    }

    [TestCleanup]
    public async Task CleanServer()
    {
        if (_server == null) return;
        await _server.StopAsync();
        _server.Dispose();
    }

    private T GetService<T>() where T : notnull
    {
        if (_server == null) throw new InvalidOperationException("Server is not started.");
        return _server.Services.GetRequiredService<T>();
    }

    [TestMethod]
    public async Task DefaultWhitelistParsesCorrectly()
    {
        // Arrange
        var whitelistService = GetService<RepoWhitelistService>();

        // Act
        var domains = await whitelistService.GetWhitelistDomainsAsync();

        // Assert
        Assert.AreEqual(2, domains.Count, "Default whitelist should have 2 domains");
        Assert.IsTrue(domains.Contains("github.com"), "Should contain github.com");
        Assert.IsTrue(domains.Contains("gitlab.com"), "Should contain gitlab.com");
    }

    [TestMethod]
    public async Task WhitelistedGitHubUrlMatches()
    {
        var whitelistService = GetService<RepoWhitelistService>();

        Assert.IsTrue(await whitelistService.IsWhitelistedAsync("https://github.com/owner/repo.git"));
        Assert.IsTrue(await whitelistService.IsWhitelistedAsync("https://github.com/org/project.git"));
    }

    [TestMethod]
    public async Task WhitelistedGitLabUrlMatches()
    {
        var whitelistService = GetService<RepoWhitelistService>();

        Assert.IsTrue(await whitelistService.IsWhitelistedAsync("https://gitlab.com/owner/repo.git"));
    }

    [TestMethod]
    public async Task NonWhitelistedUrlDoesNotMatch()
    {
        var whitelistService = GetService<RepoWhitelistService>();

        Assert.IsFalse(await whitelistService.IsWhitelistedAsync("https://evil.com/user/repo.git"));
        Assert.IsFalse(await whitelistService.IsWhitelistedAsync("https://malicious.org/repo.git"));
        Assert.IsFalse(await whitelistService.IsWhitelistedAsync("https://hacker.net/project.git"));
    }

    [TestMethod]
    public async Task SubdomainDoesNotMatchParentDomain()
    {
        var whitelistService = GetService<RepoWhitelistService>();

        // gitlab.aiursoft.com is NOT the same as gitlab.com
        Assert.IsFalse(await whitelistService.IsWhitelistedAsync("https://gitlab.aiursoft.com/aiursoft/tracer.git"));
    }

    [TestMethod]
    public async Task HttpUrlAlsoMatches()
    {
        var whitelistService = GetService<RepoWhitelistService>();

        Assert.IsTrue(await whitelistService.IsWhitelistedAsync("http://github.com/owner/repo.git"));
    }

    [TestMethod]
    public async Task EmptyWhitelistAllowsAll()
    {
        // Arrange — set whitelist to empty
        var settingsService = GetService<GlobalSettingsService>();
        await settingsService.UpdateSettingAsync(SettingsMap.RepoWhitelistDomains, string.Empty);

        var whitelistService = GetService<RepoWhitelistService>();

        // Act & Assert — empty whitelist means allow everything (backward compatible)
        Assert.IsTrue(await whitelistService.IsWhitelistedAsync("https://evil.com/user/repo.git"));
        Assert.IsTrue(await whitelistService.IsWhitelistedAsync("https://github.com/owner/repo.git"));
    }

    [TestMethod]
    public async Task CustomWhitelistTakesEffect()
    {
        // Arrange
        var settingsService = GetService<GlobalSettingsService>();
        await settingsService.UpdateSettingAsync(SettingsMap.RepoWhitelistDomains, "mycompany.com;open-source.org");

        var whitelistService = GetService<RepoWhitelistService>();

        // Act
        var domains = await whitelistService.GetWhitelistDomainsAsync();

        // Assert
        Assert.AreEqual(2, domains.Count);
        Assert.IsTrue(domains.Contains("mycompany.com"));
        Assert.IsTrue(domains.Contains("open-source.org"));
        Assert.IsFalse(domains.Contains("github.com"), "github.com should no longer be whitelisted");

        Assert.IsTrue(await whitelistService.IsWhitelistedAsync("https://mycompany.com/repo.git"));
        Assert.IsTrue(await whitelistService.IsWhitelistedAsync("https://open-source.org/proj.git"));
        Assert.IsFalse(await whitelistService.IsWhitelistedAsync("https://github.com/owner/repo.git"));
    }

    [TestMethod]
    public async Task WhitelistHandlesWhitespaceAndCasing()
    {
        // Arrange
        var settingsService = GetService<GlobalSettingsService>();
        await settingsService.UpdateSettingAsync(SettingsMap.RepoWhitelistDomains, "  GitHub.COM ;  GitLab.COM  ;  ");

        var whitelistService = GetService<RepoWhitelistService>();

        // Act
        var domains = await whitelistService.GetWhitelistDomainsAsync();

        // Assert
        Assert.AreEqual(2, domains.Count);
        Assert.IsTrue(domains.Contains("github.com"));
        Assert.IsTrue(domains.Contains("gitlab.com"));
    }

    [TestMethod]
    public async Task InvalidUrlReturnsFalse()
    {
        var whitelistService = GetService<RepoWhitelistService>();

        Assert.IsFalse(await whitelistService.IsWhitelistedAsync("not-a-valid-url"));
        Assert.IsFalse(await whitelistService.IsWhitelistedAsync(""));
    }

    [TestMethod]
    public async Task NonWhitelistedRepoBadgeStillWorks()
    {
        // The default whitelist is github.com;gitlab.com.
        // gitlab.aiursoft.com is NOT whitelisted, but badge should still work.
        var response = await _http.GetAsync("/r/gitlab.aiursoft.com/aiursoft/tracer.svg");

        response.EnsureSuccessStatusCode();
        Assert.AreEqual("image/svg+xml", response.Content.Headers.ContentType?.MediaType);

        var svg = await response.Content.ReadAsStringAsync();
        Assert.Contains("<svg", svg);
        Assert.Contains("man-hours", svg);
    }

    [TestMethod]
    public async Task NonWhitelistedRepoDoesNotAppearInHotRepos()
    {
        // Arrange — ensure whitelist is strict (only github.com)
        var settingsService = GetService<GlobalSettingsService>();
        await settingsService.UpdateSettingAsync(SettingsMap.RepoWhitelistDomains, "github.com");

        // Act — request badge for a non-whitelisted repo
        var badgeResponse = await _http.GetAsync("/r/gitlab.aiursoft.com/aiursoft/tracer.svg");
        badgeResponse.EnsureSuccessStatusCode();

        // Assert — the repo should NOT appear in the Top Repos page
        var reposResponse = await _http.GetAsync("/repos/index");
        reposResponse.EnsureSuccessStatusCode();
        var html = await reposResponse.Content.ReadAsStringAsync();

        Assert.DoesNotContain("gitlab.aiursoft.com", html, "Non-whitelisted repo should not appear in Top Repos");
        Assert.DoesNotContain("tracer", html, "Non-whitelisted repo name should not appear in Top Repos");
    }

    [TestMethod]
    public async Task WhitelistedRepoAppearsInHotReposAfterBadgeRequest()
    {
        // Arrange — set whitelist to include the test repo's domain
        var settingsService = GetService<GlobalSettingsService>();
        await settingsService.UpdateSettingAsync(SettingsMap.RepoWhitelistDomains, "github.com;gitlab.aiursoft.com");

        // Act — request badge for the now-whitelisted repo
        var badgeResponse = await _http.GetAsync("/r/gitlab.aiursoft.com/aiursoft/tracer.svg");
        badgeResponse.EnsureSuccessStatusCode();

        // Assert — the repo SHOULD appear in the Top Repos page
        var reposResponse = await _http.GetAsync("/repos/index");
        reposResponse.EnsureSuccessStatusCode();
        var html = await reposResponse.Content.ReadAsStringAsync();

        Assert.Contains("gitlab.aiursoft.com", html, "Whitelisted repo should appear in Top Repos");
    }
}
