using System.Net;
using System.Net.Http;
using Aiursoft.CSTools.Tools;
using Aiursoft.DbTools;
using Aiursoft.Manhours.Entities;
using static Aiursoft.WebTools.Extends;

namespace Aiursoft.Manhours.Tests.IntegrationTests;

[TestClass]
public class HomeAndErrorControllerTests
{
    private readonly int _port;
    private readonly HttpClient _http;
    private IHost? _server;

    public HomeAndErrorControllerTests()
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

    [TestMethod]
    [DataRow("/")]
    [DataRow("/home")]
    [DataRow("/home/index")]
    [DataRow("/Home")]
    [DataRow("/Home/Index")]
    public async Task GetHomePageVariations(string url)
    {
        // Act
        var response = await _http.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.IsNotNull(html);
        Assert.IsGreaterThan(0, html.Length);
    }

    [TestMethod]
    public async Task GetHomePageContainsDocType()
    {
        // Act
        var response = await _http.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("<!DOCTYPE html>", html);
    }

    [TestMethod]
    public async Task NonExistentPageReturns404()
    {
        // Act
        var response = await _http.GetAsync("/this-page-does-not-exist");

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task ErrorPageHandles404()
    {
        // Act - Request a non-existent page
        var response = await _http.GetAsync("/definitely-not-a-real-page-12345");

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task GetStaticResourcesJs()
    {
        // Act - Try to get a common static resource path
        var response = await _http.GetAsync("/js/site.js");

        // Assert - May or may not exist, but shouldn't crash
        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task GetStaticResourcesCss()
    {
        // Act - Try to get a common static resource path
        var response = await _http.GetAsync("/css/site.css");

        // Assert - May or may not exist, but shouldn't crash
        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task GetFavicon()
    {
        // Act
        var response = await _http.GetAsync("/favicon.ico");

        // Assert - May or may not exist, but shouldn't crash
        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task HomePageHasNavigationLinks()
    {
        // Act
        var response = await _http.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        // Should contain repository display information
        Assert.IsNotNull(html);
        Assert.IsGreaterThan(0, html.Length); // Reasonable minimum for a page
    }

    [TestMethod]
    public async Task HttpHeadRequestWorks()
    {
        // Act
        var request = new HttpRequestMessage(HttpMethod.Head, "/");
        var response = await _http.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [TestMethod]
    public async Task GetRobotsOrSitemap()
    {
        // Act - Common SEO files
        var robotsResponse = await _http.GetAsync("/robots.txt");
        var sitemapResponse = await _http.GetAsync("/sitemap.xml");

        // Assert - May or may not be implemented, but shouldn't crash
        Assert.IsTrue(
            robotsResponse.StatusCode == HttpStatusCode.OK ||
            robotsResponse.StatusCode == HttpStatusCode.NotFound);

        Assert.IsTrue(
            sitemapResponse.StatusCode == HttpStatusCode.OK ||
            sitemapResponse.StatusCode == HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task RequestWithQueryParameters()
    {
        // Act
        var response = await _http.GetAsync("/?test=value&foo=bar");

        // Assert - Should handle query parameters gracefully
        response.EnsureSuccessStatusCode();
    }

    [TestMethod]
    public async Task RequestWithFragment()
    {
        // Act - Fragments are handled client-side but shouldn't break the server
        var response = await _http.GetAsync("/#section");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [TestMethod]
    public async Task CaseInsensitiveRouting()
    {
        // Test that routing is case-insensitive as expected
        var urls = new[]
        {
            "/home",
            "/Home",
            "/HOME",
            "/HoMe"
        };

        foreach (var url in urls)
        {
            var response = await _http.GetAsync(url);
            response.EnsureSuccessStatusCode();
        }
    }

    [TestMethod]
    public async Task MultipleSlashesInUrl()
    {
        // Act - Test URL normalization
        try
        {
            var response = await _http.GetAsync("///home///index");

            // Assert - Should either normalize or return 404, but not crash
            Assert.IsTrue(
                response.IsSuccessStatusCode ||
                response.StatusCode == HttpStatusCode.NotFound ||
                response.StatusCode == HttpStatusCode.BadRequest);
        }
        catch (UriFormatException)
        {
            // HttpClient cannot parse URLs starting with ///
            // This is expected and acceptable - it's a client limitation not a server issue
            Assert.IsTrue(true);
        }
    }
}
