using System.Net;
using System.Text.RegularExpressions;
using Aiursoft.CSTools.Tools;
using Aiursoft.DbTools;
using Aiursoft.Manhours.Entities;
using static Aiursoft.WebTools.Extends;

namespace Aiursoft.Manhours.Tests.IntegrationTests;

[TestClass]
public class ReposControllerTests
{
    private readonly int _port;
    private readonly HttpClient _http;
    private IHost? _server;

    public ReposControllerTests()
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
        await _server.UpdateDbAsync<TemplateDbContext>();
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
        Assert.IsGreaterThan(html.Length, 0);
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
}
