using Aiursoft.CSTools.Tools;
using static Aiursoft.WebTools.Extends;
using System.Net;

namespace Aiursoft.Manhours.Tests.IntegrationTests;

[TestClass]
public class SecurityTests
{
    private readonly int _port;
    private readonly HttpClient _http;
    private IHost? _server;

    public SecurityTests()
    {
        _port = Network.GetAvailablePort();
        _http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = new Uri($"http://localhost:{_port}")
        };
    }

    [TestInitialize]
    public async Task CreateServer()
    {
        _server = await AppAsync<Startup>([], port: _port);
        await _server.StartAsync();
    }

    [TestCleanup]
    public async Task CleanServer()
    {
        if (_server != null)
        {
            await _server.StopAsync();
            _server.Dispose();
        }
    }

    [TestMethod]
    public async Task TestInvalidCharactersRejection()
    {
        // Test space injection
        var url = "/r/github.com/user/repo space.html";
        var response = await _http.GetAsync(url);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, "Should reject spaces");

        // Test semicolon injection
        url = "/r/github.com/user/repo;id.html";
        response = await _http.GetAsync(url);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, "Should reject semicolons");

        // Test pipe injection
        url = "/r/github.com/user/repo|id.html";
        response = await _http.GetAsync(url);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, "Should reject pipes");

        // Test valid repo
        // We expect 404 or 200, but NOT 400.
        // We use a non-existent domain to avoid real git clone and timeout.
        url = "/r/nonexistent.aiursoft.com/user/repo.html";
        response = await _http.GetAsync(url);
        Assert.AreNotEqual(HttpStatusCode.BadRequest, response.StatusCode, "Should accept valid repo characters");
    }

    [TestMethod]
    public async Task TestPathTraversalRejection()
    {
        // Test path traversal in URL
        var url = "/r/github.com/user/repo/../../../../etc/passwd.svg";
        var response = await _http.GetAsync(url);
        // This will be rejected by BadgeController because it contains '..'
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, "Should reject path traversal with 404");
    }

    [TestMethod]
    public async Task TestEncodedInjectionRejection()
    {
        // Test encoded semicolon
        var url = "/r/github.com/user/repo%3Bls.svg";
        var response = await _http.GetAsync(url);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, "Should reject encoded semicolons");
        
        // Test encoded quote
        url = "/r/github.com/user/repo%27ls.svg";
        response = await _http.GetAsync(url);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, "Should reject encoded quotes");
    }

    [TestMethod]
    public async Task TestBackslashRejection()
    {
        // Test backslash that might be converted to forward slash
        var url = "/r/github.com/user/repo\\;ls.svg";
        var response = await _http.GetAsync(url);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, "Should reject backslashes with semicolons");
    }
}
