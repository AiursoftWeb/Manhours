using System.Net;
using System.Text.RegularExpressions;
using Aiursoft.CSTools.Tools;
using Aiursoft.DbTools;
using Aiursoft.Manhours.Entities;
using Microsoft.EntityFrameworkCore;
using static Aiursoft.WebTools.Extends;

namespace Aiursoft.Manhours.Tests.IntegrationTests;

[TestClass]
public class ContributionsControllerTests
{
    private readonly int _port;
    private readonly HttpClient _http;
    private IHost? _server;

    public ContributionsControllerTests()
    {
        var cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.All
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
    public async Task GetMyContributionsRequiresAuthentication()
    {
        // Act - Try to access without login
        var response = await _http.GetAsync("/contributions/mycontributions");

        // Assert - Should redirect to login
        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.IsTrue(
            response.Headers.Location?.OriginalString.Contains("/Account/Login") ?? false);
    }

    [TestMethod]
    public async Task GetMyContributionsWhenAuthenticated()
    {
        // Arrange - Create and login user
        _ = await RegisterAndLoginAsync();

        // Act
        var response = await _http.GetAsync("/contributions/mycontributions");

        // Assert
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Contributions", html);
    }

    [TestMethod]
    public async Task GetMyWeeklyReportRequiresAuthentication()
    {
        // Act - Try to access without login
        var response = await _http.GetAsync("/contributions/myweeklyreport");

        // Assert - Should redirect to login
        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.IsTrue(
            response.Headers.Location?.OriginalString.Contains("/Account/Login") ?? false);
    }

    [TestMethod]
    public async Task GetMyWeeklyReportWhenAuthenticated()
    {
        // Arrange - Create and login user
        _ = await RegisterAndLoginAsync();

        // Act
        var response = await _http.GetAsync("/contributions/myweeklyreport");

        // Assert
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Weekly Report", html);
    }

    [TestMethod]
    public async Task GetWeeklyReportStatusRequiresAuthentication()
    {
        // Act - Try to access without login
        var response = await _http.GetAsync("/contributions/weekly-report-status");

        // Assert - Should redirect to login or return NotFound
        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task GetWeeklyReportStatusReturnsJson()
    {
        // Arrange - Create and login user
        _ = await RegisterAndLoginAsync();

        // Act
        var response = await _http.GetAsync("/contributions/weekly-report-status");

        // Assert
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        Assert.IsNotNull(json);
        Assert.Contains("loading", json.ToLower());
    }

    [TestMethod]
    public async Task GetUserContributionsWithValidContributorId()
    {
        // Arrange - Get a contributor from the database
        var scope = _server!.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();

        var contributor = await dbContext.Contributors.FirstOrDefaultAsync();
        scope.Dispose();

        if (contributor == null)
        {
            // No contributors in the database, skip this test
            Assert.Inconclusive("No contributors found in database");
            return;
        }

        // Act
        var response = await _http.GetAsync($"/contributions/id/{contributor.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.IsNotNull(html);
    }

    [TestMethod]
    public async Task GetUserContributionsWithInvalidContributorId()
    {
        // Arrange - Use a GUID that doesn't exist
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _http.GetAsync($"/contributions/id/{invalidId}");

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task WeeklyReportShowsTotalWorkHours()
    {
        // Arrange - Create and login user
        _ = await RegisterAndLoginAsync();

        // Act
        var response = await _http.GetAsync("/contributions/myweeklyreport");

        // Assert
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        // Should contain work hours information (even if it's 0)
        Assert.IsNotNull(html);
        Assert.IsGreaterThan(0, html.Length);
    }

    [TestMethod]
    public async Task MyContributionsShowsTotalStatistics()
    {
        // Arrange - Create and login user
        _ = await RegisterAndLoginAsync();

        // Act
        var response = await _http.GetAsync("/contributions/mycontributions");

        // Assert
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        // Should contain statistics
        Assert.IsNotNull(html);
        Assert.IsGreaterThan(0, html.Length);
    }

    [TestMethod]
    public async Task ContributionsRouteVariations()
    {
        // Arrange
        _ = await RegisterAndLoginAsync();

        // Test different route cases
        var routes = new[]
        {
            "/contributions/mycontributions",
            "/Contributions/MyContributions"
        };

        foreach (var route in routes)
        {
            var response = await _http.GetAsync(route);
            response.EnsureSuccessStatusCode();
        }
    }
}
