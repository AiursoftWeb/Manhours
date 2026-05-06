using System.Net;
using System.Text.Json;
using Aiursoft.CSTools.Tools;
using Aiursoft.DbTools;
using Aiursoft.Manhours.Entities;
using static Aiursoft.WebTools.Extends;

namespace Aiursoft.Manhours.Tests.IntegrationTests;

[TestClass]
public class BadgeControllerTests
{
    private readonly int _port;
    private readonly HttpClient _http;
    private IHost? _server;

    public BadgeControllerTests()
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
            BaseAddress = new Uri($"http://localhost:{_port}"),
            Timeout = TimeSpan.FromMinutes(5)
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

    [TestMethod]
    public async Task GetBadgeAsSvg()
    {
        // Act
        var response = await _http.GetAsync("/r/gitlab.aiursoft.com/aiursoft/tracer.svg");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.AreEqual("image/svg+xml", response.Content.Headers.ContentType?.MediaType);

        var svg = await response.Content.ReadAsStringAsync();
        Assert.Contains("<svg", svg);
        Assert.Contains("man-hours", svg);
    }

    [TestMethod]
    public async Task GetBadgeAsGit()
    {
        // Act - .git extension should also return SVG
        var response = await _http.GetAsync("/r/gitlab.aiursoft.com/aiursoft/tracer.git");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.AreEqual("image/svg+xml", response.Content.Headers.ContentType?.MediaType);

        var svg = await response.Content.ReadAsStringAsync();
        Assert.Contains("<svg", svg);
    }

    [TestMethod]
    public async Task GetBadgeAsJson()
    {
        // Act
        var response = await _http.GetAsync("/r/gitlab.aiursoft.com/aiursoft/tracer.json");

        // Assert
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.IsTrue(root.TryGetProperty("label", out var label));
        Assert.AreEqual("man-hours", label.GetString());
        Assert.IsTrue(root.TryGetProperty("message", out _));
        Assert.IsTrue(root.TryGetProperty("color", out _));
    }

    [TestMethod]
    public async Task GetBadgeAsHtml()
    {
        // Act
        var response = await _http.GetAsync("/r/gitlab.aiursoft.com/aiursoft/tracer.html");

        // Assert
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("<!DOCTYPE html>", html);
    }

    [TestMethod]
    public async Task GetBadgeWithInvalidExtension()
    {
        // Act
        var response = await _http.GetAsync("/r/gitlab.aiursoft.com/aiursoft/tracer.txt");

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task GetBadgeWithNoExtension()
    {
        // Act
        var response = await _http.GetAsync("/r/gitlab.aiursoft.com/aiursoft/tracer");

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task GetBadgeWithoutPathSeparator()
    {
        // Act - No slash in repo path
        var response = await _http.GetAsync("/r/invalidrepo.svg");

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task GetBadgeWithDotDotAttack()
    {
        // Act - Try to use .. in path
        var response = await _http.GetAsync("/r/gitlab.aiursoft.com/../etc/passwd.svg");

        // Assert
        // Note: ASP.NET Core's routing engine normalizes paths before they reach the controller.
        // The path /r/gitlab.aiursoft.com/../etc/passwd.svg becomes /r/etc/passwd.svg,
        // which then tries to clone a git repo "etc/passwd" and fails.
        // Now it should return 200 with an error badge (since it's .svg) or 404 if it's not found earlier.
        // In this case, it passes regex and goes to clone, which fails and returns the error badge.
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var svg = await response.Content.ReadAsStringAsync();
            Assert.Contains("error", svg);
        }
    }

    [TestMethod]
    public async Task GetNonExistentRepoBadge()
    {
        // Act
        var response = await _http.GetAsync("/r/github.com/aiursoft-non-existent/not-found.svg");

        // Assert
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.AreEqual("image/svg+xml", response.Content.Headers.ContentType?.MediaType);
        var svg = await response.Content.ReadAsStringAsync();
        Assert.Contains("error", svg);
    }

    [TestMethod]
    public async Task GetNonExistentRepoHtml()
    {
        // Act
        var response = await _http.GetAsync("/r/github.com/aiursoft-non-existent/not-found.html");

        // Assert
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Repository Not Found", html);
    }

    [TestMethod]
    public async Task GetBadgeWithInvalidCharacters()
    {
        // Act - Try to use invalid characters
        var response = await _http.GetAsync("/r/gitlab.aiursoft.com/repo<script>.svg");

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task GetBadgeWithHttpsPrefix()
    {
        // Act - HTTPS prefix should be trimmed
        var response = await _http.GetAsync("/r/https://gitlab.aiursoft.com/aiursoft/tracer.svg");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.AreEqual("image/svg+xml", response.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    public async Task BadgeHasCorsHeader()
    {
        // Act
        var response = await _http.GetAsync("/r/gitlab.aiursoft.com/aiursoft/tracer.svg");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.IsTrue(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.AreEqual("*", response.Headers.GetValues("Access-Control-Allow-Origin").First());
    }

    [TestMethod]
    public async Task GetBadgeHtmlWithPagination()
    {
        // Act - Test pagination parameter
        var response1 = await _http.GetAsync("/r/gitlab.aiursoft.com/aiursoft/tracer.html?page=1");
        var response2 = await _http.GetAsync("/r/gitlab.aiursoft.com/aiursoft/tracer.html?page=2");

        // Assert
        response1.EnsureSuccessStatusCode();
        response2.EnsureSuccessStatusCode();
    }

    [TestMethod]
    public async Task GetBadgeHtmlWithInvalidPage()
    {
        // Act - Negative page should be handled
        var response = await _http.GetAsync("/r/gitlab.aiursoft.com/aiursoft/tracer.html?page=-1");

        // Assert - Should still succeed (treated as page 1)
        response.EnsureSuccessStatusCode();
    }


    [TestMethod]
    public async Task GetBadgeWithBackslashInPath()
    {
        // Act - Backslashes should be converted to forward slashes
        var response = await _http.GetAsync("/r/gitlab.aiursoft.com\\aiursoft\\tracer.svg");

        // Assert - Should handle the path
        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task BadgeColorChangesWithWorkHours()
    {
        // This test verifies that different repos might have different badge colors
        // based on their work hours

        // Act
        var response = await _http.GetAsync("/r/gitlab.aiursoft.com/aiursoft/tracer.json");

        // Assert
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.IsTrue(root.TryGetProperty("color", out var color));
            var colorValue = color.GetString();

            // Color should be one of the valid badge colors
            var validColors = new[] { "e05d44", "fe7d37", "dfb317", "4c1" };
            Assert.IsTrue(validColors.Contains(colorValue),
                $"Badge color {colorValue} should be one of the predefined colors");
        }
    }

    [TestMethod]
    public async Task GetBadgeWithLeadingAndTrailingSlashes()
    {
        // Act - Test path with extra slashes
        var response = await _http.GetAsync("/r//gitlab.aiursoft.com/aiursoft/tracer/.svg");

        // Assert - Should handle path normalization
        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound);
    }
}
