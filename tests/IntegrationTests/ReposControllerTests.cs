using System.Net;
using System.Text.RegularExpressions;
using Aiursoft.DbTools;
using Aiursoft.Manhours.Entities;
using static Aiursoft.WebTools.Extends;

namespace Aiursoft.Manhours.Tests.IntegrationTests;

[TestClass]
public class ReposControllerTests
{
    private readonly HttpClient _http;
    private IHost? _server;

    public ReposControllerTests()
    {
        var cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.All
        };
        _http = new HttpClient(handler);
    }

    [TestInitialize]
    public async Task CreateServer()
    {
        using var reservation = new TestPortReservation();
        _http.BaseAddress = new Uri($"http://localhost:{reservation.Port}");
        _server = await AppAsync<Startup>([], port: reservation.Port);
        try
        {
            reservation.ConfigureServer(_server);
            await _server.UpdateDbAsync<ManhoursDbContext>();
            await _server.SeedAsync();
            await _server.StartAsync();
        }
        catch
        {
            _server.Dispose();
            throw;
        }
    }

    [TestCleanup]
    public async Task CleanServer()
    {
        if (_server == null) return;
        await _server.StopAsync();
        _server.Dispose();
    }

    private async Task<string> GetAntiCsrfToken(string url)
    {
        var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(html,
            @"<input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)"" />");
        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not find anti-CSRF token on page: {url}");
        }

        return match.Groups[1].Value;
    }

    private async Task<(string email, string password)> RegisterAndLoginAsync()
    {
        var email = $"test-{Guid.NewGuid()}@aiursoft.com";
        var password = "Test-Password-123";

        var registerToken = await GetAntiCsrfToken("/Account/Register");
        var registerContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Email", email },
            { "Password", password },
            { "ConfirmPassword", password },
            { "__RequestVerificationToken", registerToken }
        });
        var registerResponse = await _http.PostAsync("/Account/Register", registerContent);
        Assert.AreEqual(HttpStatusCode.Found, registerResponse.StatusCode);

        return (email, password);
    }

    [TestMethod]
    public async Task GetTopReposIndexPage()
    {
        // Act
        var response = await _http.GetAsync("/repos/index");

        // Assert
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Top Repos", html);
    }

    [TestMethod]
    public async Task GetTopReposWithPagination()
    {
        // Act - Request page 1
        var response1 = await _http.GetAsync("/repos/index?page=1");
        response1.EnsureSuccessStatusCode();
        var html1 = await response1.Content.ReadAsStringAsync();

        // Act - Request page 2
        var response2 = await _http.GetAsync("/repos/index?page=2");
        response2.EnsureSuccessStatusCode();
        var html2 = await response2.Content.ReadAsStringAsync();

        // Assert both pages load successfully
        Assert.IsNotNull(html1);
        Assert.IsNotNull(html2);
    }

    [TestMethod]
    public async Task GetTopReposWithInvalidPage()
    {
        // Act - Request page 0 (should be treated as page 1)
        var response = await _http.GetAsync("/repos/index?page=0");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [TestMethod]
    public async Task GetTopReposAsAuthenticatedUser()
    {
        // Arrange - Create and login user
        _ = await RegisterAndLoginAsync();

        // Act
        var response = await _http.GetAsync("/repos/index");

        // Assert
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Top Repos", html);
    }

    [TestMethod]
    public async Task TopReposDisplaysContributorCounts()
    {
        // Act
        var response = await _http.GetAsync("/repos/index");

        // Assert
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        // Should contain repository display information
        Assert.IsNotNull(html);
        Assert.IsGreaterThan(0, html.Length);
    }

    [TestMethod]
    public async Task GetReposAlternativeRoute()
    {
        // Test that the repos listing is accessible via different routes
        var routes = new[] { "/repos/index", "/Repos/Index" };

        foreach (var route in routes)
        {
            var response = await _http.GetAsync(route);
            response.EnsureSuccessStatusCode();
        }
    }

    [TestMethod]
    public async Task TopReposPaginationBoundaries()
    {
        // Test negative page number
        var response1 = await _http.GetAsync("/repos/index?page=-1");
        response1.EnsureSuccessStatusCode();

        // Test very large page number
        var response2 = await _http.GetAsync("/repos/index?page=999999");
        response2.EnsureSuccessStatusCode();

        // Both should return successfully even if empty
    }

    [TestMethod]
    public async Task PersonalRankingUsesOwnHoursAfterMirrorDeduplication()
    {
        var (email, _) = await RegisterAndLoginAsync();
        var key = Guid.NewGuid().ToString("N");
        using (var scope = _server!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ManhoursDbContext>();
            var me = new Contributor { Id = Guid.NewGuid(), Email = email };
            var other = new Contributor { Id = Guid.NewGuid(), Email = $"{key}@example.test" };
            var high = new Repo { Id = Guid.NewGuid(), Name = "More personal hours", Url = $"https://example.test/{key}/high.git" };
            var low = new Repo { Id = Guid.NewGuid(), Name = "More commits", Url = $"https://example.test/{key}/low.git" };
            var mirror = new Repo { Id = Guid.NewGuid(), Name = "Duplicate mirror", Url = $"https://mirror.example.test/{key}/high.git" };
            var otherOnly = new Repo { Id = Guid.NewGuid(), Name = "Other contributor repository", Url = $"https://example.test/{key}/other.git" };
            db.RepoContributions.AddRange(
                new RepoContribution { Id = Guid.NewGuid(), Repo = high, Contributor = me, TotalWorkHours = 12, CommitCount = 2 },
                new RepoContribution { Id = Guid.NewGuid(), Repo = high, Contributor = other, TotalWorkHours = 88, CommitCount = 100 },
                new RepoContribution { Id = Guid.NewGuid(), Repo = low, Contributor = me, TotalWorkHours = 3, CommitCount = 30 },
                new RepoContribution { Id = Guid.NewGuid(), Repo = low, Contributor = other, TotalWorkHours = 197, CommitCount = 100 },
                new RepoContribution { Id = Guid.NewGuid(), Repo = mirror, Contributor = me, TotalWorkHours = 11, CommitCount = 1 },
                new RepoContribution { Id = Guid.NewGuid(), Repo = otherOnly, Contributor = other, TotalWorkHours = 500, CommitCount = 10 });
            await db.SaveChangesAsync();
        }

        var html = await _http.GetStringAsync("/repos/index");
        var start = html.IndexOf("My top repositories", StringComparison.Ordinal);
        var end = html.IndexOf("Repository not tracked?", start, StringComparison.Ordinal);
        var panel = html[start..end];

        Assert.IsTrue(panel.IndexOf("More personal hours", StringComparison.Ordinal) < panel.IndexOf("More commits", StringComparison.Ordinal));
        StringAssert.Contains(panel, "12.0 Hours");
        StringAssert.Contains(panel, "3.0 Hours");
        Assert.IsFalse(panel.Contains("100.0 Hours"));
        Assert.IsFalse(panel.Contains("200.0 Hours"));
        Assert.IsFalse(panel.Contains("Duplicate mirror"));
        Assert.IsFalse(panel.Contains("Other contributor repository"));
        Assert.IsFalse(panel.Contains(">Active</span>"));
        StringAssert.Contains(html[end..], "100.0");
        StringAssert.Contains(html[end..], "200.0");
    }

    [TestMethod]
    public async Task PersonalRankingSelectsTwentyByHoursRatherThanCommitCount()
    {
        var (email, _) = await RegisterAndLoginAsync();
        var key = Guid.NewGuid().ToString("N");
        using (var scope = _server!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ManhoursDbContext>();
            var me = new Contributor { Id = Guid.NewGuid(), Email = email };
            for (var i = 1; i <= 25; i++)
            {
                db.RepoContributions.Add(new RepoContribution
                {
                    Id = Guid.NewGuid(), Contributor = me, TotalWorkHours = i, CommitCount = 100 - i,
                    Repo = new Repo { Id = Guid.NewGuid(), Name = $"Personal project {i:D2}", Url = $"https://example.test/{key}/project-{i:D2}.git" }
                });
            }
            await db.SaveChangesAsync();
        }

        var html = await _http.GetStringAsync("/repos/index");
        var start = html.IndexOf("My top repositories", StringComparison.Ordinal);
        var end = html.IndexOf("Repository not tracked?", start, StringComparison.Ordinal);
        var panel = html[start..end];

        Assert.AreEqual(20, Regex.Matches(panel, "<h5 class=\"mb-1\">").Count);
        Assert.IsTrue(panel.IndexOf("Personal project 25", StringComparison.Ordinal) < panel.IndexOf("Personal project 24", StringComparison.Ordinal));
        StringAssert.Contains(panel, "Personal project 06");
        Assert.IsFalse(panel.Contains("Personal project 05"));
        Assert.IsFalse(panel.Contains("Personal project 01"));
    }
}
